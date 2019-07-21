using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// Caches collection information.
	/// </summary>
	internal sealed class ClientCollectionCache : CollectionCache
	{
		private readonly IStoreModel storeModel;

		private readonly IAuthorizationTokenProvider tokenProvider;

		private readonly IRetryPolicyFactory retryPolicy;

		private readonly ISessionContainer sessionContainer;

		public ClientCollectionCache(ISessionContainer sessionContainer, IStoreModel storeModel, IAuthorizationTokenProvider tokenProvider, IRetryPolicyFactory retryPolicy)
		{
			if (storeModel == null)
			{
				throw new ArgumentNullException("storeModel");
			}
			this.storeModel = storeModel;
			this.tokenProvider = tokenProvider;
			this.retryPolicy = retryPolicy;
			this.sessionContainer = sessionContainer;
		}

		protected override Task<DocumentCollection> GetByRidAsync(string apiVersion, string collectionRid, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IDocumentClientRetryPolicy retryPolicyInstance = new ClearingSessionContainerClientRetryPolicy(sessionContainer, retryPolicy.GetRequestPolicy());
			return TaskHelper.InlineIfPossible(() => ReadCollectionAsync(PathsHelper.GeneratePath(ResourceType.Collection, collectionRid, isFeed: false), cancellationToken, retryPolicyInstance), retryPolicyInstance, cancellationToken);
		}

		protected override Task<DocumentCollection> GetByNameAsync(string apiVersion, string resourceAddress, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IDocumentClientRetryPolicy retryPolicyInstance = new ClearingSessionContainerClientRetryPolicy(sessionContainer, retryPolicy.GetRequestPolicy());
			return TaskHelper.InlineIfPossible(() => ReadCollectionAsync(resourceAddress, cancellationToken, retryPolicyInstance), retryPolicyInstance, cancellationToken);
		}

		private async Task<DocumentCollection> ReadCollectionAsync(string collectionLink, CancellationToken cancellationToken, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Collection, collectionLink, AuthorizationTokenType.PrimaryMasterKey, new StringKeyValueCollection()))
			{
				request.Headers["x-ms-date"] = DateTime.UtcNow.ToString("r");
				string userAuthorizationToken = tokenProvider.GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "GET", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
				request.Headers["authorization"] = userAuthorizationToken;
				using (new ActivityScope(Guid.NewGuid()))
				{
					retryPolicyInstance?.OnBeforeSendRequest(request);
					using (DocumentServiceResponse response = await storeModel.ProcessMessageAsync(request))
					{
						return new ResourceResponse<DocumentCollection>(response).Resource;
					}
				}
			}
		}
	}
}
