using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Client policy is combination of endpoint change retry + throttling retry.
	/// </summary>
	internal sealed class ClientRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private sealed class RetryContext
		{
			public int RetryCount
			{
				get;
				set;
			}

			public bool RetryRequestOnPreferredLocations
			{
				get;
				set;
			}
		}

		private const int RetryIntervalInMS = 1000;

		private const int MaxRetryCount = 120;

		private readonly IDocumentClientRetryPolicy throttlingRetry;

		private readonly GlobalEndpointManager globalEndpointManager;

		private readonly bool enableEndpointDiscovery;

		private int failoverRetryCount;

		private int sessionTokenRetryCount;

		private bool isReadRequest;

		private bool canUseMultipleWriteLocations;

		private Uri locationEndpoint;

		private RetryContext retryContext;

		private ClientSideRequestStatistics sharedStatistics;

		public ClientRetryPolicy(GlobalEndpointManager globalEndpointManager, bool enableEndpointDiscovery, RetryOptions retryOptions)
		{
			throttlingRetry = new ResourceThrottleRetryPolicy(retryOptions.MaxRetryAttemptsOnThrottledRequests, retryOptions.MaxRetryWaitTimeInSeconds);
			this.globalEndpointManager = globalEndpointManager;
			failoverRetryCount = 0;
			this.enableEndpointDiscovery = enableEndpointDiscovery;
			sessionTokenRetryCount = 0;
			canUseMultipleWriteLocations = false;
			sharedStatistics = new ClientSideRequestStatistics();
		}

		/// <summary> 
		/// Should the caller retry the operation.
		/// </summary>
		/// <param name="exception">Exception that occured when the operation was tried</param>
		/// <param name="cancellationToken"></param>
		/// <returns>True indicates caller should retry, False otherwise</returns>
		public async Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			retryContext = null;
			if (exception is HttpRequestException)
			{
				DefaultTrace.TraceWarning("Endpoint not reachable. Refresh cache and retry");
				return await ShouldRetryOnEndpointFailureAsync(isReadRequest, forceRefresh: false);
			}
			DocumentClientException ex = exception as DocumentClientException;
			if (ex?.RequestStatistics != null)
			{
				sharedStatistics = ex.RequestStatistics;
			}
			ShouldRetryResult shouldRetryResult = await ShouldRetryInternalAsync(ex?.StatusCode, ex?.GetSubStatus());
			if (shouldRetryResult != null)
			{
				return shouldRetryResult;
			}
			return await throttlingRetry.ShouldRetryAsync(exception, cancellationToken);
		}

		/// <summary>
		/// Method that is called before a request is sent to allow the retry policy implementation
		/// to modify the state of the request.
		/// </summary>
		/// <param name="request">The request being sent to the service.</param>
		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			isReadRequest = request.IsReadOnlyRequest;
			canUseMultipleWriteLocations = globalEndpointManager.CanUseMultipleWriteLocations(request);
			request.RequestContext.ClientRequestStatistics = sharedStatistics;
			request.RequestContext.ClearRouteToLocation();
			if (retryContext != null)
			{
				request.RequestContext.RouteToLocation(retryContext.RetryCount, retryContext.RetryRequestOnPreferredLocations);
			}
			locationEndpoint = globalEndpointManager.ResolveServiceEndpoint(request);
			request.RequestContext.RouteToLocation(locationEndpoint);
		}

		private async Task<ShouldRetryResult> ShouldRetryInternalAsync(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode)
		{
			if (!statusCode.HasValue && (!subStatusCode.HasValue || subStatusCode.Value == SubStatusCodes.Unknown))
			{
				return null;
			}
			if (statusCode == HttpStatusCode.Forbidden && subStatusCode == SubStatusCodes.WriteForbidden)
			{
				DefaultTrace.TraceWarning("Endpoint not writable. Refresh cache and retry");
				return await ShouldRetryOnEndpointFailureAsync(isReadRequest: false, forceRefresh: true);
			}
			if (statusCode == HttpStatusCode.Forbidden && subStatusCode == SubStatusCodes.CompletingPartitionMigration && (isReadRequest || canUseMultipleWriteLocations))
			{
				DefaultTrace.TraceWarning("Endpoint not available for reads. Refresh cache and retry");
				return await ShouldRetryOnEndpointFailureAsync(isReadRequest: true, forceRefresh: false);
			}
			if (statusCode == HttpStatusCode.NotFound && subStatusCode == SubStatusCodes.PartitionKeyRangeGone)
			{
				return ShouldRetryOnSessionNotAvailable();
			}
			return null;
		}

		private async Task<ShouldRetryResult> ShouldRetryOnEndpointFailureAsync(bool isReadRequest, bool forceRefresh)
		{
			if (!enableEndpointDiscovery || failoverRetryCount > 120)
			{
				DefaultTrace.TraceInformation("ShouldRetryOnEndpointFailureAsync() Not retrying. Retry count = {0}", failoverRetryCount);
				return ShouldRetryResult.NoRetry();
			}
			failoverRetryCount++;
			if (locationEndpoint != null)
			{
				if (isReadRequest)
				{
					globalEndpointManager.MarkEndpointUnavailableForRead(locationEndpoint);
				}
				else
				{
					globalEndpointManager.MarkEndpointUnavailableForWrite(locationEndpoint);
				}
			}
			TimeSpan retryDelay = TimeSpan.Zero;
			if (!isReadRequest)
			{
				DefaultTrace.TraceInformation("Failover happening. retryCount {0}", failoverRetryCount);
				if (failoverRetryCount > 1)
				{
					retryDelay = TimeSpan.FromMilliseconds(1000.0);
				}
			}
			else
			{
				retryDelay = TimeSpan.FromMilliseconds(1000.0);
			}
			await globalEndpointManager.RefreshLocationAsync(null, forceRefresh);
			retryContext = new RetryContext
			{
				RetryCount = failoverRetryCount,
				RetryRequestOnPreferredLocations = false
			};
			return ShouldRetryResult.RetryAfter(retryDelay);
		}

		private ShouldRetryResult ShouldRetryOnSessionNotAvailable()
		{
			sessionTokenRetryCount++;
			if (!enableEndpointDiscovery)
			{
				return ShouldRetryResult.NoRetry();
			}
			if (canUseMultipleWriteLocations)
			{
				ReadOnlyCollection<Uri> readOnlyCollection = isReadRequest ? globalEndpointManager.ReadEndpoints : globalEndpointManager.WriteEndpoints;
				if (sessionTokenRetryCount > readOnlyCollection.Count)
				{
					return ShouldRetryResult.NoRetry();
				}
				retryContext = new RetryContext
				{
					RetryCount = sessionTokenRetryCount - 1,
					RetryRequestOnPreferredLocations = (sessionTokenRetryCount > 1)
				};
				return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
			}
			if (sessionTokenRetryCount > 1)
			{
				return ShouldRetryResult.NoRetry();
			}
			retryContext = new RetryContext
			{
				RetryCount = sessionTokenRetryCount - 1,
				RetryRequestOnPreferredLocations = false
			};
			return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
		}
	}
}
