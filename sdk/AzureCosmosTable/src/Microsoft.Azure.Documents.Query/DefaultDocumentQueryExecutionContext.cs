using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Default document query execution context for single partition queries or for split proofing general requests.
	/// </summary>
	internal sealed class DefaultDocumentQueryExecutionContext : DocumentQueryExecutionContextBase
	{
		/// <summary>
		/// Whether or not a continuation is expected.
		/// </summary>
		private readonly bool isContinuationExpected;

		private readonly SchedulingStopwatch fetchSchedulingMetrics;

		private readonly FetchExecutionRangeAccumulator fetchExecutionRangeAccumulator;

		private readonly IDictionary<string, IReadOnlyList<Range<string>>> providedRangesCache;

		private long retries;

		public DefaultDocumentQueryExecutionContext(InitParams constructorParams, bool isContinuationExpected)
			: base(constructorParams)
		{
			this.isContinuationExpected = isContinuationExpected;
			fetchSchedulingMetrics = new SchedulingStopwatch();
			fetchSchedulingMetrics.Ready();
			fetchExecutionRangeAccumulator = new FetchExecutionRangeAccumulator();
			providedRangesCache = new Dictionary<string, IReadOnlyList<Range<string>>>();
			retries = -1L;
		}

		public static Task<DefaultDocumentQueryExecutionContext> CreateAsync(InitParams constructorParams, bool isContinuationExpected, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			return Task.FromResult(new DefaultDocumentQueryExecutionContext(constructorParams, isContinuationExpected));
		}

		public override void Dispose()
		{
		}

		protected override async Task<FeedResponse<dynamic>> ExecuteInternalAsync(CancellationToken token)
		{
			CollectionCache collectionCache = await base.Client.GetCollectionCacheAsync();
			PartitionKeyRangeCache partitionKeyRangeCache = await base.Client.GetPartitionKeyRangeCache();
			IDocumentClientRetryPolicy retryPolicyInstance = base.Client.ResetSessionTokenRetryPolicy.GetRequestPolicy();
			retryPolicyInstance = new InvalidPartitionExceptionRetryPolicy(collectionCache, retryPolicyInstance);
			if (base.ResourceTypeEnum.IsPartitioned())
			{
				retryPolicyInstance = new PartitionKeyRangeGoneRetryPolicy(collectionCache, partitionKeyRangeCache, PathsHelper.GetCollectionPath(base.ResourceLink), retryPolicyInstance);
			}
			return await BackoffRetryUtility<FeedResponse<object>>.ExecuteAsync((Func<Task<FeedResponse<object>>>)async delegate
			{
				fetchExecutionRangeAccumulator.BeginFetchRange();
				retries++;
				Tuple<FeedResponse<object>, string> obj = await ExecuteOnceAsync(retryPolicyInstance, token);
				FeedResponse<object> feedResponse = obj.Item1;
				string item = obj.Item2;
				if (!string.IsNullOrEmpty(feedResponse.ResponseHeaders["x-ms-documentdb-query-metrics"]))
				{
					fetchExecutionRangeAccumulator.EndFetchRange(item, feedResponse.ActivityId, feedResponse.Count, retries);
					feedResponse = new FeedResponse<object>(feedResponse, feedResponse.Count, feedResponse.Headers, feedResponse.UseETagAsContinuation, new Dictionary<string, QueryMetrics>
					{
						{
							item,
							QueryMetrics.CreateFromDelimitedStringAndClientSideMetrics(feedResponse.ResponseHeaders["x-ms-documentdb-query-metrics"], new ClientSideMetrics(retries, feedResponse.RequestCharge, fetchExecutionRangeAccumulator.GetExecutionRanges(), string.IsNullOrEmpty(feedResponse.ResponseContinuation) ? new List<Tuple<string, SchedulingTimeSpan>>
							{
								new Tuple<string, SchedulingTimeSpan>(item, fetchSchedulingMetrics.Elapsed)
							} : new List<Tuple<string, SchedulingTimeSpan>>()))
						}
					}, feedResponse.RequestStatistics, feedResponse.DisallowContinuationTokenMessage, feedResponse.ResponseLengthBytes);
				}
				retries = -1L;
				return feedResponse;
			}, (IRetryPolicy)retryPolicyInstance, token, (Action<Exception>)null);
		}

		private async Task<Tuple<FeedResponse<dynamic>, string>> ExecuteOnceAsync(IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			using (DocumentServiceRequest request = await CreateRequestAsync())
			{
				FeedResponse<object> item;
				string item2;
				if (LogicalPartitionKeyProvided(request))
				{
					item = await ExecuteRequestAsync(request, retryPolicyInstance, cancellationToken);
					item2 = string.Format("PKId({0})", request.Headers["x-ms-documentdb-partitionkey"]);
				}
				else if (PhysicalPartitionKeyRangeIdProvided(this))
				{
					request.RouteTo(new PartitionKeyRangeIdentity((await(await base.Client.GetCollectionCacheAsync()).ResolveCollectionAsync(request, CancellationToken.None)).ResourceId, base.PartitionKeyRangeId));
					item = await ExecuteRequestAsync(request, retryPolicyInstance, cancellationToken);
					item2 = base.PartitionKeyRangeId;
				}
				else if (ServiceInteropAvailable())
				{
					CollectionCache collectionCache = await base.Client.GetCollectionCacheAsync();
					DocumentCollection collection = await collectionCache.ResolveCollectionAsync(request, CancellationToken.None);
					QueryPartitionProvider queryPartitionProvider = await base.Client.GetQueryPartitionProviderAsync(cancellationToken);
					IRoutingMapProvider routingMapProvider = await base.Client.GetRoutingMapProviderAsync();
					List<CompositeContinuationToken> suppliedTokens;
					Range<string> rangeFromContinuationToken = PartitionRoutingHelper.ExtractPartitionKeyRangeFromContinuationToken(request.Headers, out suppliedTokens);
					Tuple<PartitionRoutingHelper.ResolvedRangeInfo, IReadOnlyList<Range<string>>> queryRoutingInfo = await TryGetTargetPartitionKeyRangeAsync(request, collection, queryPartitionProvider, routingMapProvider, rangeFromContinuationToken, suppliedTokens);
					if (request.IsNameBased && queryRoutingInfo == null)
					{
						request.ForceNameCacheRefresh = true;
						collection = await collectionCache.ResolveCollectionAsync(request, CancellationToken.None);
						queryRoutingInfo = await TryGetTargetPartitionKeyRangeAsync(request, collection, queryPartitionProvider, routingMapProvider, rangeFromContinuationToken, suppliedTokens);
					}
					if (queryRoutingInfo == null)
					{
						throw new NotFoundException(string.Format("{0}: Was not able to get queryRoutingInfo even after resolve collection async with force name cache refresh to the following collectionRid: {1} with the supplied tokens: {2}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), collection.ResourceId, JsonConvert.SerializeObject(suppliedTokens)));
					}
					request.RouteTo(new PartitionKeyRangeIdentity(collection.ResourceId, queryRoutingInfo.Item1.ResolvedRange.Id));
					FeedResponse<object> response = await ExecuteRequestAsync(request, retryPolicyInstance, cancellationToken);
					if (!(await PartitionRoutingHelper.TryAddPartitionKeyRangeToContinuationTokenAsync(response.Headers, queryRoutingInfo.Item2, routingMapProvider, collection.ResourceId, queryRoutingInfo.Item1)))
					{
						throw new NotFoundException(string.Format("{0}: Call to TryAddPartitionKeyRangeToContinuationTokenAsync failed to the following collectionRid: {1} with the supplied tokens: {2}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), collection.ResourceId, JsonConvert.SerializeObject(suppliedTokens)));
					}
					item = response;
					item2 = queryRoutingInfo.Item1.ResolvedRange.Id;
				}
				else
				{
					request.UseGatewayMode = true;
					item = await ExecuteRequestAsync(request, retryPolicyInstance, cancellationToken);
					item2 = "Gateway";
				}
				return new Tuple<FeedResponse<object>, string>(item, item2);
			}
		}

		private static bool LogicalPartitionKeyProvided(DocumentServiceRequest request)
		{
			if (string.IsNullOrEmpty(request.Headers["x-ms-documentdb-partitionkey"]))
			{
				return !request.ResourceType.IsPartitioned();
			}
			return true;
		}

		private static bool PhysicalPartitionKeyRangeIdProvided(DefaultDocumentQueryExecutionContext context)
		{
			return !string.IsNullOrEmpty(context.PartitionKeyRangeId);
		}

		private static bool ServiceInteropAvailable()
		{
			return !CustomTypeExtensions.ByPassQueryParsing();
		}

		private async Task<Tuple<PartitionRoutingHelper.ResolvedRangeInfo, IReadOnlyList<Range<string>>>> TryGetTargetPartitionKeyRangeAsync(DocumentServiceRequest request, DocumentCollection collection, QueryPartitionProvider queryPartitionProvider, IRoutingMapProvider routingMapProvider, Range<string> rangeFromContinuationToken, List<CompositeContinuationToken> suppliedTokens)
		{
			string text = request.Headers["x-ms-version"];
			text = (string.IsNullOrEmpty(text) ? HttpConstants.Versions.CurrentVersion : text);
			bool result = false;
			string text2 = request.Headers["x-ms-documentdb-query-enablecrosspartition"];
			if (text2 != null && !bool.TryParse(text2, out result))
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidHeaderValue, text2, "x-ms-documentdb-query-enablecrosspartition"));
			}
			if (!providedRangesCache.TryGetValue(collection.ResourceId, out IReadOnlyList<Range<string>> providedRanges))
			{
				providedRanges = ((!base.ShouldExecuteQueryRequest) ? new List<Range<string>>
				{
					new Range<string>(PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey, PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey, isMinInclusive: true, isMaxInclusive: false)
				} : PartitionRoutingHelper.GetProvidedPartitionKeyRanges(base.QuerySpec, result, parallelizeCrossPartitionQuery: false, isContinuationExpected, collection.PartitionKey, queryPartitionProvider, text, out QueryInfo _));
				providedRangesCache[collection.ResourceId] = providedRanges;
			}
			PartitionRoutingHelper.ResolvedRangeInfo resolvedRangeInfo = await PartitionRoutingHelper.TryGetTargetRangeFromContinuationTokenRange(providedRanges, routingMapProvider, collection.ResourceId, rangeFromContinuationToken, suppliedTokens);
			if (resolvedRangeInfo.ResolvedRange == null)
			{
				return null;
			}
			return Tuple.Create(resolvedRangeInfo, providedRanges);
		}

		private async Task<DocumentServiceRequest> CreateRequestAsync()
		{
			INameValueCollection nameValueCollection = await CreateCommonHeadersAsync(GetFeedOptions(ContinuationToken));
			nameValueCollection["x-ms-documentdb-query-iscontinuationexpected"] = isContinuationExpected.ToString();
			return CreateDocumentServiceRequest(nameValueCollection, base.QuerySpec, base.PartitionKeyInternal);
		}
	}
}
