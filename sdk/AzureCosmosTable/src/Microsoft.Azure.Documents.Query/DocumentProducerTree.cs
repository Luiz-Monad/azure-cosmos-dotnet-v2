using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// This class is responsible for fetching documents from a partition and all it's descendants, which is modeled as a tree of document producers.
	/// The root node is responsible for buffering documents from the root partition and the children recursively buffer documents for their corresponding partitions.
	/// The tree itself allows a user to iterate through it's documents using a comparator and Current / Move Next Async functions.
	/// Note that if a user wants to determine the current document it will take the max of it's buffered documents and the recursive max of it's children.
	/// Also note that if there are no buffered documents for any node in the recursive evaluation, then those nodes will go for a fetch.
	/// Finally note that due to the tree structure of this class it is inherently split proof.
	/// If any leaf node in the tree encounters a split exception it will spawn child document producer trees (any many as needed, so multiple splits is handled) and continue on as if the split never happened.
	/// This code does not handle merges, but we will cross that bridge when we have to (I am currently thinking about a linked list where the nodes represent document producers and you can merge adjacent nodes).
	/// As a implementation detail the documents are buffered and logically enumerated as a nested loop. The following is the pseudo code:
	/// for partition in document_producer_tree:
	///     for page in partition:
	///         for document in page:
	///             yield document.
	/// And the way this is done is by buffering pages and updating the state of the DocumentProducerTree whenever a user crosses a page boundary.
	/// </summary>
	/// <typeparam name="T">The type of document buffered.</typeparam>
	internal sealed class DocumentProducerTree<T> : IEnumerable<DocumentProducerTree<T>>, IEnumerable
	{
		/// <summary>
		/// Root of the document producer tree.
		/// </summary>
		private readonly DocumentProducer<T> root;

		/// <summary>
		/// The child partitions of this node in the tree that are added after a split.
		/// </summary>
		private readonly PriorityQueue<DocumentProducerTree<T>> children;

		/// <summary>
		/// Callback to create child document producer trees once a split happens.
		/// </summary>
		private readonly Func<PartitionKeyRange, string, DocumentProducerTree<T>> createDocumentProducerTreeCallback;

		/// <summary>
		/// The client that is used to get the routing map on a split.
		/// </summary>
		private readonly IDocumentQueryClient client;

		/// <summary>
		/// Whether or not to defer fetching the first page from all the partitions.
		/// </summary>
		private readonly bool deferFirstPage;

		/// <summary>
		/// The collection rid to to drain from. 
		/// </summary>
		private readonly string collectionRid;

		/// <summary>
		/// Semaphore to ensure mutual exclusion during fetching from a tree.
		/// This is to ensure that there is no race conditions during splits.
		/// </summary>
		private readonly SemaphoreSlim executeWithSplitProofingSemaphore;

		/// <summary>
		/// Gets the root document from the tree.
		/// </summary>
		public DocumentProducer<T> Root => root;

		/// <summary>
		/// Gets the partition key range from the current document producer tree.
		/// </summary>
		public PartitionKeyRange PartitionKeyRange
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.PartitionKeyRange;
				}
				return CurrentDocumentProducerTree.PartitionKeyRange;
			}
		}

		/// <summary>
		/// Gets or sets the filter for the current document producer tree.
		/// </summary>
		public string Filter
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.Filter;
				}
				return CurrentDocumentProducerTree.Filter;
			}
			set
			{
				if (CurrentDocumentProducerTree == this)
				{
					root.Filter = value;
				}
				else
				{
					CurrentDocumentProducerTree.Filter = value;
				}
			}
		}

		/// <summary>
		/// Gets the current (highest priority) document producer tree from all subtrees.
		/// </summary>
		public DocumentProducerTree<T> CurrentDocumentProducerTree
		{
			get
			{
				if (HasSplit && !root.HasMoreResults)
				{
					children.Enqueue(children.Dequeue());
					return children.Peek().CurrentDocumentProducerTree;
				}
				return this;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document producer tree is at the beginning of the page for the current document producer.
		/// </summary>
		public bool IsAtBeginningOfPage
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.IsAtBeginningOfPage;
				}
				return CurrentDocumentProducerTree.IsAtBeginningOfPage;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document producer tree has more results.
		/// </summary>
		public bool HasMoreResults
		{
			get
			{
				if (!root.HasMoreResults)
				{
					if (HasSplit)
					{
						return children.Peek().HasMoreResults;
					}
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document producer tree has more backend results.
		/// </summary>
		public bool HasMoreBackendResults
		{
			get
			{
				if (!root.HasMoreBackendResults)
				{
					if (HasSplit)
					{
						return children.Peek().HasMoreBackendResults;
					}
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Gets whether there are items left in the current page of the document producer tree.
		/// </summary>
		public int ItemsLeftInCurrentPage
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.ItemsLeftInCurrentPage;
				}
				return CurrentDocumentProducerTree.ItemsLeftInCurrentPage;
			}
		}

		/// <summary>
		/// Gets the buffered item count in the current document producer tree.
		/// </summary>
		public int BufferedItemCount
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.BufferedItemCount;
				}
				return CurrentDocumentProducerTree.BufferedItemCount;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document producer tree is active.
		/// </summary>
		public bool IsActive
		{
			get
			{
				if (!root.IsActive)
				{
					return children.Any((DocumentProducerTree<T> child) => child.IsActive);
				}
				return true;
			}
		}

		/// <summary>
		/// Gets or sets the page size for this document producer tree.
		/// </summary>
		public long PageSize
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.PageSize;
				}
				return CurrentDocumentProducerTree.PageSize;
			}
			set
			{
				if (CurrentDocumentProducerTree == this)
				{
					root.PageSize = value;
				}
				else
				{
					CurrentDocumentProducerTree.PageSize = value;
				}
			}
		}

		/// <summary>
		/// Gets the activity id from the current document producer tree.
		/// </summary>
		public Guid ActivityId
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.ActivityId;
				}
				return CurrentDocumentProducerTree.ActivityId;
			}
		}

		/// <summary>
		/// Gets the current item from the document producer tree.
		/// </summary>
		public T Current
		{
			get
			{
				if (CurrentDocumentProducerTree == this)
				{
					return root.Current;
				}
				return CurrentDocumentProducerTree.Current;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the document producer tree has split.
		/// </summary>
		private bool HasSplit => children.Count != 0;

		/// <summary>
		/// Initializes a new instance of the DocumentProducerTree class.
		/// </summary>
		/// <param name="partitionKeyRange">The partition key range.</param>
		/// <param name="createRequestFunc">Callback to create a request.</param>
		/// <param name="executeRequestFunc">Callback to execute a request.</param>
		/// <param name="createRetryPolicyFunc">Callback to create a retry policy.</param>
		/// <param name="produceAsyncCompleteCallback">Callback to invoke once a fetch finishes.</param>
		/// <param name="documentProducerTreeComparer">Comparer to determine, which tree to produce from.</param>
		/// <param name="equalityComparer">Comparer to see if we need to return the continuation token for a partition.</param>
		/// <param name="client">The client</param>
		/// <param name="deferFirstPage">Whether or not to defer fetching the first page.</param>
		/// <param name="collectionRid">The collection to drain from.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <param name="initialContinuationToken">The initial continuation token.</param>
		public DocumentProducerTree(PartitionKeyRange partitionKeyRange, Func<PartitionKeyRange, string, int, DocumentServiceRequest> createRequestFunc, Func<DocumentServiceRequest, IDocumentClientRetryPolicy, CancellationToken, Task<FeedResponse<T>>> executeRequestFunc, Func<IDocumentClientRetryPolicy> createRetryPolicyFunc, Action<DocumentProducerTree<T>, int, double, QueryMetrics, long, CancellationToken> produceAsyncCompleteCallback, IComparer<DocumentProducerTree<T>> documentProducerTreeComparer, IEqualityComparer<T> equalityComparer, IDocumentQueryClient client, bool deferFirstPage, string collectionRid, long initialPageSize = 50L, string initialContinuationToken = null)
		{
			if (documentProducerTreeComparer == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "documentProducerTreeComparer"));
			}
			if (createRequestFunc == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "createRequestFunc"));
			}
			if (executeRequestFunc == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "executeRequestFunc"));
			}
			if (createRetryPolicyFunc == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "createRetryPolicyFunc"));
			}
			if (produceAsyncCompleteCallback == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "produceAsyncCompleteCallback"));
			}
			if (documentProducerTreeComparer == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "documentProducerTreeComparer"));
			}
			if (equalityComparer == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "equalityComparer"));
			}
			if (client == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "client"));
			}
			if (string.IsNullOrEmpty(collectionRid))
			{
				throw new ArgumentException(string.Format("{0} can not be null or empty.", "collectionRid"));
			}
			root = new DocumentProducer<T>(partitionKeyRange, createRequestFunc, executeRequestFunc, createRetryPolicyFunc, delegate(DocumentProducer<T> documentProducer, int itemsBuffered, double resourceUnitUsage, QueryMetrics queryMetrics, long requestLength, CancellationToken token)
			{
				produceAsyncCompleteCallback(this, itemsBuffered, resourceUnitUsage, queryMetrics, requestLength, token);
			}, equalityComparer, initialPageSize, initialContinuationToken);
			children = new PriorityQueue<DocumentProducerTree<T>>(documentProducerTreeComparer, isSynchronized: true);
			this.deferFirstPage = deferFirstPage;
			this.client = client;
			this.collectionRid = collectionRid;
			createDocumentProducerTreeCallback = CreateDocumentProducerTreeCallback(createRequestFunc, executeRequestFunc, createRetryPolicyFunc, produceAsyncCompleteCallback, documentProducerTreeComparer, equalityComparer, client, deferFirstPage, collectionRid, initialPageSize);
			executeWithSplitProofingSemaphore = new SemaphoreSlim(1, 1);
		}

		[CompilerGenerated]
		private static class __o__38
		{
			public static CallSite<Func<CallSite, object, bool>> __p__0;
		}

		/// <summary>
		/// Moves to the next item in the document producer tree.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on that returns whether we successfully moved next.</returns>
		/// <remarks>This function is split proofed.</remarks>
		public async Task<bool> MoveNextAsync(CancellationToken token)
		{
			if (__o__38.__p__0 == null)
			{
				__o__38.__p__0 = CallSite<Func<CallSite, object, bool>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(bool), typeof(DocumentProducerTree<T>)));
			}
			Func<CallSite, object, bool> target = __o__38.__p__0.Target;
			CallSite<Func<CallSite, object, bool>> _003C_003Ep__ = __o__38.__p__0;
			return target(_003C_003Ep__, await ExecuteWithSplitProofing(new Func<CancellationToken, Task<object>>(MoveNextAsyncImplementation), functionNeedsBeReexecuted: false, token));
		}

		[CompilerGenerated]
		private static class __o__39
		{
			public static CallSite<Func<CallSite, object, bool>> __p__0;
		}

		/// <summary>
		/// Moves next only if the producer has not split.
		/// This is used to avoid calling move next twice during splits.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on which in turn returns whether or not we moved next.</returns>
		public async Task<bool> MoveNextIfNotSplit(CancellationToken token)
		{
			if (__o__39.__p__0 == null)
			{
				__o__39.__p__0 = CallSite<Func<CallSite, object, bool>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(bool), typeof(DocumentProducerTree<T>)));
			}
			Func<CallSite, object, bool> target = __o__39.__p__0.Target;
			CallSite<Func<CallSite, object, bool>> _003C_003Ep__ = __o__39.__p__0;
			return target(_003C_003Ep__, await ExecuteWithSplitProofing(new Func<CancellationToken, Task<object>>(MoveNextIfNotSplitAsyncImplementation), functionNeedsBeReexecuted: false, token));
		}

		/// <summary>
		/// Buffers more documents in a split proof manner.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		public Task BufferMoreDocuments(CancellationToken token)
		{
			return ExecuteWithSplitProofing(new Func<CancellationToken, Task<object>>(BufferMoreDocumentsImplementation), functionNeedsBeReexecuted: true, token);
		}

		/// <summary>
		/// Gets the document producers that need their continuation token return to the user.
		/// </summary>
		/// <returns>The document producers that need their continuation token return to the user.</returns>
		public IEnumerable<DocumentProducer<T>> GetActiveDocumentProducers()
		{
			if (!HasSplit)
			{
				if (root.IsActive)
				{
					yield return root;
				}
			}
			else if (root.IsActive && root.BufferedItemCount != 0)
			{
				yield return root;
			}
			else
			{
				foreach (DocumentProducerTree<T> child in children)
				{
					foreach (DocumentProducer<T> activeDocumentProducer in child.GetActiveDocumentProducers())
					{
						yield return activeDocumentProducer;
					}
				}
			}
		}

		/// <summary>
		/// Gets the enumerator for all the leaf level document producers.
		/// </summary>
		/// <returns>The enumerator for all the leaf level document producers.</returns>
		public IEnumerator<DocumentProducerTree<T>> GetEnumerator()
		{
			if (children.Count == 0)
			{
				yield return this;
			}
			foreach (DocumentProducerTree<T> child in children)
			{
				foreach (DocumentProducerTree<T> item in child)
				{
					yield return item;
				}
			}
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Callback to create a child document producer tree based on the partition key range.
		/// </summary>
		/// <param name="createRequestFunc">Callback to create a request.</param>
		/// <param name="executeRequestFunc">Callback to execute a request.</param>
		/// <param name="createRetryPolicyFunc">Callback to create a retry policy.</param>
		/// <param name="produceAsyncCompleteCallback">Callback to invoke once a fetch finishes.</param>
		/// <param name="documentProducerTreeComparer">Comparer to determine, which tree to produce from.</param>
		/// <param name="equalityComparer">Comparer to see if we need to return the continuation token for a partition.</param>
		/// <param name="documentClient">The client</param>
		/// <param name="deferFirstPage">Whether or not to defer fetching the first page.</param>
		/// <param name="collectionRid">The collection to drain from.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <returns>A function that given a partition key range and continuation token will create a document producer.</returns>
		private static Func<PartitionKeyRange, string, DocumentProducerTree<T>> CreateDocumentProducerTreeCallback(Func<PartitionKeyRange, string, int, DocumentServiceRequest> createRequestFunc, Func<DocumentServiceRequest, IDocumentClientRetryPolicy, CancellationToken, Task<FeedResponse<T>>> executeRequestFunc, Func<IDocumentClientRetryPolicy> createRetryPolicyFunc, Action<DocumentProducerTree<T>, int, double, QueryMetrics, long, CancellationToken> produceAsyncCompleteCallback, IComparer<DocumentProducerTree<T>> documentProducerTreeComparer, IEqualityComparer<T> equalityComparer, IDocumentQueryClient documentClient, bool deferFirstPage, string collectionRid, long initialPageSize = 50L)
		{
			return (PartitionKeyRange partitionKeyRange, string continuationToken) => new DocumentProducerTree<T>(partitionKeyRange, createRequestFunc, executeRequestFunc, createRetryPolicyFunc, produceAsyncCompleteCallback, documentProducerTreeComparer, equalityComparer, documentClient, deferFirstPage, collectionRid, initialPageSize, continuationToken);
		}

		/// <summary>
		/// Given a document client exception this function determines whether it was caused due to a split.
		/// </summary>
		/// <param name="ex">The document client exception</param>
		/// <returns>Whether or not the exception was due to a split.</returns>
		private static bool IsSplitException(DocumentClientException ex)
		{
			if (ex.StatusCode == HttpStatusCode.Gone)
			{
				return ex.GetSubStatus() == SubStatusCodes.PartitionKeyRangeGone;
			}
			return false;
		}

		/// <summary>
		/// Implementation for moving to the next item in the document producer tree.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task with whether or not move next succeeded.</returns>
		private async Task<dynamic> MoveNextAsyncImplementation(CancellationToken token)
		{
			if (!HasMoreResults)
			{
				return false;
			}
			if (CurrentDocumentProducerTree == this)
			{
				return await root.MoveNextAsync(token);
			}
			return await CurrentDocumentProducerTree.MoveNextAsync(token);
		}

		/// <summary>
		/// Implementation for moving next if the tree has not split.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on which in turn return whether we successfully moved next.</returns>
		private Task<dynamic> MoveNextIfNotSplitAsyncImplementation(CancellationToken token)
		{
			if (HasSplit)
			{
				return Task.FromResult((object)false);
			}
			return MoveNextAsyncImplementation(token);
		}

		/// <summary>
		/// Implementation for buffering more documents.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		private async Task<object> BufferMoreDocumentsImplementation(CancellationToken token)
		{
			if (CurrentDocumentProducerTree != this)
			{
				await CurrentDocumentProducerTree.BufferMoreDocuments(token);
			}
			else
			{
				if (!HasMoreBackendResults || HasSplit)
				{
					return null;
				}
				await root.BufferMoreDocuments(token);
			}
			return null;
		}

		/// <summary>
		/// This function will execute any function in a split proof manner.
		/// What it does is it will try to execute the supplied function and catch any gone exceptions do to a split.
		/// If a split happens when this function will 
		/// </summary>
		/// <param name="function">The function to execute in a split proof manner.</param>
		/// <param name="functionNeedsBeReexecuted">If the function needs to be reexecuted after split.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <remarks>
		/// <para>
		/// This function is thread safe meaning that if multiple functions want to execute in a split proof manner,
		/// then they will need to go one after another.
		/// This is required since you could have the follow scenario:
		/// Time    | CurrentDocumentProducer   | Thread 1      | Thread2
		/// 0       | 0                         | MoveNextAsync | BufferMore
		/// 1       | 0                         | Split         | Split
		/// </para>
		/// <para>
		/// Therefore thread 1 and thread 2 both think that document producer 0 got split and they both try to repair the execution context,
		/// which is a race condition.
		/// Note that this thread safety / serial behavior is only scoped to a single document producer tree
		/// meaning this should not have a performance hit on the scheduler that is prefetching from other partitions.
		/// </para>
		/// </remarks>
		/// <returns>The result of the function would have returned as if there were no splits.</returns>
		private async Task<dynamic> ExecuteWithSplitProofing(Func<CancellationToken, Task<dynamic>> function, bool functionNeedsBeReexecuted, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			while (true)
			{
				try
				{
					await executeWithSplitProofingSemaphore.WaitAsync();
					return await function(cancellationToken);
				}
				catch (DocumentClientException ex) when (IsSplitException(ex))
				{
					DocumentProducerTree<T> splitDocumentProducerTree = CurrentDocumentProducerTree;
					if (splitDocumentProducerTree.BufferedItemCount == 0)
					{
						splitDocumentProducerTree.Root.Shutdown();
					}
					foreach (PartitionKeyRange item in await GetReplacementRanges(splitDocumentProducerTree.PartitionKeyRange, collectionRid))
					{
						DocumentProducerTree<T> replacementDocumentProducerTree = createDocumentProducerTreeCallback(item, splitDocumentProducerTree.root.BackendContinuationToken);
						if (!deferFirstPage)
						{
							await replacementDocumentProducerTree.MoveNextAsync(cancellationToken);
						}
						replacementDocumentProducerTree.Filter = splitDocumentProducerTree.root.Filter;
						if (replacementDocumentProducerTree.HasMoreResults && !splitDocumentProducerTree.children.TryAdd(replacementDocumentProducerTree))
						{
							throw new InvalidOperationException("Unable to add child document producer tree");
						}
					}
					if (!functionNeedsBeReexecuted)
					{
						return true;
					}
				}
				finally
				{
					executeWithSplitProofingSemaphore.Release();
				}
			}
		}

		/// <summary>
		/// Gets the replacement ranges for the target range that got split.
		/// </summary>
		/// <param name="targetRange">The target range that got split.</param>
		/// <param name="collectionRid">The collection rid.</param>
		/// <returns>The replacement ranges for the target range that got split.</returns>
		private async Task<List<PartitionKeyRange>> GetReplacementRanges(PartitionKeyRange targetRange, string collectionRid)
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
	}
}
