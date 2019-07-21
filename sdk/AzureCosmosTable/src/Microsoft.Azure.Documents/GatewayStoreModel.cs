using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class GatewayStoreModel : IStoreModel, IDisposable
	{
		private readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(65.0);

		private readonly GlobalEndpointManager endpointManager;

		private readonly DocumentClientEventSource eventSource;

		private readonly ISessionContainer sessionContainer;

		private readonly ConsistencyLevel defaultConsistencyLevel;

		private GatewayStoreClient gatewayStoreClient;

		private CookieContainer cookieJar;

		public GatewayStoreModel(GlobalEndpointManager endpointManager, ISessionContainer sessionContainer, TimeSpan requestTimeout, ConsistencyLevel defaultConsistencyLevel, DocumentClientEventSource eventSource, JsonSerializerSettings SerializerSettings, UserAgentContainer userAgent, ApiType apiType = ApiType.None, HttpMessageHandler messageHandler = null)
		{
			cookieJar = new CookieContainer();
			this.endpointManager = endpointManager;
			HttpClient httpClient = new HttpClient(messageHandler ?? new HttpClientHandler
			{
				CookieContainer = cookieJar
			});
			this.sessionContainer = sessionContainer;
			this.defaultConsistencyLevel = defaultConsistencyLevel;
			httpClient.Timeout = ((requestTimeout > this.requestTimeout) ? requestTimeout : this.requestTimeout);
			httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
			{
				NoCache = true
			};
			httpClient.AddUserAgentHeader(userAgent);
			httpClient.AddApiTypeHeader(apiType);
			httpClient.DefaultRequestHeaders.Add("x-ms-version", HttpConstants.Versions.CurrentVersion);
			httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			this.eventSource = eventSource;
			gatewayStoreClient = new GatewayStoreClient(httpClient, this.eventSource, SerializerSettings);
		}

		public virtual async Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, CancellationToken cancellationToken = default(CancellationToken))
		{
			ApplySessionToken(request);
			DocumentServiceResponse documentServiceResponse;
			try
			{
				Uri physicalAddress = GatewayStoreClient.IsFeedRequest(request.OperationType) ? GetFeedUri(request) : GetEntityUri(request);
				documentServiceResponse = await gatewayStoreClient.InvokeAsync(request, request.ResourceType, physicalAddress, cancellationToken);
			}
			catch (DocumentClientException ex)
			{
				if (!ReplicatedResourceClient.IsMasterResource(request.ResourceType) && (ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.Conflict || (ex.StatusCode == HttpStatusCode.NotFound && ex.GetSubStatus() != SubStatusCodes.PartitionKeyRangeGone)))
				{
					CaptureSessionToken(request, ex.Headers);
				}
				throw;
			}
			CaptureSessionToken(request, documentServiceResponse.Headers);
			return documentServiceResponse;
		}

		public virtual async Task<DatabaseAccount> GetDatabaseAccountAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default(CancellationToken))
		{
			DatabaseAccount databaseAccount = null;
			using (HttpResponseMessage responseMessage = await gatewayStoreClient.SendHttpAsync(requestMessage, cancellationToken))
			{
				using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(responseMessage))
				{
					databaseAccount = documentServiceResponse.GetInternalResource(DatabaseAccount.CreateNewInstance);
				}
				IEnumerable<string> values;
				if (responseMessage.Headers.TryGetValues("x-ms-max-media-storage-usage-mb", out values) && values.Count() != 0 && long.TryParse(values.First(), out long result))
				{
					databaseAccount.MaxMediaStorageUsageInMB = result;
				}
				if (responseMessage.Headers.TryGetValues("x-ms-media-storage-usage-mb", out values) && values.Count() != 0 && long.TryParse(values.First(), out result))
				{
					databaseAccount.MediaStorageUsageInMB = result;
				}
				if (responseMessage.Headers.TryGetValues("x-ms-databaseaccount-consumed-mb", out values) && values.Count() != 0 && long.TryParse(values.First(), out result))
				{
					databaseAccount.ConsumedDocumentStorageInMB = result;
				}
				if (responseMessage.Headers.TryGetValues("x-ms-databaseaccount-provisioned-mb", out values) && values.Count() != 0 && long.TryParse(values.First(), out result))
				{
					databaseAccount.ProvisionedDocumentStorageInMB = result;
				}
				if (responseMessage.Headers.TryGetValues("x-ms-databaseaccount-reserved-mb", out values) && values.Count() != 0 && long.TryParse(values.First(), out result))
				{
					databaseAccount.ReservedDocumentStorageInMB = result;
				}
			}
			return databaseAccount;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void CaptureSessionToken(DocumentServiceRequest request, INameValueCollection responseHeaders)
		{
			if (request.ResourceType == ResourceType.Collection && request.OperationType == OperationType.Delete)
			{
				string resourceId = (!request.IsNameBased) ? request.ResourceId : responseHeaders["x-ms-content-path"];
				sessionContainer.ClearTokenByResourceId(resourceId);
			}
			else
			{
				sessionContainer.SetSessionToken(request, responseHeaders);
			}
		}

		private void ApplySessionToken(DocumentServiceRequest request)
		{
			if (request.Headers != null && !string.IsNullOrEmpty(request.Headers["x-ms-session-token"]))
			{
				if (ReplicatedResourceClient.IsMasterResource(request.ResourceType))
				{
					request.Headers.Remove("x-ms-session-token");
				}
				return;
			}
			string text = request.Headers["x-ms-consistency-level"];
			if ((defaultConsistencyLevel == ConsistencyLevel.Session || (!string.IsNullOrEmpty(text) && string.Equals(text, ConsistencyLevel.Session.ToString(), StringComparison.OrdinalIgnoreCase))) && !ReplicatedResourceClient.IsMasterResource(request.ResourceType))
			{
				string value = sessionContainer.ResolveGlobalSessionToken(request);
				if (!string.IsNullOrEmpty(value))
				{
					request.Headers["x-ms-session-token"] = value;
				}
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposing && gatewayStoreClient != null)
			{
				try
				{
					gatewayStoreClient.Dispose();
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceWarning("Exception {0} thrown during dispose of HttpClient, this could happen if there are inflight request during the dispose of client", ex);
				}
				gatewayStoreClient = null;
			}
		}

		private Uri GetEntityUri(DocumentServiceRequest entity)
		{
			string text = entity.Headers["Content-Location"];
			if (!string.IsNullOrEmpty(text))
			{
				return new Uri(endpointManager.ResolveServiceEndpoint(entity), new Uri(text).AbsolutePath);
			}
			return new Uri(endpointManager.ResolveServiceEndpoint(entity), PathsHelper.GeneratePath(entity.ResourceType, entity, isFeed: false));
		}

		private Uri GetFeedUri(DocumentServiceRequest request)
		{
			return new Uri(endpointManager.ResolveServiceEndpoint(request), PathsHelper.GeneratePath(request.ResourceType, request, isFeed: true));
		}
	}
}
