using Microsoft.Azure.Documents.Common;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class PartitionKeyMismatchRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly CollectionCache clientCollectionCache;

		private readonly IDocumentClientRetryPolicy nextRetryPolicy;

		private const int MaxRetries = 1;

		private int retriesAttempted;

		public PartitionKeyMismatchRetryPolicy(CollectionCache clientCollectionCache, IDocumentClientRetryPolicy nextRetryPolicy)
		{
			if (clientCollectionCache == null)
			{
				throw new ArgumentNullException("clientCollectionCache");
			}
			if (nextRetryPolicy == null)
			{
				throw new ArgumentNullException("nextRetryPolicy");
			}
			this.clientCollectionCache = clientCollectionCache;
			this.nextRetryPolicy = nextRetryPolicy;
		}

		/// <summary> 
		/// Should the caller retry the operation.
		/// </summary>
		/// <param name="exception">Exception that occured when the operation was tried</param>
		/// <param name="cancellationToken"></param>
		/// <returns>True indicates caller should retry, False otherwise</returns>
		public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			DocumentClientException ex = exception as DocumentClientException;
			ShouldRetryResult shouldRetryResult = ShouldRetryInternal(ex?.StatusCode, ex?.GetSubStatus(), ex?.ResourceAddress);
			if (shouldRetryResult != null)
			{
				return Task.FromResult(shouldRetryResult);
			}
			return nextRetryPolicy.ShouldRetryAsync(exception, cancellationToken);
		}

		/// <summary>
		/// Method that is called before a request is sent to allow the retry policy implementation
		/// to modify the state of the request.
		/// </summary>
		/// <param name="request">The request being sent to the service.</param>
		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			nextRetryPolicy.OnBeforeSendRequest(request);
		}

		private ShouldRetryResult ShouldRetryInternal(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, string resourceIdOrFullName)
		{
			if (!statusCode.HasValue && (!subStatusCode.HasValue || subStatusCode.Value == SubStatusCodes.Unknown))
			{
				return null;
			}
			if (statusCode == HttpStatusCode.BadRequest && subStatusCode == SubStatusCodes.PartitionKeyMismatch && retriesAttempted < 1)
			{
				if (!string.IsNullOrEmpty(resourceIdOrFullName))
				{
					clientCollectionCache.Refresh(resourceIdOrFullName);
				}
				retriesAttempted++;
				return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
			}
			return null;
		}
	}
}
