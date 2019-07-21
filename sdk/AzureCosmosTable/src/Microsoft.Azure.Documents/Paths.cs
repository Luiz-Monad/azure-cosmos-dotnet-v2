namespace Microsoft.Azure.Documents
{
	internal static class Paths
	{
		public const string Root = "/";

		public const string OperationsPathSegment = "operations";

		public const string OperationId = "operationId";

		public const string ReplicaOperations_Pause = "pause";

		public const string ReplicaOperations_Resume = "resume";

		public const string ReplicaOperations_Stop = "stop";

		public const string ReplicaOperations_Recycle = "recycle";

		public const string ReplicaOperations_Crash = "crash";

		public const string ReplicaOperations_ForceConfigRefresh = "forceConfigRefresh";

		public const string ReplicaOperations_ReportThroughputUtilization = "reportthroughpututilization";

		public const string ReplicaOperations_BatchReportThroughputUtilization = "batchreportthroughpututilization";

		public const string Operations_GetFederationConfigurations = "getfederationconfigurations";

		public const string Operations_GetConfiguration = "getconfiguration";

		public const string Operations_GetDatabaseAccountConfigurations = "getdatabaseaccountconfigurations";

		public const string Operations_GetStorageAccountKey = "getstorageaccountkey";

		public const string DatabasesPathSegment = "dbs";

		public const string Databases_Root = "//dbs/";

		public const string DatabaseId = "dbId";

		public const string Database_Root = "//dbs/{dbId}";

		public const string UsersPathSegment = "users";

		public const string Users_Root = "//dbs/{dbId}/users/";

		public const string UserId = "userid";

		public const string User_Root = "//dbs/{dbId}/users/{userid}";

		public const string UserDefinedTypesPathSegment = "udts";

		public const string UserDefinedTypes_Root = "//dbs/{dbId}/udts/";

		public const string UserDefinedTypeId = "udtId";

		public const string UserDefinedType_Root = "//dbs/{dbId}/udts/{udtId}";

		public const string PermissionsPathSegment = "permissions";

		public const string Permissions_Root = "//dbs/{dbId}/users/{userid}/permissions/";

		public const string PermissionId = "permissionId";

		public const string Permission_Root = "//dbs/{dbId}/users/{userid}/permissions/{permissionId}";

		public const string CollectionsPathSegment = "colls";

		public const string Collections_Root = "//dbs/{dbId}/colls/";

		public const string CollectionId = "collId";

		public const string Collection_Root = "//dbs/{dbId}/colls/{collId}";

		public const string StoredProceduresPathSegment = "sprocs";

		public const string StoredProcedures_Root = "//dbs/{dbId}/colls/{collId}/sprocs/";

		public const string StoredProcedureId = "sprocId";

		public const string StoredProcedure_Root = "//dbs/{dbId}/colls/{collId}/sprocs/{sprocId}";

		public const string TriggersPathSegment = "triggers";

		public const string Triggers_Root = "//dbs/{dbId}/colls/{collId}/triggers/";

		public const string TriggerId = "triggerId";

		public const string Trigger_Root = "//dbs/{dbId}/colls/{collId}/triggers/{triggerId}";

		public const string UserDefinedFunctionsPathSegment = "udfs";

		public const string UserDefinedFunctions_Root = "//dbs/{dbId}/colls/{collId}/udfs/";

		public const string UserDefinedFunctionId = "udfId";

		public const string UserDefinedFunction_Root = "//dbs/{dbId}/colls/{collId}/udfs/{udfId}";

		public const string ConflictsPathSegment = "conflicts";

		public const string Conflicts_Root = "//dbs/{dbId}/colls/{collId}/conflicts/";

		public const string ConflictId = "conflictId";

		public const string Conflict_Root = "//dbs/{dbId}/colls/{collId}/conflicts/{conflictId}";

		public const string DocumentsPathSegment = "docs";

		public const string Documents_Root = "//dbs/{dbId}/colls/{collId}/docs/";

		public const string DocumentId = "docId";

		public const string Document_Root = "//dbs/{dbId}/colls/{collId}/docs/{docId}";

		public const string AttachmentsPathSegment = "attachments";

		public const string Attachments_Root = "//dbs/{dbId}/colls/{collId}/docs/{docId}/attachments/";

		public const string AttachmentId = "attachmentId";

		public const string Attachment_Root = "//dbs/{dbId}/colls/{collId}/docs/{docId}/attachments/{attachmentId}";

		public const string PartitionKeyRangesPathSegment = "pkranges";

		public const string PartitionKeyRanges_Root = "//dbs/{dbId}/colls/{collId}/pkranges/";

		public const string PartitionKeyRangeId = "pkrangeId";

		public const string PartitionKeyRange_Root = "//dbs/{dbId}/colls/{collId}/pkranges/{pkrangeId}";

		public const string PartitionKeyRangePreSplitSegment = "presplitaction";

		public const string PartitionKeyRangePreSplit_Root = "//dbs/{dbId}/colls/{collId}/pkranges/{pkrangeId}/presplitaction/";

		public const string PartitionKeyRangePostSplitSegment = "postsplitaction";

		public const string PartitionKeyRangePostSplit_Root = "//dbs/{dbId}/colls/{collId}/pkranges/{pkrangeId}/postsplitaction/";

		public const string ParatitionKeyRangeOperations_Split = "split";

		public const string PartitionsPathSegment = "partitions";

		public const string Partitions_Root = "//partitions/";

		public const string DatabaseAccountSegment = "databaseaccount";

		public const string DatabaseAccount_Root = "//databaseaccount/";

		public const string FilesPathSegment = "files";

		public const string Files_Root = "//files/";

		public const string FileId = "fileId";

		public const string File_Root = "//files/{fileId}";

		public const string MediaPathSegment = "media";

		public const string Medias_Root = "//media/";

		public const string MediaId = "mediaId";

		public const string Media_Root = "//media/{mediaId}";

		public const string AddressPathSegment = "addresses";

		public const string Address_Root = "//addresses/";

		public const string XPReplicatorAddressPathSegment = "xpreplicatoraddreses";

		public const string XPReplicatorAddress_Root = "//xpreplicatoraddreses/";

		public const string OffersPathSegment = "offers";

		public const string Offers_Root = "//offers/";

		public const string OfferId = "offerId";

		public const string Offer_Root = "//offers/{offerId}";

		public const string TopologyPathSegment = "topology";

		public const string Topology_Root = "//topology/";

		public const string SchemasPathSegment = "schemas";

		public const string Schemas_Root = "//dbs/{dbId}/colls/{collId}/schemas/";

		public const string SchemaId = "schemaId";

		public const string Schema_Root = "//dbs/{dbId}/colls/{collId}/schemas/{schemaId}";

		public const string ServiceReservationPathSegment = "serviceReservation";

		public const string ServiceReservation_Root = "//serviceReservation/";

		public const string DataExplorerSegment = "_explorer";

		public const string DataExplorerAuthTokenSegment = "authorization";

		public const string RidRangePathSegment = "ridranges";

		public const string RidRange_Root = "//ridranges/";

		public const string DataExplorer_Root = "//_explorer";

		public const string DataExplorerAuthToken_Root = "//_explorer/authorization";

		public const string DataExplorerAuthToken_WithoutResourceId = "//_explorer/authorization/{verb}/{resourceType}";

		public const string DataExplorerAuthToken_WithResourceId = "//_explorer/authorization/{verb}/{resourceType}/{resourceId}";

		internal const string ComputeGatewayChargePathSegment = "computegatewaycharge";

		public const string ControllerOperations_BatchGetOutput = "controllerbatchgetoutput";

		public const string ControllerOperations_BatchReportCharges = "controllerbatchreportcharges";

		public const string VectorClockPathSegment = "vectorclock";
	}
}
