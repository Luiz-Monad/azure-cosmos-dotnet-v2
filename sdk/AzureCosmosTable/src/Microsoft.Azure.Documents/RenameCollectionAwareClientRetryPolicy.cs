using Microsoft.Azure.Documents.Routing;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This retry policy is designed to work with in a pair with ClientRetryPolicy.
	/// The inner retryPolicy must be a ClientRetryPolicy or a rety policy delegating to it.
	/// </summary>
	internal sealed class RenameCollectionAwareClientRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private readonly IDocumentClientRetryPolicy retryPolicy;

		private readonly ISessionContainer sessionContainer;

		private readonly ClientCollectionCache collectionCache;

		private DocumentServiceRequest request;

		private bool hasTriggered;

		public RenameCollectionAwareClientRetryPolicy(ISessionContainer sessionContainer, ClientCollectionCache collectionCache, IDocumentClientRetryPolicy retryPolicy)
		{
			this.retryPolicy = retryPolicy;
			this.sessionContainer = sessionContainer;
			this.collectionCache = collectionCache;
			request = null;
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
			return await ShouldRetryInternalAsync(ex?.StatusCode, ex?.GetSubStatus(), shouldRetryResult, cancellationToken);
		}

		private async Task<ShouldRetryResult> ShouldRetryInternalAsync(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode, ShouldRetryResult shouldRetryResult, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				DefaultTrace.TraceWarning("Cannot apply RenameCollectionAwareClientRetryPolicy as OnBeforeSendRequest has not been called and there is no DocumentServiceRequest context.");
				return shouldRetryResult;
			}
			if (!shouldRetryResult.ShouldRetry && !hasTriggered && statusCode.HasValue && subStatusCode.HasValue && request.IsNameBased && statusCode.Value == HttpStatusCode.NotFound && subStatusCode.Value == SubStatusCodes.PartitionKeyRangeGone)
			{
				DefaultTrace.TraceWarning("Clear the the token for named base request {0}", request.ResourceAddress);
				sessionContainer.ClearTokenByCollectionFullname(request.ResourceAddress);
				hasTriggered = true;
				string oldCollectionRid = request.RequestContext.ResolvedCollectionRid;
				request.ForceNameCacheRefresh = true;
				request.RequestContext.ResolvedCollectionRid = null;
				try
				{
					DocumentCollection documentCollection = await collectionCache.ResolveCollectionAsync(request, cancellationToken);
					if (documentCollection == null)
					{
						DefaultTrace.TraceCritical("Can't recover from session unavailable exception because resolving collection name {0} returned null", request.ResourceAddress);
					}
					else if (!string.IsNullOrEmpty(oldCollectionRid) && !string.IsNullOrEmpty(documentCollection.ResourceId) && !oldCollectionRid.Equals(documentCollection.ResourceId))
					{
						return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
					}
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceCritical("Can't recover from session unavailable exception because resolving collection name {0} failed with {1}", request.ResourceAddress, ex.ToString());
				}
			}
			return shouldRetryResult;
		}
	}
}
