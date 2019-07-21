using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This retry policy is designed to work with in a pair with ClientRetryPolicy.
	/// The inner retryPolicy must be a ClientRetryPolicy or a rety policy delegating to it.
	///
	/// The expectation that is the outer retry policy in the retry policy chain and nobody can overwrite ShouldRetryResult.
	/// Once we clear the session we expect call to fail and throw exceptio to the client. Otherwise we may violate session consistency.
	/// </summary>
	internal sealed class ClearingSessionContainerClientRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly IDocumentClientRetryPolicy retryPolicy;

		private readonly ISessionContainer sessionContainer;

		private DocumentServiceRequest request;

		private bool hasTriggered;

		public ClearingSessionContainerClientRetryPolicy(ISessionContainer sessionContainer, IDocumentClientRetryPolicy retryPolicy)
		{
			this.retryPolicy = retryPolicy;
			this.sessionContainer = sessionContainer;
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
			this.request = request;
			retryPolicy.OnBeforeSendRequest(request);
		}

		public async Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			ShouldRetryResult shouldRetryResult = await retryPolicy.ShouldRetryAsync(exception, cancellationToken);
			DocumentClientException ex = exception as DocumentClientException;
			return ShouldRetryInternal(ex?.StatusCode, ex?.GetSubStatus(), shouldRetryResult);
		}

		private ShouldRetryResult ShouldRetryInternal(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, ShouldRetryResult shouldRetryResult)
		{
			if (request == null)
			{
				return shouldRetryResult;
			}
			if (!shouldRetryResult.ShouldRetry && !hasTriggered && statusCode.HasValue && subStatusCode.HasValue && request.IsNameBased && statusCode.Value == HttpStatusCode.NotFound && subStatusCode.Value == SubStatusCodes.PartitionKeyRangeGone)
			{
				DefaultTrace.TraceWarning("Clear the the token for named base request {0}", request.ResourceAddress);
				sessionContainer.ClearTokenByCollectionFullname(request.ResourceAddress);
				hasTriggered = true;
			}
			return shouldRetryResult;
		}
	}
}
