namespace Microsoft.Azure.Documents
{
	internal static class Constants
	{
		public static class Quota
		{
			public const string Database = "databases";

			public const string Collection = "collections";

			public const string User = "users";

			public const string Permission = "permissions";

			public const string CollectionSize = "collectionSize";

			public const string DocumentsSize = "documentsSize";

			public const string DocumentsCount = "documentsCount";

			public const string StoredProcedure = "storedProcedures";

			public const string Trigger = "triggers";

			public const string UserDefinedFunction = "functions";

			public static char[] DelimiterChars = new char[2]
			{
				'=',
				';'
			};
		}

		public static class BlobStorageAttributes
		{
			public const string attachmentContainer = "x_ms_attachmentContainer";

			public const string backupContainer = "x_ms_backupContainer";

			public const string backupRetentionIntervalInHours = "x_ms_backupRetentionIntervalInHours";

			public const string backupIntervalInMinutes = "x_ms_backupIntervalInMinutes";

			public const string owningFederation = "x_ms_owningFederation";

			public const string owningDatabaseAccountName = "x_ms_databaseAccountName";

			public const string lastAttachmentCheckTime = "x_ms_lastAttachmentCheckTimeUtc";

			public const string orphanAttachmentProbationStartTime = "x_ms_orphanAttachmentProbationStartTimeUtc";

			public const string orphanBackupProbationStartTime = "x_ms_orphanBackupProbationStartTime";

			public const string oneBoxHostName = "x_ms_OneBoxHostName";

			public const string lastScanTime = "x_ms_lastScanTime";
		}

		public static class Offers
		{
			public const string OfferVersion_None = "";

			public const string OfferVersion_V1 = "V1";

			public const string OfferVersion_V2 = "V2";

			public const string OfferType_Invalid = "Invalid";
		}

		internal static class Indexing
		{
			public const int IndexingSchemeVersionV1 = 1;

			public const int IndexingSchemeVersionV2 = 2;
		}

		public static class PartitionedQueryExecutionInfo
		{
			public const int Version_1 = 1;

			public const int Version_2 = 2;

			public const int CurrentVersion = 2;
		}

		public static class Properties
		{
			public const string Resource = "resource";

			public const string Options = "options";

			public const string SubscriptionId = "subscriptionId";

			public const string SubscriptionUsageType = "subscriptionUsageType";

			public const string SubscriptionState = "subscriptionState";

			public const string MigratedSubscriptionId = "migratedSubscriptionId";

			public const string QuotaId = "quotaId";

			public const string OfferCategories = "offerCategories";

			public const string DocumentServiceName = "documentServiceName";

			public const string OperationId = "operationId";

			public const string AffinityGroupName = "affinityGroupName";

			public const string LocationName = "locationName";

			public const string InstanceSize = "instanceSize";

			public const string Status = "status";

			public const string RequestedStatus = "requestedStatus";

			public const string ExtendedStatus = "extendedStatus";

			public const string ExtendedResult = "extendedResult";

			public const string StatusCode = "statusCode";

			public const string DocumentEndpoint = "documentEndpoint";

			public const string TableEndpoint = "tableEndpoint";

			public const string TableSecondaryEndpoint = "tableSecondaryEndpoint";

			public const string GremlinEndpoint = "gremlinEndpoint";

			public const string CassandraEndpoint = "cassandraEndpoint";

			public const string EtcdEndpoint = "etcdEndpoint";

			public const string SqlEndpoint = "sqlEndpoint";

			public const string MongoEndpoint = "mongoEndpoint";

			public const string DatabaseAccountEndpoint = "databaseAccountEndpoint";

			public const string PrimaryMasterKey = "primaryMasterKey";

			public const string SecondaryMasterKey = "secondaryMasterKey";

			public const string PrimaryReadonlyMasterKey = "primaryReadonlyMasterKey";

			public const string SecondaryReadonlyMasterKey = "secondaryReadonlyMasterKey";

			public const string ConnectionStrings = "connectionStrings";

			public const string ConnectionString = "connectionString";

			public const string Description = "description";

			public const string ResourceKeySeed = "resourceKeySeed";

			public const string MaxStoredProceduresPerCollection = "maxStoredProceduresPerCollection";

			public const string MaxUDFsPerCollections = "maxUDFsPerCollections";

			public const string MaxTriggersPerCollection = "maxTriggersPerCollection";

			public const string IsolationLevel = "isolationLevel";

			public const string SubscriptionDisabled = "subscriptionDisabled";

			public const string IsDynamic = "isDynamic";

			public const string FederationName = "federationName";

			public const string FederationName1 = "federationName1";

			public const string FederationName2 = "federationName2";

			public const string FederationId = "federationId";

			public const string ComputeFederationId = "computeFederationId";

			public const string PlacementHint = "placementHint";

			public const string CreationTimestamp = "creationTimestamp";

			public const string ExtendedProperties = "extendedProperties";

			public const string KeyKind = "keyKind";

			public const string SystemKeyKind = "systemKeyKind";

			public const string ScaleUnits = "scaleUnits";

			public const string Location = "location";

			public const string Kind = "kind";

			public const string Region1 = "region1";

			public const string Region2 = "region2";

			public const string WritableLocations = "writableLocations";

			public const string ReadableLocations = "readableLocations";

			public const string Tags = "tags";

			public const string ResourceGroupName = "resourceGroupName";

			public const string PropertiesName = "properties";

			public const string ProvisioningState = "provisioningState";

			public const string CommunicationAPIKind = "communicationAPIKind";

			public const string UseMongoGlobalCacheAccountCursor = "useMongoGlobalCacheAccountCursor";

			public const string StorageSizeInMB = "storageSizeInMB";

			public const string DatabaseAccountOfferType = "databaseAccountOfferType";

			public const string Type = "type";

			public const string Error = "error";

			public const string State = "state";

			public const string RegistrationDate = "registrationDate";

			public const string Value = "value";

