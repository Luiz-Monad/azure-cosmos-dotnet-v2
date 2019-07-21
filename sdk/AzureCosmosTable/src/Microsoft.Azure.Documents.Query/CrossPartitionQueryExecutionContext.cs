using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Collections.Generic;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Query.ExecutionComponent;
using Microsoft.Azure.Documents.Query.ParallelQuery;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// This class is responsible for maintaining a forest of <see cref="T:Microsoft.Azure.Documents.Query.DocumentProducerTree`1" />.
	/// The trees in this forest are ordered using a priority queue and the nodes within the forest are internally ordered using a comparator.
	/// The ordering is determine by the concrete derived class.
	/// This class allows derived classes to iterate through the documents in the forest using Current and MoveNext semantics.
	/// This class is also responsible for prefetching documents if necessary using <see cref="T:Microsoft.Azure.Documents.ComparableTaskScheduler" /> whose ordering is also determined by the derived classes.
	/// This class also aggregated all metrics from sending queries to individual partitions.
	/// </summary>
	/// <typeparam name="T">The type of the documents being retrieved.</typeparam>
	internal abstract class CrossPartitionQueryExecutionContext<T> : DocumentQueryExecutionContextBase, IDocumentQueryExecutionComponent, IDisposable
	{
		/// <summary>
		/// All CrossPartitionQueries need this information on top of the parameter for DocumentQueryExecutionContextBase.
		/// I moved it out into it's own type, so that we don't have to keep passing around all the individual parameters in the factory pattern.
		/// This also allows us to check the arguments once instead of in each of the constructors.
		/// </summary>
		public struct CrossPartitionInitParams
		{
			/// <summary>
			/// Gets the collection rid to drain documents from.
			/// </summary>
			public string CollectionRid
			{
				get;
			}

			/// <summary>
			/// Gets the serialized version of the PipelinedDocumentQueryExecutionContext.
			/// </summary>
			public PartitionedQueryExecutionInfo PartitionedQueryExecutionInfo
			{
				get;
			}

			/// <summary>
			/// Gets the partition key ranges to fan out to.
			/// </summary>
			public List<PartitionKeyRange> PartitionKeyRanges
			{
				get;
			}

			/// <summary>
			/// Gets the initial page size for each document producer.
			/// </summary>
			public int InitialPageSize
			{
				get;
			}

			/// <summary>
			/// Gets the continuation token to use for resuming the context (potentially on a different machine and different SDK).
			/// </summary>
			public string RequestContinuation
			{
				get;
			}

			/// <summary>
			/// Initializes a new instance of the InitParams struct.
			/// </summary>
			/// <param name="collectionRid">The collection rid.</param>
			/// <param name="partitionedQueryExecutionInfo">The partitioned query execution info.</param>
			/// <param name="partitionKeyRanges">The partition key ranges.</param>
			/// <param name="initialPageSize">The initial page size.</param>
			/// <param name="requestContinuation">The request continuation.</param>
			public CrossPartitionInitParams(string collectionRid, PartitionedQueryExecutionInfo partitionedQueryExecutionInfo, List<PartitionKeyRange> partitionKeyRanges, int initialPageSize, string requestContinuation)
			{
				if (string.IsNullOrWhiteSpace(collectionRid))
				{
					throw new ArgumentException(string.Format("{0} can not be null, empty, or white space.", "collectionRid"));
				}
				if (partitionedQueryExecutionInfo == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "partitionedQueryExecutionInfo"));
				}
				if (partitionKeyRanges == null)
				{
					throw new ArgumentNullException(string.Format("{0} can not be null.", "partitionKeyRanges"));
				}
				foreach (PartitionKeyRange partitionKeyRange in partitionKeyRanges)
				{
					if (partitionKeyRange == null)
					{
						throw new ArgumentNullException(string.Format("{0} can not be null.", "partitionKeyRange"));
					}
				}
				if (initialPageSize <= 0)
				{
					throw new ArgumentOutOfRangeException(string.Format("{0} must be atleast 1.", "initialPageSize"));
				}
				CollectionRid = collectionRid;
				PartitionedQueryExecutionInfo = partitionedQueryExecutionInfo;
				PartitionKeyRanges = partitionKeyRanges;
				InitialPageSize = initialPageSize;
				RequestContinuation = requestContinuation;
			}
		}

		/// <summary>
		/// Comparable task for the ComparableTaskScheduler.
		/// This is specifically for tasks that fetch from partitions in a document producer tree.
		/// </summary>
		private sealed class DocumentProducerTreeComparableTask : ComparableTask
		{
			/// <summary>
			/// The producer to fetch from.
			/// </summary>
			private readonly DocumentProducerTree<T> producer;

			/// <summary>
			/// Initializes a new instance of the DocumentProducerTreeComparableTask class.
			/// </summary>
			/// <param name="producer">The producer to fetch from.</param>
			/// <param name="taskPriorityFunction">The callback to determine the fetch priority of the document producer.</param>
			public DocumentProducerTreeComparableTask(DocumentProducerTree<T> producer, Func<DocumentProducerTree<T>, int> taskPriorityFunction)
				: base(taskPriorityFunction(producer))
			{
				this.producer = producer;
			}

			/// <summary>
			/// Entry point for the function to start fetching.
			/// </summary>
			/// <param name="token">The cancellation token.</param>
			/// <returns>A task to await on.</returns>
			public override Task StartAsync(CancellationToken token)
			{
				return producer.BufferMoreDocuments(token);
			}

			/// <summary>
			/// Determines whether this class is equal to another task.
			/// </summary>
			/// <param name="other">The other task</param>
			/// <returns>Whether this class is equal to another task.</returns>
			public override bool Equals(IComparableTask other)
			{
				return Equals(other as DocumentProducerTreeComparableTask);
			}

			/// <summary>
			/// Gets the hash code for this task.
			/// </summary>
			/// <returns>The hash code for this task.</returns>
			public override int GetHashCode()
			{
				return producer.PartitionKeyRange.GetHashCode();
			}

			/// <summary>
			/// Internal implementation of equality.
			/// </summary>
			/// <param name="other">The other comparable task to check for equality.</param>
			/// <returns>Whether or not the comparable tasks are equal.</returns>
			private bool Equals(DocumentProducerTreeComparableTask other)
			{
				return producer.PartitionKeyRange.Equals(other.producer.PartitionKeyRange);
			}
		}

		/// <summary>
		/// When a document producer tree successfully fetches a page we increase the page size by this factor so that any particular document producer will only ever make O(log(n)) roundtrips, while also only ever grabbing at most twice the number of documents needed.
		/// </summary>
		private const double DynamicPageSizeAdjustmentFactor = 1.6;

		/// <summary>
		/// Priority Queue of DocumentProducerTrees that make a forest that can be iterated on.
		/// </summary>
		private readonly PriorityQueue<DocumentProducerTree<T>> documentProducerForest;

		/// <summary>
		/// Function used to determine which document producer to fetch from first
		/// </summary>
		private readonly Func<DocumentProducerTree<T>, int> fetchPrioirtyFunction;

		/// <summary>
		/// The task scheduler that kicks off all the prefetches behind the scenes.
		/// </summary>
		private readonly ComparableTaskScheduler comparableTaskScheduler;

		/// <summary>
		/// The equality comparer used to determine whether a document producer needs it's continuation token to be part of the composite continuation token.
		/// </summary>
		private readonly IEqualityComparer<T> equalityComparer;

		/// <summary>
		/// Request Charge Tracker used to atomically add request charges (doubles).
		/// </summary>
		private readonly RequestChargeTracker requestChargeTracker;

		/// <summary>
		/// The actual max page size after all the optimizations have been made it in the create document query execution context layer.
		/// </summary>
		private readonly long actualMaxPageSize;

		/// <summary>
		/// The actual max buffered item count after all the optimizations have been made it in the create document query execution context layer.
		/// </summary>
		private readonly long actualMaxBufferedItemCount;

		/// <summary>
		/// This stores all the query metrics which have been grouped by partition id.
		/// When a feed response is returned (which includes multiple partitions and potentially multiple continuations)
		/// we take a snapshot of partitionedQueryMetrics and store it in grouped query metrics.
		/// </summary>
		private IReadOnlyDictionary<string, QueryMetrics> groupedQueryMetrics;

		/// <summary>
		/// This stores the running query metrics.
		/// When a feed response is returned he take a snapshot of this bag and store it in groupedQueryMetrics.
		/// The bag is then emptied and available to store the query metric for future continuations.
		/// </summary>
		/// <remarks>
		/// Due to the nature of parallel queries and prefetches the query metrics you get for a single continuation does not always 
		/// map to how much work was done to get that continuation.
		/// For example say for a simple cross partition query we return the first page of the results from the first partition,
		/// but behind the scenes we prefetched from other partitions.
		/// Another example is for an order by query we return one page of results but it only required us to use partial pages from each partition, 
		/// but we eventually used the whole page for the next continuation; which continuation reports the cost?
		/// Basically the only thing we can ensure is if you drain a query fully you should get back the same query metrics by the end.
		/// </remarks>
		private ConcurrentBag<Tuple<string, QueryMetrics>> partitionedQueryMetrics;

		/// <summary>
		/// Total number of buffered items to determine if we can go for another prefetch while still honoring the MaxBufferedItemCount.
		/// </summary>
		private long totalBufferedItems;

		/// <summary>
		/// The total response length.
		/// </summary>
		private long totalResponseLengthBytes;

		/// <summary>
		/// Gets a value indicating whether this context is done having documents drained.
		/// </summary>
		public override bool IsDone => !HasMoreResults;

		protected int ActualMaxBufferedItemCount => (int)actualMaxBufferedItemCount;

		protected int ActualMaxPageSize => (int)actualMaxPageSize;

		/// <summary>
		/// Gets the continuation token for the context.
		/// This method is overridden by the derived class, since they all have different continuation tokens.
		/// </summary>
		protected abstract override string ContinuationToken
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether we are allowed to prefetch.
		/// </summary>
		private bool CanPrefetch => base.MaxDegreeOfParallelism != 0;

		/// <summary>
		/// Gets a value indicating whether the context still has more results.
		/// </summary>
		private bool HasMoreResults
		{
			get
			{
				if (documentProducerForest.Count != 0)
				{
					return CurrentDocumentProducerTree().HasMoreResults;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the number of documents we can still buffer.
		/// </summary>
		private long FreeItemSpace => actualMaxBufferedItemCount - Interlocked.Read(ref totalBufferedItems);

		/// <summary>
		/// Initializes a new instance of the CrossPartitionQueryExecutionContext class.
		/// </summary>
		/// <param name="initParams">Constructor parameters for the base class.</param>
		/// <param name="rewrittenQuery">
		/// Queries will get rewritten for different reasons.
		/// You can read more about this in the details from the concrete classes.
		/// </param>
		/// <param name="moveNextComparer">Comparer used to figure out that document producer tree to serve documents from next.</param>
		/// <param name="fetchPrioirtyFunction">The priority function to determine which partition to fetch documents from next.</param>
		/// <param name="equalityComparer">Used to determine whether we need to return the continuation token for a partition.</param>
		protected CrossPartitionQueryExecutionContext(InitParams initParams, string rewrittenQuery, IComparer<DocumentProducerTree<T>> moveNextComparer, Func<DocumentProducerTree<T>, int> fetchPrioirtyFunction, IEqualityComparer<T> equalityComparer)
			: base(initParams)
		{
			if (!string.IsNullOrWhiteSpace(rewrittenQuery))
			{
				querySpec = new SqlQuerySpec(rewrittenQuery, base.QuerySpec.Parameters);
			}
			if (moveNextComparer == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null", "moveNextComparer"));
			}
			if (fetchPrioirtyFunction == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null", "fetchPrioirtyFunction"));
			}
			if (equalityComparer == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null", "equalityComparer"));
			}
			documentProducerForest = new PriorityQueue<DocumentProducerTree<T>>(moveNextComparer, isSynchronized: true);
			this.fetchPrioirtyFunction = fetchPrioirtyFunction;
			comparableTaskScheduler = new ComparableTaskScheduler(initParams.FeedOptions.MaxDegreeOfParallelism);
			this.equalityComparer = equalityComparer;
			requestChargeTracker = new RequestChargeTracker();
			partitionedQueryMetrics = new ConcurrentBag<Tuple<string, QueryMetrics>>();
			actualMaxPageSize = base.MaxItemCount.GetValueOrDefault(ParallelQueryConfig.GetConfig().ClientInternalMaxItemCount);
			if (actualMaxPageSize < 0)
			{
				throw new OverflowException("actualMaxPageSize should never be less than 0");
			}
			if (actualMaxPageSize > int.MaxValue)
			{
				throw new OverflowException("actualMaxPageSize should never be greater than int.MaxValue");
			}
			if (IsMaxBufferedItemCountSet(base.MaxBufferedItemCount))
			{
				actualMaxBufferedItemCount = base.MaxBufferedItemCount;
			}
			else
			{
				actualMaxBufferedItemCount = ParallelQueryConfig.GetConfig().DefaultMaximumBufferSize;
			}
			if (actualMaxBufferedItemCount < 0)
			{
				throw new OverflowException("actualMaxBufferedItemCount should never be less than 0");
			}
			if (actualMaxBufferedItemCount > int.MaxValue)
			{
				throw new OverflowException("actualMaxBufferedItemCount should never be greater than int.MaxValue");
			}
		}

		/// <summary>
		/// Gets the response headers for the context.
		/// </summary>
		/// <returns>The response headers for the context.</returns>
		public INameValueCollection GetResponseHeaders()
		{
			StringKeyValueCollection stringKeyValueCollection = new StringKeyValueCollection();
			stringKeyValueCollection["x-ms-continuation"] = ContinuationToken;
			if (ContinuationToken == "[]")
			{
				throw new InvalidOperationException("Somehow a document query execution context returned an empty array of continuations.");
			}
			SetQueryMetrics();
			IReadOnlyDictionary<string, QueryMetrics> queryMetrics = GetQueryMetrics();
			if (queryMetrics != null && queryMetrics.Count != 0)
			{
				stringKeyValueCollection["x-ms-documentdb-query-metrics"] = QueryMetrics.CreateFromIEnumerable(queryMetrics.Values).ToDelimitedString();
			}
			stringKeyValueCollection["x-ms-request-charge"] = requestChargeTracker.GetAndResetCharge().ToString(CultureInfo.InvariantCulture);
			return stringKeyValueCollection;
		}

		/// <summary>
		/// Gets the query metrics that are set in SetQueryMetrics
		/// </summary>
		/// <returns>The grouped query metrics.</returns>
		public IReadOnlyDictionary<string, QueryMetrics> GetQueryMetrics()
		{
			return new PartitionedQueryMetrics(groupedQueryMetrics);
		}

		/// <summary>
		/// After a split you need to maintain the continuation tokens for all the child document producers until a condition is met.
		/// For example lets say that a document producer is at continuation X and it gets split,
		/// then the children each get continuation X, but since you only drain from one of them at a time you are left with the first child having 
		/// continuation X + delta and the second child having continuation X (draw this out if you are following along).
		/// At this point you have the answer the question: "Which continuation token do you return to the user?".
		/// Let's say you return X, then when you come back to the first child you will be repeating work, thus returning some documents more than once.
		/// Let's say you return X + delta, then you fine when you return to the first child, but when you get to the second child you don't have a continuation token
		/// meaning that you will be repeating all the document for the second partition up until X and again you will be returning some documents more than once.
		/// Thus you have to return the continuation token for both children.
		/// Both this means you are returning more than 1 continuation token for the rest of the query.
		/// Well a naive optimization is to flush the continuation for a child partition once you are done draining from it, which isn't bad for a parallel query,
		/// but if you have an order by query you might not be done with a producer until the end of the query.
		/// The next optimization for a parallel query is to flush the continuation token the moment you start reading from a child partition.
		/// This works for a parallel query, but breaks for an order by query.
		/// The final realization is that for an order by query you are only choosing between multiple child partitions when their is a tie,
		/// so the key is that you can dump the continuation token the moment you come across a new order by item.
		/// For order by queries that is determined by the order by field and for parallel queries that is the moment you come by a new rid (which is any document, since rids are unique within a partition).
		/// So by passing an equality comparer to the document producers they can determine whether they are still "active".
		/// </summary>
		/// <returns>
		/// Returns all document producers whose continuation token you have to return.
		/// Only during a split will this list contain more than 1 item.
		/// </returns>
		public IEnumerable<DocumentProducer<T>> GetActiveDocumentProducers()
		{
			lock (documentProducerForest)
			{
				DocumentProducerTree<T> currentDocumentProducerTree = documentProducerForest.Peek().CurrentDocumentProducerTree;
				if (currentDocumentProducerTree.HasMoreResults && !currentDocumentProducerTree.IsActive)
				{
					yield return currentDocumentProducerTree.Root;
				}
				foreach (DocumentProducerTree<T> item in documentProducerForest)
				{
					foreach (DocumentProducer<T> activeDocumentProducer in item.GetActiveDocumentProducers())
					{
						yield return activeDocumentProducer;
					}
				}
			}
		}

		/// <summary>
		/// Gets the current document producer tree that should be drained from.
		/// </summary>
		/// <returns>The current document producer tree that should be drained from.</returns>
		public DocumentProducerTree<T> CurrentDocumentProducerTree()
		{
			return documentProducerForest.Peek();
		}

		/// <summary>
		/// Pushes a document producer back to the queue.
		/// </summary>
		/// <returns>The current document producer tree that should be drained from.</returns>
		public void PushCurrentDocumentProducerTree(DocumentProducerTree<T> documentProducerTree)
		{
			documentProducerForest.Enqueue(documentProducerTree);
		}

		/// <summary>
		/// Pops the current document producer tree that should be drained from.
		/// </summary>
		/// <returns>The current document producer tree that should be drained from.</returns>
		public DocumentProducerTree<T> PopCurrentDocumentProducerTree()
		{
			return documentProducerForest.Dequeue();
		}

		/// <summary>
		/// Disposes of the context and implements IDisposable.
		/// </summary>
		public override void Dispose()
		{
			comparableTaskScheduler.Dispose();
		}

		/// <summary>
		/// Stops the execution context.
		/// </summary>
		public void Stop()
		{
			comparableTaskScheduler.Stop();
		}

		/// <summary>
		/// Drains documents from this execution context.
		/// This method is abstract and meant for the concrete classes to implement.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements to drain (you might get less).</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task that when awaited on will return a feed response.</returns>
		public abstract Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token);

		/// <summary>
		/// Initializes cross partition query execution context by initializing the necessary document producers.
		/// </summary>
		/// <param name="collectionRid">The collection to drain from.</param>
		/// <param name="partitionKeyRanges">The partitions to target.</param>
		/// <param name="initialPageSize">The page size to start the document producers off with.</param>
		/// <param name="querySpecForInit">The query specification for the rewritten query.</param>
		/// <param name="targetRangeToContinuationMap">Map from partition to it's corresponding continuation token.</param>
		/// <param name="deferFirstPage">Whether or not we should defer the fetch of the first page from each partition.</param>
		/// <param name="filter">The filter to inject in the predicate.</param>
		/// <param name="filterCallback">The callback used to filter each partition.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		protected async Task InitializeAsync(string collectionRid, IReadOnlyList<PartitionKeyRange> partitionKeyRanges, int initialPageSize, SqlQuerySpec querySpecForInit, Dictionary<string, string> targetRangeToContinuationMap, bool deferFirstPage, string filter, Func<DocumentProducerTree<T>, Task> filterCallback, CancellationToken token)
		{
			CollectionCache collectionCache = await base.Client.GetCollectionCacheAsync();
			INameValueCollection requestHeaders = await CreateCommonHeadersAsync(GetFeedOptions(null));
			List<DocumentProducerTree<T>> list = new List<DocumentProducerTree<T>>();
			foreach (PartitionKeyRange partitionKeyRange in partitionKeyRanges)
			{
				DocumentProducerTree<T> documentProducerTree2 = new DocumentProducerTree<T>(initialContinuationToken: (targetRangeToContinuationMap != null && targetRangeToContinuationMap.ContainsKey(partitionKeyRange.Id)) ? targetRangeToContinuationMap[partitionKeyRange.Id] : null, partitionKeyRange: partitionKeyRange, createRequestFunc: delegate(PartitionKeyRange pkRange, string continuationToken, int pageSize)
				{
					INameValueCollection nameValueCollection = requestHeaders.Clone();
					nameValueCollection["x-ms-continuation"] = continuationToken;
					nameValueCollection["x-ms-max-item-count"] = pageSize.ToString(CultureInfo.InvariantCulture);
					return CreateDocumentServiceRequest(nameValueCollection, querySpecForInit, pkRange, collectionRid);
				}, executeRequestFunc: base.ExecuteRequestAsync<T>, createRetryPolicyFunc: () => new NonRetriableInvalidPartitionExceptionRetryPolicy(collectionCache, base.Client.ResetSessionTokenRetryPolicy.GetRequestPolicy()), produceAsyncCompleteCallback: OnDocumentProducerTreeCompleteFetching, documentProducerTreeComparer: documentProducerForest.Comparer, equalityComparer: equalityComparer, client: base.Client, deferFirstPage: deferFirstPage, collectionRid: collectionRid, initialPageSize: initialPageSize)
				{
					Filter = filter
				};
				if (CanPrefetch)
				{
					TryScheduleFetch(documentProducerTree2);
				}
				list.Add(documentProducerTree2);
			}
			foreach (DocumentProducerTree<T> documentProducerTree in list)
			{
				if (!deferFirstPage)
				{
					await documentProducerTree.MoveNextIfNotSplit(token);
				}
				if (filterCallback != null)
				{
					await filterCallback(documentProducerTree);
				}
				if (documentProducerTree.HasMoreResults)
				{
					documentProducerForest.Enqueue(documentProducerTree);
				}
			}
		}

		/// <summary>
		/// <para>
		/// If a query encounters split up resuming using continuation, we need to regenerate the continuation tokens. 
		/// Specifically, since after split we will have new set of ranges, we need to remove continuation token for the 
		/// parent partition and introduce continuation token for the child partitions. 
		/// </para>
		/// <para>
		/// This function does that. Also in that process, we also check validity of the input continuation tokens. For example, 
		/// even after split the boundary ranges of the child partitions should match with the parent partitions. If the Min and Max
		/// range of a target partition in the continuation token was Min1 and Max1. Then the Min and Max range info for the two 
		/// corresponding child partitions C1Min, C1Max, C2Min, and C2Max should follow the constrain below:
		///  PMax = C2Max &gt; C2Min &gt; C1Max &gt; C1Min = PMin.
		/// </para>
		/// </summary>
		/// <param name="partitionKeyRanges">The partition key ranges to extract continuation tokens for.</param>
		/// <param name="suppliedContinuationTokens">The continuation token that the user supplied.</param>
		/// <param name="targetRangeToContinuationTokenMap">The output dictionary of partition key range to continuation token.</param>
		/// <typeparam name="TContinuationToken">The type of continuation token to generate.</typeparam>
		/// <Remarks>
		/// The code assumes that merge doesn't happen. 
		/// </Remarks>
		/// <returns>The index of the partition whose MinInclusive is equal to the suppliedContinuationTokens</returns>
		protected int FindTargetRangeAndExtractContinuationTokens<TContinuationToken>(List<PartitionKeyRange> partitionKeyRanges, IEnumerable<Tuple<TContinuationToken, Range<string>>> suppliedContinuationTokens, out Dictionary<string, TContinuationToken> targetRangeToContinuationTokenMap)
		{
			if (partitionKeyRanges == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "partitionKeyRanges"));
			}
			if (partitionKeyRanges.Count < 1)
			{
				throw new ArgumentException(string.Format("{0} must have atleast one element.", "partitionKeyRanges"));
			}
			foreach (PartitionKeyRange partitionKeyRange in partitionKeyRanges)
			{
				if (partitionKeyRange == null)
				{
					throw new ArgumentException(string.Format("{0} can not have null elements.", "partitionKeyRanges"));
				}
			}
			if (suppliedContinuationTokens == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "suppliedContinuationTokens"));
			}
			if (suppliedContinuationTokens.Count() < 1)
			{
				throw new ArgumentException(string.Format("{0} must have atleast one element.", "suppliedContinuationTokens"));
			}
			if (suppliedContinuationTokens.Count() > partitionKeyRanges.Count)
			{
				throw new ArgumentException(string.Format("{0} can not have more elements than {1}.", "suppliedContinuationTokens", "partitionKeyRanges"));
			}
			targetRangeToContinuationTokenMap = new Dictionary<string, TContinuationToken>();
			Tuple<TContinuationToken, Range<string>> tuple2 = (from tuple in suppliedContinuationTokens
			orderby tuple.Item2.Min
			select tuple).First();
			TContinuationToken item = tuple2.Item1;
			PartitionKeyRange item2 = new PartitionKeyRange
			{
				MinInclusive = tuple2.Item2.Min,
				MaxExclusive = tuple2.Item2.Max
			};
			int num = partitionKeyRanges.BinarySearch(item2, Comparer<PartitionKeyRange>.Create((PartitionKeyRange range1, PartitionKeyRange range2) => string.CompareOrdinal(range1.MinInclusive, range2.MinInclusive)));
			if (num < 0)
			{
				throw new BadRequestException($"{RMResources.InvalidContinuationToken} - Could not find continuation token: {item}");
			}
			foreach (Tuple<TContinuationToken, Range<string>> suppliedContinuationToken in suppliedContinuationTokens)
			{
				TContinuationToken item3 = suppliedContinuationToken.Item1;
				Range<string> range3 = suppliedContinuationToken.Item2;
				IEnumerable<PartitionKeyRange> enumerable = from partitionKeyRange in partitionKeyRanges.Where(delegate(PartitionKeyRange partitionKeyRange)
				{
					if (string.CompareOrdinal(range3.Min, partitionKeyRange.MinInclusive) <= 0)
					{
						return string.CompareOrdinal(range3.Max, partitionKeyRange.MaxExclusive) >= 0;
					}
					return false;
				})
				orderby partitionKeyRange.MinInclusive
				select partitionKeyRange;
				if (enumerable.Count() == 0)
				{
					throw new BadRequestException($"{RMResources.InvalidContinuationToken} - Could not find continuation token: {item3}");
				}
				string max = range3.Max;
				string maxExclusive = enumerable.Last().MaxExclusive;
				string minInclusive = enumerable.Last().MinInclusive;
				string maxExclusive2 = enumerable.First().MaxExclusive;
				string minInclusive2 = enumerable.First().MinInclusive;
				string min = range3.Min;
				if (!(max == maxExclusive) || string.CompareOrdinal(maxExclusive, minInclusive) < 0 || (enumerable.Count() != 1 && string.CompareOrdinal(minInclusive, maxExclusive2) < 0) || string.CompareOrdinal(maxExclusive2, minInclusive2) < 0 || !(minInclusive2 == min))
				{
					throw new BadRequestException($"{RMResources.InvalidContinuationToken} - PMax = C2Max > C2Min > C1Max > C1Min = PMin: {item3}");
				}
				foreach (PartitionKeyRange item4 in enumerable)
				{
					targetRangeToContinuationTokenMap.Add(item4.Id, item3);
				}
			}
			return num;
		}

		protected virtual long GetAndResetResponseLengthBytes()
		{
			return Interlocked.Exchange(ref totalResponseLengthBytes, 0L);
		}

		protected virtual long IncrementResponseLengthBytes(long incrementValue)
		{
			return Interlocked.Add(ref totalResponseLengthBytes, incrementValue);
		}

		/// <summary>
		/// Placeholder function to implement DocumentQueryExecutionContextBase class.
		/// </summary>
		/// <param name="token">The cancellation token that doesn't get used.</param>
		/// <returns>A dummy task to await on.</returns>
		protected override Task<FeedResponse<dynamic>> ExecuteInternalAsync(CancellationToken token)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Since query metrics are being aggregated asynchronously to the feed reponses as explained in the member documentation,
		/// this function allows us to take a snapshot of the query metrics.
		/// </summary>
		private void SetQueryMetrics()
		{
			groupedQueryMetrics = (from tuple in Interlocked.Exchange(ref partitionedQueryMetrics, new ConcurrentBag<Tuple<string, QueryMetrics>>())
			group tuple.Item2 by tuple.Item1).ToDictionary((IGrouping<string, QueryMetrics> group) => group.Key, (IGrouping<string, QueryMetrics> group) => QueryMetrics.CreateFromIEnumerable(group));
		}

		/// <summary>
		/// Tries to schedule a fetch from the document producer tree.
		/// </summary>
		/// <param name="documentProducerTree">The document producer tree to schedule a fetch for.</param>
		/// <returns>Whether or not the fetch was successfully scheduled.</returns>
		private bool TryScheduleFetch(DocumentProducerTree<T> documentProducerTree)
		{
			return comparableTaskScheduler.TryQueueTask(new DocumentProducerTreeComparableTask(documentProducerTree, fetchPrioirtyFunction));
		}

		/// <summary>
		/// Function that is given to all the document producers to call on once they are done fetching.
		/// This is so that the CrossPartitionQueryExecutionContext can aggregate metadata from them.
		/// </summary>
		/// <param name="producer">The document producer that just finished fetching.</param>
		/// <param name="itemsBuffered">The number of items that the producer just fetched.</param>
		/// <param name="resourceUnitUsage">The amount of RUs that the producer just consumed.</param>
		/// <param name="queryMetrics">The query metrics that the producer just got back from the backend.</param>
		/// <param name="responseLengthBytes">The length of the response the producer just got back in bytes.</param>
		/// <param name="token">The cancellation token.</param>
		/// <remarks>
		/// This function is by nature a bit racy.
		/// A query might be fully drained but a background task is still fetching documents so this will get called after the context is done.
		/// </remarks>
		private void OnDocumentProducerTreeCompleteFetching(DocumentProducerTree<T> producer, int itemsBuffered, double resourceUnitUsage, QueryMetrics queryMetrics, long responseLengthBytes, CancellationToken token)
		{
			requestChargeTracker.AddCharge(resourceUnitUsage);
			Interlocked.Add(ref totalBufferedItems, itemsBuffered);
			IncrementResponseLengthBytes(responseLengthBytes);
			partitionedQueryMetrics.Add(Tuple.Create(producer.PartitionKeyRange.Id, queryMetrics));
			producer.PageSize = Math.Min((long)((double)producer.PageSize * 1.6), actualMaxPageSize);
			if (producer.HasMoreBackendResults)
			{
				long num = Math.Min(producer.PageSize, 4194304L);
				if (CanPrefetch && FreeItemSpace > num)
				{
					TryScheduleFetch(producer);
				}
			}
		}

		/// <summary>
		/// Gets the formatting for a trace.
		/// </summary>
		/// <param name="message">The message to format</param>
		/// <returns>The formatted message ready for a trace.</returns>
		private string GetTrace(string message)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1}, ActivityId: {2} | {3}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), base.CorrelatedActivityId, (documentProducerForest.Count != 0) ? CurrentDocumentProducerTree().ActivityId : Guid.Empty, message);
		}

		private static bool IsMaxBufferedItemCountSet(int maxBufferedItemCount)
		{
			return maxBufferedItemCount != 0;
		}
	}
}
