using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Query
{
	internal sealed class PartitionedQueryExecutionInfoInternal
	{
		[JsonProperty("queryInfo")]
		public QueryInfo QueryInfo
		{
			get;
			set;
		}

		[JsonProperty("queryRanges")]
		public List<Range<PartitionKeyInternal>> QueryRanges
		{
			get;
			set;
		}
	}
}
