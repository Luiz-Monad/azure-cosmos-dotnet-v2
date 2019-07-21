using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// AddressCache implementation for client SDK. Supports cross region address routing based on
	/// avaialbility and preference list.
	/// </summary>
	internal sealed class GlobalAddressResolver : IAddressResolver, IDisposable
	{
		private sealed class EndpointCache
		{
			public GatewayAddressCache AddressCache
			{
				get;
				set;
			}

			public AddressResolver AddressResolver
			{
				get;
				set;
			}
		}

		private const int MaxBackupReadRegions = 3;

		private readonly GlobalEndpointManager endpointManager;

		private readonly Protocol protocol;

		private readonly IAuthorizationTokenProvider tokenProvider;

		private readonly UserAgentContainer userAgentContainer;

		private readonly CollectionCache collectionCache;

		private readonly PartitionKeyRangeCache routingMapProvider;

		private readonly int maxEndpoints;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly HttpMessageHandler messageHandler;

		private readonly ConcurrentDictionary<Uri, EndpointCache> addressCacheByEndpoint;

		private readonly TimeSpan requestTimeout;

		private readonly ApiType apiType;

		public GlobalAddressResolver(GlobalEndpointManager endpointManager, Protocol protocol, IAuthorizationTokenProvider tokenProvider, CollectionCache collectionCache, PartitionKeyRangeCache routingMapProvider, UserAgentContainer userAgentContainer, IServiceConfigurationReader serviceConfigReader, HttpMessageHandler messageHandler, ConnectionPolicy connectionPolicy, ApiType apiType)
		{
			this.endpointManager = endpointManager;
			this.protocol = protocol;
			this.tokenProvider = tokenProvider;
			this.userAgentContainer = userAgentContainer;
			this.collectionCache = collectionCache;
			this.routingMapProvider = routingMapProvider;
			this.serviceConfigReader = serviceConfigReader;
			this.messageHandler = messageHandler;
			requestTimeout = connectionPolicy.RequestTimeout;
			this.apiType = apiType;
			int num = (!connectionPolicy.EnableReadRequestsFallback.HasValue || connectionPolicy.EnableReadRequestsFallback.Value) ? 3 : 0;
			maxEndpoints = num + 2;
			addressCacheByEndpoint = new ConcurrentDictionary<Uri, EndpointCache>();
			foreach (Uri writeEndpoint in endpointManager.WriteEndpoints)
			{
				GetOrAddEndpoint(writeEndpoint);
			}
			foreach (Uri readEndpoint in endpointManager.ReadEndpoints)
			{
				GetOrAddEndpoint(readEndpoint);
			}
		}

		public async Task OpenAsync(string databaseName, DocumentCollection collection, CancellationToken cancellationToken)
		{
			CollectionRoutingMap collectionRoutingMap = await routingMapProvider.TryLookupAsync(collection.ResourceId, null, null, cancellationToken);
			if (collectionRoutingMap != null)
			{
				List<PartitionKeyRangeIdentity> partitionKeyRangeIdentities = (from range in collectionRoutingMap.OrderedPartitionKeyRanges
				select new PartitionKeyRangeIdentity(collection.ResourceId, range.Id)).ToList();
				List<Task> list = new List<Task>();
				foreach (EndpointCache value in addressCacheByEndpoint.Values)
				{
					list.Add(value.AddressCache.OpenAsync(databaseName, collection, partitionKeyRangeIdentities, cancellationToken));
				}
				await Task.WhenAll(list);
			}
		}

		public Task<PartitionAddressInformation> ResolveAsync(DocumentServiceRequest request, bool forceRefresh, CancellationToken cancellationToken)
		{
			return GetAddressResolver(request).ResolveAsync(request, forceRefresh, cancellationToken);
		}

		/// <summary>
		/// ReplicatedResourceClient will use this API to get the direct connectivity AddressCache for given request.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private IAddressResolver GetAddressResolver(DocumentServiceRequest request)
		{
			Uri endpoint = endpointManager.ResolveServiceEndpoint(request);
			return GetOrAddEndpoint(endpoint).AddressResolver;
		}

		public void Dispose()
		{
			foreach (EndpointCache value in addressCacheByEndpoint.Values)
			{
				value.AddressCache.Dispose();
			}
		}

		private EndpointCache GetOrAddEndpoint(Uri endpoint)
		{
			EndpointCache orAdd = addressCacheByEndpoint.GetOrAdd(endpoint, delegate(Uri resolvedEndpoint)
			{
				GatewayAddressCache addressCache = new GatewayAddressCache(resolvedEndpoint, protocol, tokenProvider, userAgentContainer, serviceConfigReader, requestTimeout, 600L, messageHandler, apiType);
				string location = endpointManager.GetLocation(endpoint);
				AddressResolver addressResolver = new AddressResolver(null, new NullRequestSigner(), location);
				addressResolver.InitializeCaches(collectionCache, routingMapProvider, addressCache);
				return new EndpointCache
				{
					AddressCache = addressCache,
					AddressResolver = addressResolver
				};
			});
			if (addressCacheByEndpoint.Count > maxEndpoints)
			{
				Queue<Uri> queue = new Queue<Uri>(endpointManager.WriteEndpoints.Union(endpointManager.ReadEndpoints).Reverse());
				while (addressCacheByEndpoint.Count > maxEndpoints && queue.Count > 0)
				{
					addressCacheByEndpoint.TryRemove(queue.Dequeue(), out EndpointCache _);
				}
			}
			return orAdd;
		}
	}
}
