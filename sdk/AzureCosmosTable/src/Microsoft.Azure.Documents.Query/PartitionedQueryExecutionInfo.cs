using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Query
{
	internal sealed class PartitionedQueryExecutionInfo
	{
		[JsonProperty("partitionedQueryExecutionInfoVersion")]
		public int Version
		{
			get;
			private set;
		}

		[JsonProperty("queryInfo")]
		public QueryInfo QueryInfo
		{
			get;
			set;
		}

		[JsonProperty("queryRanges")]
		public List<Range<string>> QueryRanges
		{
			get;
			set;
		}

		public PartitionedQueryExecutionInfo()
		{
			Version = 2;
		}
	}
}
