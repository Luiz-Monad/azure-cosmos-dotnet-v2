using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// ParallelDocumentQueryExecutionContext is a concrete implementation for CrossPartitionQueryExecutionContext.
	/// This class is responsible for draining cross partition queries that do not have order by conditions.
	/// The way parallel queries work is that it drains from the left most partition first.
	/// This class handles draining in the correct order and can also stop and resume the query 
	/// by generating a continuation token and resuming from said continuation token.
	/// </summary>
	internal sealed class ParallelDocumentQueryExecutionContext : CrossPartitionQueryExecutionContext<object>
	{
		/// <summary>
		/// For parallel queries we drain from left partition to right,
		/// then by rid order within those partitions.
		/// </summary>
		private sealed class ParllelDocumentProducerTreeComparer : IComparer<DocumentProducerTree<object>>
		{
			/// <summary>
			/// Compares two document producer trees in a parallel context and returns their comparison.
			/// </summary>
			/// <param name="documentProducerTree1">The first document producer tree.</param>
			/// <param name="documentProducerTree2">The second document producer tree.</param>
			/// <returns>
			/// A negative number if the first comes before the second.
			/// Zero if the two document producer trees are interchangeable.
			/// A positive number if the second comes before the first.
			/// </returns>
			public int Compare(DocumentProducerTree<object> documentProducerTree1, DocumentProducerTree<object> documentProducerTree2)
			{
				if (documentProducerTree1 == documentProducerTree2)
				{
					return 0;
				}
				if (documentProducerTree1.HasMoreResults && !documentProducerTree2.HasMoreResults)
				{
					return -1;
				}
				if (!documentProducerTree1.HasMoreResults && documentProducerTree2.HasMoreResults)
				{
					return 1;
				}
				PartitionKeyRange partitionKeyRange = documentProducerTree1.PartitionKeyRange;
				PartitionKeyRange partitionKeyRange2 = documentProducerTree2.PartitionKeyRange;
				return string.CompareOrdinal(partitionKeyRange.MinInclusive, partitionKeyRange2.MinInclusive);
			}
		}

		/// <summary>
		/// Comparer used to determine if we should return the continuation token to the user
		/// </summary>
		/// <remarks>This basically just says that the two object are never equals, so that we don't return a continuation for a partition we have started draining.</remarks>
		private sealed class ParallelEqualityComparer : IEqualityComparer<object>
		{
			/// <summary>
			/// Returns whether two parallel query items are equal.
			/// </summary>
			/// <param name="x">The first item.</param>
			/// <param name="y">The second item.</param>
			/// <returns>Whether two parallel query items are equal.</returns>
			public new bool Equals(object x, object y)
			{
				return x == y;
			}

			/// <summary>
			/// Gets the hash code of an object.
			/// </summary>
			/// <param name="obj">The object to hash.</param>
			/// <returns>The hash code for the object.</returns>
			public int GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
		}

		/// <summary>
		/// The comparer used to determine which document to serve next.
		/// </summary>
		private static readonly IComparer<DocumentProducerTree<object>> MoveNextComparer = new ParllelDocumentProducerTreeComparer();

		/// <summary>
		/// The function to determine which partition to fetch from first.
		/// </summary>
		private static readonly Func<DocumentProducerTree<object>, int> FetchPriorityFunction = (DocumentProducerTree<object> documentProducerTree) => int.Parse(documentProducerTree.PartitionKeyRange.Id);

		/// <summary>
		/// The comparer used to determine, which continuation tokens should be returned to the user.
		/// </summary>
		private static readonly IEqualityComparer<object> EqualityComparer = new ParallelEqualityComparer();

		/// <summary>
		/// For parallel queries the continuation token semantically holds two pieces of information:
		/// 1) What physical partition did the user read up to
		/// 2) How far into said partition did they read up to
		/// And since the client consumes queries strictly in a left to right order we can partition the documents:
		/// 1) Documents left of the continuation token have been drained
		/// 2) Documents to the right of the continuation token still need to be served.
		/// This is useful since we can have a single continuation token for all partitions.
		/// </summary>
		protected override string ContinuationToken
		{
			get
			{
				if (IsDone)
				{
					return null;
				}
				IEnumerable<DocumentProducer<object>> activeDocumentProducers = GetActiveDocumentProducers();
				if (activeDocumentProducers.Count() <= 0)
				{
					return null;
				}
				return JsonConvert.SerializeObject(from documentProducer in activeDocumentProducers
				select new CompositeContinuationToken
				{
					Token = documentProducer.PreviousContinuationToken,
					Range = documentProducer.PartitionKeyRange.ToRange()
				}, DefaultJsonSerializationSettings.Value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the ParallelDocumentQueryExecutionContext class.
		/// </summary>
		/// <param name="constructorParams">The parameters for constructing the base class.</param>
		/// <param name="rewrittenQuery">The rewritten query.</param>
		private ParallelDocumentQueryExecutionContext(InitParams constructorParams, string rewrittenQuery)
			: base(constructorParams, rewrittenQuery, MoveNextComparer, FetchPriorityFunction, EqualityComparer)
		{
		}

		/// <summary>
		/// Creates a ParallelDocumentQueryExecutionContext
		/// </summary>
		/// <param name="constructorParams">The params the construct the base class.</param>
		/// <param name="initParams">The params to initialize the cross partition context.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on, which in turn returns a ParallelDocumentQueryExecutionContext.</returns>
		public static async Task<ParallelDocumentQueryExecutionContext> CreateAsync(InitParams constructorParams, CrossPartitionQueryExecutionContext<dynamic>.CrossPartitionInitParams initParams, CancellationToken token)
		{
			ParallelDocumentQueryExecutionContext context = new ParallelDocumentQueryExecutionContext(constructorParams, initParams.PartitionedQueryExecutionInfo.QueryInfo.RewrittenQuery);
			await context.InitializeAsync(initParams.CollectionRid, initParams.PartitionKeyRanges, initParams.InitialPageSize, initParams.RequestContinuation, token);
			return context;
		}

		/// <summary>
		/// Drains documents from this execution context.
		/// </summary>
		/// <param name="maxElements">The maximum number of documents to drains.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task that when awaited on returns a FeedResponse of results.</returns>
		public override async Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token)
		{
			DocumentProducerTree<object> currentDocumentProducerTree = PopCurrentDocumentProducerTree();
			if (currentDocumentProducerTree.Current == null)
			{
				await currentDocumentProducerTree.MoveNextAsync(token);
			}
			int itemsLeftInCurrentPage = currentDocumentProducerTree.ItemsLeftInCurrentPage;
			List<object> results = new List<object>();
			for (int i = 0; i < Math.Min(itemsLeftInCurrentPage, maxElements); i++)
			{
				results.Add(currentDocumentProducerTree.Current);
				await currentDocumentProducerTree.MoveNextAsync(token);
			}
			PushCurrentDocumentProducerTree(currentDocumentProducerTree);
			return new FeedResponse<object>(results, results.Count, GetResponseHeaders(), useETagAsContinuation: false, GetQueryMetrics(), null, null, GetAndResetResponseLengthBytes());
		}

		/// <summary>
		/// Initialize the execution context.
		/// </summary>
		/// <param name="collectionRid">The collection rid.</param>
		/// <param name="partitionKeyRanges">The partition key ranges to drain documents from.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <param name="requestContinuation">The continuation token to resume from.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		private Task InitializeAsync(string collectionRid, List<PartitionKeyRange> partitionKeyRanges, int initialPageSize, string requestContinuation, CancellationToken token)
		{
			Dictionary<string, CompositeContinuationToken> targetRangeToContinuationMap = null;
			IReadOnlyList<PartitionKeyRange> partitionKeyRanges2;
			if (string.IsNullOrEmpty(requestContinuation))
			{
				partitionKeyRanges2 = partitionKeyRanges;
			}
			else
			{
				CompositeContinuationToken[] array = null;
				try
				{
					array = JsonConvert.DeserializeObject<CompositeContinuationToken[]>(requestContinuation, DefaultJsonSerializationSettings.Value);
					CompositeContinuationToken[] array2 = array;
					foreach (CompositeContinuationToken compositeContinuationToken in array2)
					{
						if (compositeContinuationToken.Range == null || compositeContinuationToken.Range.IsEmpty)
						{
							throw new BadRequestException($"Invalid Range in the continuation token {requestContinuation} for Parallel~Context.");
						}
					}
					if (array.Length == 0)
					{
						throw new BadRequestException($"Invalid format for continuation token {requestContinuation} for Parallel~Context.");
					}
				}
				catch (JsonException ex)
				{
					throw new BadRequestException($"Invalid JSON in continuation token {requestContinuation} for Parallel~Context, exception: {ex.Message}");
				}
				partitionKeyRanges2 = GetPartitionKeyRangesForContinuation(array, partitionKeyRanges, out targetRangeToContinuationMap);
			}
			return InitializeAsync(collectionRid, partitionKeyRanges2, initialPageSize, base.QuerySpec, targetRangeToContinuationMap?.ToDictionary((KeyValuePair<string, CompositeContinuationToken> kvp) => kvp.Key, (KeyValuePair<string, CompositeContinuationToken> kvp) => kvp.Value.Token), deferFirstPage: true, null, null, token);
		}

		/// <summary>
		/// Given a continuation token and a list of partitionKeyRanges this function will return a list of partition key ranges you should resume with.
		/// Note that the output list is just a right hand slice of the input list, since we know that for any continuation of a parallel query it is just
		/// resuming from the partition that the query left off that.
		/// </summary>
		/// <param name="suppliedCompositeContinuationTokens">The continuation tokens that the user has supplied.</param>
		/// <param name="partitionKeyRanges">The partition key ranges.</param>
		/// <param name="targetRangeToContinuationMap">The output dictionary of partition key ranges to continuation token.</param>
		/// <returns>The subset of partition to actually target.</returns>
		private IReadOnlyList<PartitionKeyRange> GetPartitionKeyRangesForContinuation(CompositeContinuationToken[] suppliedCompositeContinuationTokens, List<PartitionKeyRange> partitionKeyRanges, out Dictionary<string, CompositeContinuationToken> targetRangeToContinuationMap)
		{
			targetRangeToContinuationMap = new Dictionary<string, CompositeContinuationToken>();
			int num = FindTargetRangeAndExtractContinuationTokens(partitionKeyRanges, from token in suppliedCompositeContinuationTokens
			select Tuple.Create(token, token.Range), out targetRangeToContinuationMap);
			return new PartialReadOnlyList<PartitionKeyRange>(partitionKeyRanges, num, partitionKeyRanges.Count - num);
		}
	}
}
