using Microsoft.Azure.Documents.Common;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	internal class NonRetriableInvalidPartitionExceptionRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly CollectionCache clientCollectionCache;

		private readonly IDocumentClientRetryPolicy nextPolicy;

		public NonRetriableInvalidPartitionExceptionRetryPolicy(CollectionCache clientCollectionCache, IDocumentClientRetryPolicy nextPolicy)
		{
			if (clientCollectionCache == null)
			{
				throw new ArgumentNullException("clientCollectionCache");
			}
			if (nextPolicy == null)
			{
				throw new ArgumentNullException("nextPolicy");
			}
			this.clientCollectionCache = clientCollectionCache;
			this.nextPolicy = nextPolicy;
		}

		public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			DocumentClientException ex = exception as DocumentClientException;
			ShouldRetryResult shouldRetryResult = ShouldRetryInternal(ex?.StatusCode, ex?.GetSubStatus(), ex?.ResourceAddress);
			if (shouldRetryResult != null)
			{
				return Task.FromResult(shouldRetryResult);
			}
			return nextPolicy.ShouldRetryAsync(exception, cancellationToken);
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			nextPolicy.OnBeforeSendRequest(request);
		}

		private ShouldRetryResult ShouldRetryInternal(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, string resourceIdOrFullName)
		{
			if (!statusCode.HasValue && (!subStatusCode.HasValue || subStatusCode.Value == SubStatusCodes.Unknown))
			{
				return null;
			}
			if (statusCode == HttpStatusCode.Gone && subStatusCode == SubStatusCodes.NameCacheIsStale)
			{
				if (!string.IsNullOrEmpty(resourceIdOrFullName))
				{
					clientCollectionCache.Refresh(resourceIdOrFullName);
				}
				return ShouldRetryResult.NoRetry(new NotFoundException());
			}
			return null;
		}
	}
}
