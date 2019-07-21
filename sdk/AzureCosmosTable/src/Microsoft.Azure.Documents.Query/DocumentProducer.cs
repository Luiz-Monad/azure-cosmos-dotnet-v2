using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// The DocumentProducer is the base unit of buffering and iterating through documents.
	/// Note that a document producer will let you iterate through documents within the pages of a partition and maintain any state.
	/// In pseudo code this works out to:
	/// for page in partition:
	///     for document in page:
	///         yield document
	///     update_state()
	/// </summary>
	/// <typeparam name="T">The type of document buffered.</typeparam>
	internal sealed class DocumentProducer<T>
	{
		public delegate void ProduceAsyncCompleteDelegate(DocumentProducer<T> producer, int numberOfDocuments, double requestCharge, QueryMetrics queryMetrics, long responseLengthInBytes, CancellationToken token);

		private struct TryMonad<TResult>
		{
			private readonly TResult result;

			private readonly ExceptionDispatchInfo exceptionDispatchInfo;

			private readonly bool succeeded;

			private TryMonad(TResult result, ExceptionDispatchInfo exceptionDispatchInfo, bool succeeded)
			{
				this.result = result;
				this.exceptionDispatchInfo = exceptionDispatchInfo;
				this.succeeded = succeeded;
			}

			public static TryMonad<TResult> FromResult(TResult result)
			{
				return new TryMonad<TResult>(result, null, succeeded: true);
			}

			public static TryMonad<TResult> FromException(Exception exception)
			{
				return new TryMonad<TResult>(default(TResult), ExceptionDispatchInfo.Capture(exception), succeeded: false);
			}

			public TOutput Match<TOutput>(Func<TResult, TOutput> onSuccess, Func<ExceptionDispatchInfo, TOutput> onError)
			{
				if (succeeded)
				{
					return onSuccess(result);
				}
				return onError(exceptionDispatchInfo);
			}
		}

		/// <summary>
		/// The buffered pages that is thread safe, since the producer and consumer of the queue can be on different threads.
		/// We buffer TryMonad of FeedResponse of T, since we want to buffer exceptions,
		/// so that the exception is thrown on the consumer thread (instead of the background producer thread), thus observing the exception.
		/// </summary>
		private readonly AsyncCollection<TryMonad<FeedResponse<T>>> bufferedPages;

		/// <summary>
		/// The document producer can only be fetching one page at a time.
		/// Since the fetch function can be called by the execution contexts or the scheduler, we use this semaphore to keep the fetch function thread safe.
		/// </summary>
		private readonly SemaphoreSlim fetchSemaphore;

		/// <summary>
		/// The callback function used to create a <see cref="T:Microsoft.Azure.Documents.DocumentServiceRequest" /> that is the entry point to fetch documents from the backend.
		/// </summary>
		private readonly Func<PartitionKeyRange, string, int, DocumentServiceRequest> createRequestFunc;

		/// <summary>
		/// The callback used to take a <see cref="T:Microsoft.Azure.Documents.DocumentServiceRequest" /> and retrieve a page of documents as a <see cref="T:Microsoft.Azure.Documents.Client.FeedResponse`1" />
		/// </summary>
		private readonly Func<DocumentServiceRequest, IDocumentClientRetryPolicy, CancellationToken, Task<FeedResponse<T>>> executeRequestFunc;

		/// <summary>
		/// Callback used to create a retry policy that will be used to determine when and how to retry fetches.
		/// </summary>
		private readonly Func<IDocumentClientRetryPolicy> createRetryPolicyFunc;

		/// <summary>
		/// Once a document producer tree finishes fetching document they should call on this function so that the higher level execution context can aggregate the number of documents fetched, the request charge, and the query metrics.
		/// </summary>
		private readonly ProduceAsyncCompleteDelegate produceAsyncCompleteCallback;

		/// <summary>
		/// Keeps track of when a fetch happens and ends to calculate scheduling metrics.
		/// </summary>
		private readonly SchedulingStopwatch fetchSchedulingMetrics;

		/// <summary>
		/// Keeps track of fetch ranges.
		/// </summary>
		private readonly FetchExecutionRangeAccumulator fetchExecutionRangeAccumulator;

		/// <summary>
		/// Equality comparer to determine if you have come across a distinct document according to the sort order.
		/// </summary>
		private readonly IEqualityComparer<T> equalityComparer;

		/// <summary>
		/// The current element in the iteration.
		/// </summary>
		private T current;

		/// <summary>
		/// Over the duration of the life time of a document producer the page size will change, since we have an adaptive page size.
		/// </summary>
		private long pageSize;

		/// <summary>
		/// Previous continuation token for the page that the user has read from the document producer.
		/// This is used for generating the composite continuation token.
		/// </summary>
		private string previousContinuationToken;

		/// <summary>
		/// The current continuation token that the user has read from the document producer tree.
		/// This is used for determining whether there are more results.
		/// </summary>
		private string currentContinuationToken;

		/// <summary>
		/// The current backend continuation token, since the document producer tree may buffer multiple pages ahead of the consumer.
		/// </summary>
		private string backendContinuationToken;

		/// <summary>
		/// The number of items left in the current page, which is used by parallel queries since they need to drain full pages.
		/// </summary>
		private int itemsLeftInCurrentPage;

		/// <summary>
		/// The number of items currently buffered, which is used by the scheduler incase you want to implement give less full document producers a higher priority.
		/// </summary>
		private long bufferedItemCount;

		/// <summary>
		/// The current page that is being enumerated.
		/// </summary>
		private IEnumerator<T> currentPage;

		/// <summary>
		/// The last activity id seen from the backend.
		/// </summary>
		private Guid activityId;

		/// <summary>
		/// Whether or not the document producer has started fetching.
		/// </summary>
		private bool hasStartedFetching;

		/// <summary>
		/// Whether or not the document producer has started fetching.
		/// </summary>
		private bool hasMoreResults;

		/// <summary>
		/// Filter predicate for the document producer that is used by order by execution context.
		/// </summary>
		private string filter;

		/// <summary>
		/// Whether we are at the beginning of the page. This is needed for order by queries in order to determine if we need to skip a document for a join.
		/// </summary>
		private bool isAtBeginningOfPage;

		/// <summary>
		/// Whether or not we need to emit a continuation token for this document producer at the end of a continuation.
		/// </summary>
		private bool isActive;

		/// <summary>
		/// Need this flag so that the document producer stops buffering more results after a fatal exception.
		/// </summary>
		private bool documentProducerHitException;

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.Query.DocumentProducer`1.PartitionKeyRange" /> for the partition that this document producer is fetching from.
		/// </summary>
		public PartitionKeyRange PartitionKeyRange
		{
			get;
		}

		/// <summary>
		/// Gets or sets the filter predicate for the document producer that is used by order by execution context.
		/// </summary>
		public string Filter
		{
			get
			{
				return filter;
			}
			set
			{
				filter = value;
			}
		}

		/// <summary>
		/// Gets the previous continuation token.
		/// </summary>
		public string PreviousContinuationToken => previousContinuationToken;

		/// <summary>
		/// Gets the backend continuation token.
		/// </summary>
		public string BackendContinuationToken => backendContinuationToken;

		/// <summary>
		/// Gets a value indicating whether the continuation token for this producer needs to be given back as part of the composite continuation token.
		/// </summary>
		public bool IsActive => isActive;

		/// <summary>
		/// Gets a value indicating whether this producer is at the beginning of the page.
		/// </summary>
		public bool IsAtBeginningOfPage => isAtBeginningOfPage;

		/// <summary>
		/// Gets a value indicating whether this producer has more results.
		/// </summary>
		public bool HasMoreResults => hasMoreResults;

		/// <summary>
		/// Gets a value indicating whether this producer has more backend results.
		/// </summary>
		public bool HasMoreBackendResults
		{
			get
			{
				if (hasStartedFetching)
				{
					if (hasStartedFetching)
					{
						return !string.IsNullOrEmpty(backendContinuationToken);
					}
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Gets how many items are left in the current page.
		/// </summary>
		public int ItemsLeftInCurrentPage => itemsLeftInCurrentPage;

		/// <summary>
		/// Gets how many documents are buffered in this producer.
		/// </summary>
		public int BufferedItemCount => (int)bufferedItemCount;

		/// <summary>
		/// Gets or sets the page size of this producer.
		/// </summary>
		public long PageSize
		{
			get
			{
				return pageSize;
			}
			set
			{
				pageSize = value;
			}
		}

		/// <summary>
		/// Gets the activity for the last request made by this document producer.
		/// </summary>
		public Guid ActivityId => activityId;

		/// <summary>
		/// Gets the current document in this producer.
		/// </summary>
		public T Current => current;

		/// <summary>
		/// Initializes a new instance of the DocumentProducer class.
		/// </summary>
		/// <param name="partitionKeyRange">The partition key range.</param>
		/// <param name="createRequestFunc">The callback to create a request.</param>
		/// <param name="executeRequestFunc">The callback to execute the request.</param>
		/// <param name="createRetryPolicyFunc">The callback to create the retry policy.</param>
		/// <param name="produceAsyncCompleteCallback">The callback to call once you are done fetching.</param>
		/// <param name="equalityComparer">The comparer to use to determine whether the producer has seen a new document.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <param name="initialContinuationToken">The initial continuation token.</param>
		public DocumentProducer(PartitionKeyRange partitionKeyRange, Func<PartitionKeyRange, string, int, DocumentServiceRequest> createRequestFunc, Func<DocumentServiceRequest, IDocumentClientRetryPolicy, CancellationToken, Task<FeedResponse<T>>> executeRequestFunc, Func<IDocumentClientRetryPolicy> createRetryPolicyFunc, ProduceAsyncCompleteDelegate produceAsyncCompleteCallback, IEqualityComparer<T> equalityComparer, long initialPageSize = 50L, string initialContinuationToken = null)
		{
			bufferedPages = new AsyncCollection<TryMonad<FeedResponse<T>>>();
			fetchSemaphore = new SemaphoreSlim(1, 1);
			if (partitionKeyRange == null)
			{
				throw new ArgumentNullException("partitionKeyRange");
			}
			if (createRequestFunc == null)
			{
				throw new ArgumentNullException("createRequestFunc");
			}
			if (executeRequestFunc == null)
			{
				throw new ArgumentNullException("executeRequestFunc");
			}
			if (createRetryPolicyFunc == null)
			{
				throw new ArgumentNullException("createRetryPolicyFunc");
			}
			if (produceAsyncCompleteCallback == null)
			{
				throw new ArgumentNullException("produceAsyncCompleteCallback");
			}
			if (equalityComparer == null)
			{
				throw new ArgumentNullException("equalityComparer");
			}
			PartitionKeyRange = partitionKeyRange;
			this.createRequestFunc = createRequestFunc;
			this.executeRequestFunc = executeRequestFunc;
			this.createRetryPolicyFunc = createRetryPolicyFunc;
			this.produceAsyncCompleteCallback = produceAsyncCompleteCallback;
			this.equalityComparer = equalityComparer;
			pageSize = initialPageSize;
			currentContinuationToken = initialContinuationToken;
			backendContinuationToken = initialContinuationToken;
			previousContinuationToken = initialContinuationToken;
			if (!string.IsNullOrEmpty(initialContinuationToken))
			{
				hasStartedFetching = true;
				isActive = true;
			}
			fetchSchedulingMetrics = new SchedulingStopwatch();
			fetchSchedulingMetrics.Ready();
			fetchExecutionRangeAccumulator = new FetchExecutionRangeAccumulator();
			hasMoreResults = true;
		}

		/// <summary>
		/// Moves to the next document in the producer.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>Whether or not we successfully moved to the next document.</returns>
		public async Task<bool> MoveNextAsync(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			T originalCurrent = current;
			bool num = await MoveNextAsyncImplementation(token);
			if (!num || (originalCurrent != null && !equalityComparer.Equals(originalCurrent, current)))
			{
				isActive = false;
			}
			return num;
		}

		/// <summary>
		/// Buffers more documents if the producer is empty.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		public async Task BufferMoreIfEmpty(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			if (bufferedPages.Count == 0)
			{
				await BufferMoreDocuments(token);
			}
		}

		/// <summary>
		/// Buffers more documents in the producer.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on.</returns>
		public async Task BufferMoreDocuments(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			try
			{
				await fetchSemaphore.WaitAsync();
				if (HasMoreBackendResults && !documentProducerHitException)
				{
					fetchSchedulingMetrics.Start();
					fetchExecutionRangeAccumulator.BeginFetchRange();
					int arg = (int)Math.Min(pageSize, 2147483647L);
					using (DocumentServiceRequest request = createRequestFunc(PartitionKeyRange, backendContinuationToken, arg))
					{
						IDocumentClientRetryPolicy retryPolicy = createRetryPolicyFunc();
						retryPolicy.OnBeforeSendRequest(request);
						int retries = 0;
						while (true)
						{
							try
							{
								FeedResponse<T> feedResponse = await executeRequestFunc(request, retryPolicy, token);
								fetchExecutionRangeAccumulator.EndFetchRange(PartitionKeyRange.Id, feedResponse.ActivityId, feedResponse.Count, retries);
								fetchSchedulingMetrics.Stop();
								hasStartedFetching = true;
								backendContinuationToken = feedResponse.ResponseContinuation;
								activityId = Guid.Parse(feedResponse.ActivityId);
								await bufferedPages.AddAsync(TryMonad<FeedResponse<T>>.FromResult(feedResponse));
								Interlocked.Add(ref bufferedItemCount, feedResponse.Count);
								QueryMetrics queryMetrics = QueryMetrics.Zero;
								if (feedResponse.ResponseHeaders["x-ms-documentdb-query-metrics"] != null)
								{
									queryMetrics = QueryMetrics.CreateFromDelimitedStringAndClientSideMetrics(feedResponse.ResponseHeaders["x-ms-documentdb-query-metrics"], new ClientSideMetrics(retries, feedResponse.RequestCharge, fetchExecutionRangeAccumulator.GetExecutionRanges(), new List<Tuple<string, SchedulingTimeSpan>>()));
								}
								if (!HasMoreBackendResults)
								{
									queryMetrics = QueryMetrics.CreateWithSchedulingMetrics(queryMetrics, new List<Tuple<string, SchedulingTimeSpan>>
									{
										new Tuple<string, SchedulingTimeSpan>(PartitionKeyRange.Id, fetchSchedulingMetrics.Elapsed)
									});
								}
								produceAsyncCompleteCallback(this, feedResponse.Count, feedResponse.RequestCharge, queryMetrics, feedResponse.ResponseLengthBytes, token);
							}
							catch (Exception exception)
							{
								ShouldRetryResult shouldRetryResult = await retryPolicy.ShouldRetryAsync(exception, token);
								if (shouldRetryResult.ShouldRetry)
								{
									await Task.Delay(shouldRetryResult.BackoffTime);
									retries++;
									continue;
								}
								Exception exception2 = (shouldRetryResult.ExceptionToThrow == null) ? exception : shouldRetryResult.ExceptionToThrow;
								await bufferedPages.AddAsync(TryMonad<FeedResponse<T>>.FromException(exception2));
								documentProducerHitException = true;
							}
							break;
						}
					}
				}
			}
			finally
			{
				fetchSchedulingMetrics.Stop();
				fetchSemaphore.Release();
			}
		}

		public void Shutdown()
		{
			hasMoreResults = false;
		}

		/// <summary>
		/// Implementation of move next async.
		/// After this function is called the wrapper function determines if a distinct document has been read and updates the 'isActive' flag.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>Whether or not we successfully moved to the next document in the producer.</returns>
		private async Task<bool> MoveNextAsyncImplementation(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			if (!HasMoreResults)
			{
				return false;
			}
			if (MoveNextDocumentWithinCurrentPage())
			{
				return true;
			}
			if (await MoveNextPage(token))
			{
				return true;
			}
			hasMoreResults = false;
			return false;
		}

		private bool MoveToFirstDocumentInPage()
		{
			if (currentPage == null || !currentPage.MoveNext())
			{
				return false;
			}
			current = currentPage.Current;
			isAtBeginningOfPage = true;
			return true;
		}

		/// <summary>
		/// Tries to moved to the next document within the current page that we are reading from.
		/// </summary>
		/// <returns>Whether the operation was successful.</returns>
		private bool MoveNextDocumentWithinCurrentPage()
		{
			if (currentPage == null)
			{
				return false;
			}
			bool result = currentPage.MoveNext();
			current = currentPage.Current;
			isAtBeginningOfPage = false;
			Interlocked.Decrement(ref bufferedItemCount);
			Interlocked.Decrement(ref itemsLeftInCurrentPage);
			return result;
		}

		/// <summary>
		/// Tries to the move to the next page in the document producer.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>Whether the operation was successful.</returns>
		private async Task<bool> MoveNextPage(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			if (itemsLeftInCurrentPage != 0)
			{
				throw new InvalidOperationException("Tried to move onto the next page before finishing the first page.");
			}
			await BufferMoreIfEmpty(token);
			if (bufferedPages.Count == 0)
			{
				return false;
			}
			FeedResponse<T> feedResponse = (await bufferedPages.TakeAsync(token)).Match((FeedResponse<T> page) => page, delegate(ExceptionDispatchInfo exceptionDispatchInfo)
			{
				exceptionDispatchInfo.Throw();
				return null;
			});
			previousContinuationToken = currentContinuationToken;
			currentContinuationToken = feedResponse.ResponseContinuation;
			currentPage = feedResponse.GetEnumerator();
			isAtBeginningOfPage = true;
			itemsLeftInCurrentPage = feedResponse.Count;
			if (MoveToFirstDocumentInPage())
			{
				return true;
			}
			if (currentContinuationToken != null)
			{
				return await MoveNextPage(token);
			}
			return false;
		}

		[StructLayout(LayoutKind.Auto)]
		[CompilerGenerated]
		private struct _CreateFaultedTask_d__63 : IAsyncStateMachine
		{
			public int __1__state;

			public AsyncTaskMethodBuilder<FeedResponse<T>> __t__builder;

			public Exception exception;

			private TaskAwaiter<int> __u__1;

			private void MoveNext()
			{
				int num = __1__state;
				try
				{
					TaskAwaiter<int> awaiter;
					if (num == 0)
					{
						awaiter = __u__1;
						__u__1 = default(TaskAwaiter<int>);
						num = (__1__state = -1);
						goto IL_005b;
					}
					awaiter = Task.FromResult(0).GetAwaiter();
					if (awaiter.IsCompleted)
					{
						goto IL_005b;
					}
					num = (__1__state = 0);
					__u__1 = awaiter;
					__t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
					goto end_IL_0007;
					IL_005b:
					awaiter.GetResult();
					throw exception;
					end_IL_0007:;
				}
				catch (Exception ex)
				{
					__1__state = -2;
					__t__builder.SetException(ex);
				}
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				__t__builder.SetStateMachine(stateMachine);
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[AsyncStateMachine(typeof(DocumentProducer<>._CreateFaultedTask_d__63))]
		private static Task<FeedResponse<T>> CreateFaultedTask(Exception exception)
		{
			_CreateFaultedTask_d__63 stateMachine = default(_CreateFaultedTask_d__63);
			stateMachine.exception = exception;
			stateMachine.__t__builder = AsyncTaskMethodBuilder<FeedResponse<T>>.Create();
			stateMachine.__1__state = -1;
			AsyncTaskMethodBuilder<FeedResponse<T>> __t__builder = stateMachine.__t__builder;
			__t__builder.Start(ref stateMachine);
			return stateMachine.__t__builder.Task;
		}
	}
}
