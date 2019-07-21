using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Query.ParallelQuery;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Factory class for creating the appropriate DocumentQueryExecutionContext for the provided type of query.
	/// </summary>
	internal static class DocumentQueryExecutionContextFactory
	{
		private const int PageSizeFactorForTop = 5;

		public static Task<IDocumentQueryExecutionContext> CreateDocumentQueryExecutionContextAsync(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, IEnumerable<string> documentFeedLinks, bool isContinuationExpected, CancellationToken token, Guid correlatedActivityId)
		{
			return MultiCollectionDocumentQueryExecutionContext.CreateAsync(client, resourceTypeEnum, resourceType, expression, feedOptions, documentFeedLinks, isContinuationExpected, token, correlatedActivityId);
		}

		public static async Task<IDocumentQueryExecutionContext> CreateDocumentQueryExecutionContextAsync(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, string resourceLink, bool isContinuationExpected, CancellationToken token, Guid correlatedActivityId)
		{
			DocumentCollection collection = null;
			if (resourceTypeEnum.IsCollectionChild())
			{
				CollectionCache collectionCache = await client.GetCollectionCacheAsync();
				using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Query, resourceTypeEnum, resourceLink, AuthorizationTokenType.Invalid))
				{
					collection = await collectionCache.ResolveCollectionAsync(request, token);
					if (feedOptions != null && feedOptions.PartitionKey != null && feedOptions.PartitionKey.Equals(PartitionKey.None))
					{
						feedOptions.PartitionKey = PartitionKey.FromInternalKey(collection.NonePartitionKeyValue);
					}
				}
			}
			DocumentQueryExecutionContextBase.InitParams constructorParams = new DocumentQueryExecutionContextBase.InitParams(client, resourceTypeEnum, resourceType, expression, feedOptions, resourceLink, getLazyFeedResponse: false, correlatedActivityId);
			if (CustomTypeExtensions.ByPassQueryParsing())
			{
				return await ProxyDocumentQueryExecutionContext.CreateAsync(client, resourceTypeEnum, resourceType, expression, feedOptions, resourceLink, token, collection, isContinuationExpected, correlatedActivityId);
			}
			DefaultDocumentQueryExecutionContext queryExecutionContext = await DefaultDocumentQueryExecutionContext.CreateAsync(constructorParams, isContinuationExpected, token);
			if (resourceTypeEnum.IsCollectionChild() && resourceTypeEnum.IsPartitioned() && (feedOptions.EnableCrossPartitionQuery || !isContinuationExpected))
			{
				PartitionedQueryExecutionInfo partitionedQueryExecutionInfo = await queryExecutionContext.GetPartitionedQueryExecutionInfoAsync(collection.PartitionKey, requireFormattableOrderByQuery: true, isContinuationExpected, allowNonValueAggregateQuery: false, token);
				if (ShouldCreateSpecializedDocumentQueryExecutionContext(resourceTypeEnum, feedOptions, partitionedQueryExecutionInfo, collection.PartitionKey, isContinuationExpected))
				{
					List<PartitionKeyRange> targetRanges;
					if (!string.IsNullOrEmpty(feedOptions.PartitionKeyRangeId))
					{
						List<PartitionKeyRange> list = new List<PartitionKeyRange>();
						List<PartitionKeyRange> list2 = list;
						list2.Add(await queryExecutionContext.GetTargetPartitionKeyRangeById(collection.ResourceId, feedOptions.PartitionKeyRangeId));
						targetRanges = list;
					}
					else
					{
						List<Range<string>> providedRanges = partitionedQueryExecutionInfo.QueryRanges;
						if (feedOptions.PartitionKey != null)
						{
							providedRanges = new List<Range<string>>
							{
								Range<string>.GetPointRange(feedOptions.PartitionKey.InternalKey.GetEffectivePartitionKeyString(collection.PartitionKey))
							};
						}
						targetRanges = await queryExecutionContext.GetTargetPartitionKeyRanges(collection.ResourceId, providedRanges);
					}
					return await CreateSpecializedDocumentQueryExecutionContext(constructorParams, partitionedQueryExecutionInfo, targetRanges, collection.ResourceId, isContinuationExpected, token);
				}
			}
			return queryExecutionContext;
		}

		public static Task<IDocumentQueryExecutionContext> CreateSpecializedDocumentQueryExecutionContext(DocumentQueryExecutionContextBase.InitParams constructorParams, PartitionedQueryExecutionInfo partitionedQueryExecutionInfo, List<PartitionKeyRange> targetRanges, string collectionRid, bool isContinuationExpected, CancellationToken cancellationToken)
		{
			long num = constructorParams.FeedOptions.MaxItemCount.GetValueOrDefault(ParallelQueryConfig.GetConfig().ClientInternalPageSize);
			if (num < -1 || num == 0L)
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, "Invalid MaxItemCount {0}", num));
			}
			QueryInfo queryInfo = partitionedQueryExecutionInfo.QueryInfo;
			bool hasTop = queryInfo.HasTop;
			if (queryInfo.HasOrderBy)
			{
				int value;
				if (queryInfo.HasTop && (value = partitionedQueryExecutionInfo.QueryInfo.Top.Value) > 0)
				{
					long num2 = (long)Math.Min(Math.Ceiling((double)value / (double)targetRanges.Count) * 5.0, value);
					num = ((num <= 0) ? num2 : Math.Min(num2, num));
				}
				else if (isContinuationExpected)
				{
					if (num < 0)
					{
						num = Math.Max(constructorParams.FeedOptions.MaxBufferedItemCount, ParallelQueryConfig.GetConfig().DefaultMaximumBufferSize);
					}
					num = (long)Math.Min(Math.Ceiling((double)num / (double)targetRanges.Count) * 5.0, num);
				}
			}
			return PipelinedDocumentQueryExecutionContext.CreateAsync(constructorParams, collectionRid, partitionedQueryExecutionInfo, targetRanges, (int)num, constructorParams.FeedOptions.RequestContinuation, cancellationToken);
		}

		private static bool ShouldCreateSpecializedDocumentQueryExecutionContext(ResourceType resourceTypeEnum, FeedOptions feedOptions, PartitionedQueryExecutionInfo partitionedQueryExecutionInfo, PartitionKeyDefinition partitionKeyDefinition, bool isContinuationExpected)
		{
			if ((!IsCrossPartitionQuery(resourceTypeEnum, feedOptions, partitionKeyDefinition, partitionedQueryExecutionInfo) || (!IsTopOrderByQuery(partitionedQueryExecutionInfo) && !IsAggregateQuery(partitionedQueryExecutionInfo) && !IsOffsetLimitQuery(partitionedQueryExecutionInfo) && !IsParallelQuery(feedOptions))) && string.IsNullOrEmpty(feedOptions.PartitionKeyRangeId) && !IsAggregateQueryWithoutContinuation(partitionedQueryExecutionInfo, isContinuationExpected))
			{
				return IsDistinctQuery(partitionedQueryExecutionInfo);
			}
			return true;
		}

		private static bool IsCrossPartitionQuery(ResourceType resourceTypeEnum, FeedOptions feedOptions, PartitionKeyDefinition partitionKeyDefinition, PartitionedQueryExecutionInfo partitionedQueryExecutionInfo)
		{
			if (resourceTypeEnum.IsPartitioned() && feedOptions.PartitionKey == null && feedOptions.EnableCrossPartitionQuery && partitionKeyDefinition.Paths.Count > 0)
			{
				if (partitionedQueryExecutionInfo.QueryRanges.Count == 1)
				{
					return !partitionedQueryExecutionInfo.QueryRanges[0].IsSingleValue;
				}
				return true;
			}
			return false;
		}

		private static bool IsTopOrderByQuery(PartitionedQueryExecutionInfo partitionedQueryExecutionInfo)
		{
			if (partitionedQueryExecutionInfo.QueryInfo != null)
			{
				if (!partitionedQueryExecutionInfo.QueryInfo.HasOrderBy)
				{
					return partitionedQueryExecutionInfo.QueryInfo.HasTop;
				}
				return true;
			}
			return false;
		}

		private static bool IsAggregateQuery(PartitionedQueryExecutionInfo partitionedQueryExecutionInfo)
		{
			if (partitionedQueryExecutionInfo.QueryInfo != null)
			{
				return partitionedQueryExecutionInfo.QueryInfo.HasAggregates;
			}
			return false;
		}

		private static bool IsAggregateQueryWithoutContinuation(PartitionedQueryExecutionInfo partitionedQueryExecutionInfo, bool isContinuationExpected)
		{
			if (IsAggregateQuery(partitionedQueryExecutionInfo))
			{
				return !isContinuationExpected;
			}
			return false;
		}

		private static bool IsDistinctQuery(PartitionedQueryExecutionInfo partitionedQueryExecutionInfo)
		{
			return partitionedQueryExecutionInfo.QueryInfo.HasDistinct;
		}

		private static bool IsParallelQuery(FeedOptions feedOptions)
		{
			return feedOptions.MaxDegreeOfParallelism != 0;
		}

		private static bool IsOffsetLimitQuery(PartitionedQueryExecutionInfo partitionedQueryExecutionInfo)
		{
			if (partitionedQueryExecutionInfo.QueryInfo.HasOffset)
			{
				return partitionedQueryExecutionInfo.QueryInfo.HasLimit;
			}
			return false;
		}
	}
}