			public const string NextLink = "nextLink";

			public const string DestinationSubscription = "destinationSubscription";

			public const string TargetResourceGroup = "targetResourceGroup";

			public const string ConfigurationOverrides = "configurationOverrides";

			public const string Name = "name";

			public const string Fqdn = "fqdn";

			public const string Ipv4Address = "Ipv4Address";

			public const string RoleNameSuffix = "roleNameSuffix";

			public const string ReservedCname = "reservedCname";

			public const string ServiceType = "serviceType";

			public const string ServiceCount = "serviceCount";

			public const string TargetCapacityInMB = "targetCapacityInMB";

			public const string IsRuntimeServiceBindingEnabled = "isRuntimeServiceBindingEnabled";

			public const string MaxDocumentCollectionCount = "maxDocumentCollectionCount";

			public const string Reserved = "reserved";

			public const string Resources = "resources";

			public const string NamingConfiguration = "namingConfiguration";

			public const string DatabaseAccountName = "databaseAccountName";

			public const string Health = "health";

			public const string APIVersion = "APIVersion";

			public const string EmitArrayContainsForMongoQueries = "emitArrayContainsForMongoQueries";

			public const string Flights = "flights";

			public const string MigratedRegionalAccountName = "migratedRegionalAccountName";

			public const string DatabaseResourceId = "databaseResourceId";

			public const string SubscriptionKind = "subscriptionKind";

			public const string PlacementPolicies = "placementPolicies";

			public const string Action = "action";

			public const string Audience = "audience";

			public const string AudienceKind = "audienceKind";

			public const string OfferKind = "offerKind";

			public const string TenantId = "tenantId";

			public const string SupportedCapabilities = "supportedCapabilities";

			public const string CapabilityResource = "capabilityResource";

			public const string DocumentType = "documentType";

			public const string SystemDatabaseAccountStoreType = "systemDatabaseAccountStoreType";

			public const string FederationType = "federationType";

			public const string UseEPKandNameAsPrimaryKeyInDocumentTable = "useEPKandNameAsPrimaryKeyInDocumentTable";

			public const string EnableUserDefinedType = "enableUserDefinedType";

			public const string ExcludeOwnerIdFromDocumentTable = "excludeOwnerIdFromDocumentTable";

			public const string EnableQuerySupportForHybridRow = "enableQuerySupportForHybridRow";

			public const string FederationProxyFqdn = "federationProxyFqdn";

			public const string FederationProxyReservedCname = "federationProxyReservedCname";

			public const string EnableMultiMasterMigration = "enableMultiMasterMigration";

			public const string MasterValue = "masterValue";

			public const string SecondaryValue = "secondaryValue";

			public const string ArmLocation = "armLocation";

			public const string GlobalDatabaseAccountName = "globalDatabaseAccountName";

			public const string FailoverPriority = "failoverPriority";

			public const string FailoverPolicies = "failoverPolicies";

			public const string Locations = "locations";

			public const string WriteLocations = "writeLocations";

			public const string ReadLocations = "readLocations";

			public const string LocationType = "locationType";

			public const string MasterServiceUri = "masterServiceUri";

			public const string ConnectorStorageAccounts = "connectorStorageAccounts";

			public const string UserProvidedConnectorStorageAccountsInfo = "userProvidedConnectorStorageAccountsInfo";

			public const string LocalizedValue = "localizedValue";

			public const string Unit = "unit";

			public const string ResourceUri = "resourceUri";

			public const string PrimaryAggregationType = "primaryAggregationType";

			public const string MetricAvailabilities = "metricAvailabilities";

			public const string MetricValues = "metricValues";

			public const string TimeGrain = "timeGrain";

			public const string Retention = "retention";

			public const string TimeStamp = "timestamp";

			public const string Average = "average";

			public const string Minimum = "minimum";

			public const string Maximum = "maximum";

			public const string MetricCount = "count";

			public const string Total = "total";

			public const string StartTime = "startTime";

			public const string EndTime = "endTime";

			public const string DisplayName = "displayName";

			public const string Limit = "limit";

			public const string CurrentValue = "currentValue";

			public const string NextResetTime = "nextResetTime";

			public const string QuotaPeriod = "quotaPeriod";

			public const string SupportedRegions = "supportedRegions";

			public const string Percentiles = "percentiles";

			public const string SourceRegion = "sourceRegion";

			public const string TargetRegion = "targetRegion";

			public const string P10 = "P10";

			public const string P25 = "P25";

			public const string P50 = "P50";

			public const string P75 = "P75";

			public const string P90 = "P90";

			public const string P95 = "P95";

			public const string P99 = "P99";

			public const string Id = "id";

			public const string RId = "_rid";

			public const string SelfLink = "_self";

			public const string LastModified = "_ts";

			public const string Count = "_count";

			public const string ETag = "_etag";

			public const string TimeToLive = "ttl";

			public const string DefaultTimeToLive = "defaultTtl";

			public const string TimeToLivePropertyPath = "ttlPropertyPath";

			public const string DatabasesLink = "_dbs";

			public const string CollectionsLink = "_colls";

			public const string UsersLink = "_users";

			public const string PermissionsLink = "_permissions";

			public const string AttachmentsLink = "_attachments";

			public const string StoredProceduresLink = "_sprocs";

			public const string TriggersLink = "_triggers";

			public const string UserDefinedFunctionsLink = "_udfs";

			public const string ConflictsLink = "_conflicts";

			public const string DocumentsLink = "_docs";

			public const string ResourceLink = "resource";

			public const string MediaLink = "media";

			public const string SchemasLink = "_schemas";

			public const string PermissionMode = "permissionMode";

			public const string ResourceKey = "key";

			public const string Token = "_token";

			public const string FederationOperationKind = "federationOperationKind";

			public const string RollbackKind = "rollbackKind";

			public const string IndexingPolicy = "indexingPolicy";

			public const string Automatic = "automatic";

