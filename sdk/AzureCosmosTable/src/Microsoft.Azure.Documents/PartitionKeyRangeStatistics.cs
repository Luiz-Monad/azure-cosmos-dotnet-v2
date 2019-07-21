using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents statistics for a partition key range in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// For usage, please refer to the example in <see cref="P:Microsoft.Azure.Documents.DocumentCollection.PartitionKeyRangeStatistics" />.
	/// </remarks>
	public sealed class PartitionKeyRangeStatistics
	{
		/// <summary>
		/// Gets the ID of a partition key range in the Azure Cosmos DB service.
		/// </summary> 
		[JsonProperty(PropertyName = "id")]
		public string PartitionKeyRangeId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the size in KB of a partition key range in the Azure Cosmos DB service.
		/// </summary> 
		[JsonProperty(PropertyName = "sizeInKB")]
		public long SizeInKB
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the document count of a partition key range in the Azure Cosmos DB service.
		/// </summary> 
		[JsonProperty(PropertyName = "documentCount")]
		public long DocumentCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the partition key statistics for a partition key range in the Azure Cosmos DB service.
		/// </summary> 
		/// <remarks>
		/// This is reported based on a sub-sampling of partition keys within the partition key range and hence these are approximate. If your partition keys are below 1GB of storage, they may not show up in the reported statistics.
		/// </remarks>
		[JsonProperty(PropertyName = "partitionKeys")]
		public IReadOnlyList<PartitionKeyStatistics> PartitionKeyStatistics
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the stringified version of <see cref="T:Microsoft.Azure.Documents.PartitionKeyRangeStatistics" /> object in the Azure Cosmos DB service.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
