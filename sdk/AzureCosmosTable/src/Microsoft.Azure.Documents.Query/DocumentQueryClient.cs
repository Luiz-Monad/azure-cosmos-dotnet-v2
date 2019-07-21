using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	internal sealed class DocumentQueryClient : IDocumentQueryClient, IDisposable
	{
		private readonly DocumentClient innerClient;

		private QueryPartitionProvider queryPartitionProvider;

		private readonly SemaphoreSlim semaphore;

		QueryCompatibilityMode IDocumentQueryClient.QueryCompatibilityMode
		{
			get
			{
				return innerClient.QueryCompatibilityMode;
			}
			set
			{
				innerClient.QueryCompatibilityMode = value;
			}
		}

		IRetryPolicyFactory IDocumentQueryClient.ResetSessionTokenRetryPolicy
		{
			get
			{
				return innerClient.ResetSessionTokenRetryPolicy;
			}
		}

		Uri IDocumentQueryClient.ServiceEndpoint
		{
			get
			{
				return innerClient.ReadEndpoint;
			}
		}

		[Obsolete("Support for IPartitionResolver is now obsolete.")]
		IDictionary<string, IPartitionResolver> IDocumentQueryClient.PartitionResolvers
		{
			get
			{
				return innerClient.PartitionResolvers;
			}
		}

		ConnectionMode IDocumentQueryClient.ConnectionMode
		{
			get
			{
				return innerClient.ConnectionPolicy.ConnectionMode;
			}
		}

		Action<IQueryable> IDocumentQueryClient.OnExecuteScalarQueryCallback
		{
			get
			{
				return innerClient.OnExecuteScalarQueryCallback;
			}
		}

		public DocumentQueryClient(DocumentClient innerClient)
		{
			if (innerClient == null)
			{
				throw new ArgumentNullException("innerClient");
			}
			this.innerClient = innerClient;
			semaphore = new SemaphoreSlim(1, 1);
		}

		public void Dispose()
		{
			innerClient.Dispose();
			if (queryPartitionProvider != null)
			{
				queryPartitionProvider.Dispose();
			}
		}

		async Task<CollectionCache> IDocumentQueryClient.GetCollectionCacheAsync()
		{
			return await innerClient.GetCollectionCacheAsync();
		}

		async Task<IRoutingMapProvider> IDocumentQueryClient.GetRoutingMapProviderAsync()
		{
			return await innerClient.GetPartitionKeyRangeCacheAsync();
		}

		public async Task<QueryPartitionProvider> GetQueryPartitionProviderAsync(CancellationToken cancellationToken)
		{
			if (queryPartitionProvider == null)
			{
				await semaphore.WaitAsync(cancellationToken);
				if (queryPartitionProvider == null)
				{
					cancellationToken.ThrowIfCancellationRequested();
					queryPartitionProvider = new QueryPartitionProvider(await innerClient.GetQueryEngineConfiguration());
				}
				semaphore.Release();
			}
			return queryPartitionProvider;
		}

		public Task<DocumentServiceResponse> ExecuteQueryAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return innerClient.ExecuteQueryAsync(request, retryPolicyInstance, cancellationToken);
		}

		public Task<DocumentServiceResponse> ReadFeedAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return innerClient.ReadFeedAsync(request, retryPolicyInstance, cancellationToken);
		}

		public Task<ConsistencyLevel> GetDefaultConsistencyLevelAsync()
		{
			return innerClient.GetDefaultConsistencyLevelAsync();
		}

		public Task<ConsistencyLevel?> GetDesiredConsistencyLevelAsync()
		{
			return innerClient.GetDesiredConsistencyLevelAsync();
		}

		public Task EnsureValidOverwrite(ConsistencyLevel requestedConsistencyLevel)
		{
			innerClient.EnsureValidOverwrite(requestedConsistencyLevel);
			return CompletedTask.Instance;
		}

		public Task<PartitionKeyRangeCache> GetPartitionKeyRangeCache()
		{
			return innerClient.GetPartitionKeyRangeCacheAsync();
		}
	}
}