			public const string StringPrecision = "StringPrecision";

			public const string NumericPrecision = "NumericPrecision";

			public const string MaxPathDepth = "maxPathDepth";

			public const string IndexingMode = "indexingMode";

			public const string IndexType = "IndexType";

			public const string IndexKind = "kind";

			public const string DataType = "dataType";

			public const string Precision = "precision";

			public const string PartitionKind = "kind";

			public const string SystemKey = "systemKey";

			public const string Paths = "paths";

			public const string Path = "path";

			public const string FrequentPaths = "Frequent";

			public const string IncludedPaths = "includedPaths";

			public const string InFrequentPaths = "InFrequent";

			public const string ExcludedPaths = "excludedPaths";

			public const string Indexes = "indexes";

			public const string IndexingSchemeVersion = "IndexVersion";

			public const string CompositeIndexes = "compositeIndexes";

			public const string Order = "order";

			public const string SpatialIndexes = "spatialIndexes";

			public const string Types = "types";

			public const string EnableIndexingSchemeV2 = "enableIndexingSchemeV2";

			public const string GeospatialType = "geospatialType";

			public const string UniqueKeyPolicy = "uniqueKeyPolicy";

			public const string UniqueKeys = "uniqueKeys";

			public const string ChangeFeedPolicy = "changeFeedPolicy";

			public const string LogRetentionDuration = "retentionDuration";

			public const string SchemaPolicy = "schemaPolicy";

			public const string Quota = "quota";

			public const string ResourceType = "resourceType";

			public const string ServiceIndex = "serviceIndex";

			public const string PartitionIndex = "partitionIndex";

			public const string ModuleEvent = "moduleEvent";

			public const string ModuleEventReason = "moduleEventReason";

			public const string ModuleStatus = "moduleStatus";

			public const string ThrottleLevel = "throttleLevel";

			public const string ProcessId = "processId";

			public const string HasFaulted = "hasFaulted";

			public const string Result = "result";

			public const string ConsistencyPolicy = "consistencyPolicy";

			public const string DefaultConsistencyLevel = "defaultConsistencyLevel";

			public const string MaxStalenessPrefix = "maxStalenessPrefix";

			public const string MaxStalenessIntervalInSeconds = "maxIntervalInSeconds";

			public const string ReplicationPolicy = "replicationPolicy";

			public const string AsyncReplication = "asyncReplication";

			public const string MaxReplicaSetSize = "maxReplicasetSize";

			public const string MinReplicaSetSize = "minReplicaSetSize";

			public const string WritePolicy = "writePolicy";

			public const string PrimaryCheckpointInterval = "primaryLoggingIntervalInMilliSeconds";

			public const string SecondaryCheckpointInterval = "secondaryLoggingIntervalInMilliSeconds";

			public const string Collection = "RootResourceName";

			public const string CollectionId = "collectionId";

			public const string Completed = "completed";

			public const string BackupOfferType = "OfferType";

			public const string BackupDatabaseAccountName = "x_ms_databaseAccountName";

			public const string BackupContainerName = "backupContainerName";

			public const string BackupStoreUri = "fileUploaderUri";

			public const string BackupStoreAccountName = "fileUploaderAccountName";

			public const string BackupPolicy = "backupPolicy";

			public const string BackupIntervalInMinutes = "backupIntervalInMinutes";

			public const string BackupRetentionIntervalInHours = "backupRetentionIntervalInHours";

			public const string GetBackupIntervalInMinutes = "getBackupIntervalInMinutes";

			public const string GetBackupRetentionIntervalInHours = "getBackupRetentionIntervalInHours";

			public const string BackupStorageAccountsEnabled = "BackupStorageAccountsEnabled";

			public const string BackupStorageAccountNames = "BackupStorageAccountNames";

			public const string BackupStorageUris = "BackupStorageUris";

			public const string NumberOfDedicatedBackupStorageAccountsToAdd = "NumberOfDedicatedBackupStorageAccountsToAdd";

			public const string RestorePolicy = "restorePolicy";

			public const string SourceServiceName = "sourceServiceName";

			public const string RegionalDatabaseAccountInstanceId = "regionalDatabaseAccountInstanceId";

			public const string GlobalDatabaseAccountInstanceId = "globalDatabaseAccountInstanceId";

			public const string SourceServiceLocation = "sourceServiceLocation";

			public const string ReuseSourceDatabaseAccountAccessKeys = "reuseSourceDatabaseAccountAccessKeys";

			public const string RecreateDatabase = "recreateDatabase";

			public const string LatestBackupSnapshotInDateTime = "latestBackupSnapshotInDateTime";

			public const string TargetFederation = "targetFederation";

			public const string CollectionOrDatabaseResourceIds = "collectionOrDatabaseResourceIds";

			public const string SourceHasMultipleStorageAccounts = "sourceHasMultipleStorageAccounts";

			public const string AllowPartialRestore = "allowPartialRestore";

			public const string RestoredSourceDatabaseAccountName = "restoredSourceDatabaseAccountName";

			public const string RestoredAtTimestamp = "restoredAtTimestamp";

			public const string PrimaryReadCoefficient = "primaryReadCoefficient";

			public const string SecondaryReadCoefficient = "secondaryReadCoefficient";

			public const string Body = "body";

			public const string TriggerType = "triggerType";

			public const string TriggerOperation = "triggerOperation";

			public const string MaxSize = "maxSize";

			public const string CurrentUsage = "currentUsage";

			public const string Content = "content";

			public const string ContentType = "contentType";

			public const string Code = "code";

			public const string Message = "message";

			public const string ErrorDetails = "errorDetails";

			public const string AdditionalErrorInfo = "additionalErrorInfo";

			public const string IsPrimary = "isPrimary";

			public const string Protocol = "protocol";

			public const string LogicalUri = "logicalUri";

			public const string PhysicalUri = "physcialUri";

			public const string AuthorizationFormat = "type={0}&ver={1}&sig={2}";

