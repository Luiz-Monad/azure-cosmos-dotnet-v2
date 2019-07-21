using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal abstract class TransportClient : IDisposable
	{
		public virtual void Dispose()
		{
		}

		public virtual Task<StoreResponse> InvokeResourceOperationAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, new ResourceOperation(request.OperationType, request.ResourceType), request);
		}

		public Task<StoreResponse> CreateOfferAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateOffer, request);
		}

		public Task<StoreResponse> GetOfferAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadOffer, request);
		}

		public Task<StoreResponse> ListOffersAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadOfferFeed, request);
		}

		public Task<StoreResponse> DeleteOfferAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteOffer, request);
		}

		public Task<StoreResponse> ReplaceOfferAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceOffer, request);
		}

		public Task<StoreResponse> QueryOfferAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Offer, request);
		}

		public Task<StoreResponse> GetPartitionSetAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadPartitionSetInformation, request);
		}

		public Task<StoreResponse> GetRestoreMetadataFeedAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadRestoreMetadataFeed, request);
		}

		public Task<StoreResponse> GetReplicaAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadReplica, request);
		}

		public Task<StoreResponse> ListDatabasesAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadDatabaseFeed, request);
		}

		public Task<StoreResponse> HeadDatabasesAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.HeadDatabaseFeed, request);
		}

		public Task<StoreResponse> GetDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadDatabase, request);
		}

		public Task<StoreResponse> CreateDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateDatabase, request);
		}

		public Task<StoreResponse> UpsertDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertDatabase, request);
		}

		public Task<StoreResponse> PatchDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.PatchDatabase, request);
		}

		public Task<StoreResponse> ReplaceDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceDatabase, request);
		}

		public Task<StoreResponse> DeleteDatabaseAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteDatabase, request);
		}

		public Task<StoreResponse> QueryDatabasesAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Database, request);
		}

		public Task<StoreResponse> ListDocumentCollectionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadCollectionFeed, request);
		}

		public Task<StoreResponse> GetDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadCollection, request);
		}

		public Task<StoreResponse> HeadDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.HeadCollection, request);
		}

		public Task<StoreResponse> CreateDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateCollection, request);
		}

		public Task<StoreResponse> PatchDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.PatchCollection, request);
		}

		public Task<StoreResponse> ReplaceDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceCollection, request);
		}

		public Task<StoreResponse> DeleteDocumentCollectionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteCollection, request);
		}

		public Task<StoreResponse> QueryDocumentCollectionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Collection, request);
		}

		public Task<StoreResponse> ListStoredProceduresAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadStoredProcedureFeed, request);
		}

		public Task<StoreResponse> GetStoredProcedureAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadStoredProcedure, request);
		}

		public Task<StoreResponse> CreateStoredProcedureAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateStoredProcedure, request);
		}

		public Task<StoreResponse> UpsertStoredProcedureAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertStoredProcedure, request);
		}

		public Task<StoreResponse> ReplaceStoredProcedureAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceStoredProcedure, request);
		}

		public Task<StoreResponse> DeleteStoredProcedureAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteStoredProcedure, request);
		}

		public Task<StoreResponse> QueryStoredProceduresAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.StoredProcedure, request);
		}

		public Task<StoreResponse> ListTriggersAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReadTriggerFeed, request);
		}

		public Task<StoreResponse> GetTriggerAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReadTrigger, request);
		}

		public Task<StoreResponse> CreateTriggerAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXCreateTrigger, request);
		}

		public Task<StoreResponse> UpsertTriggerAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXUpsertTrigger, request);
		}

		public Task<StoreResponse> ReplaceTriggerAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReplaceTrigger, request);
		}

		public Task<StoreResponse> DeleteTriggerAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXDeleteTrigger, request);
		}

		public Task<StoreResponse> QueryTriggersAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Trigger, request);
		}

		public Task<StoreResponse> ListUserDefinedFunctionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReadUserDefinedFunctionFeed, request);
		}

		public Task<StoreResponse> GetUserDefinedFunctionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReadUserDefinedFunction, request);
		}

		public Task<StoreResponse> CreateUserDefinedFunctionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXCreateUserDefinedFunction, request);
		}

		public Task<StoreResponse> UpsertUserDefinedFunctionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXUpsertUserDefinedFunction, request);
		}

		public Task<StoreResponse> ReplaceUserDefinedFunctionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXReplaceUserDefinedFunction, request);
		}

		public Task<StoreResponse> DeleteUserDefinedFunctionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XXDeleteUserDefinedFunction, request);
		}

		public Task<StoreResponse> QueryUserDefinedFunctionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.UserDefinedFunction, request);
		}

		public Task<StoreResponse> ListConflictsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XReadConflictFeed, request);
		}

		public Task<StoreResponse> GetConflictAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XReadConflict, request);
		}

		public Task<StoreResponse> DeleteConflictAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XDeleteConflict, request);
		}

		public Task<StoreResponse> QueryConflictsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Conflict, request);
		}

		public Task<StoreResponse> ListDocumentsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadDocumentFeed, request);
		}

		public Task<StoreResponse> GetDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadDocument, request);
		}

		public Task<StoreResponse> CreateDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateDocument, request);
		}

		public Task<StoreResponse> UpsertDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertDocument, request);
		}

		public Task<StoreResponse> PatchDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.PatchDocument, request);
		}

		public Task<StoreResponse> ReplaceDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceDocument, request);
		}

		public Task<StoreResponse> DeleteDocumentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteDocument, request);
		}

		public Task<StoreResponse> QueryDocumentsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Document, request);
		}

		public Task<StoreResponse> ListAttachmentsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadAttachmentFeed, request);
		}

		public Task<StoreResponse> GetAttachmentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadAttachment, request);
		}

		public Task<StoreResponse> CreateAttachmentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateAttachment, request);
		}

		public Task<StoreResponse> UpsertAttachmentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertAttachment, request);
		}

		public Task<StoreResponse> ReplaceAttachmentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceAttachment, request);
		}

		public Task<StoreResponse> DeleteAttachmentAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteAttachment, request);
		}

		public Task<StoreResponse> QueryAttachmentsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Attachment, request);
		}

		public Task<StoreResponse> ListUsersAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadUserFeed, request);
		}

		public Task<StoreResponse> GetUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadUser, request);
		}

		public Task<StoreResponse> CreateUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreateUser, request);
		}

		public Task<StoreResponse> UpsertUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertUser, request);
		}

		public Task<StoreResponse> PatchUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.PatchUser, request);
		}

		public Task<StoreResponse> ReplaceUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplaceUser, request);
		}

		public Task<StoreResponse> DeleteUserAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeleteUser, request);
		}

		public Task<StoreResponse> QueryUsersAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.User, request);
		}

		public Task<StoreResponse> ListPermissionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadPermissionFeed, request);
		}

		public Task<StoreResponse> GetPermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReadPermission, request);
		}

		public Task<StoreResponse> CreatePermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.CreatePermission, request);
		}

		public Task<StoreResponse> UpsertPermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.UpsertPermission, request);
		}

		public Task<StoreResponse> PatchPermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.PatchPermission, request);
		}

		public Task<StoreResponse> ReplacePermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ReplacePermission, request);
		}

		public Task<StoreResponse> DeletePermissionAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.DeletePermission, request);
		}

		public Task<StoreResponse> QueryPermissionsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeQueryStoreAsync(physicalAddress, ResourceType.Permission, request);
		}

		public Task<StoreResponse> ListRecordsAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XReadRecordFeed, request);
		}

		public Task<StoreResponse> CreateRecordAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XCreateRecord, request);
		}

		public Task<StoreResponse> ReadRecordAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XReadRecord, request);
		}

		public Task<StoreResponse> PatchRecordAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XUpdateRecord, request);
		}

		public Task<StoreResponse> DeleteRecordAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.XDeleteRecord, request);
		}

		public Task<StoreResponse> ExecuteAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			return InvokeStoreAsync(physicalAddress, ResourceOperation.ExecuteDocumentFeed, request);
		}

		public static void ThrowServerException(string resourceAddress, StoreResponse storeResponse, Uri physicalAddress, Guid activityId, DocumentServiceRequest request = null)
		{
			string text = null;
			if (storeResponse.Status < 300 || storeResponse.Status == 304 || (request != null && request.IsValidStatusCodeForExceptionlessRetry(storeResponse.Status, storeResponse.SubStatusCode)))
			{
				return;
			}
			INameValueCollection responseHeaders;
			DocumentClientException ex;
			switch (storeResponse.Status)
			{
			case 401:
				text = GetErrorResponse(storeResponse, RMResources.Unauthorized, out responseHeaders);
				ex = new UnauthorizedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 403:
				text = GetErrorResponse(storeResponse, RMResources.Forbidden, out responseHeaders);
				ex = new ForbiddenException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 404:
				text = GetErrorResponse(storeResponse, RMResources.NotFound, out responseHeaders);
				ex = new NotFoundException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 400:
				text = GetErrorResponse(storeResponse, RMResources.BadRequest, out responseHeaders);
				ex = new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 405:
				text = GetErrorResponse(storeResponse, RMResources.MethodNotAllowed, out responseHeaders);
				ex = new MethodNotAllowedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 410:
			{
				LogGoneException(physicalAddress, activityId.ToString());
				text = GetErrorResponse(storeResponse, RMResources.Gone, out responseHeaders);
				uint result = 0u;
				string text2 = responseHeaders.Get("x-ms-substatus");
				if (!string.IsNullOrEmpty(text2) && !uint.TryParse(text2, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
				{
					ex = new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, string.IsNullOrEmpty(text) ? RMResources.BadRequest : text), responseHeaders, physicalAddress);
					break;
				}
				switch (result)
				{
				case 1000u:
					ex = new InvalidPartitionException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
					break;
				case 1002u:
					ex = new PartitionKeyRangeGoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
					break;
				case 1007u:
					ex = new PartitionKeyRangeIsSplittingException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
					break;
				case 1008u:
					ex = new PartitionIsMigratingException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
					break;
				default:
					ex = new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), responseHeaders, physicalAddress);
					break;
				}
				break;
			}
			case 409:
				text = GetErrorResponse(storeResponse, RMResources.EntityAlreadyExists, out responseHeaders);
				ex = new ConflictException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 412:
				text = GetErrorResponse(storeResponse, RMResources.PreconditionFailed, out responseHeaders);
				ex = new PreconditionFailedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 413:
				text = GetErrorResponse(storeResponse, string.Format(CultureInfo.CurrentUICulture, RMResources.RequestEntityTooLarge, "x-ms-max-item-count"), out responseHeaders);
				ex = new RequestEntityTooLargeException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 423:
				text = GetErrorResponse(storeResponse, RMResources.Locked, out responseHeaders);
				ex = new LockedException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 429:
				text = GetErrorResponse(storeResponse, RMResources.TooManyRequests, out responseHeaders);
				ex = new RequestRateTooLargeException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 503:
				text = GetErrorResponse(storeResponse, RMResources.ServiceUnavailable, out responseHeaders);
				ex = new ServiceUnavailableException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 408:
				text = GetErrorResponse(storeResponse, RMResources.RequestTimeout, out responseHeaders);
				ex = new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 449:
				text = GetErrorResponse(storeResponse, RMResources.RetryWith, out responseHeaders);
				ex = new RetryWithException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			case 500:
				text = GetErrorResponse(storeResponse, RMResources.InternalServerError, out responseHeaders);
				ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			default:
				DefaultTrace.TraceCritical("Unrecognized status code {0} returned by backend. ActivityId {1}", storeResponse.Status, activityId);
				LogException(null, physicalAddress, resourceAddress, activityId);
				text = GetErrorResponse(storeResponse, RMResources.InvalidBackendResponse, out responseHeaders);
				ex = new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, text), responseHeaders, physicalAddress);
				break;
			}
			ex.LSN = storeResponse.LSN;
			ex.PartitionKeyRangeId = storeResponse.PartitionKeyRangeId;
			ex.ResourceAddress = resourceAddress;
			throw ex;
		}

		protected Task<StoreResponse> InvokeQueryStoreAsync(Uri physicalAddress, ResourceType resourceType, DocumentServiceRequest request)
		{
			OperationType operationType = (!string.Equals(request.Headers["Content-Type"], "application/sql", StringComparison.Ordinal)) ? OperationType.Query : OperationType.SqlQuery;
			return InvokeStoreAsync(physicalAddress, ResourceOperation.Query(operationType, resourceType), request);
		}

		internal abstract Task<StoreResponse> InvokeStoreAsync(Uri physicalAddress, ResourceOperation resourceOperation, DocumentServiceRequest request);

		protected static async Task<string> GetErrorResponseAsync(HttpResponseMessage responseMessage)
		{
			if (responseMessage.Content != null)
			{
				return GetErrorFromStream(await responseMessage.Content.ReadAsStreamAsync());
			}
			return "";
		}

		protected static string GetErrorResponse(StoreResponse storeResponse, string defaultMessage, out INameValueCollection responseHeaders)
		{
			string text = null;
			responseHeaders = new StringKeyValueCollection();
			if (storeResponse.ResponseBody != null)
			{
				text = GetErrorFromStream(storeResponse.ResponseBody);
			}
			if (storeResponse.ResponseHeaderNames != null)
			{
				for (int i = 0; i < storeResponse.ResponseHeaderNames.Count(); i++)
				{
					responseHeaders.Add(storeResponse.ResponseHeaderNames[i], storeResponse.ResponseHeaderValues[i]);
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return defaultMessage;
		}

		protected static string GetErrorFromStream(Stream responseStream)
		{
			using (responseStream)
			{
				return new StreamReader(responseStream).ReadToEnd();
			}
		}

		protected static void LogException(Uri physicalAddress, string activityId)
		{
			DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "Store Request Failed. Store Physical Address {0} ActivityId {1}", physicalAddress, activityId));
		}

		protected static void LogException(Exception exception, Uri physicalAddress, string rid, Guid activityId)
		{
			if (exception != null)
			{
				DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "Store Request Failed. Exception {0} Store Physical Address {1} RID {2} ActivityId {3}", exception.Message, physicalAddress, rid, activityId.ToString()));
			}
			else
			{
				DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "Store Request Failed. Store Physical Address {0} RID {1} ActivityId {2}", physicalAddress, rid, activityId.ToString()));
			}
		}

		protected static void LogGoneException(Uri physicalAddress, string activityId)
		{
			DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "Listener not found. Store Physical Address {0} ActivityId {1}", physicalAddress, activityId));
		}
	}
}
