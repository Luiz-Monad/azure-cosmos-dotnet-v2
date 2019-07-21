using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	internal abstract class DocumentQueryExecutionContextBase : IDocumentQueryExecutionContext, IDisposable
	{
		public struct InitParams
		{
			public IDocumentQueryClient Client
			{
				get;
			}

			public ResourceType ResourceTypeEnum
			{
				get;
			}

			public Type ResourceType
			{
				get;
			}

			public Expression Expression
			{
				get;
			}

			public FeedOptions FeedOptions
			{
				get;
			}

			public string ResourceLink
			{
				get;
			}

			public bool GetLazyFeedResponse
			{
				get;
			}

			public Guid CorrelatedActivityId
			{
				get;
			}

			public InitParams(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, string resourceLink, bool getLazyFeedResponse, Guid correlatedActivityId)
			{
				if (client == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "client"));
				}
				if ((object)resourceType == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "resourceType"));
				}
				if (expression == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "expression"));
				}
				if (feedOptions == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "feedOptions"));
				}
				if (correlatedActivityId == Guid.Empty)
				{
					throw new ArgumentException(string.Format("{0} can not be empty.", "correlatedActivityId"));
				}
				Client = client;
				ResourceTypeEnum = resourceTypeEnum;
				ResourceType = resourceType;
				Expression = expression;
				FeedOptions = feedOptions;
				ResourceLink = resourceLink;
				GetLazyFeedResponse = getLazyFeedResponse;
				CorrelatedActivityId = correlatedActivityId;
			}
		}

		public static readonly FeedResponse<dynamic> EmptyFeedResponse = new FeedResponse<object>(Enumerable.Empty<object>(), Enumerable.Empty<object>().Count(), new StringKeyValueCollection(), useETagAsContinuation: false, null, null, null, 0L);

		protected SqlQuerySpec querySpec;

		private readonly IDocumentQueryClient client;

		private readonly ResourceType resourceTypeEnum;

		private readonly Type resourceType;

		private readonly Expression expression;

		private readonly FeedOptions feedOptions;

		private readonly string resourceLink;

		private readonly bool getLazyFeedResponse;

		private bool isExpressionEvaluated;

		private FeedResponse<dynamic> lastPage;

		private readonly Guid correlatedActivityId;

		public bool ShouldExecuteQueryRequest => QuerySpec != null;

		public IDocumentQueryClient Client => client;

		public Type ResourceType => resourceType;

		public ResourceType ResourceTypeEnum => resourceTypeEnum;

		public string ResourceLink => resourceLink;

		public int? MaxItemCount => feedOptions.MaxItemCount;

		protected SqlQuerySpec QuerySpec
		{
			get
			{
				if (!isExpressionEvaluated)
				{
					querySpec = DocumentQueryEvaluator.Evaluate(expression);
					isExpressionEvaluated = true;
				}
				return querySpec;
			}
		}

		protected PartitionKeyInternal PartitionKeyInternal
		{
			get
			{
				if (feedOptions.PartitionKey != null)
				{
					return feedOptions.PartitionKey.InternalKey;
				}
				return null;
			}
		}

		protected int MaxBufferedItemCount => feedOptions.MaxBufferedItemCount;

		protected int MaxDegreeOfParallelism => feedOptions.MaxDegreeOfParallelism;

		protected string PartitionKeyRangeId => feedOptions.PartitionKeyRangeId;

		protected virtual string ContinuationToken
		{
			get
			{
				if (lastPage != null)
				{
					return lastPage.ResponseContinuation;
				}
				return feedOptions.RequestContinuation;
			}
		}

		public virtual bool IsDone
		{
			get
			{
				if (lastPage != null)
				{
					return string.IsNullOrEmpty(lastPage.ResponseContinuation);
				}
				return false;
			}
		}

		public Guid CorrelatedActivityId => correlatedActivityId;

		protected DocumentQueryExecutionContextBase(InitParams initParams)
		{
			client = initParams.Client;
			resourceTypeEnum = initParams.ResourceTypeEnum;
			resourceType = initParams.ResourceType;
			expression = initParams.Expression;
			feedOptions = initParams.FeedOptions;
			resourceLink = initParams.ResourceLink;
			getLazyFeedResponse = initParams.GetLazyFeedResponse;
			correlatedActivityId = initParams.CorrelatedActivityId;
			isExpressionEvaluated = false;
		}

		public async Task<PartitionedQueryExecutionInfo> GetPartitionedQueryExecutionInfoAsync(PartitionKeyDefinition partitionKeyDefinition, bool requireFormattableOrderByQuery, bool isContinuationExpected, bool allowNonValueAggregateQuery, CancellationToken cancellationToken)
		{
			return (await client.GetQueryPartitionProviderAsync(cancellationToken)).GetPartitionedQueryExecutionInfo(QuerySpec, partitionKeyDefinition, requireFormattableOrderByQuery, isContinuationExpected, allowNonValueAggregateQuery);
		}

		public virtual async Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken cancellationToken)
		{
			if (IsDone)
			{
				throw new InvalidOperationException(RMResources.DocumentQueryExecutionContextIsDone);
			}
			lastPage = await ExecuteInternalAsync(cancellationToken);
			return lastPage;
		}

		public FeedOptions GetFeedOptions(string continuationToken)
		{
			return new FeedOptions(feedOptions)
			{
				RequestContinuation = continuationToken
			};
		}

		public async Task<INameValueCollection> CreateCommonHeadersAsync(FeedOptions feedOptions)
		{
			INameValueCollection requestHeaders = new StringKeyValueCollection();
			ConsistencyLevel defaultConsistencyLevel = await client.GetDefaultConsistencyLevelAsync();
			ConsistencyLevel? consistencyLevel = await client.GetDesiredConsistencyLevelAsync();
			if (!string.IsNullOrEmpty(feedOptions.SessionToken) && !ReplicatedResourceClient.IsReadingFromMaster(resourceTypeEnum, OperationType.ReadFeed) && (defaultConsistencyLevel == ConsistencyLevel.Session || (consistencyLevel.HasValue && consistencyLevel.Value == ConsistencyLevel.Session)))
			{
				requestHeaders["x-ms-session-token"] = feedOptions.SessionToken;
			}
			requestHeaders["x-ms-continuation"] = feedOptions.RequestContinuation;
			requestHeaders["x-ms-documentdb-isquery"] = bool.TrueString;
			if (feedOptions.MaxItemCount.HasValue)
			{
				requestHeaders["x-ms-max-item-count"] = feedOptions.MaxItemCount.ToString();
			}
			requestHeaders["x-ms-documentdb-query-enablecrosspartition"] = feedOptions.EnableCrossPartitionQuery.ToString();
			if (feedOptions.MaxDegreeOfParallelism != 0)
			{
				requestHeaders["x-ms-documentdb-query-parallelizecrosspartitionquery"] = bool.TrueString;
			}
			if (this.feedOptions.EnableScanInQuery.HasValue)
			{
				requestHeaders["x-ms-documentdb-query-enable-scan"] = this.feedOptions.EnableScanInQuery.ToString();
			}
			if (this.feedOptions.EmitVerboseTracesInQuery.HasValue)
			{
				requestHeaders["x-ms-documentdb-query-emit-traces"] = this.feedOptions.EmitVerboseTracesInQuery.ToString();
			}
			if (this.feedOptions.EnableLowPrecisionOrderBy.HasValue)
			{
				requestHeaders["x-ms-documentdb-query-enable-low-precision-order-by"] = this.feedOptions.EnableLowPrecisionOrderBy.ToString();
			}
			if (!string.IsNullOrEmpty(this.feedOptions.FilterBySchemaResourceId))
			{
				requestHeaders["x-ms-documentdb-filterby-schema-rid"] = this.feedOptions.FilterBySchemaResourceId;
			}
			if (this.feedOptions.ResponseContinuationTokenLimitInKb.HasValue)
			{
				requestHeaders["x-ms-documentdb-responsecontinuationtokenlimitinkb"] = this.feedOptions.ResponseContinuationTokenLimitInKb.ToString();
			}
			if (this.feedOptions.DisableRUPerMinuteUsage)
			{
				requestHeaders["x-ms-documentdb-disable-ru-per-minute-usage"] = bool.TrueString;
			}
			if (this.feedOptions.ConsistencyLevel.HasValue)
			{
				await client.EnsureValidOverwrite(feedOptions.ConsistencyLevel.Value);
				requestHeaders.Set("x-ms-consistency-level", this.feedOptions.ConsistencyLevel.Value.ToString());
			}
			else if (consistencyLevel.HasValue)
			{
				requestHeaders.Set("x-ms-consistency-level", consistencyLevel.Value.ToString());
			}
			if (this.feedOptions.EnumerationDirection.HasValue)
			{
				requestHeaders.Set("x-ms-enumeration-direction", this.feedOptions.EnumerationDirection.Value.ToString());
			}
			if (this.feedOptions.ReadFeedKeyType.HasValue)
			{
				requestHeaders.Set("x-ms-read-key-type", this.feedOptions.ReadFeedKeyType.Value.ToString());
			}
			if (this.feedOptions.StartId != null)
			{
				requestHeaders.Set("x-ms-start-id", this.feedOptions.StartId);
			}
			if (this.feedOptions.EndId != null)
			{
				requestHeaders.Set("x-ms-end-id", this.feedOptions.EndId);
			}
			if (this.feedOptions.StartEpk != null)
			{
				requestHeaders.Set("x-ms-start-epk", this.feedOptions.StartEpk);
			}
			if (this.feedOptions.EndEpk != null)
			{
				requestHeaders.Set("x-ms-end-epk", this.feedOptions.EndEpk);
			}
			if (this.feedOptions.PopulateQueryMetrics)
			{
				requestHeaders["x-ms-documentdb-populatequerymetrics"] = bool.TrueString;
			}
			if (this.feedOptions.ForceQueryScan)
			{
				requestHeaders["x-ms-documentdb-force-query-scan"] = bool.TrueString;
			}
			if (this.feedOptions.ContentSerializationFormat.HasValue)
			{
				requestHeaders["x-ms-documentdb-content-serialization-format"] = this.feedOptions.ContentSerializationFormat.Value.ToString();
			}
			if (this.feedOptions.MergeStaticId != null)
			{
				requestHeaders.Set("x-ms-cosmos-merge-static-id", this.feedOptions.MergeStaticId);
			}
			return requestHeaders;
		}

		public DocumentServiceRequest CreateDocumentServiceRequest(INameValueCollection requestHeaders, SqlQuerySpec querySpec, PartitionKeyInternal partitionKey)
		{
			DocumentServiceRequest documentServiceRequest = CreateDocumentServiceRequest(requestHeaders, querySpec);
			PopulatePartitionKeyInfo(documentServiceRequest, partitionKey);
			return documentServiceRequest;
		}

		public DocumentServiceRequest CreateDocumentServiceRequest(INameValueCollection requestHeaders, SqlQuerySpec querySpec, PartitionKeyRange targetRange, string collectionRid)
		{
			DocumentServiceRequest documentServiceRequest = CreateDocumentServiceRequest(requestHeaders, querySpec);
			PopulatePartitionKeyRangeInfo(documentServiceRequest, targetRange, collectionRid);
			return documentServiceRequest;
		}

		public Task<FeedResponse<dynamic>> ExecuteRequestAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			if (!ShouldExecuteQueryRequest)
			{
				return ExecuteReadFeedRequestAsync(request, retryPolicyInstance, cancellationToken);
			}
			return ExecuteQueryRequestAsync(request, retryPolicyInstance, cancellationToken);
		}

		public Task<FeedResponse<T>> ExecuteRequestAsync<T>(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			if (!ShouldExecuteQueryRequest)
			{
				return ExecuteReadFeedRequestAsync<T>(request, retryPolicyInstance, cancellationToken);
			}
			return ExecuteQueryRequestAsync<T>(request, retryPolicyInstance, cancellationToken);
		}

		public async Task<FeedResponse<dynamic>> ExecuteQueryRequestAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return GetFeedResponse(await ExecuteQueryRequestInternalAsync(request, retryPolicyInstance, cancellationToken));
		}

		public async Task<FeedResponse<T>> ExecuteQueryRequestAsync<T>(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return GetFeedResponse<T>(await ExecuteQueryRequestInternalAsync(request, retryPolicyInstance, cancellationToken));
		}

		public async Task<FeedResponse<dynamic>> ExecuteReadFeedRequestAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return GetFeedResponse(await client.ReadFeedAsync(request, retryPolicyInstance, cancellationToken));
		}

		public async Task<FeedResponse<T>> ExecuteReadFeedRequestAsync<T>(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			return GetFeedResponse<T>(await client.ReadFeedAsync(request, retryPolicyInstance, cancellationToken));
		}

		public void PopulatePartitionKeyRangeInfo(DocumentServiceRequest request, PartitionKeyRange range, string collectionRid)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			if (range == null)
			{
				throw new ArgumentNullException("range");
			}
			if (resourceTypeEnum.IsPartitioned())
			{
				request.RouteTo(new PartitionKeyRangeIdentity(collectionRid, range.Id));
			}
		}

		public async Task<PartitionKeyRange> GetTargetPartitionKeyRangeById(string collectionResourceId, string partitionKeyRangeId)
		{
			PartitionKeyRange range = await(await client.GetRoutingMapProviderAsync()).TryGetPartitionKeyRangeByIdAsync(collectionResourceId, partitionKeyRangeId);
			if (range == null && PathsHelper.IsNameBased(resourceLink))
			{
				(await Client.GetCollectionCacheAsync()).Refresh(resourceLink);
			}
			if (range == null)
			{
				throw new NotFoundException(string.Format("{0}: GetTargetPartitionKeyRangeById(collectionResourceId:{1}, partitionKeyRangeId: {2}) failed due to stale cache", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), collectionResourceId, partitionKeyRangeId));
			}
			return range;
		}

		public async Task<List<PartitionKeyRange>> GetTargetPartitionKeyRanges(string collectionResourceId, List<Range<string>> providedRanges)
		{
			List<PartitionKeyRange> ranges = await(await client.GetRoutingMapProviderAsync()).TryGetOverlappingRangesAsync(collectionResourceId, providedRanges);
			if (ranges == null && PathsHelper.IsNameBased(resourceLink))
			{
				(await Client.GetCollectionCacheAsync()).Refresh(resourceLink);
			}
			if (ranges == null)
			{
				throw new NotFoundException(string.Format("{0}: GetTargetPartitionKeyRanges(collectionResourceId:{1}, providedRanges: {2} failed due to stale cache", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), collectionResourceId, string.Join(",", providedRanges)));
			}
			return ranges;
		}

		public abstract void Dispose();

		protected abstract Task<FeedResponse<dynamic>> ExecuteInternalAsync(CancellationToken cancellationToken);

		protected async Task<List<PartitionKeyRange>> GetReplacementRanges(PartitionKeyRange targetRange, string collectionRid)
		{
			List<PartitionKeyRange> list = (await(await client.GetRoutingMapProviderAsync()).TryGetOverlappingRangesAsync(collectionRid, targetRange.ToRange(), forceRefresh: true)).ToList();
			string minInclusive = list.First().MinInclusive;
			string maxExclusive = list.Last().MaxExclusive;
			if (!minInclusive.Equals(targetRange.MinInclusive, StringComparison.Ordinal) || !maxExclusive.Equals(targetRange.MaxExclusive, StringComparison.Ordinal))
			{
				throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, "Target range and Replacement range has mismatched min/max. Target range: [{0}, {1}). Replacement range: [{2}, {3}).", targetRange.MinInclusive, targetRange.MaxExclusive, minInclusive, maxExclusive));
			}
			return list;
		}

		protected bool NeedPartitionKeyRangeCacheRefresh(DocumentClientException ex)
		{
			if (ex.StatusCode == HttpStatusCode.Gone)
			{
				return ex.GetSubStatus() == SubStatusCodes.PartitionKeyRangeGone;
			}
			return false;
		}

		private async Task<DocumentServiceResponse> ExecuteQueryRequestInternalAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			try
			{
				return await client.ExecuteQueryAsync(request, retryPolicyInstance, cancellationToken);
			}
			finally
			{
				request.Body.Position = 0L;
			}
		}

		private DocumentServiceRequest CreateDocumentServiceRequest(INameValueCollection requestHeaders, SqlQuerySpec querySpec)
		{
			DocumentServiceRequest documentServiceRequest = (querySpec != null) ? CreateQueryDocumentServiceRequest(requestHeaders, querySpec) : CreateReadFeedDocumentServiceRequest(requestHeaders);
			if (feedOptions.JsonSerializerSettings != null)
			{
				documentServiceRequest.SerializerSettings = feedOptions.JsonSerializerSettings;
			}
			return documentServiceRequest;
		}

		private DocumentServiceRequest CreateQueryDocumentServiceRequest(INameValueCollection requestHeaders, SqlQuerySpec querySpec)
		{
			QueryCompatibilityMode queryCompatibilityMode = client.QueryCompatibilityMode;
			DocumentServiceRequest documentServiceRequest;
			string s;
			if ((uint)queryCompatibilityMode > 1u && queryCompatibilityMode == QueryCompatibilityMode.SqlQuery)
			{
				if (querySpec.Parameters != null && querySpec.Parameters.Count > 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unsupported argument in query compatibility mode '{0}'", client.QueryCompatibilityMode), "querySpec.Parameters");
				}
				documentServiceRequest = DocumentServiceRequest.Create(OperationType.SqlQuery, resourceTypeEnum, resourceLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders);
				documentServiceRequest.Headers["Content-Type"] = "application/sql";
				s = querySpec.QueryText;
			}
			else
			{
				documentServiceRequest = DocumentServiceRequest.Create(OperationType.Query, resourceTypeEnum, resourceLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders);
				documentServiceRequest.Headers["Content-Type"] = "application/query+json";
				s = JsonConvert.SerializeObject((object)querySpec);
			}
			documentServiceRequest.Body = new MemoryStream(Encoding.UTF8.GetBytes(s));
			return documentServiceRequest;
		}

		private DocumentServiceRequest CreateReadFeedDocumentServiceRequest(INameValueCollection requestHeaders)
		{
			if (resourceTypeEnum == Microsoft.Azure.Documents.ResourceType.Database || resourceTypeEnum == Microsoft.Azure.Documents.ResourceType.Offer)
			{
				return DocumentServiceRequest.Create(OperationType.ReadFeed, (string)null, resourceTypeEnum, AuthorizationTokenType.PrimaryMasterKey, requestHeaders);
			}
			return DocumentServiceRequest.Create(OperationType.ReadFeed, resourceTypeEnum, resourceLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders);
		}

		private void PopulatePartitionKeyInfo(DocumentServiceRequest request, PartitionKeyInternal partitionKey)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			if (resourceTypeEnum.IsPartitioned() && partitionKey != null)
			{
				request.Headers["x-ms-documentdb-partitionkey"] = partitionKey.ToJsonString();
			}
		}

		private FeedResponse<dynamic> GetFeedResponse(DocumentServiceResponse response)
		{
			int itemCount = 0;
			long responseLengthBytes = response.ResponseBody.CanSeek ? response.ResponseBody.Length : 0;
			return new FeedResponse<object>(response.GetQueryResponse(resourceType, out itemCount), itemCount, response.Headers, response.RequestStats, responseLengthBytes);
		}

		private FeedResponse<T> GetFeedResponse<T>(DocumentServiceResponse response)
		{
			int itemCount = 0;
			long responseLengthBytes = response.ResponseBody.CanSeek ? response.ResponseBody.Length : 0;
			return new FeedResponse<T>(response.GetQueryResponse<T>(resourceType, getLazyFeedResponse, out itemCount), itemCount, response.Headers, response.RequestStats, responseLengthBytes);
		}
	}
}
