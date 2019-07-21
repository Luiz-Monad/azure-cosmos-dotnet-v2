using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class PartitionKeyRangeGoneRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly CollectionCache collectionCache;

		private readonly IDocumentClientRetryPolicy nextRetryPolicy;

		private readonly PartitionKeyRangeCache partitionKeyRangeCache;

		private readonly string collectionLink;

		private bool retried;

		public PartitionKeyRangeGoneRetryPolicy(CollectionCache collectionCache, PartitionKeyRangeCache partitionKeyRangeCache, string collectionLink, IDocumentClientRetryPolicy nextRetryPolicy)
		{
			this.collectionCache = collectionCache;
			this.partitionKeyRangeCache = partitionKeyRangeCache;
			this.collectionLink = collectionLink;
			this.nextRetryPolicy = nextRetryPolicy;
		}

		/// <summary> 
		/// Should the caller retry the operation.
		/// </summary>
		/// <param name="exception">Exception that occured when the operation was tried</param>
		/// <param name="cancellationToken"></param>
		/// <returns>True indicates caller should retry, False otherwise</returns>
		public async Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			DocumentClientException ex = exception as DocumentClientException;
			ShouldRetryResult shouldRetryResult = await ShouldRetryInternalAsync(ex?.StatusCode, ex?.GetSubStatus(), cancellationToken);
			if (shouldRetryResult != null)
			{
				return shouldRetryResult;
			}
			return (nextRetryPolicy == null) ? ShouldRetryResult.NoRetry() : (await(nextRetryPolicy?.ShouldRetryAsync(exception, cancellationToken)));
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			nextRetryPolicy.OnBeforeSendRequest(request);
		}

		private async Task<ShouldRetryResult> ShouldRetryInternalAsync(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, CancellationToken cancellationToken)
		{
			if (!statusCode.HasValue && (!subStatusCode.HasValue || subStatusCode.Value == SubStatusCodes.Unknown))
			{
				return null;
			}
			if (statusCode == HttpStatusCode.Gone && subStatusCode == SubStatusCodes.PartitionKeyRangeGone)
			{
				if (retried)
				{
					return ShouldRetryResult.NoRetry();
				}
				using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Collection, collectionLink, null, AuthorizationTokenType.PrimaryMasterKey))
				{
					DocumentCollection collection = await collectionCache.ResolveCollectionAsync(request, cancellationToken);
					CollectionRoutingMap collectionRoutingMap = await partitionKeyRangeCache.TryLookupAsync(collection.ResourceId, null, request, cancellationToken);
					if (collectionRoutingMap != null)
					{
						await partitionKeyRangeCache.TryLookupAsync(collection.ResourceId, collectionRoutingMap, request, cancellationToken);
					}
				}
				retried = true;
				return ShouldRetryResult.RetryAfter(TimeSpan.FromSeconds(0.0));
			}
			return null;
		}
	}
}
