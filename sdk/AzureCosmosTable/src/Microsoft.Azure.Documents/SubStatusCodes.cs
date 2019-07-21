namespace Microsoft.Azure.Documents
{
	internal enum SubStatusCodes
	{
		Unknown = 0,
		PartitionKeyMismatch = 1001,
		CrossPartitionQueryNotServable = 1004,
		ScriptCompileError = 0xFFFF,
		AnotherOfferReplaceOperationIsInProgress = 3205,
		NameCacheIsStale = 1000,
		PartitionKeyRangeGone = 1002,
		CompletingSplit = 1007,
		CompletingPartitionMigration = 1008,
		WriteForbidden = 3,
		ProvisionLimitReached = 1005,
		DatabaseAccountNotFound = 1008,
		RedundantCollectionPut = 1009,
		SharedThroughputDatabaseQuotaExceeded = 1010,
		SharedThroughputOfferGrowNotNeeded = 1011,
		ReadSessionNotAvailable = 1002,
		OwnerResourceNotFound = 1003,
		ConfigurationNameNotFound = 1004,
		ConfigurationPropertyNotFound = 1005,
		ConflictWithControlPlane = 1006,
		DatabaseNameAlreadyExists = 3206,
		ConfigurationNameAlreadyExists = 3207,
		InsufficientBindablePartitions = 1007,
		ComputeFederationNotFound = 1012,
		SplitIsDisabled = 2001,
		CollectionsInPartitionGotUpdated = 2002,
		CanNotAcquirePKRangesLock = 2003,
		ResourceNotFound = 2004,
		CanNotAcquireOfferOwnerLock = 2005,
		MigrationIsDisabled = 2006,
		ConfigurationNameNotEmpty = 3001,
		ClientTcpChannelFull = 3208
	}
}
