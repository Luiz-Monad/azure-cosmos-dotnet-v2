namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class PartitionKeyAndResourceTokenPair
	{
		public PartitionKeyInternal PartitionKey
		{
			get;
			private set;
		}

		public string ResourceToken
		{
			get;
			private set;
		}

		public PartitionKeyAndResourceTokenPair(PartitionKeyInternal partitionKey, string resourceToken)
		{
			PartitionKey = partitionKey;
			ResourceToken = resourceToken;
		}
	}
}