			public const string MasterToken = "master";

			public const string ResourceToken = "resource";

			public const string TokenVersion = "1.0";

			public const string AuthSchemaType = "type";

			public const string AuthVersion = "ver";

			public const string AuthSignature = "sig";

			public const string readPermissionMode = "read";

			public const string allPermissionMode = "all";

			public const string PackageVersion = "packageVersion";

			public const string FabricApplicationVersion = "fabricApplicationVersion";

			public const string FabricRingCodeVersion = "fabricRingCodeVersion";

			public const string FabricRingConfigVersion = "fabricRingConfigVersion";

			public const string CertificateVersion = "certificateVersion";

			public const string DeploymentId = "deploymentId";

			public const string FederationKind = "federationKind";

			public const string SupportedPlacementHints = "supportedPlacementHints";

			public const string PrimarySystemKeyReadOnly = "primarySystemKeyReadOnly";

			public const string SecondarySystemKeyReadOnly = "secondarySystemKeyReadOnly";

			public const string UsePrimarySystemKeyReadOnly = "UsePrimarySystemKeyReadOnly";

			public const string PrimarySystemKeyReadWrite = "primarySystemKeyReadWrite";

			public const string SecondarySystemKeyReadWrite = "secondarySystemKeyReadWrite";

			public const string UsePrimarySystemKeyReadWrite = "UsePrimarySystemKeyReadWrite";

			public const string PrimarySystemKeyAll = "primarySystemKeyAll";

			public const string SecondarySystemKeyAll = "secondarySystemKeyAll";

			public const string UsePrimarySystemKeyAll = "UsePrimarySystemKeyAll";

			public const string UseSecondaryComputeGatewayKey = "UseSecondaryComputeGatewayKey";

			public const string Weight = "Weight";

			public const string BatchId = "BatchId";

			public const string IsCappedForServerPartitionAllocation = "isCappedForServerPartitionAllocation";

			public const string IsDirty = "IsDirty";

			public const string IsAvailabilityZoneFederation = "IsAvailabilityZoneFederation";

			public const string AZIndex = "AZIndex";

			public const string IsFederationUnavailable = "isFederationUnavailable";

			public const string ServiceExtensions = "serviceExtensions";

			public const string HostedServicesDescription = "hostedServicesDescription";

			public const string FederationProxyResource = "federationProxyResource";

			public const string Version = "Version";

			public const string ManagementEndPoint = "ManagementEndPoint";

			public const string StorageServiceName = "name";

			public const string StorageServiceLocation = "location";

			public const string BlobEndpoint = "blobEndpoint";

			public const string StorageServiceSubscriptionId = "subscriptionId";

			public const string StorageServiceIndex = "storageIndex";

			public const string IsWritable = "isWritable";

			public const string StorageServiceKind = "storageServiceKind";

			public const string StorageAccountType = "storageAccountType";

			public const string StorageServiceResourceGroupName = "resourceGroupName";

			public const string StorageServiceFederationId = "federationId";

			public const string StorageAccountSku = "storageAccountSku";

			public const string ConnectorOffer = "connectorOffer";

			public const string EnableCassandraConnector = "enableCassandraConnector";

			public const string ConnectorMetadataAccountInfo = "connectorMetadataAccountInfo";

			public const string ConnectorMetadataAccountPrimaryKey = "connectorMetadataAccountPrimaryKey";

			public const string ConnectorMetadataAccountSecondaryKey = "connectorMetadataAccountSecondaryKey";

			public const string StorageServiceResource = "storageServiceResource";

			public const string OfferName = "offerName";

			public const string MaxNumberOfSupportedNodes = "maxNumberOfSupportedNodes";

			public const string MaxAllocationsPerStorageAccount = "maxAllocationsPerStorageAccount";

			public const string StorageAccountCount = "storageAccountCount";

			public const string ResourceId = "resourceId";

			public const string AddressesLink = "addresses";

			public const string UserReplicationPolicy = "userReplicationPolicy";

			public const string UserConsistencyPolicy = "userConsistencyPolicy";

			public const string SystemReplicationPolicy = "systemReplicationPolicy";

			public const string ReadPolicy = "readPolicy";

			public const string QueryEngineConfiguration = "queryEngineConfiguration";

			public const string EnableMultipleWriteLocations = "enableMultipleWriteLocations";

			public const string CanEnableMultipleWriteLocations = "canEnableMultipleWriteLocations";

			public const string EnableSnapshotAcrossDocumentStoreAndIndex = "enableSnapshotAcrossDocumentStoreAndIndex";

			public const string IsZoneRedundant = "isZoneRedundant";

			public const string EnableThroughputAutoScale = "enableThroughputAutoScale";

			public const string LSN = "_lsn";

			public const string LLSN = "llsn";

			public const string SourceResourceId = "resourceId";

			public const string ResourceTypeDocument = "document";

			public const string ResourceTypeStoredProcedure = "storedProcedure";

			public const string ResourceTypeTrigger = "trigger";

			public const string ResourceTypeUserDefinedFunction = "userDefinedFunction";

			public const string ResourceTypeAttachment = "attachment";

			public const string ConflictLSN = "conflict_lsn";

			public const string OperationKindCreate = "create";

			public const string OperationKindPatch = "patch";

			public const string OperationKindReplace = "replace";

			public const string OperationKindDelete = "delete";

			public const string Conflict = "conflict";

			public const string OperationType = "operationType";

			public const string TombstoneResourceType = "resourceType";

			public const string OwnerId = "ownerId";

			public const string CollectionResourceId = "collectionResourceId";

			public const string PartitionKeyRangeResourceId = "partitionKeyRangeResourceId";

			public const string WellKnownServiceUrl = "wellKnownServiceUrl";

			public const string LinkRelationType = "linkRelationType";

			public const string Region = "region";

			public const string GeoLinkIdentifier = "geoLinkIdentifier";

