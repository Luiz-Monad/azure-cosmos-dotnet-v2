using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// AddressCache implementation for client SDK. Supports cross region address routing based on 
	/// availability and preference list.
	/// </summary>
	/// Marking it as non-sealed in order to unit test it using Moq framework
	internal class GlobalEndpointManager : IDisposable
	{
		private const int DefaultBackgroundRefreshLocationTimeIntervalInMS = 300000;

		private const string BackgroundRefreshLocationTimeIntervalInMS = "BackgroundRefreshLocationTimeIntervalInMS";

		private int backgroundRefreshLocationTimeIntervalInMS = 300000;

		private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

		private readonly LocationCache locationCache;

		private readonly Uri defaultEndpoint;

		private readonly ConnectionPolicy connectionPolicy;

		private readonly IDocumentClientInternal owner;

		private readonly object refreshLock;

		private readonly AsyncCache<string, DatabaseAccount> databaseAccountCache;

		private bool isRefreshing;

		public ReadOnlyCollection<Uri> ReadEndpoints => locationCache.ReadEndpoints;

		public ReadOnlyCollection<Uri> WriteEndpoints => locationCache.WriteEndpoints;

		public GlobalEndpointManager(IDocumentClientInternal owner, ConnectionPolicy connectionPolicy)
		{
			locationCache = new LocationCache(new ReadOnlyCollection<string>(connectionPolicy.PreferredLocations), owner.ServiceEndpoint, connectionPolicy.EnableEndpointDiscovery, connectionPolicy.MaxConnectionLimit, connectionPolicy.UseMultipleWriteLocations);
			this.owner = owner;
			defaultEndpoint = owner.ServiceEndpoint;
			this.connectionPolicy = connectionPolicy;
			databaseAccountCache = new AsyncCache<string, DatabaseAccount>();
			this.connectionPolicy.PreferenceChanged += OnPreferenceChanged;
			isRefreshing = false;
			refreshLock = new object();
		}

		public static async Task<DatabaseAccount> GetDatabaseAccountFromAnyLocationsAsync(Uri defaultEndpoint, IList<string> locations, Func<Uri, Task<DatabaseAccount>> getDatabaseAccountFn)
		{
			try
			{
				return await getDatabaseAccountFn(defaultEndpoint);
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceInformation("Fail to reach global gateway {0}, {1}", defaultEndpoint, ex.ToString());
				if (locations.Count == 0)
				{
					throw;
				}
			}
			for (int index = 0; index < locations.Count; index++)
			{
				try
				{
					return await getDatabaseAccountFn(LocationHelper.GetLocationEndpoint(defaultEndpoint, locations[index]));
				}
				catch (Exception ex2)
				{
					DefaultTrace.TraceInformation("Fail to reach location {0}, {1}", locations[index], ex2.ToString());
					if (index == locations.Count - 1)
					{
						throw;
					}
				}
			}
			throw new Exception();
		}

		public virtual Uri ResolveServiceEndpoint(DocumentServiceRequest request)
		{
			return locationCache.ResolveServiceEndpoint(request);
		}

		/// <summary>
		/// Returns location corresponding to the endpoint
		/// </summary>
		/// <param name="endpoint"></param>
		public string GetLocation(Uri endpoint)
		{
			return locationCache.GetLocation(endpoint);
		}

		public void MarkEndpointUnavailableForRead(Uri endpoint)
		{
			DefaultTrace.TraceInformation("Marking endpoint {0} unavailable for read", endpoint);
			locationCache.MarkEndpointUnavailableForRead(endpoint);
		}

		public void MarkEndpointUnavailableForWrite(Uri endpoint)
		{
			DefaultTrace.TraceInformation("Marking endpoint {0} unavailable for Write", endpoint);
			locationCache.MarkEndpointUnavailableForWrite(endpoint);
		}

		public bool CanUseMultipleWriteLocations(DocumentServiceRequest request)
		{
			return locationCache.CanUseMultipleWriteLocations(request);
		}

		public void Dispose()
		{
			connectionPolicy.PreferenceChanged -= OnPreferenceChanged;
			if (!cancellationTokenSource.IsCancellationRequested)
			{
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}
		}

		public async Task RefreshLocationAsync(DatabaseAccount databaseAccount, bool forceRefresh = false)
		{
			if (!cancellationTokenSource.IsCancellationRequested)
			{
				if (forceRefresh)
				{
					DatabaseAccount databaseAccount2 = await RefreshDatabaseAccountInternalAsync();
					locationCache.OnDatabaseAccountRead(databaseAccount2);
				}
				else
				{
					lock (refreshLock)
					{
						if (isRefreshing)
						{
							return;
						}
						isRefreshing = true;
					}
					try
					{
						await RefreshLocationPrivateAsync(databaseAccount);
					}
					catch
					{
						isRefreshing = false;
						throw;
					}
				}
			}
		}

		private async Task RefreshLocationPrivateAsync(DatabaseAccount databaseAccount)
		{
			if (cancellationTokenSource.IsCancellationRequested)
			{
				return;
			}
			DefaultTrace.TraceInformation("RefreshLocationAsync() refreshing locations");
			if (databaseAccount != null)
			{
				locationCache.OnDatabaseAccountRead(databaseAccount);
			}
			bool canRefreshInBackground = false;
			if (locationCache.ShouldRefreshEndpoints(out canRefreshInBackground))
			{
				if (databaseAccount == null && !canRefreshInBackground)
				{
					databaseAccount = await RefreshDatabaseAccountInternalAsync();
					locationCache.OnDatabaseAccountRead(databaseAccount);
				}
				StartRefreshLocationTimerAsync();
			}
			else
			{
				isRefreshing = false;
			}
		}

		[SuppressMessage("", "AsyncFixer03", Justification = "Async start is by-design")]
		private async void StartRefreshLocationTimerAsync()
		{
			if (!cancellationTokenSource.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(backgroundRefreshLocationTimeIntervalInMS, cancellationTokenSource.Token);
					DefaultTrace.TraceInformation("StartRefreshLocationTimerAsync() - Invoking refresh");
					await RefreshLocationPrivateAsync(await RefreshDatabaseAccountInternalAsync());
				}
				catch (Exception ex)
				{
					if (!cancellationTokenSource.IsCancellationRequested || (!(ex is TaskCanceledException) && !(ex is ObjectDisposedException)))
					{
						DefaultTrace.TraceCritical("StartRefreshLocationTimerAsync() - Unable to refresh database account from any location. Exception: {0}", ex.ToString());
						StartRefreshLocationTimerAsync();
					}
				}
			}
		}

		private Task<DatabaseAccount> GetDatabaseAccountAsync(Uri serviceEndpoint)
		{
			return owner.GetDatabaseAccountInternalAsync(serviceEndpoint, cancellationTokenSource.Token);
		}

		private void OnPreferenceChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			locationCache.OnLocationPreferenceChanged(new ReadOnlyCollection<string>(connectionPolicy.PreferredLocations));
		}

		private Task<DatabaseAccount> RefreshDatabaseAccountInternalAsync()
		{
			return databaseAccountCache.GetAsync(string.Empty, null, () => GetDatabaseAccountFromAnyLocationsAsync(defaultEndpoint, connectionPolicy.PreferredLocations, GetDatabaseAccountAsync), cancellationTokenSource.Token, forceRefresh: true);
		}
	}
}
