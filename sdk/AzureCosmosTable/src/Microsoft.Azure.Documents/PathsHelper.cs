using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal static class PathsHelper
	{
		private static bool isClientSideValidationEnabled = true;

		/// <summary>
		///     The output resourceId can be 
		///     a: (Rid based) DgJ5AJeIfQABAAAAAAAAAPy3CWY= 
		///     b: (name based) dbs/dbName/colls/collectionName/docs/documentName/attachments/attachmentName",
		///     For name based, it always trimmed, RemoveTrailingSlashes, RemoveLeadingSlashes,  urldecoded
		/// </summary>
		/// <param name="resourceUrl"></param>
		/// <param name="isFeed"></param>
		/// <param name="resourcePath"> like dbs, colls</param>
		/// <param name="resourceIdOrFullName"></param>
		/// <param name="isNameBased"></param>
		/// <param name="clientVersion"></param>
		/// <returns></returns>
		public static bool TryParsePathSegments(string resourceUrl, out bool isFeed, out string resourcePath, out string resourceIdOrFullName, out bool isNameBased, string clientVersion = "")
		{
			string databaseName = string.Empty;
			string collectionName = string.Empty;
			return TryParsePathSegmentsWithDatabaseAndCollectionNames(resourceUrl, out isFeed, out resourcePath, out resourceIdOrFullName, out isNameBased, out databaseName, out collectionName, clientVersion);
		}

		/// <summary>
		///     The output resourceId can be 
		///     a: (Rid based) DgJ5AJeIfQABAAAAAAAAAPy3CWY= 
		///     b: (name based) dbs/dbName/colls/collectionName/docs/documentName/attachments/attachmentName",
		///     For name based, it always trimmed, RemoveTrailingSlashes, RemoveLeadingSlashes,  urldecoded
		/// </summary>
		/// <param name="resourceUrl"></param>
		/// <param name="isFeed"></param>
		/// <param name="resourcePath"> like dbs, colls</param>
		/// <param name="resourceIdOrFullName"></param>
		/// <param name="isNameBased"></param>
		/// <param name="databaseName"></param>
		/// <param name="collectionName"></param>
		/// <param name="clientVersion"></param>
		/// <param name="parseDatabaseAndCollectionNames"></param>
		/// <returns></returns>
		public static bool TryParsePathSegmentsWithDatabaseAndCollectionNames(string resourceUrl, out bool isFeed, out string resourcePath, out string resourceIdOrFullName, out bool isNameBased, out string databaseName, out string collectionName, string clientVersion = "", bool parseDatabaseAndCollectionNames = false)
		{
			resourcePath = string.Empty;
			resourceIdOrFullName = string.Empty;
			isFeed = false;
			isNameBased = false;
			databaseName = string.Empty;
			collectionName = string.Empty;
			if (string.IsNullOrEmpty(resourceUrl))
			{
				return false;
			}
			string[] array = resourceUrl.Split(new char[1]
			{
				'/'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (array == null || array.Length < 1)
			{
				return false;
			}
			int num = array.Length;
			string text = array[num - 1].Trim(new char[1]
			{
				'/'
			});
			string text2 = (num >= 2) ? array[num - 2].Trim(new char[1]
			{
				'/'
			}) : string.Empty;
			if (IsRootOperation(text2, text) || IsTopLevelOperationOperation(text2, text))
			{
				isFeed = false;
				resourceIdOrFullName = string.Empty;
				resourcePath = "/";
				return true;
			}
			if (num >= 2 && !array[0].Equals("media", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("offers", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("partitions", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("databaseaccount", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("topology", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("ridranges", StringComparison.OrdinalIgnoreCase) && !array[0].Equals("vectorclock", StringComparison.OrdinalIgnoreCase) && (!ResourceId.TryParse(array[1], out ResourceId rid) || !rid.IsDatabaseId))
			{
				isNameBased = true;
				return TryParseNameSegments(resourceUrl, array, out isFeed, out resourcePath, out resourceIdOrFullName, out databaseName, out collectionName, parseDatabaseAndCollectionNames);
			}
			if (num % 2 != 0 && IsResourceType(text))
			{
				isFeed = true;
				resourcePath = text;
				if (!text.Equals("dbs", StringComparison.OrdinalIgnoreCase))
				{
					resourceIdOrFullName = text2;
				}
			}
			else
			{
				if (!IsResourceType(text2))
				{
					return false;
				}
				isFeed = false;
				resourcePath = text2;
				resourceIdOrFullName = text;
				if (!string.IsNullOrEmpty(clientVersion) && resourcePath.Equals("media", StringComparison.OrdinalIgnoreCase))
				{
					string attachmentId = null;
					byte storageIndex = 0;
					if (!MediaIdHelper.TryParseMediaId(resourceIdOrFullName, out attachmentId, out storageIndex))
					{
						return false;
					}
					resourceIdOrFullName = attachmentId;
				}
			}
			return true;
		}

		public static void ParseDatabaseNameAndCollectionNameFromUrlSegments(string[] segments, out string databaseName, out string collectionName)
		{
			databaseName = string.Empty;
			collectionName = string.Empty;
			if (segments != null && segments.Length >= 2 && string.Equals(segments[0], "dbs", StringComparison.OrdinalIgnoreCase))
			{
				databaseName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(segments[1])));
				if (segments.Length >= 4 && string.Equals(segments[2], "colls", StringComparison.OrdinalIgnoreCase))
				{
					collectionName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(segments[3])));
				}
			}
		}

		private static bool TryParseNameSegments(string resourceUrl, string[] segments, out bool isFeed, out string resourcePath, out string resourceFullName, out string databaseName, out string collectionName, bool parseDatabaseAndCollectionNames)
		{
			isFeed = false;
			resourcePath = string.Empty;
			resourceFullName = string.Empty;
			databaseName = string.Empty;
			collectionName = string.Empty;
			if (segments == null || segments.Length < 1)
			{
				return false;
			}
			if (segments.Length % 2 == 0)
			{
				if (IsResourceType(segments[segments.Length - 2]))
				{
					resourcePath = segments[segments.Length - 2];
					resourceFullName = resourceUrl;
					resourceFullName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(resourceFullName)));
					if (parseDatabaseAndCollectionNames)
					{
						ParseDatabaseNameAndCollectionNameFromUrlSegments(segments, out databaseName, out collectionName);
					}
					return true;
				}
			}
			else if (IsResourceType(segments[segments.Length - 1]))
			{
				isFeed = true;
				resourcePath = segments[segments.Length - 1];
				resourceFullName = resourceUrl.Substring(0, UrlUtility.RemoveTrailingSlashes(resourceUrl).LastIndexOf("/", StringComparison.CurrentCultureIgnoreCase));
				resourceFullName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(resourceFullName)));
				if (parseDatabaseAndCollectionNames)
				{
					ParseDatabaseNameAndCollectionNameFromUrlSegments(segments, out databaseName, out collectionName);
				}
				return true;
			}
			return false;
		}

		public static ResourceType GetResourcePathSegment(string resourcePathSegment)
		{
			if (string.IsNullOrEmpty(resourcePathSegment))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.StringArgumentNullOrEmpty, "resourcePathSegment"));
			}
			switch (resourcePathSegment.ToLowerInvariant())
			{
			case "attachments":
				return ResourceType.Attachment;
			case "colls":
				return ResourceType.Collection;
			case "dbs":
				return ResourceType.Database;
			case "permissions":
				return ResourceType.Permission;
			case "users":
				return ResourceType.User;
			case "udts":
				return ResourceType.UserDefinedType;
			case "docs":
				return ResourceType.Document;
			case "sprocs":
				return ResourceType.StoredProcedure;
			case "udfs":
				return ResourceType.UserDefinedFunction;
			case "triggers":
				return ResourceType.Trigger;
			case "conflicts":
				return ResourceType.Conflict;
			case "offers":
				return ResourceType.Offer;
			case "schemas":
				return ResourceType.Schema;
			case "pkranges":
				return ResourceType.PartitionKeyRange;
			case "media":
				return ResourceType.Media;
			case "addresses":
				return ResourceType.Address;
			default:
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourcePathSegment));
			}
		}

		public static string GetResourcePath(ResourceType resourceType)
		{
			switch (resourceType)
			{
			case ResourceType.Database:
				return "dbs";
			case ResourceType.Collection:
				return "colls";
			case ResourceType.Document:
				return "docs";
			case ResourceType.StoredProcedure:
				return "sprocs";
			case ResourceType.UserDefinedFunction:
				return "udfs";
			case ResourceType.Trigger:
				return "triggers";
			case ResourceType.Conflict:
				return "conflicts";
			case ResourceType.Attachment:
				return "attachments";
			case ResourceType.User:
				return "users";
			case ResourceType.UserDefinedType:
				return "udts";
			case ResourceType.Permission:
				return "permissions";
			case ResourceType.Offer:
				return "offers";
			case ResourceType.PartitionKeyRange:
				return "pkranges";
			case ResourceType.Media:
				return "//media/";
			case ResourceType.Schema:
				return "schemas";
			case ResourceType.MasterPartition:
			case ResourceType.ServerPartition:
				return "partitions";
			case ResourceType.RidRange:
				return "ridranges";
			case ResourceType.VectorClock:
				return "vectorclock";
			case ResourceType.Address:
			case ResourceType.ServiceFabricService:
			case ResourceType.Replica:
			case ResourceType.Record:
			case ResourceType.BatchApply:
			case ResourceType.PartitionSetInformation:
			case ResourceType.XPReplicatorAddress:
			case ResourceType.DatabaseAccount:
			case ResourceType.Topology:
			case ResourceType.RestoreMetadata:
				return "/";
			default:
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString()));
			}
		}

		public static string GeneratePath(ResourceType resourceType, DocumentServiceRequest request, bool isFeed, bool notRequireValidation = false)
		{
			if (request.IsNameBased)
			{
				return GeneratePathForNameBased(resourceType, request.ResourceAddress, isFeed, notRequireValidation);
			}
			return GeneratePath(resourceType, request.ResourceId, isFeed);
		}

		public static string GenerateUserDefinedTypePath(string databaseName, string typeName)
		{
			return "dbs/" + databaseName + "/udts/" + typeName;
		}

		public static string GetCollectionPath(string resourceFullName)
		{
			if (resourceFullName != null)
			{
				int num = (resourceFullName.Length > 0 && resourceFullName[0] == '/') ? resourceFullName.IndexOfNth('/', 5) : resourceFullName.IndexOfNth('/', 4);
				if (num > 0)
				{
					return resourceFullName.Substring(0, num);
				}
			}
			return resourceFullName;
		}

		public static string GetDatabasePath(string resourceFullName)
		{
			if (resourceFullName != null)
			{
				int num = (resourceFullName.Length > 0 && resourceFullName[0] == '/') ? resourceFullName.IndexOfNth('/', 3) : resourceFullName.IndexOfNth('/', 2);
				if (num > 0)
				{
					return resourceFullName.Substring(0, num);
				}
			}
			return resourceFullName;
		}

		public static string GetParentByIndex(string resourceFullName, int segmentIndex)
		{
			int num = resourceFullName.IndexOfNth('/', segmentIndex);
			if (num > 0)
			{
				return resourceFullName.Substring(0, num);
			}
			num = resourceFullName.IndexOfNth('/', segmentIndex - 1);
			if (num > 0)
			{
				return resourceFullName;
			}
			return null;
		}

		public static string GeneratePathForNameBased(Type resourceType, string resourceOwnerFullName, string resourceName)
		{
			if (resourceName == null)
			{
				return null;
			}
			if ((object)resourceType == typeof(Database))
			{
				return "dbs/" + resourceName;
			}
			if (resourceOwnerFullName == null)
			{
				return null;
			}
			if ((object)resourceType == typeof(DocumentCollection))
			{
				return resourceOwnerFullName + "/colls/" + resourceName;
			}
			if ((object)resourceType == typeof(StoredProcedure))
			{
				return resourceOwnerFullName + "/sprocs/" + resourceName;
			}
			if ((object)resourceType == typeof(UserDefinedFunction))
			{
				return resourceOwnerFullName + "/udfs/" + resourceName;
			}
			if ((object)resourceType == typeof(Trigger))
			{
				return resourceOwnerFullName + "/triggers/" + resourceName;
			}
			if ((object)resourceType == typeof(Conflict))
			{
				return resourceOwnerFullName + "/conflicts/" + resourceName;
			}
			if (typeof(Attachment).IsAssignableFrom(resourceType))
			{
				return resourceOwnerFullName + "/attachments/" + resourceName;
			}
			if ((object)resourceType == typeof(User))
			{
				return resourceOwnerFullName + "/users/" + resourceName;
			}
			if ((object)resourceType == typeof(UserDefinedType))
			{
				return resourceOwnerFullName + "/udts/" + resourceName;
			}
			if (typeof(Permission).IsAssignableFrom(resourceType))
			{
				return resourceOwnerFullName + "/permissions/" + resourceName;
			}
			if (typeof(Document).IsAssignableFrom(resourceType))
			{
				return resourceOwnerFullName + "/docs/" + resourceName;
			}
			if ((object)resourceType == typeof(Offer))
			{
				return "offers/" + resourceName;
			}
			if ((object)resourceType == typeof(Schema))
			{
				return resourceOwnerFullName + "/schemas/" + resourceName;
			}
			if (typeof(Resource).IsAssignableFrom(resourceType))
			{
				return null;
			}
			throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString()));
		}

		internal static void SetClientSidevalidation(bool validation)
		{
			isClientSideValidationEnabled = validation;
		}

		private static string GeneratePathForNameBased(ResourceType resourceType, string resourceFullName, bool isFeed, bool notRequireValidation = false)
		{
			if (isFeed && string.IsNullOrEmpty(resourceFullName) && resourceType != 0)
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType));
			}
			string text = null;
			ResourceType resourceType2;
			if (!isFeed)
			{
				resourceType2 = resourceType;
				text = resourceFullName;
			}
			else
			{
				switch (resourceType)
				{
				case ResourceType.Database:
					return "dbs";
				case ResourceType.Collection:
					resourceType2 = ResourceType.Database;
					text = resourceFullName + "/colls";
					break;
				case ResourceType.StoredProcedure:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/sprocs";
					break;
				case ResourceType.UserDefinedFunction:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/udfs";
					break;
				case ResourceType.Trigger:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/triggers";
					break;
				case ResourceType.Conflict:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/conflicts";
					break;
				case ResourceType.Attachment:
					resourceType2 = ResourceType.Document;
					text = resourceFullName + "/attachments";
					break;
				case ResourceType.User:
					resourceType2 = ResourceType.Database;
					text = resourceFullName + "/users";
					break;
				case ResourceType.UserDefinedType:
					resourceType2 = ResourceType.Database;
					text = resourceFullName + "/udts";
					break;
				case ResourceType.Permission:
					resourceType2 = ResourceType.User;
					text = resourceFullName + "/permissions";
					break;
				case ResourceType.Document:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/docs";
					break;
				case ResourceType.Offer:
					return resourceFullName + "/offers";
				case ResourceType.PartitionKeyRange:
					return resourceFullName + "/pkranges";
				case ResourceType.Schema:
					resourceType2 = ResourceType.Collection;
					text = resourceFullName + "/schemas";
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString()));
				}
			}
			if (!notRequireValidation && isClientSideValidationEnabled && !ValidateResourceFullName(resourceType2, resourceFullName))
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType));
			}
			return text;
		}

		public static string GeneratePath(ResourceType resourceType, string ownerOrResourceId, bool isFeed)
		{
			if (isFeed && string.IsNullOrEmpty(ownerOrResourceId) && resourceType != 0 && resourceType != ResourceType.Offer && resourceType != ResourceType.DatabaseAccount && resourceType != ResourceType.MasterPartition && resourceType != ResourceType.ServerPartition && resourceType != ResourceType.Topology && resourceType != ResourceType.RidRange && resourceType != ResourceType.VectorClock)
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType));
			}
			if (isFeed && resourceType == ResourceType.Database)
			{
				return "dbs";
			}
			if (resourceType == ResourceType.Database)
			{
				return "dbs/" + ownerOrResourceId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Collection)
			{
				ResourceId resourceId = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId.DatabaseId.ToString() + "/colls";
			}
			if (resourceType == ResourceType.Collection)
			{
				ResourceId resourceId2 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId2.DatabaseId.ToString() + "/colls/" + resourceId2.DocumentCollectionId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Offer)
			{
				return "offers";
			}
			if (resourceType == ResourceType.Offer)
			{
				return "offers/" + ownerOrResourceId.ToString();
			}
			if (isFeed && resourceType == ResourceType.StoredProcedure)
			{
				ResourceId resourceId3 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId3.DatabaseId.ToString() + "/colls/" + resourceId3.DocumentCollectionId.ToString() + "/sprocs";
			}
			if (resourceType == ResourceType.StoredProcedure)
			{
				ResourceId resourceId4 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId4.DatabaseId.ToString() + "/colls/" + resourceId4.DocumentCollectionId.ToString() + "/sprocs/" + resourceId4.StoredProcedureId.ToString();
			}
			if (isFeed && resourceType == ResourceType.UserDefinedFunction)
			{
				ResourceId resourceId5 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId5.DatabaseId.ToString() + "/colls/" + resourceId5.DocumentCollectionId.ToString() + "/udfs";
			}
			if (resourceType == ResourceType.UserDefinedFunction)
			{
				ResourceId resourceId6 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId6.DatabaseId.ToString() + "/colls/" + resourceId6.DocumentCollectionId.ToString() + "/udfs/" + resourceId6.UserDefinedFunctionId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Trigger)
			{
				ResourceId resourceId7 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId7.DatabaseId.ToString() + "/colls/" + resourceId7.DocumentCollectionId.ToString() + "/triggers";
			}
			if (resourceType == ResourceType.Trigger)
			{
				ResourceId resourceId8 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId8.DatabaseId.ToString() + "/colls/" + resourceId8.DocumentCollectionId.ToString() + "/triggers/" + resourceId8.TriggerId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Conflict)
			{
				ResourceId resourceId9 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId9.DatabaseId.ToString() + "/colls/" + resourceId9.DocumentCollectionId.ToString() + "/conflicts";
			}
			if (resourceType == ResourceType.Conflict)
			{
				ResourceId resourceId10 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId10.DatabaseId.ToString() + "/colls/" + resourceId10.DocumentCollectionId.ToString() + "/conflicts/" + resourceId10.ConflictId.ToString();
			}
			if (isFeed && resourceType == ResourceType.PartitionKeyRange)
			{
				ResourceId resourceId11 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId11.DatabaseId.ToString() + "/colls/" + resourceId11.DocumentCollectionId.ToString() + "/pkranges";
			}
			if (resourceType == ResourceType.PartitionKeyRange)
			{
				ResourceId resourceId12 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId12.DatabaseId.ToString() + "/colls/" + resourceId12.DocumentCollectionId.ToString() + "/pkranges/" + resourceId12.PartitionKeyRangeId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Attachment)
			{
				ResourceId resourceId13 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId13.DatabaseId.ToString() + "/colls/" + resourceId13.DocumentCollectionId.ToString() + "/docs/" + resourceId13.DocumentId.ToString() + "/attachments";
			}
			if (resourceType == ResourceType.Attachment)
			{
				ResourceId resourceId14 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId14.DatabaseId.ToString() + "/colls/" + resourceId14.DocumentCollectionId.ToString() + "/docs/" + resourceId14.DocumentId.ToString() + "/attachments/" + resourceId14.AttachmentId.ToString();
			}
			if (isFeed && resourceType == ResourceType.User)
			{
				return "dbs/" + ownerOrResourceId + "/users";
			}
			if (resourceType == ResourceType.User)
			{
				ResourceId resourceId15 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId15.DatabaseId.ToString() + "/users/" + resourceId15.UserId.ToString();
			}
			if (isFeed && resourceType == ResourceType.UserDefinedType)
			{
				return "dbs/" + ownerOrResourceId + "/udts";
			}
			if (resourceType == ResourceType.UserDefinedType)
			{
				ResourceId resourceId16 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId16.DatabaseId.ToString() + "/udts/" + resourceId16.UserDefinedTypeId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Permission)
			{
				ResourceId resourceId17 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId17.DatabaseId.ToString() + "/users/" + resourceId17.UserId.ToString() + "/permissions";
			}
			if (resourceType == ResourceType.Permission)
			{
				ResourceId resourceId18 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId18.DatabaseId.ToString() + "/users/" + resourceId18.UserId.ToString() + "/permissions/" + resourceId18.PermissionId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Document)
			{
				ResourceId resourceId19 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId19.DatabaseId.ToString() + "/colls/" + resourceId19.DocumentCollectionId.ToString() + "/docs";
			}
			if (resourceType == ResourceType.Document)
			{
				ResourceId resourceId20 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId20.DatabaseId.ToString() + "/colls/" + resourceId20.DocumentCollectionId.ToString() + "/docs/" + resourceId20.DocumentId.ToString();
			}
			if (isFeed && resourceType == ResourceType.Schema)
			{
				ResourceId resourceId21 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId21.DatabaseId.ToString() + "/colls/" + resourceId21.DocumentCollectionId.ToString() + "/schemas";
			}
			if (resourceType == ResourceType.Schema)
			{
				ResourceId resourceId22 = ResourceId.Parse(ownerOrResourceId);
				return "dbs/" + resourceId22.DatabaseId.ToString() + "/colls/" + resourceId22.DocumentCollectionId.ToString() + "/schemas/" + resourceId22.SchemaId.ToString();
			}
			if (isFeed && resourceType == ResourceType.DatabaseAccount)
			{
				return "databaseaccount";
			}
			if (resourceType == ResourceType.DatabaseAccount)
			{
				return "databaseaccount/" + ownerOrResourceId;
			}
			if (isFeed && resourceType == ResourceType.MasterPartition)
			{
				return "partitions";
			}
			if (resourceType == ResourceType.MasterPartition)
			{
				return "partitions/" + ownerOrResourceId;
			}
			if (isFeed && resourceType == ResourceType.ServerPartition)
			{
				return "partitions";
			}
			if (resourceType == ResourceType.ServerPartition)
			{
				return "partitions/" + ownerOrResourceId;
			}
			if (isFeed && resourceType == ResourceType.Topology)
			{
				return "topology";
			}
			switch (resourceType)
			{
			case ResourceType.Topology:
				return "topology/" + ownerOrResourceId;
			case ResourceType.RidRange:
				return "ridranges/" + ownerOrResourceId;
			case ResourceType.VectorClock:
				return "vectorclock/" + ownerOrResourceId;
			default:
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString()));
			}
		}

		public static string GenerateRootOperationPath(OperationType operationType)
		{
			switch (operationType)
			{
			case OperationType.Pause:
				return "operations/pause";
			case OperationType.Recycle:
				return "operations/recycle";
			case OperationType.Resume:
				return "operations/resume";
			case OperationType.Stop:
				return "operations/stop";
			case OperationType.Crash:
				return "operations/crash";
			case OperationType.ForceConfigRefresh:
				return "operations/forceConfigRefresh";
			case OperationType.ReportThroughputUtilization:
				return "operations/reportthroughpututilization";
			case OperationType.BatchReportThroughputUtilization:
				return "operations/batchreportthroughpututilization";
			case OperationType.ControllerBatchGetOutput:
				return "operations/controllerbatchgetoutput";
			case OperationType.ControllerBatchReportCharges:
				return "operations/controllerbatchreportcharges";
			case OperationType.GetConfiguration:
				return "operations/getconfiguration";
			case OperationType.GetFederationConfigurations:
				return "operations/getfederationconfigurations";
			case OperationType.GetDatabaseAccountConfigurations:
				return "operations/getdatabaseaccountconfigurations";
			case OperationType.GetStorageAccountKey:
				return "operations/getstorageaccountkey";
			default:
				throw new NotFoundException();
			}
		}

		private static bool IsResourceType(string resourcePathSegment)
		{
			if (string.IsNullOrEmpty(resourcePathSegment))
			{
				return false;
			}
			switch (resourcePathSegment.ToLowerInvariant())
			{
			case "attachments":
			case "colls":
			case "dbs":
			case "permissions":
			case "users":
			case "udts":
			case "docs":
			case "sprocs":
			case "triggers":
			case "udfs":
			case "conflicts":
			case "media":
			case "offers":
			case "partitions":
			case "databaseaccount":
			case "topology":
			case "pkranges":
			case "presplitaction":
			case "postsplitaction":
			case "schemas":
			case "ridranges":
			case "vectorclock":
			case "addresses":
				return true;
			default:
				return false;
			}
		}

		private static bool IsRootOperation(string operationSegment, string operationTypeSegment)
		{
			if (string.IsNullOrEmpty(operationSegment))
			{
				return false;
			}
			if (string.IsNullOrEmpty(operationTypeSegment))
			{
				return false;
			}
			if (string.Compare(operationSegment, "operations", StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			switch (operationTypeSegment.ToLowerInvariant())
			{
			case "pause":
			case "resume":
			case "stop":
			case "recycle":
			case "crash":
			case "reportthroughpututilization":
			case "batchreportthroughpututilization":
			case "controllerbatchgetoutput":
			case "controllerbatchreportcharges":
			case "getfederationconfigurations":
			case "getconfiguration":
			case "getstorageaccountkey":
			case "getdatabaseaccountconfigurations":
				return true;
			default:
				return false;
			}
		}

		private static bool IsTopLevelOperationOperation(string replicaSegment, string addressSegment)
		{
			if (string.IsNullOrEmpty(replicaSegment) && (string.Compare(addressSegment, "xpreplicatoraddreses", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(addressSegment, "computegatewaycharge", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(addressSegment, "serviceReservation", StringComparison.OrdinalIgnoreCase) == 0))
			{
				return true;
			}
			return false;
		}

		internal static bool IsNameBased(string resourceIdOrFullName)
		{
			if (!string.IsNullOrEmpty(resourceIdOrFullName) && resourceIdOrFullName.Length > 4 && resourceIdOrFullName[3] == '/')
			{
				return true;
			}
			return false;
		}

		internal static int IndexOfNth(this string str, char value, int n)
		{
			if (string.IsNullOrEmpty(str) || n <= 0 || n > str.Length)
			{
				return -1;
			}
			int num = n;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == value && --num == 0)
				{
					return i;
				}
			}
			return -1;
		}

		internal static bool ValidateResourceFullName(ResourceType resourceType, string resourceFullName)
		{
			string[] array = resourceFullName.Split(new char[1]
			{
				'/'
			}, StringSplitOptions.RemoveEmptyEntries);
			string[] resourcePathArray = GetResourcePathArray(resourceType);
			if (resourcePathArray == null)
			{
				return false;
			}
			if (array.Length != resourcePathArray.Length * 2)
			{
				return false;
			}
			for (int i = 0; i < resourcePathArray.Length; i++)
			{
				if (string.Compare(resourcePathArray[i], array[2 * i], StringComparison.Ordinal) != 0)
				{
					return false;
				}
			}
			return true;
		}

		private static string[] GetResourcePathArray(ResourceType resourceType)
		{
			List<string> list = new List<string>();
			list.Add("dbs");
			switch (resourceType)
			{
			case ResourceType.User:
			case ResourceType.Permission:
				list.Add("users");
				if (resourceType == ResourceType.Permission)
				{
					list.Add("permissions");
				}
				break;
			case ResourceType.UserDefinedType:
				list.Add("udts");
				break;
			case ResourceType.Collection:
			case ResourceType.Document:
			case ResourceType.Attachment:
			case ResourceType.Conflict:
			case ResourceType.StoredProcedure:
			case ResourceType.Trigger:
			case ResourceType.UserDefinedFunction:
			case ResourceType.Schema:
			case ResourceType.PartitionKeyRange:
				list.Add("colls");
				switch (resourceType)
				{
				case ResourceType.StoredProcedure:
					list.Add("sprocs");
					break;
				case ResourceType.UserDefinedFunction:
					list.Add("udfs");
					break;
				case ResourceType.Trigger:
					list.Add("triggers");
					break;
				case ResourceType.Conflict:
					list.Add("conflicts");
					break;
				case ResourceType.Schema:
					list.Add("schemas");
					break;
				case ResourceType.Document:
				case ResourceType.Attachment:
					list.Add("docs");
					if (resourceType == ResourceType.Attachment)
					{
						list.Add("attachments");
					}
					break;
				case ResourceType.PartitionKeyRange:
					list.Add("pkranges");
					break;
				}
				break;
			default:
				return null;
			case ResourceType.Database:
				break;
			}
			return list.ToArray();
		}

		internal static bool ValidateResourceId(ResourceType resourceType, string resourceId)
		{
			switch (resourceType)
			{
			case ResourceType.Conflict:
				return ValidateConflictId(resourceId);
			case ResourceType.Database:
				return ValidateDatabaseId(resourceId);
			case ResourceType.Collection:
				return ValidateDocumentCollectionId(resourceId);
			case ResourceType.Document:
				return ValidateDocumentId(resourceId);
			case ResourceType.Permission:
				return ValidatePermissionId(resourceId);
			case ResourceType.StoredProcedure:
				return ValidateStoredProcedureId(resourceId);
			case ResourceType.Trigger:
				return ValidateTriggerId(resourceId);
			case ResourceType.UserDefinedFunction:
				return ValidateUserDefinedFunctionId(resourceId);
			case ResourceType.User:
				return ValidateUserId(resourceId);
			case ResourceType.UserDefinedType:
				return ValidateUserDefinedTypeId(resourceId);
			case ResourceType.Attachment:
				return ValidateAttachmentId(resourceId);
			case ResourceType.Schema:
				return ValidateSchemaId(resourceId);
			default:
				return false;
			}
		}

		internal static bool ValidateDatabaseId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Database != 0;
			}
			return false;
		}

		internal static bool ValidateDocumentCollectionId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.DocumentCollection != 0;
			}
			return false;
		}

		internal static bool ValidateDocumentId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Document != 0;
			}
			return false;
		}

		internal static bool ValidateConflictId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Conflict != 0;
			}
			return false;
		}

		internal static bool ValidateAttachmentId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Attachment != 0;
			}
			return false;
		}

		internal static bool ValidatePermissionId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Permission != 0;
			}
			return false;
		}

		internal static bool ValidateStoredProcedureId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.StoredProcedure != 0;
			}
			return false;
		}

		internal static bool ValidateTriggerId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Trigger != 0;
			}
			return false;
		}

		internal static bool ValidateUserDefinedFunctionId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.UserDefinedFunction != 0;
			}
			return false;
		}

		internal static bool ValidateUserId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.User != 0;
			}
			return false;
		}

		internal static bool ValidateUserDefinedTypeId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.UserDefinedType != 0;
			}
			return false;
		}

		internal static bool ValidateSchemaId(string resourceIdString)
		{
			ResourceId rid = null;
			if (ResourceId.TryParse(resourceIdString, out rid))
			{
				return rid.Schema != 0;
			}
			return false;
		}

		internal static bool IsPublicResource(Type resourceType)
		{
			if ((object)resourceType == typeof(Database) || (object)resourceType == typeof(DocumentCollection) || (object)resourceType == typeof(StoredProcedure) || (object)resourceType == typeof(UserDefinedFunction) || (object)resourceType == typeof(Trigger) || (object)resourceType == typeof(Conflict) || typeof(Attachment).IsAssignableFrom(resourceType) || (object)resourceType == typeof(User) || typeof(Permission).IsAssignableFrom(resourceType) || typeof(Document).IsAssignableFrom(resourceType) || (object)resourceType == typeof(Offer) || (object)resourceType == typeof(Schema))
			{
				return true;
			}
			return false;
		}

		internal static void ParseCollectionSelfLink(string collectionSelfLink, out string databaseId, out string collectionId)
		{
			string[] array = collectionSelfLink.Split(RuntimeConstants.Separators.Url, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 4 || !string.Equals(array[0], "dbs", StringComparison.OrdinalIgnoreCase) || !string.Equals(array[2], "colls", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(RMResources.BadUrl, "collectionSelfLink");
			}
			databaseId = array[1];
			collectionId = array[3];
		}
	}
}