			public const string DatalossNumber = "datalossNumber";

			public const string ConfigurationNumber = "configurationNumber";

			public const string PartitionId = "partitionId";

			public const string SchemaVersion = "schemaVersion";

			public const string ReplicaId = "replicaId";

			public const string ReplicaRole = "replicaRole";

			public const string ReplicatorAddress = "replicatorAddress";

			public const string ReplicaStatus = "replicaStatus";

			public const string IsAvailableForWrites = "isAvailableForWrites";

			public const string OfferType = "offerType";

			public const string OfferResourceId = "offerResourceId";

			public const string OfferThroughput = "offerThroughput";

			public const string OfferIsRUPerMinuteThroughputEnabled = "offerIsRUPerMinuteThroughputEnabled";

			public const string OfferIsAutoScaleEnabled = "offerIsAutoScaleEnabled";

			public const string OfferVersion = "offerVersion";

			public const string OfferContent = "content";

			public const string CollectionThroughputInfo = "collectionThroughputInfo";

			public const string MinimumRuForCollection = "minimumRUForCollection";

			public const string NumPhysicalPartitions = "numPhysicalPartitions";

			public const string UserSpecifiedThroughput = "userSpecifiedThroughput";

			public const string OfferMinimumThroughputParameters = "offerMinimumThroughputParameters";

			public const string MaxConsumedStorageEverInKB = "maxConsumedStorageEverInKB";

			public const string MaxThroughputEverProvisioned = "maxThroughputEverProvisioned";

			public const string MaxCountOfSharedThroughputCollectionsEverCreated = "maxCountOfSharedThroughputCollectionsEverCreated";

			public const string OfferLastReplaceTimestamp = "offerLastReplaceTimestamp";

			public const string AutoScaleSettings = "offerAutoScaleSettings";

			public const string AutoScaleMode = "mode";

			public const string AutoScaleProvisionedThroughput = "provisionedThroughput";

			public const string AutoScaleFactor = "scaleFactor";

			public const string EnforceRUPerGB = "enforceRUPerGB";

			public const string CurrentProgress = "currentProgress";

			public const string CatchupCapability = "catchupCapability";

			public const string PublicAddress = "publicAddress";

			public const string OperationName = "name";

			public const string OperationProperties = "properties";

			public const string OperationNextLink = "nextLink";

			public const string OperationDisplay = "display";

			public const string OperationDisplayProvider = "provider";

			public const string OperationDisplayResource = "resource";

			public const string OperationDisplayOperation = "operation";

			public const string OperationDisplayDescription = "description";

			public const string PartitionKey = "partitionKey";

			public const string PartitionKeyRangeId = "partitionKeyRangeId";

			public const string MinInclusiveEffectivePartitionKey = "minInclusiveEffectivePartitionKey";

			public const string MaxExclusiveEffectivePartitionKey = "maxExclusiveEffectivePartitionKey";

			public const string MinInclusive = "minInclusive";

			public const string MaxExclusive = "maxExclusive";

			public const string RidPrefix = "ridPrefix";

			public const string ThroughputFraction = "throughputFraction";

			public const string PartitionKeyRangeStatus = "status";

			public const string Parents = "parents";

			public const string NodeStatus = "NodeStatus";

			public const string NodeName = "NodeName";

			public const string HealthState = "HealthState";

			public const string EnablePartitionKeyMonitor = "enablePartitionKeyMonitor";

			public const string Origin = "origin";

			public const string AzureMonitorServiceSpecification = "serviceSpecification";

			public const string DiagnosticLogSpecifications = "logSpecifications";

			public const string DiagnosticLogsName = "name";

			public const string DiagnosticLogsDisplayName = "displayName";

			public const string BlobDuration = "blobDuration";

			public const string FederationPolicyOverride = "federationPolicyOverride";

			public const string MaxCapacityUnits = "maxCapacityUnits";

			public const string MaxDatabaseAccounts = "maxDatabaseAccounts";

			public const string MaxBindableServicesPercentOfFreeSpace = "maxBindableServicesPercentOfFreeSpace";

			public const string DisabledDatabaseAccountManager = "disabledDatabaseAccountManager";

			public const string EnableBsonSchemaOnNewAccounts = "enableBsonSchemaOnNewAccounts";

			public const string MetricSpecifications = "metricSpecifications";

			public const string DisplayDescription = "displayDescription";

			public const string AggregationType = "aggregationType";

			public const string LockAggregationType = "lockAggregationType";

			public const string SourceMdmAccount = "sourceMdmAccount";

			public const string SourceMdmNamespace = "sourceMdmNamespace";

			public const string FillGapWithZero = "fillGapWithZero";

			public const string Category = "category";

			public const string ResourceIdOverride = "resourceIdDimensionNameOverride";

			public const string Dimensions = "dimensions";

			public const string InternalName = "internalName";

			public const string IsHidden = "isHidden";

			public const string DefaultDimensionValues = "defaultDimensionValues";

			public const string Availabilities = "availabilities";

			public const string InternalMetricName = "internalMetricName";

			public const string SupportedTimeGrainTypes = "supportedTimeGrainTypes";

			public const string SupportedAggregationTypes = "supportedAggregationTypes";

			public const string IpRangeFilter = "ipRangeFilter";

			public const string EnableAutomaticFailover = "enableAutomaticFailover";

			public const string SkipGracefulFailoverAttempt = "skipGracefulFailoverAttempt";

			public const string IsEnabled = "isenabled";

			public const string FabricUri = "fabricUri";

			public const string ResourcePartitionKey = "resourcePartitionKey";

			public const string Topology = "topology";

			public const string AdjacencyList = "adjacencyList";

			public const string WriteRegion = "writeRegion";

			public const string GlobalConfigurationNumber = "globalConfigurationNumber";

			public const string WriteStatusRevokedSatelliteRegions = "writeStatusRevokedSatelliteRegions";

			public const string PreviousWriteRegion = "previousWriteRegion";

