using Microsoft.Azure.Documents.Common;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	internal class InvalidPartitionExceptionRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly CollectionCache clientCollectionCache;

		private readonly IDocumentClientRetryPolicy nextPolicy;

		private bool retried;

		public InvalidPartitionExceptionRetryPolicy(CollectionCache clientCollectionCache, IDocumentClientRetryPolicy nextPolicy)
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
			if (nextPolicy == null)
			{
				return Task.FromResult(ShouldRetryResult.NoRetry());
			}
			return nextPolicy.ShouldRetryAsync(exception, cancellationToken);
		}

		private ShouldRetryResult ShouldRetryInternal(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, string resourceIdOrFullName)
		{
			if (!statusCode.HasValue && (!subStatusCode.HasValue || subStatusCode.Value == SubStatusCodes.Unknown))
			{
				return null;
			}
			if (statusCode == HttpStatusCode.Gone && subStatusCode == SubStatusCodes.NameCacheIsStale)
			{
				if (!retried)
				{
					if (!string.IsNullOrEmpty(resourceIdOrFullName))
					{
						clientCollectionCache.Refresh(resourceIdOrFullName);
					}
					retried = true;
					return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
				}
				return ShouldRetryResult.NoRetry();
			}
			return null;
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			nextPolicy.OnBeforeSendRequest(request);
		}
	}
}
