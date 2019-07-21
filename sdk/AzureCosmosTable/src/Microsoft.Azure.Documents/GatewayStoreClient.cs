using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class GatewayStoreClient : TransportClient
	{
		private HttpClient httpClient;

		private readonly ICommunicationEventSource eventSource;

		private JsonSerializerSettings SerializerSettings;

		public GatewayStoreClient(HttpClient httpClient, ICommunicationEventSource eventSource, JsonSerializerSettings serializerSettings = null)
		{
			this.httpClient = httpClient;
			SerializerSettings = serializerSettings;
			this.eventSource = eventSource;
		}

		public async Task<DocumentServiceResponse> InvokeAsync(DocumentServiceRequest request, ResourceType resourceType, Uri physicalAddress, CancellationToken cancellationToken)
		{
			using (HttpResponseMessage responseMessage = await InvokeClientAsync(request, resourceType, physicalAddress, cancellationToken))
			{
				return await ParseResponseAsync(responseMessage, request.SerializerSettings ?? SerializerSettings, request);
			}
		}

		public static bool IsFeedRequest(OperationType requestOperationType)
		{
			if (requestOperationType != 0 && requestOperationType != OperationType.Upsert && requestOperationType != OperationType.ReadFeed && requestOperationType != OperationType.Query)
			{
				return requestOperationType == OperationType.SqlQuery;
			}
			return true;
		}

		internal override async Task<StoreResponse> InvokeStoreAsync(Uri baseAddress, ResourceOperation resourceOperation, DocumentServiceRequest request)
		{
			Uri physicalAddress = IsFeedRequest(request.OperationType) ? HttpTransportClient.GetResourceFeedUri(resourceOperation.resourceType, baseAddress, request) : HttpTransportClient.GetResourceEntryUri(resourceOperation.resourceType, baseAddress, request);
			using (HttpResponseMessage responseMessage = await InvokeClientAsync(request, resourceOperation.resourceType, physicalAddress, default(CancellationToken)))
			{
				return await HttpTransportClient.ProcessHttpResponse(request.ResourceAddress, string.Empty, responseMessage, physicalAddress, request);
			}
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		internal Task<HttpResponseMessage> SendHttpAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default(CancellationToken))
		{
			return httpClient.SendHttpAsync(requestMessage, cancellationToken);
		}

		internal static async Task<DocumentServiceResponse> ParseResponseAsync(HttpResponseMessage responseMessage, JsonSerializerSettings serializerSettings = null, DocumentServiceRequest request = null)
		{
			using (responseMessage)
			{
				if (responseMessage.StatusCode < HttpStatusCode.BadRequest)
				{
					MemoryStream bufferedStream = new MemoryStream();
					await responseMessage.Content.CopyToAsync(bufferedStream);
					bufferedStream.Position = 0L;
					INameValueCollection headers = ExtractResponseHeaders(responseMessage);
					return new DocumentServiceResponse(bufferedStream, headers, responseMessage.StatusCode, serializerSettings);
				}
				if (request != null && request.IsValidStatusCodeForExceptionlessRetry((int)responseMessage.StatusCode))
				{
					return new DocumentServiceResponse(null, ExtractResponseHeaders(responseMessage), responseMessage.StatusCode, serializerSettings);
				}
				throw await CreateDocumentClientException(responseMessage);
			}
		}

		internal static INameValueCollection ExtractResponseHeaders(HttpResponseMessage responseMessage)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
			{
				if (string.Compare(header.Key, "x-ms-alt-content-path", StringComparison.Ordinal) == 0)
				{
					foreach (string item in header.Value)
					{
						nameValueCollection.Add(header.Key, Uri.UnescapeDataString(item));
					}
				}
				else
				{
					foreach (string item2 in header.Value)
					{
						nameValueCollection.Add(header.Key, item2);
					}
				}
			}
			if (responseMessage.Content != null)
			{
				foreach (KeyValuePair<string, IEnumerable<string>> header2 in responseMessage.Content.Headers)
				{
					if (string.Compare(header2.Key, "x-ms-alt-content-path", StringComparison.Ordinal) == 0)
					{
						foreach (string item3 in header2.Value)
						{
							nameValueCollection.Add(header2.Key, Uri.UnescapeDataString(item3));
						}
					}
					else
					{
						foreach (string item4 in header2.Value)
						{
							nameValueCollection.Add(header2.Key, item4);
						}
					}
				}
				return nameValueCollection;
			}
			return nameValueCollection;
		}

		internal static async Task<DocumentClientException> CreateDocumentClientException(HttpResponseMessage responseMessage)
		{
			Trace.CorrelationManager.ActivityId = Guid.Empty;
			bool isNameBased = false;
			bool isFeed = false;
			PathsHelper.TryParsePathSegments(responseMessage.RequestMessage.RequestUri.LocalPath, out isFeed, out string _, out string resourceIdOrFullName, out isNameBased);
			if (string.Equals(responseMessage.Content?.Headers?.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
			{
				return new DocumentClientException(JsonSerializable.LoadFrom<Error>(await responseMessage.Content.ReadAsStreamAsync()), responseMessage.Headers, responseMessage.StatusCode)
				{
					StatusDescription = responseMessage.ReasonPhrase,
					ResourceAddress = resourceIdOrFullName
				};
			}
			return new DocumentClientException(await responseMessage.Content.ReadAsStringAsync(), null, responseMessage.Headers, responseMessage.StatusCode, responseMessage.RequestMessage.RequestUri)
			{
				StatusDescription = responseMessage.ReasonPhrase,
				ResourceAddress = resourceIdOrFullName
			};
		}

		internal static bool IsAllowedRequestHeader(string headerName)
		{
			if (!headerName.StartsWith("x-ms", StringComparison.OrdinalIgnoreCase))
			{
				switch (headerName)
				{
				case "authorization":
				case "Accept":
				case "Content-Type":
				case "Host":
				case "If-Match":
				case "If-Modified-Since":
				case "If-None-Match":
				case "If-Range":
				case "If-Unmodified-Since":
				case "User-Agent":
				case "Prefer":
				case "x-ms-documentdb-query":
				case "A-IM":
					return true;
				default:
					return false;
				}
			}
			return true;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		private async Task<HttpRequestMessage> PrepareRequestMessageAsync(DocumentServiceRequest request, Uri physicalAddress)
		{
			HttpMethod head = HttpMethod.Head;
			HttpMethod method;
			if (request.OperationType == OperationType.Create || request.OperationType == OperationType.Upsert || request.OperationType == OperationType.Query || request.OperationType == OperationType.SqlQuery || request.OperationType == OperationType.ExecuteJavaScript || request.OperationType == OperationType.QueryPlan)
			{
				method = HttpMethod.Post;
			}
			else if (request.OperationType == OperationType.Read || request.OperationType == OperationType.ReadFeed)
			{
				method = HttpMethod.Get;
			}
			else if (request.OperationType == OperationType.Replace)
			{
				method = HttpMethod.Put;
			}
			else
			{
				if (request.OperationType != OperationType.Delete)
				{
					throw new NotImplementedException();
				}
				method = HttpMethod.Delete;
			}
			HttpRequestMessage requestMessage = new HttpRequestMessage(method, physicalAddress);
			if (request.Body != null)
			{
				await request.EnsureBufferedBodyAsync();
				MemoryStream memoryStream = new MemoryStream();
				request.CloneableBody.WriteTo(memoryStream);
				memoryStream.Position = 0L;
				requestMessage.Content = new StreamContent(memoryStream);
			}
			if (request.Headers != null)
			{
				foreach (string header in request.Headers)
				{
					if (IsAllowedRequestHeader(header))
					{
						if (header.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
						{
							requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(request.Headers[header]);
						}
						else
						{
							requestMessage.Headers.TryAddWithoutValidation(header, request.Headers[header]);
						}
					}
				}
			}
			Guid activityId = Trace.CorrelationManager.ActivityId;
			requestMessage.Headers.Add("x-ms-activity-id", activityId.ToString());
			return requestMessage;
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		private async Task<HttpResponseMessage> InvokeClientAsync(DocumentServiceRequest request, ResourceType resourceType, Uri physicalAddress, CancellationToken cancellationToken)
		{
			return await BackoffRetryUtility<HttpResponseMessage>.ExecuteAsync(async delegate
			{
				using (HttpRequestMessage requestMessage = await PrepareRequestMessageAsync(request, physicalAddress))
				{
					DateTime sendTimeUtc = DateTime.UtcNow;
					Guid localGuid = Guid.NewGuid();
					eventSource.Request(Guid.Empty, localGuid, requestMessage.RequestUri.ToString(), resourceType.ToResourceTypeString(), requestMessage.Headers);
					try
					{
						HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(requestMessage, cancellationToken);
						double totalMilliseconds = (DateTime.UtcNow - sendTimeUtc).TotalMilliseconds;
						Guid activityId = Guid.Empty;
						if (httpResponseMessage.Headers.TryGetValues("x-ms-activity-id", out IEnumerable<string> values) && values.Count() != 0)
						{
							activityId = new Guid(values.First());
						}
						eventSource.Response(activityId, localGuid, (short)httpResponseMessage.StatusCode, totalMilliseconds, httpResponseMessage.Headers);
						return httpResponseMessage;
					}
					catch (TaskCanceledException innerException)
					{
						if (!cancellationToken.IsCancellationRequested)
						{
							throw new RequestTimeoutException(innerException, requestMessage.RequestUri);
						}
						throw;
					}
				}
			}, new WebExceptionRetryPolicy(), cancellationToken);
		}
	}
}
