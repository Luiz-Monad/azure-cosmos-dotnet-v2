namespace Microsoft.Azure.Documents
{
	internal enum ResourceType
	{
		Unknown = -1,
		Database = 0,
		Collection = 1,
		Document = 2,
		Attachment = 3,
		User = 4,
		Permission = 5,
		Progress = 6,
		Replica = 7,
		Tombstone = 8,
		Module = 9,
		SmallMaxInvalid = 10,
		LargeInvalid = 100,
		ModuleCommand = 103,
		Index = 104,
		IndexBookmark = 105,
		IndexSize = 106,
		Conflict = 107,
		Record = 108,
		StoredProcedure = 109,
		Trigger = 110,
		UserDefinedFunction = 111,
		BatchApply = 112,
		Offer = 113,
		PartitionSetInformation = 114,
		XPReplicatorAddress = 115,
		Timestamp = 117,
		DatabaseAccount = 118,
		MasterPartition = 120,
		ServerPartition = 121,
		Topology = 122,
		SchemaContainer = 123,
		Schema = 124,
		PartitionKeyRange = 125,
		LogStoreLogs = 126,
		RestoreMetadata = 0x7F,
		PreviousImage = 0x80,
		VectorClock = 129,
		RidRange = 130,
		ComputeGatewayCharges = 131,
		UserDefinedType = 133,
		Batch = 135,
		Key = -2,
		Media = -3,
		ServiceFabricService = -4,
		Address = -5,
		ControllerService = -6
	}
}