			public const string NextWriteRegion = "nextWriteRegion";

			public const string Capabilities = "capabilities";

			public const string DefaultCapabilities = "defaultCapabilities";

			public const string CapabilityVisibilityPolicies = "capabilityVisibilityPolicies";

			public const string NamingServiceSettings = "namingServiceSettings";

			public const string IsEnabledForAll = "isEnabledForAll";

			public const string IsDefault = "isDefault";

			public const string Key = "key";

			public const string IsCapabilityRingFenced = "isCapabilityRingFenced";

			public const string IsEnabledForAllToOptIn = "isEnabledForAllToOptIn";

			public const string ConfigValueByKey = "configValueByKey";

			public const string NameBasedCollectionUri = "nameBasedCollectionUri";

			public const string UsePolicyStore = "usePolicyStore";

			public const string UsePolicyStoreRuntime = "usePolicyStoreRuntime";

			public const string AccountEndpoint = "AccountEndpoint";

			public const string EncryptedAccountKey = "EncryptedAccountKey";

			public const string PolicyStoreConnectionInfoNameBasedCollectionUri = "NameBasedCollectionUri";

			public const string BillingStoreConnectionInfoAccountEndpoint = "AccountEndpoint";

			public const string BillingStoreConnectionInfoEncryptedAccountKey = "EncryptedAccountKey";

			public const string UseConfigurationStoreForReads = "useConfigurationStoreForReads";

			public const string UseConfigurationStoreForWrites = "useConfigurationStoreForWrites";

			public const string UseNamingServiceForWrites = "useNamingServiceForWrites";

			public const string ConfigurationStoreConnectionInfoAccountEndpoint = "AccountEndpoint";

			public const string ConfigurationStoreConnectionInfoEncryptedAccountKey = "EncryptedAccountKey";

			public const string ConfigurationStoreConnectionInfoNameBasedCollectionUri = "NameBasedCollectionUri";

			public const string ConfigurationLevel = "configurationLevel";

			public const string SubscriptionTenantId = "tenantId";

			public const string SubscriptionLocationPlacementId = "locationPlacementId";

			public const string SubscriptionQuotaId = "quotaId";

			public const string Schema = "schema";

			public const string PartitionedQueryExecutionInfoVersion = "partitionedQueryExecutionInfoVersion";

			public const string QueryInfo = "queryInfo";

			public const string QueryRanges = "queryRanges";

			public const string DatabaseAccounts = "databaseAccounts";

			public const string SchemaDiscoveryPolicy = "schemaDiscoveryPolicy";

			public const string SchemaBuilderMode = "mode";

			public const string EnableBsonSchema = "enableBsonSchema";

			public const string EnableV2Billing = "enableV2Billing";

			public const string Statistics = "statistics";

			public const string SizeInKB = "sizeInKB";

			public const string DocumentCount = "documentCount";

			public const string PartitionKeys = "partitionKeys";

			public const string AccountStorageQuotaInGB = "databaseAccountStorageQuotaInGB";

			public const string Percentage = "percentage";

			public const string PartitionKeyDefinitionVersion = "version";

			public const string EnforcePlacementHintConstraintOnPartitionAllocation = "enforcePlacementHintConstraintOnPartitionAllocation";

			public const string CanUsePolicyStore = "canUsePolicyStore";

			public const string MaxDatabaseAccountCount = "maxDatabaseAccountCount";

			public const string MaxRegionsPerGlobalDatabaseAccount = "maxRegionsPerGlobalDatabaseAccount";

			public const string DisableManualFailoverThrottling = "disableManualFailoverThrottling";

			public const string DisableRemoveRegionThrottling = "disableRemoveRegionThrottling";

			public const string DisableDisableMultipleWriteLocationsThrottling = "disableDisableMultipleWriteLocationsThrottling";

			public const string DisableDisableMultipleWriteLocationsDelay = "disableDisableMultipleWriteLocationsDelay";

			public const string DisableMultipleWriteLocationsDelayInSeconds = "disableMultipleWriteLocationsDelayInSeconds";

			public const string DefaultSubscriptionPolicySet = "defaultSubscriptionPolicySet";

			public const string SubscriptionPolicySetByLocation = "subscriptionPolicySetByLocation";

			public const string PlacementPolicy = "placementPolicy";

			public const string SubscriptionPolicy = "subscriptionPolicy";

			public const string LocationPolicySettings = "locationPolicySettings";

			public const string DefaultLocationPolicySet = "defaultLocationPolicySet";

			public const string LocationPolicySetBySubscriptionKind = "locationPolicySetBySubscriptionKind";

			public const string SubscriptionPolicySettings = "subscriptionPolicySettings";

			public const string CapabilityPolicySettings = "capabilityPolicySettings";

			public const string LocationVisibilityPolicy = "locationVisibilityPolicy";

			public const string IsVisible = "isVisible";

			public const string IsVirtualNetworkFilterEnabled = "isVirtualNetworkFilterEnabled";

			public const string AccountVNETFilterEnabled = "accountVNETFilterEnabled";

			public const string VirtualNetworkArmUrl = "virtualNetworkArmUrl";

			public const string VirtualNetworkResourceEntries = "virtualNetworkResourceEntries";

			public const string VirtualNetworkResourceIds = "virtualNetworkResourceIds";

			public const string VNetDatabaseAccountEntries = "vNetDatabaseAccountEntry";

			public const string VirtualNetworkTrafficTags = "vnetFilter";

			public const string VNetETag = "etag";

			public const string PrivateEndpointProxyETag = "etag";

			public const string VirtualNetworkRules = "virtualNetworkRules";

			public const string EnabledApiTypes = "EnabledApiTypes";

			public const string VirtualNetworkPrivateIpConfig = "vnetPrivateIps";

			public const string IgnoreMissingVNetServiceEndpoint = "ignoreMissingVNetServiceEndpoint";

