using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class HttpTransportClient : TransportClient
	{
		private readonly HttpClient httpClient;

		private readonly ICommunicationEventSource eventSource;

		public const string Match = "Match";

		public HttpTransportClient(int requestTimeout, ICommunicationEventSource eventSource, UserAgentContainer userAgent = null, int idleTimeoutInSeconds = -1)
		{
			httpClient = new HttpClient();
			httpClient.Timeout = TimeSpan.FromSeconds(requestTimeout);
			httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
			{
				NoCache = true
			};
			httpClient.DefaultRequestHeaders.Add("x-ms-version", HttpConstants.Versions.CurrentVersion);
			if (userAgent == null)
			{
				userAgent = new UserAgentContainer();
			}
			httpClient.AddUserAgentHeader(userAgent);
			httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			this.eventSource = eventSource;
		}

		public override void Dispose()
		{
			base.Dispose();
			if (httpClient != null)
			{
				httpClient.Dispose();
			}
		}

		private void BeforeRequest(Guid activityId, Uri uri, ResourceType resourceType, HttpRequestHeaders requestHeaders)
		{
			eventSource.Request(activityId, Guid.Empty, uri.ToString(), resourceType.ToResourceTypeString(), requestHeaders);
		}

		private void AfterRequest(Guid activityId, HttpStatusCode statusCode, double durationInMilliSeconds, HttpResponseHeaders responseHeaders)
		{
			eventSource.Response(activityId, Guid.Empty, (short)statusCode, durationInMilliSeconds, responseHeaders);
		}

		internal override async Task<StoreResponse> InvokeStoreAsync(Uri physicalAddress, ResourceOperation resourceOperation, DocumentServiceRequest request)
		{
			Guid activityId = Trace.CorrelationManager.ActivityId;
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			nameValueCollection.Add("x-ms-request-validation-failure", "1");
			if (!request.IsBodySeekableClonableAndCountable)
			{
				throw new InternalServerErrorException(RMResources.InternalServerError, nameValueCollection);
			}
			if (resourceOperation.operationType == OperationType.Recreate)
			{
				DefaultTrace.TraceCritical("Received Recreate request on Http client");
				throw new InternalServerErrorException(RMResources.InternalServerError, nameValueCollection);
			}
			using (HttpRequestMessage requestMessage = PrepareHttpMessage(activityId, physicalAddress, resourceOperation, request))
			{
				HttpResponseMessage responseMessage = null;
				DateTime sendTimeUtc = DateTime.UtcNow;
				try
				{
					BeforeRequest(activityId, requestMessage.RequestUri, request.ResourceType, requestMessage.Headers);
					responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
				}
				catch (Exception ex)
				{
					Trace.CorrelationManager.ActivityId = activityId;
					if (WebExceptionUtility.IsWebExceptionRetriable(ex))
					{
						DefaultTrace.TraceInformation("Received retriable exception {0} sending the request to {1}, will reresolve the address send time UTC: {2}", ex, physicalAddress, sendTimeUtc);
						throw new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), ex, null, physicalAddress.ToString());
					}
					if (request.IsReadOnlyRequest)
					{
						DefaultTrace.TraceInformation("Received exception {0} on readonly requestsending the request to {1}, will reresolve the address send time UTC: {2}", ex, physicalAddress, sendTimeUtc);
						throw new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), ex, null, physicalAddress.ToString());
					}
					throw new ServiceUnavailableException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.ServiceUnavailable), ex, null, physicalAddress)
					{
						Headers = 
						{
							{
								"x-ms-request-validation-failure",
								"1"
							},
							{
								"x-ms-write-request-trigger-refresh",
								"1"
							}
						}
					};
				}
				finally
				{
					double totalMilliseconds = (DateTime.UtcNow - sendTimeUtc).TotalMilliseconds;
					AfterRequest(activityId, responseMessage?.StatusCode ?? ((HttpStatusCode)0), totalMilliseconds, responseMessage?.Headers);
				}
				using (responseMessage)
				{
					return await ProcessHttpResponse(request.ResourceAddress, activityId.ToString(), responseMessage, physicalAddress, request);
				}
			}
		}

		private static void AddHeader(HttpRequestHeaders requestHeaders, string headerName, DocumentServiceRequest request)
		{
			string value = request.Headers[headerName];
			if (!string.IsNullOrEmpty(value))
			{
				requestHeaders.Add(headerName, value);
			}
		}

		private static void AddHeader(HttpContentHeaders requestHeaders, string headerName, DocumentServiceRequest request)
		{
			string value = request.Headers[headerName];
			if (!string.IsNullOrEmpty(value))
			{
				requestHeaders.Add(headerName, value);
			}
		}

		private static void AddHeader(HttpRequestHeaders requestHeaders, string headerName, string headerValue)
		{
			if (!string.IsNullOrEmpty(headerValue))
			{
				requestHeaders.Add(headerName, headerValue);
			}
		}

		private string GetMatch(DocumentServiceRequest request, ResourceOperation resourceOperation)
		{
			switch (resourceOperation.operationType)
			{
			case OperationType.ExecuteJavaScript:
			case OperationType.Patch:
			case OperationType.Delete:
			case OperationType.Replace:
			case OperationType.Upsert:
				return request.Headers["If-Match"];
			case OperationType.Read:
			case OperationType.ReadFeed:
				return request.Headers["If-None-Match"];
			default:
				return null;
			}
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000: DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		private HttpRequestMessage PrepareHttpMessage(Guid activityId, Uri physicalAddress, ResourceOperation resourceOperation, DocumentServiceRequest request)
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
			AddHeader(httpRequestMessage.Headers, "x-ms-version", request);
			AddHeader(httpRequestMessage.Headers, "User-Agent", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-max-item-count", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-pre-trigger-include", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-pre-trigger-exclude", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-post-trigger-include", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-post-trigger-exclude", request);
			AddHeader(httpRequestMessage.Headers, "authorization", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-indexing-directive", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-migratecollection-directive", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-consistency-level", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-session-token", request);
			AddHeader(httpRequestMessage.Headers, "Prefer", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-expiry-seconds", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-query-enable-scan", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-query-emit-traces", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-cancharge", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-canthrottle", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-query-enable-low-precision-order-by", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-script-enable-logging", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-is-readonly-script", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-content-serialization-format", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-continuation", request.Continuation);
			AddHeader(httpRequestMessage.Headers, "x-ms-activity-id", activityId.ToString());
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-partitionkey", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-partitionkeyrangeid", request);
			string dateHeader = Helpers.GetDateHeader(request.Headers);
			AddHeader(httpRequestMessage.Headers, "x-ms-date", dateHeader);
			AddHeader(httpRequestMessage.Headers, "Match", GetMatch(request, resourceOperation));
			AddHeader(httpRequestMessage.Headers, "If-Modified-Since", request);
			AddHeader(httpRequestMessage.Headers, "A-IM", request);
			if (!request.IsNameBased)
			{
				AddHeader(httpRequestMessage.Headers, "x-docdb-resource-id", request.ResourceId);
			}
			AddHeader(httpRequestMessage.Headers, "x-docdb-entity-id", request.EntityId);
			string headerValue = request.Headers["x-ms-is-fanout-request"];
			AddHeader(httpRequestMessage.Headers, "x-ms-is-fanout-request", headerValue);
			if (request.ResourceType == ResourceType.Collection)
			{
				AddHeader(httpRequestMessage.Headers, "collection-partition-index", request.Headers["collection-partition-index"]);
				AddHeader(httpRequestMessage.Headers, "collection-service-index", request.Headers["collection-service-index"]);
			}
			if (request.Headers["x-ms-bind-replica"] != null)
			{
				AddHeader(httpRequestMessage.Headers, "x-ms-bind-replica", request.Headers["x-ms-bind-replica"]);
				AddHeader(httpRequestMessage.Headers, "x-ms-primary-master-key", request.Headers["x-ms-primary-master-key"]);
				AddHeader(httpRequestMessage.Headers, "x-ms-secondary-master-key", request.Headers["x-ms-secondary-master-key"]);
				AddHeader(httpRequestMessage.Headers, "x-ms-primary-readonly-key", request.Headers["x-ms-primary-readonly-key"]);
				AddHeader(httpRequestMessage.Headers, "x-ms-secondary-readonly-key", request.Headers["x-ms-secondary-readonly-key"]);
			}
			if (request.Headers["x-ms-can-offer-replace-complete"] != null)
			{
				AddHeader(httpRequestMessage.Headers, "x-ms-can-offer-replace-complete", request.Headers["x-ms-can-offer-replace-complete"]);
			}
			AddHeader(httpRequestMessage.Headers, "x-ms-is-auto-scale", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-isquery", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-query", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-is-upsert", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-supportspatiallegacycoordinates", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-partitioncount", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-collection-rid", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-filterby-schema-rid", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-usepolygonssmallerthanahemisphere", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-gateway-signature", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-populatequotainfo", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-disable-ru-per-minute-usage", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-populatequerymetrics", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-force-query-scan", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-responsecontinuationtokenlimitinkb", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-remote-storage-type", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-share-throughput", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-populatepartitionstatistics", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-documentdb-populatecollectionthroughputinfo", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-remaining-time-in-ms-on-client", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-client-retry-attempt-count", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-target-lsn", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-target-global-committed-lsn", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-federation-for-auth", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-exclude-system-properties", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-fanout-operation-state", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-allow-tentative-writes", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-include-tentative-writes", request);
			AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-preserve-full-content", request);
			if (resourceOperation.operationType == OperationType.Batch)
			{
				AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-is-batch-request", request);
				AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-batch-continue-on-error", request);
				AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-batch-ordered", request);
				AddHeader(httpRequestMessage.Headers, "x-ms-cosmos-batch-atomic", request);
			}
			Stream content = null;
			if (request.Body != null)
			{
				content = request.CloneableBody.Clone();
			}
			switch (resourceOperation.operationType)
			{
			case OperationType.Create:
			case OperationType.Batch:
				httpRequestMessage.RequestUri = GetResourceFeedUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.ExecuteJavaScript:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.Delete:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Delete;
				break;
			case OperationType.Read:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Get;
				break;
			case OperationType.ReadFeed:
				httpRequestMessage.RequestUri = GetResourceFeedUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Get;
				break;
			case OperationType.Replace:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Put;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.Patch:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = new HttpMethod("PATCH");
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.QueryPlan:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				AddHeader(httpRequestMessage.Content.Headers, "Content-Type", request);
				break;
			case OperationType.SqlQuery:
			case OperationType.Query:
				httpRequestMessage.RequestUri = GetResourceFeedUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				AddHeader(httpRequestMessage.Content.Headers, "Content-Type", request);
				break;
			case OperationType.Upsert:
				httpRequestMessage.RequestUri = GetResourceFeedUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.Head:
				httpRequestMessage.RequestUri = GetResourceEntryUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Head;
				break;
			case OperationType.HeadFeed:
				httpRequestMessage.RequestUri = GetResourceFeedUri(resourceOperation.resourceType, physicalAddress, request);
				httpRequestMessage.Method = HttpMethod.Head;
				break;
			case OperationType.ForceConfigRefresh:
			case OperationType.Pause:
			case OperationType.Resume:
			case OperationType.Stop:
			case OperationType.Recycle:
			case OperationType.Crash:
				httpRequestMessage.RequestUri = GetRootOperationUri(physicalAddress, resourceOperation.operationType);
				httpRequestMessage.Method = HttpMethod.Post;
				break;
			case OperationType.ServiceReservation:
				httpRequestMessage.RequestUri = physicalAddress;
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			case OperationType.ControllerBatchGetOutput:
			case OperationType.ControllerBatchReportCharges:
			case OperationType.ReportThroughputUtilization:
			case OperationType.BatchReportThroughputUtilization:
				httpRequestMessage.RequestUri = GetRootOperationUri(physicalAddress, resourceOperation.operationType);
				httpRequestMessage.Method = HttpMethod.Post;
				httpRequestMessage.Content = new StreamContent(content);
				break;
			default:
				throw new NotFoundException();
			}
			return httpRequestMessage;
		}

		internal static Uri GetResourceFeedUri(ResourceType resourceType, Uri physicalAddress, DocumentServiceRequest request)
		{
			switch (resourceType)
			{
			case ResourceType.Attachment:
				return GetAttachmentFeedUri(physicalAddress, request);
			case ResourceType.Collection:
				return GetCollectionFeedUri(physicalAddress, request);
			case ResourceType.Conflict:
				return GetConflictFeedUri(physicalAddress, request);
			case ResourceType.Database:
				return GetDatabaseFeedUri(physicalAddress);
			case ResourceType.Document:
				return GetDocumentFeedUri(physicalAddress, request);
			case ResourceType.Permission:
				return GetPermissionFeedUri(physicalAddress, request);
			case ResourceType.StoredProcedure:
				return GetStoredProcedureFeedUri(physicalAddress, request);
			case ResourceType.Trigger:
				return GetTriggerFeedUri(physicalAddress, request);
			case ResourceType.User:
				return GetUserFeedUri(physicalAddress, request);
			case ResourceType.UserDefinedType:
				return GetUserDefinedTypeFeedUri(physicalAddress, request);
			case ResourceType.UserDefinedFunction:
				return GetUserDefinedFunctionFeedUri(physicalAddress, request);
			case ResourceType.Schema:
				return GetSchemaFeedUri(physicalAddress, request);
			case ResourceType.Offer:
				return GetOfferFeedUri(physicalAddress, request);
			case ResourceType.Replica:
			case ResourceType.Module:
			case ResourceType.ModuleCommand:
			case ResourceType.Record:
				throw new NotFoundException();
			default:
				throw new NotFoundException();
			}
		}

		internal static Uri GetResourceEntryUri(ResourceType resourceType, Uri physicalAddress, DocumentServiceRequest request)
		{
			switch (resourceType)
			{
			case ResourceType.Attachment:
				return GetAttachmentEntryUri(physicalAddress, request);
			case ResourceType.Collection:
				return GetCollectionEntryUri(physicalAddress, request);
			case ResourceType.Conflict:
				return GetConflictEntryUri(physicalAddress, request);
			case ResourceType.Database:
				return GetDatabaseEntryUri(physicalAddress, request);
			case ResourceType.Document:
				return GetDocumentEntryUri(physicalAddress, request);
			case ResourceType.Permission:
				return GetPermissionEntryUri(physicalAddress, request);
			case ResourceType.StoredProcedure:
				return GetStoredProcedureEntryUri(physicalAddress, request);
			case ResourceType.Trigger:
				return GetTriggerEntryUri(physicalAddress, request);
			case ResourceType.User:
				return GetUserEntryUri(physicalAddress, request);
			case ResourceType.UserDefinedType:
				return GetUserDefinedTypeEntryUri(physicalAddress, request);
			case ResourceType.UserDefinedFunction:
				return GetUserDefinedFunctionEntryUri(physicalAddress, request);
			case ResourceType.Schema:
				return GetSchemaEntryUri(physicalAddress, request);
			case ResourceType.Offer:
				return GetOfferEntryUri(physicalAddress, request);
			case ResourceType.Replica:
				return GetRootFeedUri(physicalAddress);
			case ResourceType.Module:
			case ResourceType.ModuleCommand:
			case ResourceType.Record:
				throw new NotFoundException();
			default:
				throw new NotFoundException();
			}
		}

		private static Uri GetRootFeedUri(Uri baseAddress)
		{
			return baseAddress;
		}

		private static Uri GetRootOperationUri(Uri baseAddress, OperationType operationType)
		{
			return new Uri(baseAddress, PathsHelper.GenerateRootOperationPath(operationType));
		}

		private static Uri GetDatabaseFeedUri(Uri baseAddress)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Database, string.Empty, isFeed: true));
		}

		private static Uri GetDatabaseEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Database, request, isFeed: false));
		}

		private static Uri GetCollectionFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Collection, request, isFeed: true));
		}

		private static Uri GetStoredProcedureFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.StoredProcedure, request, isFeed: true));
		}

		private static Uri GetTriggerFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Trigger, request, isFeed: true));
		}

		private static Uri GetUserDefinedFunctionFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.UserDefinedFunction, request, isFeed: true));
		}

		private static Uri GetCollectionEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Collection, request, isFeed: false));
		}

		private static Uri GetStoredProcedureEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.StoredProcedure, request, isFeed: false));
		}

		private static Uri GetTriggerEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Trigger, request, isFeed: false));
		}

		private static Uri GetUserDefinedFunctionEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.UserDefinedFunction, request, isFeed: false));
		}

		private static Uri GetDocumentFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Document, request, isFeed: true));
		}

		private static Uri GetDocumentEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Document, request, isFeed: false));
		}

		private static Uri GetConflictFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Conflict, request, isFeed: true));
		}

		private static Uri GetConflictEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Conflict, request, isFeed: false));
		}

		private static Uri GetAttachmentFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Attachment, request, isFeed: true));
		}

		private static Uri GetAttachmentEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Attachment, request, isFeed: false));
		}

		private static Uri GetUserFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.User, request, isFeed: true));
		}

		private static Uri GetUserEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.User, request, isFeed: false));
		}

		private static Uri GetUserDefinedTypeFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.UserDefinedType, request, isFeed: true));
		}

		private static Uri GetUserDefinedTypeEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.UserDefinedType, request, isFeed: false));
		}

		private static Uri GetPermissionFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Permission, request, isFeed: true));
		}

		private static Uri GetPermissionEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Permission, request, isFeed: false));
		}

		private static Uri GetOfferFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Offer, request, isFeed: true));
		}

		private static Uri GetSchemaFeedUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Schema, request, isFeed: true));
		}

		private static Uri GetSchemaEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Schema, request, isFeed: false));
		}

		private static Uri GetOfferEntryUri(Uri baseAddress, DocumentServiceRequest request)
		{
			return new Uri(baseAddress, PathsHelper.GeneratePath(ResourceType.Offer, request, isFeed: false));
		}

		public static Task<StoreResponse> ProcessHttpResponse(string resourceAddress, string activityId, HttpResponseMessage response, Uri physicalAddress, DocumentServiceRequest request)
		{
			if (response == null)
			{
				InternalServerErrorException ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.InvalidBackendResponse), physicalAddress);
				ex.Headers.Set("x-ms-activity-id", activityId);
				ex.Headers.Add("x-ms-request-validation-failure", "1");
				throw ex;
			}
			if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified)
			{
				return CreateStoreResponseFromHttpResponse(response);
			}
			return CreateErrorResponseFromHttpResponse(resourceAddress, activityId, response, request);
		}

		private static async Task<StoreResponse> CreateErrorResponseFromHttpResponse(string resourceAddress, string activityId, HttpResponseMessage response, DocumentServiceRequest request)
		{
			using (response)
			{
				HttpStatusCode statusCode = response.StatusCode;
				string text = await TransportClient.GetErrorResponseAsync(response);
				long result = -1L;
				if (response.Headers.TryGetValues("lsn", out IEnumerable<string> values))
				{
					long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
				}
				string partitionKeyRangeId = null;
				if (response.Headers.TryGetValues("x-ms-documentdb-partitionkeyrangeid", out IEnumerable<string> values2))
				{
					partitionKeyRangeId = values2.FirstOrDefault();
				}
				DocumentClientException ex;
				switch (statusCode)
				{
				case HttpStatusCode.Unauthorized:
					ex = new UnauthorizedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Unauthorized : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.Forbidden:
					ex = new ForbiddenException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Forbidden : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.NotFound:
					if (response.Content != null && response.Content.Headers != null && response.Content.Headers.ContentType != null && !string.IsNullOrEmpty(response.Content.Headers.ContentType.MediaType) && response.Content.Headers.ContentType.MediaType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
					{
						ex = new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), response.RequestMessage.RequestUri)
						{
							LSN = result,
							PartitionKeyRangeId = partitionKeyRangeId
						};
						ex.Headers.Set("x-ms-activity-id", activityId);
					}
					else
					{
						if (request.IsValidStatusCodeForExceptionlessRetry((int)statusCode))
						{
							return await CreateStoreResponseFromHttpResponse(response, includeContent: false);
						}
						ex = new NotFoundException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.NotFound : text), response.Headers, response.RequestMessage.RequestUri);
					}
					break;
				case HttpStatusCode.BadRequest:
					ex = new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.BadRequest : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.MethodNotAllowed:
					ex = new MethodNotAllowedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.MethodNotAllowed : text), null, response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.Gone:
				{
					TransportClient.LogGoneException(response.RequestMessage.RequestUri, activityId);
					uint result2 = 0u;
					try
					{
						IEnumerable<string> values3 = response.Headers.GetValues("x-ms-substatus");
						if (values3 != null && values3.Any() && !uint.TryParse(values3.First(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result2))
						{
							ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.InvalidBackendResponse), response.Headers, response.RequestMessage.RequestUri);
							break;
						}
					}
					catch (InvalidOperationException)
					{
						DefaultTrace.TraceInformation("SubStatus doesn't exist in the header");
					}
					switch (result2)
					{
					case 1000u:
						ex = new InvalidPartitionException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Gone : text), response.Headers, response.RequestMessage.RequestUri);
						break;
					case 1002u:
						ex = new PartitionKeyRangeGoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Gone : text), response.Headers, response.RequestMessage.RequestUri);
						break;
					case 1007u:
						ex = new PartitionKeyRangeIsSplittingException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Gone : text), response.Headers, response.RequestMessage.RequestUri);
						break;
					case 1008u:
						ex = new PartitionIsMigratingException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Gone : text), response.Headers, response.RequestMessage.RequestUri);
						break;
					default:
						ex = new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), response.Headers, response.RequestMessage.RequestUri);
						ex.Headers.Set("x-ms-activity-id", activityId);
						break;
					}
					break;
				}
				case HttpStatusCode.Conflict:
					if (request.IsValidStatusCodeForExceptionlessRetry((int)statusCode))
					{
						return await CreateStoreResponseFromHttpResponse(response, includeContent: false);
					}
					ex = new ConflictException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.EntityAlreadyExists : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.PreconditionFailed:
					if (request.IsValidStatusCodeForExceptionlessRetry((int)statusCode))
					{
						return await CreateStoreResponseFromHttpResponse(response, includeContent: false);
					}
					ex = new PreconditionFailedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.PreconditionFailed : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.RequestEntityTooLarge:
					ex = new RequestEntityTooLargeException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.Format(CultureInfo.CurrentUICulture, RMResources.RequestEntityTooLarge, "x-ms-max-item-count")), response.Headers, response.RequestMessage.RequestUri);
					break;
				case (HttpStatusCode)423/*HttpStatusCode.Locked*/:
					ex = new LockedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.Locked : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.ServiceUnavailable:
					ex = new ServiceUnavailableException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.ServiceUnavailable : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case HttpStatusCode.RequestTimeout:
					ex = new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.RequestTimeout : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case (HttpStatusCode)449:
					ex = new RetryWithException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.RetryWith : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				case (HttpStatusCode)429/*HttpStatusCode.TooManyRequests*/:
				{
					if (request.IsValidStatusCodeForExceptionlessRetry((int)statusCode))
					{
						return await CreateStoreResponseFromHttpResponse(response, includeContent: false);
					}
					ex = new RequestRateTooLargeException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.TooManyRequests : text), response.Headers, response.RequestMessage.RequestUri);
					IEnumerable<string> enumerable = null;
					try
					{
						enumerable = response.Headers.GetValues("x-ms-retry-after-ms");
					}
					catch (InvalidOperationException)
					{
						DefaultTrace.TraceWarning("RequestRateTooLargeException being thrown without RetryAfter.");
					}
					if (enumerable != null && enumerable.Any())
					{
						ex.Headers.Set("x-ms-retry-after-ms", enumerable.First());
					}
					break;
				}
				case HttpStatusCode.InternalServerError:
					ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.InternalServerError : text), response.Headers, response.RequestMessage.RequestUri);
					break;
				default:
					DefaultTrace.TraceCritical("Unrecognized status code {0} returned by backend. ActivityId {1}", statusCode, activityId);
					TransportClient.LogException(response.RequestMessage.RequestUri, activityId);
					ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.InvalidBackendResponse), response.Headers, response.RequestMessage.RequestUri);
					break;
				}
				ex.LSN = result;
				ex.PartitionKeyRangeId = partitionKeyRangeId;
				ex.ResourceAddress = resourceAddress;
				throw ex;
			}
		}

		internal static string GetHeader(string[] names, string[] values, string name)
		{
			for (int i = 0; i < names.Length; i++)
			{
				if (string.Equals(names[i], name, StringComparison.Ordinal))
				{
					return values[i];
				}
			}
			return null;
		}

		public static async Task<StoreResponse> CreateStoreResponseFromHttpResponse(HttpResponseMessage responseMessage, bool includeContent = true)
		{
			StoreResponse response = new StoreResponse();
			using (responseMessage)
			{
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
				{
					list.Add(header.Key);
					if (string.Compare(header.Key, "x-ms-alt-content-path", StringComparison.Ordinal) == 0)
					{
						list2.Add(Uri.UnescapeDataString(header.Value.SingleOrDefault()));
					}
					else
					{
						list2.Add(header.Value.SingleOrDefault());
					}
				}
				response.ResponseHeaderNames = list.ToArray();
				response.ResponseHeaderValues = list2.ToArray();
				response.Status = (int)responseMessage.StatusCode;
				if (includeContent && responseMessage.Content != null)
				{
					Stream bufferredStream = new MemoryStream();
					await responseMessage.Content.CopyToAsync(bufferredStream);
					bufferredStream.Position = 0L;
					response.ResponseBody = bufferredStream;
				}
				return response;
			}
		}
	}
}
