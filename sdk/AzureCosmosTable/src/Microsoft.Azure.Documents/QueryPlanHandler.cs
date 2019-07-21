using Microsoft.Azure.Documents.Query;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	internal sealed class QueryPlanHandler
	{
		private static class QueryPlanExceptionFactory
		{
			private static readonly ArgumentException QueryContainsUnsupportedAggregates = new ArgumentException(FormatExceptionMessage("Aggregate"));

			private static readonly ArgumentException QueryContainsUnsupportedCompositeAggregate = new ArgumentException(FormatExceptionMessage("CompositeAggregate"));

			private static readonly ArgumentException QueryContainsUnsupportedGroupBy = new ArgumentException(FormatExceptionMessage("GroupBy"));

			private static readonly ArgumentException QueryContainsUnsupportedMultipleAggregates = new ArgumentException(FormatExceptionMessage("MultipleAggregates"));

			private static readonly ArgumentException QueryContainsUnsupportedDistinct = new ArgumentException(FormatExceptionMessage("Distinct"));

			private static readonly ArgumentException QueryContainsUnsupportedOffsetAndLimit = new ArgumentException(FormatExceptionMessage("OffsetAndLimit"));

			private static readonly ArgumentException QueryContainsUnsupportedOrderBy = new ArgumentException(FormatExceptionMessage("OrderBy"));

			private static readonly ArgumentException QueryContainsUnsupportedMultipleOrderBy = new ArgumentException(FormatExceptionMessage("MultipleOrderBy"));

			private static readonly ArgumentException QueryContainsUnsupportedTop = new ArgumentException(FormatExceptionMessage("Top"));

			public static void ThrowIfNotSupported(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures)
			{
				Lazy<List<Exception>> lazy = new Lazy<List<Exception>>(() => new List<Exception>());
				AddExceptionsForAggregateQueries(queryInfo, supportedQueryFeatures, lazy);
				AddExceptionsForDistinctQueries(queryInfo, supportedQueryFeatures, lazy);
				AddExceptionForGroupByQueries(queryInfo, supportedQueryFeatures, lazy);
				AddExceptionsForTopQueries(queryInfo, supportedQueryFeatures, lazy);
				AddExceptionsForOrderByQueries(queryInfo, supportedQueryFeatures, lazy);
				AddExceptionsForOffsetLimitQueries(queryInfo, supportedQueryFeatures, lazy);
				if (lazy.IsValueCreated)
				{
					throw new QueryPlanHandlerException(lazy.Value);
				}
			}

			private static void AddExceptionsForAggregateQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if (!queryInfo.HasAggregates)
				{
					return;
				}
				if (queryInfo.Aggregates.Length == 1)
				{
					if (queryInfo.HasSelectValue)
					{
						if (!supportedQueryFeatures.HasFlag(QueryFeatures.Aggregate))
						{
							exceptions.Value.Add(QueryContainsUnsupportedAggregates);
						}
					}
					else if (!supportedQueryFeatures.HasFlag(QueryFeatures.CompositeAggregate))
					{
						exceptions.Value.Add(QueryContainsUnsupportedCompositeAggregate);
					}
				}
				else if (!supportedQueryFeatures.HasFlag(QueryFeatures.MultipleAggregates))
				{
					exceptions.Value.Add(QueryContainsUnsupportedMultipleAggregates);
				}
			}

			private static void AddExceptionsForDistinctQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if (queryInfo.HasDistinct && !supportedQueryFeatures.HasFlag(QueryFeatures.Distinct))
				{
					exceptions.Value.Add(QueryContainsUnsupportedDistinct);
				}
			}

			private static void AddExceptionsForOffsetLimitQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if ((queryInfo.HasLimit || queryInfo.HasOffset) && !supportedQueryFeatures.HasFlag(QueryFeatures.OffsetAndLimit))
				{
					exceptions.Value.Add(QueryContainsUnsupportedOffsetAndLimit);
				}
			}

			private static void AddExceptionsForOrderByQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if (!queryInfo.HasOrderBy)
				{
					return;
				}
				if (queryInfo.OrderByExpressions.Length == 1)
				{
					if (!supportedQueryFeatures.HasFlag(QueryFeatures.OrderBy))
					{
						exceptions.Value.Add(QueryContainsUnsupportedOrderBy);
					}
				}
				else if (!supportedQueryFeatures.HasFlag(QueryFeatures.MultipleOrderBy))
				{
					exceptions.Value.Add(QueryContainsUnsupportedMultipleOrderBy);
				}
			}

			private static void AddExceptionForGroupByQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if (queryInfo.HasGroupBy && !supportedQueryFeatures.HasFlag(QueryFeatures.GroupBy))
				{
					exceptions.Value.Add(QueryContainsUnsupportedGroupBy);
				}
			}

			private static void AddExceptionsForTopQueries(QueryInfo queryInfo, QueryFeatures supportedQueryFeatures, Lazy<List<Exception>> exceptions)
			{
				if (queryInfo.HasTop && !supportedQueryFeatures.HasFlag(QueryFeatures.Top))
				{
					exceptions.Value.Add(QueryContainsUnsupportedTop);
				}
			}

			private static string FormatExceptionMessage(string feature)
			{
				return $"Query contained {feature}, which the calling client does not support.";
			}
		}

		private sealed class QueryPlanHandlerException : AggregateException
		{
			private const string QueryContainsUnsupportedFeaturesExceptionMessage = "Query contains 1 or more unsupported features. Upgrade your SDK to a version that does support the requested features:";

			public QueryPlanHandlerException(IEnumerable<Exception> innerExceptions)
				: base("Query contains 1 or more unsupported features. Upgrade your SDK to a version that does support the requested features:" + Environment.NewLine + string.Join(Environment.NewLine, from innerException in innerExceptions
				select innerException.Message), innerExceptions)
			{
			}
		}

		private readonly QueryPartitionProvider queryPartitionProvider;

		public QueryPlanHandler(QueryPartitionProvider queryPartitionProvider)
		{
			if (queryPartitionProvider == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "queryPartitionProvider"));
			}
			this.queryPartitionProvider = queryPartitionProvider;
		}

		public PartitionedQueryExecutionInfo GetQueryPlan(SqlQuerySpec sqlQuerySpec, PartitionKeyDefinition partitionKeyDefinition, QueryFeatures supportedQueryFeatures)
		{
			if (sqlQuerySpec == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "sqlQuerySpec"));
			}
			if (partitionKeyDefinition == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "partitionKeyDefinition"));
			}
			PartitionedQueryExecutionInfo partitionedQueryExecutionInfo = queryPartitionProvider.GetPartitionedQueryExecutionInfo(sqlQuerySpec, partitionKeyDefinition, requireFormattableOrderByQuery: true, isContinuationExpected: false, allowNonValueAggregateQuery: true);
			if (partitionedQueryExecutionInfo == null || partitionedQueryExecutionInfo.QueryRanges == null || partitionedQueryExecutionInfo.QueryInfo == null || partitionedQueryExecutionInfo.QueryRanges.Any(delegate(Range<string> range)
			{
				if (range.Min != null)
				{
					return range.Max == null;
				}
				return true;
			}))
			{
				throw new InvalidOperationException(string.Format("{0} has invalid properties", "partitionedQueryExecutionInfo"));
			}
			QueryPlanExceptionFactory.ThrowIfNotSupported(partitionedQueryExecutionInfo.QueryInfo, supportedQueryFeatures);
			return partitionedQueryExecutionInfo;
		}
	}
}