			public const string SubnetTrafficTag = "subnetTrafficTag";

			public const string Owner = "owner";

			public const string VNetTrafficTag = "vNetTrafficTag";

			public const string VirtualNetworkAcled = "virtualNetworkAcled";

			public const string VirtualNetworkResourceGuid = "virtualNetworkResourceGuid";

			public const string VirtualNetworkLocation = "virtualNetworkLocation";

			public const string PrimaryLocations = "primaryLocations";

			public const string AcledSubscriptions = "acledSubscriptions";

			public const string AcledSubnets = "acledSubnets";

			public const string RetryAfter = "retryAfter";

			public const string OperationPollingUri = "operationPollingUri";

			public const string EnableFederationDecommission = "enableFederationDecommission2";

			public const string AutoMigrationScheduleIntervalInSeconds = "autoMigrationScheduleIntervalInSeconds2";

			public const string MasterPartitionAutoMigrationProbability = "masterPartitionAutoMigrationProbability2";

			public const string ServerPartitionAutoMigrationProbability = "serverPartitionAutoMigrationProbability2";

			public const string StorageAccountId = "storageAccountId";

			public const string WorkspaceId = "workspaceId";

			public const string EventHubAuthorizationRuleId = "eventHubAuthorizationRuleId";

			public const string Enabled = "enabled";

			public const string Days = "days";

			public const string RetentionPolicy = "retentionPolicy";

			public const string Metrics = "metrics";

			public const string Logs = "logs";

			public const string IsAtpEnabled = "isEnabled";

			public const string EnableExtendedResourceLimit = "enableExtendedResourceLimit";

			public const string ExtendedResourceNameLimitInBytes = "extendedResourceNameLimitInBytes";

			public const string ExtendedResourceContentLimitInKB = "extendedResourceContentLimitInKB";

			public const string MaxResourceSize = "maxResourceSize";

			public const string MaxFeasibleRequestChargeInSeconds = "MaxFeasibleRequestChargeInSeconds";

			public const string ReplicationChargeEnabled = "replicationChargeEnabled";

			public const string BypassMultiRegionStrongVersionCheck = "bypassMultiRegionStrongVersionCheck";

			public const string XPCatchupConfigurationEnabled = "xpCatchupConfigurationEnabled";

			public const string IsRemoteStoreRestoreEnabled = "remoteStoreRestoreEnabled";

			public const string RetentionPeriodInSeconds = "retentionPeriodInSeconds";

			public const string BlobNamePrefix = "blobNamePrefix";

			public const string BlobPartitionLevel = "blobPartitionLevel";

			public const string DryRun = "dryRun";

			public const string UseCustomRetentionPeriod = "UseCustomRetentionPeriod";

			public const string CustomRetentionPeriodInMins = "CustomRetentionPeriodInMins";

			public const string DisallowOfferOnSharedThroughputCollection = "disallowOfferOnSharedThroughputCollection";

			public const string AllowOnlyPartitionedCollectionsForSharedOffer = "allowOnlyPartitionedCollectionsForSharedThroughputOffer";

			public const string ConflictResolutionPolicy = "conflictResolutionPolicy";

			public const string Mode = "mode";

			public const string ConflictResolutionPath = "conflictResolutionPath";

			public const string ConflictResolutionProcedure = "conflictResolutionProcedure";

			public const string Progress = "progress";

			public const string ReservedServicesInfo = "reservedServicesInfo";

			public const string RoleInstanceCounts = "RoleInstanceCounts";

			public const string RoleName = "RoleName";

			public const string InstanceCount = "InstanceCount";

			public const string ExtensionName = "ExtensionName";

			public const string ExtensionVersion = "ExtensionVersion";

			public const string Cors = "cors";

			public const string AllowedOrigins = "allowedOrigins";

			public const string AllowedMethods = "allowedMethods";

			public const string AllowedHeaders = "allowedHeaders";

			public const string ExposedHeaders = "exposedHeaders";

			public const string MaxAgeInSeconds = "maxAgeInSeconds";

			public const string PrimaryClientCertificatePemBytes = "primaryClientCertificatePemBytes";

			public const string SecondaryClientCertificatePemBytes = "secondaryClientCertificatePemBytes";

			public const string EnableLsnInDocumentContent = "enableLsnInDocumentContent";

			public const string MaxContentPerCollectionInGB = "maxContentPerCollection";

			public const string SkipRepublishConfigFromTargetFederation = "skipRepublishConfigFromTargetFederation";

			public const string MaxDegreeOfParallelismForPublishingFederationKeys = "maxDegreeOfParallelismForPublishingFederationKeys";

			public const string ProxyName = "proxyName";

			public const string GroupId = "groupId";

			public const string ConnectionDetails = "connectionDetails";

			public const string GroupIds = "groupIds";

			public const string InternalFqdn = "internalFqdn";

			public const string CustomerVisibleFqdns = "customerVisibleFqdns";

			public const string RequiredMembers = "requiredMembers";

			public const string ManualPrivateLinkServiceConnections = "manualPrivateLinkServiceConnections";

			public const string PrivateLinkServiceConnections = "privateLinkServiceConnections";

			public const string PrivateLinkServiceProxies = "privateLinkServiceProxies";

			public const string RemotePrivateEndpoint = "remotePrivateEndpoint";

			public const string MemberName = "memberName";

			public const string GroupConnectivityInformation = "groupConnectivityInformation";

			public const string RemotePrivateLinkServiceConnectionState = "remotePrivateLinkServiceConnectionState";

			public const string RemotePrivateEndpointConnection = "remotePrivateEndpointConnection";

			public const string PrivateEndpointConnectionId = "privateEndpointConnectionId";

			public const string ActionsRequired = "actionsRequired";

			public const string RequestMessage = "requestMessage";

			public const string LinkIdentifier = "linkIdentifier";

			public const string PrivateIpAddress = "privateIpAddress";

			public const string PrivateIpConfigGroups = "privateIpConfigGroups";

