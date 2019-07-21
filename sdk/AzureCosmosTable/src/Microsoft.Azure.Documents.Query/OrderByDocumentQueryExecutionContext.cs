using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections.Generic;
using Microsoft.Azure.Documents.Query.ParallelQuery;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// OrderByDocumentQueryExecutionContext is a concrete implementation for CrossPartitionQueryExecutionContext.
	/// This class is responsible for draining cross partition queries that have order by conditions.
	/// The way order by queries work is that they are doing a k-way merge of sorted lists from each partition with an added condition.
	/// The added condition is that if 2 or more top documents from different partitions are equivalent then we drain from the left most partition first.
	/// This way we can generate a single continuation token for all n partitions.
	/// This class is able to stop and resume execution by generating continuation tokens and reconstructing an execution context from said token.
	/// </summary>
	internal sealed class OrderByDocumentQueryExecutionContext : CrossPartitionQueryExecutionContext<OrderByQueryResult>
	{
		/// <summary>
		/// Struct to hold all the filters for every partition.
		/// </summary>
		private struct FormattedFilterInfo
		{
			/// <summary>
			/// Filters for current partition.
			/// </summary>
			public readonly string FiltersForTargetRange;

			/// <summary>
			/// Filters for partitions left of the current partition.
			/// </summary>
			public readonly string FilterForRangesLeftOfTargetRanges;

			/// <summary>
			/// Filters for partitions right of the current partition.
			/// </summary>
			public readonly string FilterForRangesRightOfTargetRanges;

			/// <summary>
			/// Initializes a new instance of the FormattedFilterInfo struct.
			/// </summary>
			/// <param name="leftFilter">The filters for the partitions left of the current partition.</param>
			/// <param name="targetFilter">The filters for the current partition.</param>
			/// <param name="rightFilters">The filters for the partitions right of the current partition.</param>
			public FormattedFilterInfo(string leftFilter, string targetFilter, string rightFilters)
			{
				FilterForRangesLeftOfTargetRanges = leftFilter;
				FiltersForTargetRange = targetFilter;
				FilterForRangesRightOfTargetRanges = rightFilters;
			}
		}

		/// <summary>
		/// Equality comparer used to determine if a document producer needs it's continuation token returned.
		/// Basically just says that the continuation token can be flushed once you stop seeing duplicates.
		/// </summary>
		private sealed class OrderByEqualityComparer : IEqualityComparer<OrderByQueryResult>
		{
			/// <summary>
			/// The order by comparer.
			/// </summary>
			private readonly OrderByConsumeComparer orderByConsumeComparer;

			/// <summary>
			/// Initializes a new instance of the OrderByEqualityComparer class.
			/// </summary>
			/// <param name="orderByConsumeComparer">The order by consume comparer.</param>
			public OrderByEqualityComparer(OrderByConsumeComparer orderByConsumeComparer)
			{
				if (orderByConsumeComparer == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "orderByConsumeComparer"));
				}
				this.orderByConsumeComparer = orderByConsumeComparer;
			}

			/// <summary>
			/// Gets whether two OrderByQueryResult instances are equal.
			/// </summary>
			/// <param name="x">The first.</param>
			/// <param name="y">The second.</param>
			/// <returns>Whether two OrderByQueryResult instances are equal.</returns>
			public bool Equals(OrderByQueryResult x, OrderByQueryResult y)
			{
				return orderByConsumeComparer.CompareOrderByItems(x.OrderByItems, y.OrderByItems) == 0;
			}

			/// <summary>
			/// Gets the hash code for object.
			/// </summary>
			/// <param name="obj">The object to hash.</param>
			/// <returns>The hash code for the OrderByQueryResult object.</returns>
			public int GetHashCode(OrderByQueryResult obj)
			{
				return 0;
			}
		}

		/// <summary>
		/// Order by queries are rewritten to allow us to inject a filter.
		/// This placeholder is so that we can just string replace it with the filter we want without having to understand the structure of the query.
		/// </summary>
		private const string FormatPlaceHolder = "{documentdb-formattableorderbyquery-filter}";

		/// <summary>
		/// If query does not need a filter then we replace the FormatPlaceHolder with "true", since
		/// "SELECT * FROM c WHERE blah and true" is the same as "SELECT * FROM c where blah"
		/// </summary>
		private const string True = "true";

		/// <summary>
		/// Function to determine the priority of fetches.
		/// Basically we are fetching from the partition with the least number of buffered documents first.
		/// </summary>
		private static readonly Func<DocumentProducerTree<OrderByQueryResult>, int> FetchPriorityFunction = (DocumentProducerTree<OrderByQueryResult> documentProducerTree) => documentProducerTree.BufferedItemCount;

		/// <summary>
		/// JsonSerializerSettings for serializing the continuation token.
		/// </summary>
		private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
		{
			StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
		};

		/// <summary>
		/// Skip count used for JOIN queries.
		/// You can read up more about this in the documentation for the continuation token.
		/// </summary>
		private int skipCount;

		/// <summary>
		/// We need to keep track of the previousRid, since order by queries don't drain full pages.
		/// </summary>
		private string previousRid;

		/// <summary>
		/// Gets the continuation token for an order by query.
		/// </summary>
		protected override string ContinuationToken
		{
			get
			{
				if (IsDone)
				{
					return null;
				}
				IEnumerable<DocumentProducer<OrderByQueryResult>> activeDocumentProducers = GetActiveDocumentProducers();
				if (activeDocumentProducers.Count() <= 0)
				{
					return null;
				}
				return JsonConvert.SerializeObject(activeDocumentProducers.Select(delegate(DocumentProducer<OrderByQueryResult> documentProducer)
				{
					OrderByQueryResult current = documentProducer.Current;
					string filter = documentProducer.Filter;
					return new OrderByContinuationToken(new CompositeContinuationToken
					{
						Token = documentProducer.PreviousContinuationToken,
						Range = documentProducer.PartitionKeyRange.ToRange()
					}, current.OrderByItems, current.Rid, ShouldIncrementSkipCount(documentProducer) ? (skipCount + 1) : 0, filter);
				}), JsonSerializerSettings);
			}
		}

		/// <summary>
		/// Initializes a new instance of the OrderByDocumentQueryExecutionContext class.
		/// </summary>
		/// <param name="initPararms">The params used to construct the base class.</param>
		/// <param name="rewrittenQuery">
		/// For cross partition order by queries a query like "SELECT c.id, c.field_0 ORDER BY r.field_7 gets rewritten as:
		/// <![CDATA[
		/// SELECT r._rid, [{"item": r.field_7}] AS orderByItems, {"id": r.id, "field_0": r.field_0} AS payload
		/// FROM r
		/// WHERE({ document db - formattable order by query - filter})
		/// ORDER BY r.field_7]]>
		/// This is needed because we need to add additional filters to the query when we resume from a continuation,
		/// and it lets us easily parse out the _rid orderByItems, and payload without parsing the entire document (and having to remember the order by field).
		/// </param>
		/// <param name="consumeComparer">Comparer used to internally compare documents from different sorted partitions.</param>
		private OrderByDocumentQueryExecutionContext(InitParams initPararms, string rewrittenQuery, OrderByConsumeComparer consumeComparer)
			: base(initPararms, rewrittenQuery, (IComparer<DocumentProducerTree<OrderByQueryResult>>)consumeComparer, FetchPriorityFunction, (IEqualityComparer<OrderByQueryResult>)new OrderByEqualityComparer(consumeComparer))
		{
		}

		/// <summary>
		/// Creates an OrderByDocumentQueryExecutionContext
		/// </summary>
		/// <param name="constructorParams">The parameters for the base class constructor.</param>
		/// <param name="initParams">The parameters to initialize the base class.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on, which in turn creates an OrderByDocumentQueryExecutionContext.</returns>
		public static async Task<OrderByDocumentQueryExecutionContext> CreateAsync(InitParams constructorParams, CrossPartitionQueryExecutionContext<dynamic>.CrossPartitionInitParams initParams, CancellationToken token)
		{
			OrderByDocumentQueryExecutionContext context = new OrderByDocumentQueryExecutionContext(constructorParams, initParams.PartitionedQueryExecutionInfo.QueryInfo.RewrittenQuery, new OrderByConsumeComparer(initParams.PartitionedQueryExecutionInfo.QueryInfo.OrderBy));
			await context.InitializeAsync(initParams.RequestContinuation, initParams.CollectionRid, initParams.PartitionKeyRanges, initParams.InitialPageSize, initParams.PartitionedQueryExecutionInfo.QueryInfo.OrderBy, initParams.PartitionedQueryExecutionInfo.QueryInfo.OrderByExpressions, token);
			return context;
		}

		/// <summary>
		/// Drains a page of documents from this context.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A task that when awaited on return a page of documents.</returns>
		public override async Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken cancellationToken)
		{
			List<object> results = new List<object>();
			while (!IsDone && results.Count < maxElements)
			{
				DocumentProducerTree<OrderByQueryResult> currentDocumentProducerTree = PopCurrentDocumentProducerTree();
				OrderByQueryResult current = currentDocumentProducerTree.Current;
				results.Add(current.Payload);
				if (ShouldIncrementSkipCount(currentDocumentProducerTree.CurrentDocumentProducerTree.Root))
				{
					skipCount++;
				}
				else
				{
					skipCount = 0;
				}
				previousRid = current.Rid;
				await currentDocumentProducerTree.MoveNextAsync(cancellationToken);
				PushCurrentDocumentProducerTree(currentDocumentProducerTree);
			}
			return new FeedResponse<object>(results, results.Count, GetResponseHeaders(), useETagAsContinuation: false, GetQueryMetrics(), null, null, GetAndResetResponseLengthBytes());
		}

		/// <summary>
		/// Gets whether or not we should increment the skip count based on the rid of the document.
		/// </summary>
		/// <param name="currentDocumentProducer">The current document producer.</param>
		/// <returns>Whether or not we should increment the skip count.</returns>
		private bool ShouldIncrementSkipCount(DocumentProducer<OrderByQueryResult> currentDocumentProducer)
		{
			if (!currentDocumentProducer.IsAtBeginningOfPage)
			{
				return string.Equals(previousRid, currentDocumentProducer.Current.Rid, StringComparison.Ordinal);
			}
			return false;
		}

		/// <summary>
		/// Initializes this execution context.
		/// </summary>
		/// <param name="requestContinuation">The continuation token to resume from (or null if none).</param>
		/// <param name="collectionRid">The collection rid.</param>
		/// <param name="partitionKeyRanges">The partition key ranges to drain from.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <param name="sortOrders">The sort orders.</param>
		/// <param name="orderByExpressions">The order by expressions.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		private async Task InitializeAsync(string requestContinuation, string collectionRid, List<PartitionKeyRange> partitionKeyRanges, int initialPageSize, SortOrder[] sortOrders, string[] orderByExpressions, CancellationToken cancellationToken)
		{
			if (requestContinuation == null)
			{
				await InitializeAsync(token: cancellationToken, collectionRid: collectionRid, partitionKeyRanges: partitionKeyRanges, initialPageSize: initialPageSize, querySpecForInit: new SqlQuerySpec(base.QuerySpec.QueryText.Replace("{documentdb-formattableorderbyquery-filter}", "true"), base.QuerySpec.Parameters), targetRangeToContinuationMap: null, deferFirstPage: false, filter: null, filterCallback: null);
				return;
			}
			OrderByContinuationToken[] suppliedContinuationTokens = ValidateAndExtractContinuationToken(requestContinuation, sortOrders, orderByExpressions);
			Dictionary<string, OrderByContinuationToken> targetRangeToOrderByContinuationMap = null;
			RangeFilterInitializationInfo[] partitionKeyRangesInitializationInfo = GetPartitionKeyRangesInitializationInfo(suppliedContinuationTokens, partitionKeyRanges, sortOrders, orderByExpressions, out targetRangeToOrderByContinuationMap);
			for (int i = 0; i < partitionKeyRangesInitializationInfo.Length; i++)
			{
				RangeFilterInitializationInfo rangeFilterInitializationInfo = partitionKeyRangesInitializationInfo[i];
				if (rangeFilterInitializationInfo.StartIndex <= rangeFilterInitializationInfo.EndIndex)
				{
					await InitializeAsync(collectionRid, new PartialReadOnlyList<PartitionKeyRange>(partitionKeyRanges, rangeFilterInitializationInfo.StartIndex, rangeFilterInitializationInfo.EndIndex - rangeFilterInitializationInfo.StartIndex + 1), initialPageSize, new SqlQuerySpec(base.QuerySpec.QueryText.Replace("{documentdb-formattableorderbyquery-filter}", rangeFilterInitializationInfo.Filter), base.QuerySpec.Parameters), targetRangeToOrderByContinuationMap.ToDictionary((KeyValuePair<string, OrderByContinuationToken> kvp) => kvp.Key, (KeyValuePair<string, OrderByContinuationToken> kvp) => kvp.Value.CompositeContinuationToken.Token), deferFirstPage: false, rangeFilterInitializationInfo.Filter, async delegate(DocumentProducerTree<OrderByQueryResult> documentProducerTree)
					{
						if (targetRangeToOrderByContinuationMap.TryGetValue(documentProducerTree.Root.PartitionKeyRange.Id, out OrderByContinuationToken value))
						{
							await FilterAsync(documentProducerTree, sortOrders, value, cancellationToken);
						}
					}, cancellationToken);
				}
			}
		}

		/// <summary>
		/// Validates and extracts out the order by continuation tokens 
		/// </summary>
		/// <param name="requestContinuation">The string continuation token.</param>
		/// <param name="sortOrders">The sort orders.</param>
		/// <param name="orderByExpressions">The order by expressions.</param>
		/// <returns>The continuation tokens.</returns>
		private OrderByContinuationToken[] ValidateAndExtractContinuationToken(string requestContinuation, SortOrder[] sortOrders, string[] orderByExpressions)
		{
			if (string.IsNullOrWhiteSpace(requestContinuation))
			{
				throw new ArgumentNullException("continuation can not be null or empty.");
			}
			try
			{
				OrderByContinuationToken[] array = JsonConvert.DeserializeObject<OrderByContinuationToken[]>(requestContinuation, DefaultJsonSerializationSettings.Value);
				if (array.Length == 0)
				{
					throw new BadRequestException($"Order by continuation token can not be empty: {requestContinuation}.");
				}
				OrderByContinuationToken[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					if (array2[i].OrderByItems.Length != sortOrders.Length)
					{
						throw new BadRequestException($"Invalid order-by items in ontinutaion token {requestContinuation} for OrderBy~Context.");
					}
				}
				return array;
			}
			catch (JsonException ex)
			{
				throw new BadRequestException($"Invalid JSON in continuation token {requestContinuation} for OrderBy~Context, exception: {ex.Message}");
			}
		}

		/// <summary>
		/// When resuming an order by query we need to filter the document producers.
		/// </summary>
		/// <param name="producer">The producer to filter down.</param>
		/// <param name="sortOrders">The sort orders.</param>
		/// <param name="continuationToken">The continuation token.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		private async Task FilterAsync(DocumentProducerTree<OrderByQueryResult> producer, SortOrder[] sortOrders, OrderByContinuationToken continuationToken, CancellationToken cancellationToken)
		{
			foreach (DocumentProducerTree<OrderByQueryResult> tree in producer)
			{
				if (!ResourceId.TryParse(continuationToken.Rid, out ResourceId continuationRid))
				{
					throw new BadRequestException($"Invalid Rid in the continuation token {continuationToken.CompositeContinuationToken.Token} for OrderBy~Context.");
				}
				Dictionary<string, ResourceId> resourceIds = new Dictionary<string, ResourceId>();
				int itemToSkip = continuationToken.SkipCount;
				bool continuationRidVerified = false;
				do
				{
					OrderByQueryResult current = tree.Current;
					int num = 0;
					for (int i = 0; i < sortOrders.Length; i++)
					{
						num = ItemComparer.Instance.Compare(continuationToken.OrderByItems[i].GetItem(), current.OrderByItems[i].GetItem());
						if (num != 0)
						{
							num = ((sortOrders[i] != SortOrder.Descending) ? num : (-num));
							break;
						}
					}
					if (num < 0)
					{
						break;
					}
					if (num == 0)
					{
						if (!resourceIds.TryGetValue(current.Rid, out ResourceId value))
						{
							if (!ResourceId.TryParse(current.Rid, out value))
							{
								throw new BadRequestException($"Invalid Rid in the continuation token {continuationToken.CompositeContinuationToken.Token} for OrderBy~Context.");
							}
							resourceIds.Add(current.Rid, value);
						}
						if (!continuationRidVerified)
						{
							if (continuationRid.Database != value.Database || continuationRid.DocumentCollection != value.DocumentCollection)
							{
								throw new BadRequestException($"Invalid Rid in the continuation token {continuationToken.CompositeContinuationToken.Token} for OrderBy~Context.");
							}
							continuationRidVerified = true;
						}
						num = continuationRid.Document.CompareTo(value.Document);
						if (sortOrders[0] == SortOrder.Descending)
						{
							num = -num;
						}
						if (num < 0)
						{
							break;
						}
						if (num == 0)
						{
							int num2 = itemToSkip;
							itemToSkip = num2 - 1;
							if (num2 <= 0)
							{
								break;
							}
						}
					}
				}
				while (await tree.MoveNextAsync(cancellationToken));
				continuationRid = null;
			}
		}

		/// <summary>
		/// Gets the filters for every partition.
		/// </summary>
		/// <param name="suppliedContinuationTokens">The supplied continuation token.</param>
		/// <param name="partitionKeyRanges">The partition key ranges.</param>
		/// <param name="sortOrders">The sort orders.</param>
		/// <param name="orderByExpressions">The order by expressions.</param>
		/// <param name="targetRangeToContinuationTokenMap">The dictionary of target ranges to continuation token map.</param>
		/// <returns>The filters for every partition.</returns>
		private RangeFilterInitializationInfo[] GetPartitionKeyRangesInitializationInfo(OrderByContinuationToken[] suppliedContinuationTokens, List<PartitionKeyRange> partitionKeyRanges, SortOrder[] sortOrders, string[] orderByExpressions, out Dictionary<string, OrderByContinuationToken> targetRangeToContinuationTokenMap)
		{
			int num = FindTargetRangeAndExtractContinuationTokens(partitionKeyRanges, from token in suppliedContinuationTokens
			select Tuple.Create(token, token.CompositeContinuationToken.Range), out targetRangeToContinuationTokenMap);
			FormattedFilterInfo formattedFilters = GetFormattedFilters(orderByExpressions, suppliedContinuationTokens, sortOrders);
			return new RangeFilterInitializationInfo[3]
			{
				new RangeFilterInitializationInfo(formattedFilters.FilterForRangesLeftOfTargetRanges, 0, num - 1),
				new RangeFilterInitializationInfo(formattedFilters.FiltersForTargetRange, num, num),
				new RangeFilterInitializationInfo(formattedFilters.FilterForRangesRightOfTargetRanges, num + 1, partitionKeyRanges.Count - 1)
			};
		}

		/// <summary>
		/// Gets the formatted filters for every partition.
		/// </summary>
		/// <param name="expressions">The filter expressions.</param>
		/// <param name="continuationTokens">The continuation token.</param>
		/// <param name="sortOrders">The sort orders.</param>
		/// <returns>The formatted filters for every partition.</returns>
		private FormattedFilterInfo GetFormattedFilters(string[] expressions, OrderByContinuationToken[] continuationTokens, SortOrder[] sortOrders)
		{
			for (int i = 0; i < continuationTokens.Length; i++)
			{
			}
			Tuple<string, string, string> formattedFilters = GetFormattedFilters(expressions, (from queryItem in continuationTokens[0].OrderByItems
			select queryItem.GetItem()).ToArray(), sortOrders);
			return new FormattedFilterInfo(formattedFilters.Item1, formattedFilters.Item2, formattedFilters.Item3);
		}

		private void AppendToBuilders(Tuple<StringBuilder, StringBuilder, StringBuilder> builders, object str)
		{
			AppendToBuilders(builders, str, str, str);
		}

		private void AppendToBuilders(Tuple<StringBuilder, StringBuilder, StringBuilder> builders, object left, object target, object right)
		{
			builders.Item1.Append(left);
			builders.Item2.Append(target);
			builders.Item3.Append(right);
		}

		private Tuple<string, string, string> GetFormattedFilters(string[] expressions, object[] orderByItems, SortOrder[] sortOrders)
		{
			int num = expressions.Length;
			bool num2 = num == 1;
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			StringBuilder stringBuilder3 = new StringBuilder();
			Tuple<StringBuilder, StringBuilder, StringBuilder> builders = new Tuple<StringBuilder, StringBuilder, StringBuilder>(stringBuilder, stringBuilder3, stringBuilder2);
			if (num2)
			{
				string arg = expressions.First();
				SortOrder sortOrder = sortOrders.First();
				string arg2 = JsonConvert.SerializeObject(orderByItems.First(), DefaultJsonSerializationSettings.Value);
				stringBuilder.Append(string.Format("{0} {1} {2}", arg, (sortOrder == SortOrder.Descending) ? "<" : ">", arg2));
				stringBuilder2.Append(string.Format("{0} {1} {2}", arg, (sortOrder == SortOrder.Descending) ? "<=" : ">=", arg2));
				stringBuilder3.Append(string.Format("{0} {1} {2}", arg, (sortOrder == SortOrder.Descending) ? "<=" : ">=", arg2));
			}
			else
			{
				for (int i = 1; i <= num; i++)
				{
					ArraySegment<string> arraySegment = new ArraySegment<string>(expressions, 0, i);
					ArraySegment<SortOrder> arraySegment2 = new ArraySegment<SortOrder>(sortOrders, 0, i);
					ArraySegment<object> arraySegment3 = new ArraySegment<object>(orderByItems, 0, i);
					bool flag = i == num;
					AppendToBuilders(builders, "(");
					for (int j = 0; j < i; j++)
					{
						string str = arraySegment.ElementAt(j);
						SortOrder sortOrder2 = arraySegment2.ElementAt(j);
						object value = arraySegment3.ElementAt(j);
						bool flag2 = j == i - 1;
						AppendToBuilders(builders, str);
						AppendToBuilders(builders, " ");
						if (flag2)
						{
							string str2 = (sortOrder2 == SortOrder.Descending) ? "<" : ">";
							AppendToBuilders(builders, str2);
							if (flag)
							{
								AppendToBuilders(builders, "", "=", "=");
							}
						}
						else
						{
							AppendToBuilders(builders, "=");
						}
						string str3 = JsonConvert.SerializeObject(value, DefaultJsonSerializationSettings.Value);
						AppendToBuilders(builders, " ");
						AppendToBuilders(builders, str3);
						AppendToBuilders(builders, " ");
						if (!flag2)
						{
							AppendToBuilders(builders, "AND ");
						}
					}
					AppendToBuilders(builders, ")");
					if (!flag)
					{
						AppendToBuilders(builders, " OR ");
					}
				}
			}
			return new Tuple<string, string, string>(stringBuilder.ToString(), stringBuilder2.ToString(), stringBuilder3.ToString());
		}
	}
}
