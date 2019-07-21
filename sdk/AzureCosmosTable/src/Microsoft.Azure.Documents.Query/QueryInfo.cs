using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Documents.Query
{
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class QueryInfo
	{
		[JsonProperty("distinctType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public DistinctQueryType DistinctType
		{
			get;
			set;
		}

		[JsonProperty("top")]
		public int? Top
		{
			get;
			set;
		}

		[JsonProperty("offset")]
		public int? Offset
		{
			get;
			set;
		}

		[JsonProperty("limit")]
		public int? Limit
		{
			get;
			set;
		}

		[JsonProperty("orderBy", ItemConverterType = typeof(StringEnumConverter))]
		public SortOrder[] OrderBy
		{
			get;
			set;
		}

		[JsonProperty("orderByExpressions")]
		public string[] OrderByExpressions
		{
			get;
			set;
		}

		[JsonProperty("groupByExpressions")]
		public string[] GroupByExpressions
		{
			get;
			set;
		}

		[JsonProperty("aggregates", ItemConverterType = typeof(StringEnumConverter))]
		public AggregateOperator[] Aggregates
		{
			get;
			set;
		}

		[JsonProperty("rewrittenQuery")]
		public string RewrittenQuery
		{
			get;
			set;
		}

		[JsonProperty("hasSelectValue")]
		public bool HasSelectValue
		{
			get;
			set;
		}

		public bool HasDistinct => DistinctType != DistinctQueryType.None;

		public bool HasTop => Top.HasValue;

		public bool HasAggregates
		{
			get
			{
				if (Aggregates != null)
				{
					return Aggregates.Length != 0;
				}
				return false;
			}
		}

		public bool HasGroupBy
		{
			get
			{
				if (GroupByExpressions != null)
				{
					return GroupByExpressions.Length != 0;
				}
				return false;
			}
		}

		public bool HasOrderBy
		{
			get
			{
				if (OrderBy != null)
				{
					return OrderBy.Length != 0;
				}
				return false;
			}
		}

		public bool HasOffset => Offset.HasValue;

		public bool HasLimit => Limit.HasValue;
	}
}
