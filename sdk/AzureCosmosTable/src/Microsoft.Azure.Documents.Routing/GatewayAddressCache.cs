using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	internal class GatewayAddressCache : IAddressCache, IDisposable
	{
		private const string protocolFilterFormat = "{0} eq {1}";

		private const string AddressResolutionBatchSize = "AddressResolutionBatchSize";

		private const int DefaultBatchSize = 50;

		private readonly Uri serviceEndpoint;

		private readonly Uri addressEndpoint;

		private readonly AsyncCache<PartitionKeyRangeIdentity, PartitionAddressInformation> serverPartitionAddressCache;

		private readonly ConcurrentDictionary<PartitionKeyRangeIdentity, DateTime> suboptimalServerPartitionTimestamps;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly long suboptimalPartitionForceRefreshIntervalInSeconds;

		private readonly Protocol protocol;

		private readonly string protocolFilter;

		private readonly IAuthorizationTokenProvider tokenProvider;

		private HttpClient httpClient;

		private Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> masterPartitionAddressCache;

		private DateTime suboptimalMasterPartitionTimestamp;

		public Uri ServiceEndpoint => serviceEndpoint;

		public GatewayAddressCache(Uri serviceEndpoint, Protocol protocol, IAuthorizationTokenProvider tokenProvider, UserAgentContainer userAgent, IServiceConfigurationReader serviceConfigReader, TimeSpan requestTimeout, long suboptimalPartitionForceRefreshIntervalInSeconds = 600L, HttpMessageHandler messageHandler = null, ApiType apiType = ApiType.None)
		{
			addressEndpoint = new Uri(serviceEndpoint + "/addresses");
			this.protocol = protocol;
			this.tokenProvider = tokenProvider;
			this.serviceEndpoint = serviceEndpoint;
			this.serviceConfigReader = serviceConfigReader;
			serverPartitionAddressCache = new AsyncCache<PartitionKeyRangeIdentity, PartitionAddressInformation>();
			suboptimalServerPartitionTimestamps = new ConcurrentDictionary<PartitionKeyRangeIdentity, DateTime>();
			suboptimalMasterPartitionTimestamp = DateTime.MaxValue;
			this.suboptimalPartitionForceRefreshIntervalInSeconds = suboptimalPartitionForceRefreshIntervalInSeconds;
			httpClient = ((messageHandler == null) ? new HttpClient() : new HttpClient(messageHandler));
			httpClient.Timeout = requestTimeout;
			protocolFilter = string.Format(CultureInfo.InvariantCulture, "{0} eq {1}", "protocol", ProtocolString(this.protocol));
			httpClient.DefaultRequestHeaders.Add("x-ms-version", HttpConstants.Versions.CurrentVersion);
			httpClient.AddUserAgentHeader(userAgent);
			httpClient.AddApiTypeHeader(apiType);
		}

		[SuppressMessage("", "AsyncFixer02", Justification = "Multi task completed with await")]
		[SuppressMessage("", "AsyncFixer04", Justification = "Multi task completed outside of await")]
		public async Task OpenAsync(string databaseName, DocumentCollection collection, IReadOnlyList<PartitionKeyRangeIdentity> partitionKeyRangeIdentities, CancellationToken cancellationToken)
		{
			List<Task<FeedResource<Address>>> list = new List<Task<FeedResource<Address>>>();
			int num = 50;
			string resourceFullName = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}/{3}", "dbs", Uri.EscapeUriString(databaseName), "colls", Uri.EscapeUriString(collection.Id));
			using (DocumentServiceRequest request = DocumentServiceRequest.CreateFromName(OperationType.Read, resourceFullName, ResourceType.Collection, AuthorizationTokenType.PrimaryMasterKey))
			{
				for (int i = 0; i < partitionKeyRangeIdentities.Count; i += num)
				{
					list.Add(GetServerAddressesViaGatewayAsync(request, collection.ResourceId, from range in partitionKeyRangeIdentities.Skip(i).Take(num)
					select range.PartitionKeyRangeId, forceRefresh: false));
				}
			}
			FeedResource<Address>[] array = await Task.WhenAll(list);
			for (int j = 0; j < array.Length; j++)
			{
				foreach (Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> item in from @group in (from addressInfo in array[j]
				where ProtocolFromString(addressInfo.Protocol) == protocol
				select addressInfo).GroupBy((Address address) => address.PartitionKeyRangeId, StringComparer.Ordinal)
				select ToPartitionAddressAndRange(collection.ResourceId, @group.ToList()))
				{
					serverPartitionAddressCache.Set(new PartitionKeyRangeIdentity(collection.ResourceId, item.Item1.PartitionKeyRangeId), item.Item2);
				}
			}
		}

		public async Task<PartitionAddressInformation> TryGetAddresses(DocumentServiceRequest request, PartitionKeyRangeIdentity partitionKeyRangeIdentity, ServiceIdentity serviceIdentity, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			if (partitionKeyRangeIdentity == null)
			{
				throw new ArgumentNullException("partitionKeyRangeIdentity");
			}
			try
			{
				if (partitionKeyRangeIdentity.PartitionKeyRangeId == "M")
				{
					return (await ResolveMasterAsync(request, forceRefreshPartitionAddresses)).Item2;
				}
				DateTime value;
				if (suboptimalServerPartitionTimestamps.TryGetValue(partitionKeyRangeIdentity, out value) && DateTime.UtcNow.Subtract(value) > TimeSpan.FromSeconds(suboptimalPartitionForceRefreshIntervalInSeconds) && suboptimalServerPartitionTimestamps.TryUpdate(partitionKeyRangeIdentity, DateTime.MaxValue, value))
				{
					forceRefreshPartitionAddresses = true;
				}
				PartitionAddressInformation partitionAddressInformation;
				if (forceRefreshPartitionAddresses || request.ForceCollectionRoutingMapRefresh)
				{
					partitionAddressInformation = await serverPartitionAddressCache.GetAsync(partitionKeyRangeIdentity, null, () => GetAddressesForRangeId(request, partitionKeyRangeIdentity.CollectionRid, partitionKeyRangeIdentity.PartitionKeyRangeId, forceRefreshPartitionAddresses), cancellationToken, forceRefresh: true);
					suboptimalServerPartitionTimestamps.TryRemove(partitionKeyRangeIdentity, out DateTime _);
				}
				else
				{
					partitionAddressInformation = await serverPartitionAddressCache.GetAsync(partitionKeyRangeIdentity, null, () => GetAddressesForRangeId(request, partitionKeyRangeIdentity.CollectionRid, partitionKeyRangeIdentity.PartitionKeyRangeId, forceRefresh: false), cancellationToken);
				}
				int maxReplicaSetSize = serviceConfigReader.UserReplicationPolicy.MaxReplicaSetSize;
				if (partitionAddressInformation.AllAddresses.Count() < maxReplicaSetSize)
				{
					suboptimalServerPartitionTimestamps.TryAdd(partitionKeyRangeIdentity, DateTime.UtcNow);
				}
				return partitionAddressInformation;
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode == HttpStatusCode.NotFound || (ex.StatusCode == HttpStatusCode.Gone && ex.GetSubStatus() == SubStatusCodes.PartitionKeyRangeGone))
				{
					suboptimalServerPartitionTimestamps.TryRemove(partitionKeyRangeIdentity, out DateTime _);
					return null;
				}
				throw;
			}
			catch (Exception)
			{
				if (forceRefreshPartitionAddresses)
				{
					suboptimalServerPartitionTimestamps.TryRemove(partitionKeyRangeIdentity, out DateTime _);
				}
				throw;
			}
		}

		private async Task<Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation>> ResolveMasterAsync(DocumentServiceRequest request, bool forceRefresh)
		{
			Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> tuple = masterPartitionAddressCache;
			int targetReplicaSetSize = serviceConfigReader.SystemReplicationPolicy.MaxReplicaSetSize;
			forceRefresh = (forceRefresh || (tuple != null && tuple.Item2.AllAddresses.Count() < targetReplicaSetSize && DateTime.UtcNow.Subtract(suboptimalMasterPartitionTimestamp) > TimeSpan.FromSeconds(suboptimalPartitionForceRefreshIntervalInSeconds)));
			if (forceRefresh || request.ForceCollectionRoutingMapRefresh || masterPartitionAddressCache == null)
			{
				string entryUrl = PathsHelper.GeneratePath(ResourceType.Database, string.Empty, isFeed: true);
				try
				{
					FeedResource<Address> source = await GetMasterAddressesViaGatewayAsync(request, ResourceType.Database, null, entryUrl, forceRefresh, useMasterCollectionResolver: false);
					tuple = (masterPartitionAddressCache = ToPartitionAddressAndRange(string.Empty, source.ToList()));
					suboptimalMasterPartitionTimestamp = DateTime.MaxValue;
				}
				catch (Exception)
				{
					suboptimalMasterPartitionTimestamp = DateTime.MaxValue;
					throw;
				}
			}
			if (tuple.Item2.AllAddresses.Count() < targetReplicaSetSize && suboptimalMasterPartitionTimestamp.Equals(DateTime.MaxValue))
			{
				suboptimalMasterPartitionTimestamp = DateTime.UtcNow;
			}
			return tuple;
		}

		private async Task<PartitionAddressInformation> GetAddressesForRangeId(DocumentServiceRequest request, string collectionRid, string partitionKeyRangeId, bool forceRefresh)
		{
			Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> tuple = (from @group in (from addressInfo in await GetServerAddressesViaGatewayAsync(request, collectionRid, new string[1]
			{
				partitionKeyRangeId
			}, forceRefresh)
			where ProtocolFromString(addressInfo.Protocol) == protocol
			select addressInfo).GroupBy((Address address) => address.PartitionKeyRangeId, StringComparer.Ordinal)
			select ToPartitionAddressAndRange(collectionRid, @group.ToList())).SingleOrDefault((Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> addressInfo) => StringComparer.Ordinal.Equals(addressInfo.Item1.PartitionKeyRangeId, partitionKeyRangeId));
			if (tuple == null)
			{
				throw new PartitionKeyRangeGoneException(string.Format(CultureInfo.InvariantCulture, RMResources.PartitionKeyRangeNotFound, partitionKeyRangeId, collectionRid))
				{
					ResourceAddress = collectionRid
				};
			}
			return tuple.Item2;
		}

		private async Task<FeedResource<Address>> GetMasterAddressesViaGatewayAsync(DocumentServiceRequest request, ResourceType resourceType, string resourceAddress, string entryUrl, bool forceRefresh, bool useMasterCollectionResolver)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection(StringComparer.Ordinal);
			nameValueCollection.Add("$resolveFor", HttpUtility.UrlEncode(entryUrl));
			INameValueCollection nameValueCollection2 = new StringKeyValueCollection(StringComparer.Ordinal);
			if (forceRefresh)
			{
				nameValueCollection2.Set("x-ms-force-refresh", bool.TrueString);
			}
			if (useMasterCollectionResolver)
			{
				nameValueCollection2.Set("x-ms-use-master-collection-resolver", bool.TrueString);
			}
			if (request.ForceCollectionRoutingMapRefresh)
			{
				nameValueCollection2.Set("x-ms-collectionroutingmap-refresh", bool.TrueString);
			}
			nameValueCollection.Add("$filter", protocolFilter);
			string resourcePath = PathsHelper.GetResourcePath(resourceType);
			nameValueCollection2.Set("x-ms-date", DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
			string userAuthorizationToken = tokenProvider.GetUserAuthorizationToken(resourceAddress, resourcePath, "GET", nameValueCollection2, AuthorizationTokenType.PrimaryMasterKey);
			nameValueCollection2.Set("authorization", userAuthorizationToken);
			Uri uri = UrlUtility.SetQuery(addressEndpoint, UrlUtility.CreateQuery(nameValueCollection));
			string identifier = LogAddressResolutionStart(request, uri);
			using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(uri, nameValueCollection2))
			{
				using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(httpResponseMessage))
				{
					LogAddressResolutionEnd(request, identifier);
					return documentServiceResponse.GetResource<FeedResource<Address>>();
				}
			}
		}

		private async Task<FeedResource<Address>> GetServerAddressesViaGatewayAsync(DocumentServiceRequest request, string collectionRid, IEnumerable<string> partitionKeyRangeIds, bool forceRefresh)
		{
			string str = PathsHelper.GeneratePath(ResourceType.Document, collectionRid, isFeed: true);
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			nameValueCollection.Add("$resolveFor", HttpUtility.UrlEncode(str));
			INameValueCollection nameValueCollection2 = new StringKeyValueCollection();
			if (forceRefresh)
			{
				nameValueCollection2.Set("x-ms-force-refresh", bool.TrueString);
			}
			if (request.ForceCollectionRoutingMapRefresh)
			{
				nameValueCollection2.Set("x-ms-collectionroutingmap-refresh", bool.TrueString);
			}
			nameValueCollection.Add("$filter", protocolFilter);
			nameValueCollection.Add("$partitionKeyRangeIds", string.Join(",", partitionKeyRangeIds));
			string resourcePath = PathsHelper.GetResourcePath(ResourceType.Document);
			nameValueCollection2.Set("x-ms-date", DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
			string text = null;
			try
			{
				text = tokenProvider.GetUserAuthorizationToken(collectionRid, resourcePath, "GET", nameValueCollection2, AuthorizationTokenType.PrimaryMasterKey);
			}
			catch (UnauthorizedException)
			{
			}
			if (text == null && request.IsNameBased)
			{
				string collectionPath = PathsHelper.GetCollectionPath(request.ResourceAddress);
				text = tokenProvider.GetUserAuthorizationToken(collectionPath, resourcePath, "GET", nameValueCollection2, AuthorizationTokenType.PrimaryMasterKey);
			}
			nameValueCollection2.Set("authorization", text);
			Uri uri = UrlUtility.SetQuery(addressEndpoint, UrlUtility.CreateQuery(nameValueCollection));
			string identifier = LogAddressResolutionStart(request, uri);
			using (HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(uri, nameValueCollection2))
			{
				using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(httpResponseMessage))
				{
					LogAddressResolutionEnd(request, identifier);
					return documentServiceResponse.GetResource<FeedResource<Address>>();
				}
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing && httpClient != null)
			{
				try
				{
					httpClient.Dispose();
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceWarning("Exception {0} thrown during dispose of HttpClient, this could happen if there are inflight request during the dispose of client", ex);
				}
				httpClient = null;
			}
		}

		internal Tuple<PartitionKeyRangeIdentity, PartitionAddressInformation> ToPartitionAddressAndRange(string collectionRid, IList<Address> addresses)
		{
			Address address = addresses.First();
			AddressInformation[] replicaAddresses = (from addr in addresses
			select new AddressInformation
			{
				IsPrimary = addr.IsPrimary,
				PhysicalUri = addr.PhysicalUri,
				Protocol = ProtocolFromString(addr.Protocol),
				IsPublic = true
			}).ToArray();
			return Tuple.Create(new PartitionKeyRangeIdentity(collectionRid, address.PartitionKeyRangeId), new PartitionAddressInformation(replicaAddresses));
		}

		private static string LogAddressResolutionStart(DocumentServiceRequest request, Uri targetEndpoint)
		{
			string result = null;
			if (request.RequestContext.ClientRequestStatistics != null)
			{
				result = request.RequestContext.ClientRequestStatistics.RecordAddressResolutionStart(targetEndpoint);
			}
			return result;
		}

		private static void LogAddressResolutionEnd(DocumentServiceRequest request, string identifier)
		{
			if (request.RequestContext.ClientRequestStatistics != null)
			{
				request.RequestContext.ClientRequestStatistics.RecordAddressResolutionEnd(identifier);
			}
		}

		private static Protocol ProtocolFromString(string protocol)
		{
			string a = protocol.ToLowerInvariant();
			if (!(a == "https"))
			{
				if (a == "rntbd")
				{
					return Protocol.Tcp;
				}
				throw new ArgumentOutOfRangeException("protocol");
			}
			return Protocol.Https;
		}

		private static string ProtocolString(Protocol protocol)
		{
			switch (protocol)
			{
			case Protocol.Https:
				return "https";
			case Protocol.Tcp:
				return "rntbd";
			default:
				throw new ArgumentOutOfRangeException("protocol");
			}
		}
	}
}