			public const string PrivateLinkServiceConnectionName = "privateLinkServiceConnectionName";

			public const string RedirectMapId = "redirectMapId";

			public const string AccountPrivateEndpointConnectionEnabled = "accountPrivateEndpointConnectionEnabled";

			public const string PrivateIpConfigs = "privateIpConfigs";

			public const string PrivateEndpoint = "privateEndpoint";

			public const string PrivateLinkServiceConnectionState = "privateLinkServiceConnectionState";

			public const string PrivateEndpointArmUrl = "privateEndpointArmUrl";

			public const string PrivateLinkServiceProxyName = "privateLinkServiceProxyName";

			public const string PrivateEndpointConnections = "privateEndpointConnections";

			public const string ArmSubscriptionId = "armSubscriptionId";
		}

		public static class DocumentResourceExtendedProperties
		{
			public const string ResourceGroupName = "ResourceGroupName";

			public const string Tags = "Tags";
		}

		public static class RestoreMetadataResourceProperties
		{
			public const string RId = "_rid";

			public const string LSN = "lsn";

			public const string CollectionDeletionTimestamp = "_ts";

			public const string DatabaseName = "databaseName";

			public const string CollectionName = "collectionName";

			public const string CollectionResourceId = "collectionResourceId";

			public const string PartitionKeyRangeContent = "partitionKeyRangeContent";

			public const string CollectionContent = "collectionContent";

			public const string OfferContent = "offerContent";

			public const string CollectionSecurityIdentifier = "collectionSecurityIdentifier";

			public const string CollectionCreationTimestamp = "creationTimestamp";

			public const string RemoteStoreType = "remoteStorageType";
		}

		public static class CollectionRestoreParamsProperties
		{
			public const string Version = "Version";

			public const string SourcePartitionKeyRangeId = "SourcePartitionKeyRangeId";

			public const string SourcePartitionKeyRangeRid = "SourcePartitionKeyRangeRid";

			public const string SourceSecurityIdentifier = "SourceSecurityId";

			public const string RestorePointInTime = "RestorePointInTime";

			public const string PartitionCount = "PartitionCount";

			public const string RestoreState = "RestoreState";
		}

		public static class InternalIndexingProperties
		{
			public const string PropertyName = "internalIndexingProperties";

			public const string LogicalIndexVersion = "logicalIndexVersion";

			public const string IndexEncodingOptions = "indexEncodingOptions";

			public const string EnableIndexingFullFidelity = "enableIndexingFullFidelity";
		}

		public static class TypeSystemPolicy
		{
			public const string PropertyName = "typeSystemPolicy";

			public const string TypeSystem = "typeSystem";

			public const string CosmosCore = "CosmosCore";

			public const string Cql = "Cql";

			public const string Bson = "Bson";
		}

		public static class UpgradeTypes
		{
			public const string ClusterManifest = "Fabric";

			public const string Package = "Package";

			public const string Application = "App";
		}

		public static class StorageKeyManagementProperties
		{
			public const string StorageAccountSubscriptionId = "storageAccountSubscriptionId";

			public const string StorageAccountResourceGroup = "storageAccountResourceGroup";

			public const string StorageAccountName = "storageAccountName";

			public const string StorageAccountUri = "storageAccountUri";

			public const string StorageAccountPrimaryKeyInUse = "storageAccountPrimaryKeyInUse";

			public const string StorageAccountSecondaryKeyInUse = "storageAccountSecondaryKeyInUse";

			public const string StorageAccountPrimaryKey = "storageAccountPrimaryKey";

			public const string StorageAccountSecondaryKey = "storageAccountSecondaryKey";

			public const string StorageAccountType = "storageAccountType";

			public const string ForceRefresh = "forceRefresh";

			public const string Key1Name = "key1";

			public const string Key2Name = "key2";

			public const string EnableStorageAccountKeyFetch = "enableStorageAccountKeyFetch";

			public const string CachedStorageAccountKeyRefreshIntervalInHours = "cachedStorageAccountKeyRefreshIntervalInHours";

			public const string StorageKeyManagementClientRequestTimeoutInSeconds = "storageKeyManagementClientRequestTimeoutInSeconds";

			public const string StorageKeyManagementAADAuthRetryIntervalInSeconds = "storageKeyManagementAADAuthRetryIntervalInSeconds";

			public const string StorageKeyManagementAADAuthRetryCount = "storageKeyManagementAADAuthRetryCount";

			public const string BackupRestoreAADCertSubjectName = "backupRestoreAADCertSubjectName";

			public const string BackupRestoreAADEndpoint = "backupRestoreAADEndpoint";

			public const string BackupRestoreAADClientId = "backupRestoreAADClientId";

			public const string BackupRestoreAADTenantId = "backupRestoreAADTenantId";

			public const string BackupRestoreARMEndpoint = "backupRestoreARMEndpoint";
		}

		public static class SystemSubscriptionUsageType
		{
			public const string CassandraConnectorStorage = "cassandraconnectorstorage";
		}

		public static class ConnectorOfferKind
		{
			public const string Cassandra = "cassandra";
		}

		public static class ConnectorOfferName
		{
			public const string Small = "small";
		}

		public const char DBSeparator = ';';

		public const char ModeSeparator = ':';

		public const char StartArray = '[';

		public const char EndArray = ']';

		public const char PartitionSeparator = ',';

		public const string DataContractNamespace = "http://schemas.microsoft.com/windowsazure";

		public const int MaxResourceSizeInBytes = 2097151;

		public const int MaxDirectModeBatchRequestBodySizeInBytes = 2202010;

		public const int MaxOperationsInDirectModeBatchRequest = 100;

		public const int MaxGatewayModeBatchRequestBodySizeInBytes = 16777216;

		public const int MaxOperationsInGatewayModeBatchRequest = 1000;

		public const string TableSecondaryEndpointSuffix = "-secondary";
	}
}
