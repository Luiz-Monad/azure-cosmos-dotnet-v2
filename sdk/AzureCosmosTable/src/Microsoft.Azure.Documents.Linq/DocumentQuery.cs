using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents.Query;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Linq
{

    internal sealed class DocumentQuery<T> : IDocumentQuery<T>, IDocumentQuery, IDisposable, IOrderedQueryable<T>, IEnumerable<T>, IEnumerable, IOrderedQueryable, IQueryable, IQueryable<T>
    {

        [CompilerGenerated]
        private static class __o__35
        {
            public static CallSite<Func<CallSite, object, FeedResponse<T>>> __p__0;
        }

        [StructLayout(LayoutKind.Auto)]
        [CompilerGenerated]
        private struct _ExecuteAllAsync_d__35 : IAsyncStateMachine
        {
            [CompilerGenerated]
            private sealed class __c__DisplayClass35_0
            {
                public DocumentQuery<T> __4__this;

                public CancellationToken cancellationToken;

                internal Task<IDocumentQueryExecutionContext> _ExecuteAllAsync_b__0()
                {
                    return __4__this.CreateDocumentQueryExecutionContextAsync(false, cancellationToken);
                }
            }

            [CompilerGenerated]
            private sealed class __c__DisplayClass35_1
            {
                public IDocumentQueryExecutionContext localQueryExecutionContext;

                public __c__DisplayClass35_0 CS___8__locals1;

                public Func<Task<FeedResponse<object>>> __9__1 = null;

                internal Task<FeedResponse<dynamic>> _ExecuteAllAsync_b__1()
                {
                    return localQueryExecutionContext.ExecuteNextAsync(CS___8__locals1.cancellationToken);
                }
            }

            public int __1__state;

            public AsyncTaskMethodBuilder<List<T>> __t__builder;

            public DocumentQuery<T> __4__this;

            public CancellationToken cancellationToken;

            private __c__DisplayClass35_1 __8__1;

            private List<T> _result_5__2;

            private TaskAwaiter<IDocumentQueryExecutionContext> __u__1;

            private Func<CallSite, object, FeedResponse<T>> __7__wrap2;

            private CallSite<Func<CallSite, object, FeedResponse<T>>> __7__wrap3;

            private Nullable<TaskAwaiter<FeedResponse<dynamic>>> __u__2;

            private void MoveNext()
            {
                int num = __1__state;
                List<T> result2;
                try
                {
                    CancellationToken cancellationToken;
                    TaskAwaiter<IDocumentQueryExecutionContext> awaiter;
                    if (num != 0)
                    {
                        if (num == 1)
                        {
                            goto IL_00e5;
                        }
			            DocumentQuery<T> documentQuery = __4__this;
                        __c__DisplayClass35_0 __c__DisplayClass35_ = new __c__DisplayClass35_0();
                        __c__DisplayClass35_.__4__this = __4__this;
                        __c__DisplayClass35_.cancellationToken = cancellationToken;
                        _result_5__2 = new List<T>();
                        __8__1 = new __c__DisplayClass35_1();
                        __8__1.CS___8__locals1 = __c__DisplayClass35_;
                        awaiter = TaskHelper.InlineIfPossible(() => documentQuery.CreateDocumentQueryExecutionContextAsync(false, cancellationToken), null, cancellationToken).GetAwaiter();
                        if (!awaiter.IsCompleted)
                        {
                            num = (__1__state = 0);
                            __u__1 = awaiter;
                            __t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                            return;
                        }
                    }
                    else
                    {
                        awaiter = __u__1;
                        __u__1 = default(TaskAwaiter<IDocumentQueryExecutionContext>);
                        num = (__1__state = -1);
                    }
                    IDocumentQueryExecutionContext result = awaiter.GetResult();
                    __8__1.localQueryExecutionContext = result;
                    goto IL_00e5;
                    IL_00e5:
                    try
                    {
                        TaskAwaiter<FeedResponse<dynamic>> val;
                        if (num == 1)
                        {
                            val = __u__2.Value;
                            __u__2 = null;
                            num = (__1__state = -1);
                            goto IL_02c1;
                        }
                        goto IL_0340;
                        IL_0340:
                        if (!__8__1.localQueryExecutionContext.IsDone)
                        {
                            if (DocumentQuery<T>.__o__35.__p__0 == null)
                            {
                                DocumentQuery<T>.__o__35.__p__0 = CallSite<Func<CallSite, object, FeedResponse<T>>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(FeedResponse<T>), typeof(DocumentQuery<T>)));
                            }
                            __7__wrap2 = DocumentQuery<T>.__o__35.__p__0.Target;
                            __7__wrap3 = DocumentQuery<T>.__o__35.__p__0;
                            var __8__1 = this.__8__1;
                            val = (TaskHelper.InlineIfPossible(() => __8__1.localQueryExecutionContext.ExecuteNextAsync(cancellationToken), null, cancellationToken)).GetAwaiter();
                            if (!val.IsCompleted)
                            {
                                num = (__1__state = 1);
                                __u__2 = val;
                                ICriticalNotifyCompletion awaiter2 = val as ICriticalNotifyCompletion;
                                if (awaiter2 == null)
                                {
                                    INotifyCompletion awaiter3 = (INotifyCompletion)val;
                                    __t__builder.AwaitOnCompleted(ref awaiter3, ref this);
                                    awaiter3 = null;
                                }
                                else
                                {
                                    __t__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
                                }
                                awaiter2 = null;
                                return;
                            }
                            goto IL_02c1;
                        }
                        goto end_IL_00e6;
                        IL_02c1:
                        var arg = val.GetResult();
                        FeedResponse<T> collection = __7__wrap2(__7__wrap3, arg);
                        __7__wrap2 = null;
                        __7__wrap3 = null;
                        _result_5__2.AddRange(collection);
                        goto IL_0340;
                        end_IL_00e6:;
                    }
                    finally
                    {
                        if (num < 0 && __8__1.localQueryExecutionContext != null)
                        {
                            __8__1.localQueryExecutionContext.Dispose();
                        }
                    }
                    __8__1 = null;
                    result2 = _result_5__2;
                }
                catch (Exception exception)
                {
                    __1__state = -2;
                    __t__builder.SetException(exception);
                    return;
                }
                __1__state = -2;
                __t__builder.SetResult(result2);
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

        [StructLayout(LayoutKind.Auto)]
        [CompilerGenerated]
        private struct _ExecuteNextPrivateAsync_d__36<TResponse> : IAsyncStateMachine
        {
            public int __1__state;

            public AsyncTaskMethodBuilder<FeedResponse<TResponse>> __t__builder;

            public DocumentQuery<T> __4__this;

            public CancellationToken cancellationToken;

            private TaskAwaiter<IDocumentQueryExecutionContext> __u__1;

            private TaskAwaiter<FeedResponse<object>> __u__2;

            private void MoveNext()
            {
                int num = __1__state;
                DocumentQuery<T> documentQuery = __4__this;
                FeedResponse<TResponse> result2;
                try
                {
                    TaskAwaiter<IDocumentQueryExecutionContext> awaiter2;
                    TaskAwaiter<FeedResponse<object>> awaiter;
                    IDocumentQueryExecutionContext documentQueryExecutionContext;
                    switch (num)
                    {
                    default:
                        if (documentQuery.queryExecutionContext == null)
                        {
                            awaiter2 = documentQuery.CreateDocumentQueryExecutionContextAsync(true, cancellationToken).GetAwaiter();
                            if (!awaiter2.IsCompleted)
                            {
                                num = (__1__state = 0);
                                __u__1 = awaiter2;
                                __t__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
                                return;
                            }
                            goto IL_0086;
                        }
                        if (documentQuery.queryExecutionContext.IsDone)
                        {
                            documentQuery.queryExecutionContext.Dispose();
                            awaiter2 = documentQuery.CreateDocumentQueryExecutionContextAsync(true, cancellationToken).GetAwaiter();
                            if (!awaiter2.IsCompleted)
                            {
                                num = (__1__state = 1);
                                __u__1 = awaiter2;
                                __t__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
                                return;
                            }
                            goto IL_0112;
                        }
                        goto IL_0123;
                    case 0:
                        awaiter2 = __u__1;
                        __u__1 = default(TaskAwaiter<IDocumentQueryExecutionContext>);
                        num = (__1__state = -1);
                        goto IL_0086;
                    case 1:
                        awaiter2 = __u__1;
                        __u__1 = default(TaskAwaiter<IDocumentQueryExecutionContext>);
                        num = (__1__state = -1);
                        goto IL_0112;
                    case 2:
                        {
                            awaiter = __u__2;
                            __u__2 = default(TaskAwaiter<FeedResponse<object>>);
                            num = (__1__state = -1);
                            break;
                        }
                        IL_0112:
                        documentQueryExecutionContext = (documentQuery.queryExecutionContext = awaiter2.GetResult());
                        goto IL_0123;
                        IL_0123:
                        awaiter = documentQuery.queryExecutionContext.ExecuteNextAsync(cancellationToken).GetAwaiter();
                        if (!awaiter.IsCompleted)
                        {
                            num = (__1__state = 2);
                            __u__2 = awaiter;
                            __t__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                            return;
                        }
                        break;
                        IL_0086:
                        documentQueryExecutionContext = (documentQuery.queryExecutionContext = awaiter2.GetResult());
                        goto IL_0123;
                    }
                    FeedResponse<object> result = awaiter.GetResult();
                    FeedResponse<TResponse> feedResponse = (dynamic)result;
                    feedResponse = new FeedResponse<TResponse>(feedResponse, feedResponse.Count, feedResponse.Headers, feedResponse.UseETagAsContinuation, feedResponse.QueryMetrics, feedResponse.RequestStatistics, result.DisallowContinuationTokenMessage, feedResponse.ResponseLengthBytes);
                    if (!documentQuery.HasMoreResults && !documentQuery.tracedLastExecution)
                    {
                        DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1} | Last ExecuteNextAsync with ExecuteNextAsyncMetrics: [{2}]", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), documentQuery.CorrelatedActivityId, documentQuery.executeNextAysncMetrics));
                        documentQuery.tracedLastExecution = true;
                    }
                    result2 = feedResponse;
                }
                catch (Exception exception)
                {
                    __1__state = -2;
                    __t__builder.SetException(exception);
                    return;
                }
                __1__state = -2;
                __t__builder.SetResult(result2);
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

        public static readonly FeedResponse<dynamic> EmptyFeedResponse = new FeedResponse<object>(Enumerable.Empty<object>(), 0, new StringKeyValueCollection(), false, null, null, null, 0L);

        private readonly IDocumentQueryClient client;

        private readonly ResourceType resourceTypeEnum;

        private readonly Type resourceType;

        private readonly string documentsFeedOrDatabaseLink;

        private readonly FeedOptions feedOptions;

        private readonly object partitionKey;

        private readonly Expression expression;

        private readonly DocumentQueryProvider queryProvider;

        private readonly SchedulingStopwatch executeNextAysncMetrics;

        private readonly Guid correlatedActivityId;

        private IDocumentQueryExecutionContext queryExecutionContext;

        private bool tracedFirstExecution;

        private bool tracedLastExecution;

        public Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        public Expression Expression
        {
            get
            {
                return expression;
            }
        }

        public IQueryProvider Provider
        {
            get
            {
                return queryProvider;
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are additional results to retrieve. 
        /// </summary>
        public bool HasMoreResults
        {
            get
            {
                if (queryExecutionContext != null)
                {
                    return !queryExecutionContext.IsDone;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the unique ID for this instance of DocumentQuery used to correlate all activityIds generated when fetching from a partition collection.
        /// </summary>
        public Guid CorrelatedActivityId
        {
            get
            {
                return correlatedActivityId;
            }
        }

        public DocumentQuery(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, string documentsFeedOrDatabaseLink, Expression expression, FeedOptions feedOptions, object partitionKey = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            this.client = client;
            this.resourceTypeEnum = resourceTypeEnum;
            this.resourceType = resourceType;
            this.documentsFeedOrDatabaseLink = documentsFeedOrDatabaseLink;
            this.feedOptions = ((feedOptions == null) ? new FeedOptions() : new FeedOptions(feedOptions));
            if (this.feedOptions.MaxBufferedItemCount < 0)
            {
                this.feedOptions.MaxBufferedItemCount = int.MaxValue;
            }
            if (this.feedOptions.MaxDegreeOfParallelism < 0)
            {
                this.feedOptions.MaxDegreeOfParallelism = int.MaxValue;
            }
            if (this.feedOptions.MaxItemCount < 0)
            {
                this.feedOptions.MaxItemCount = int.MaxValue;
            }
            this.partitionKey = partitionKey;
            this.expression = (expression ?? Expression.Constant(this));
            queryProvider = new DocumentQueryProvider(client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, feedOptions, partitionKey, this.client.OnExecuteScalarQueryCallback);
            executeNextAysncMetrics = new SchedulingStopwatch();
            executeNextAysncMetrics.Ready();
            correlatedActivityId = Guid.NewGuid();
        }

        public DocumentQuery(DocumentClient client, ResourceType resourceTypeEnum, Type resourceType, string documentsFeedOrDatabaseLink, Expression expression, FeedOptions feedOptions, object partitionKey = null)
            : this((IDocumentQueryClient)new DocumentQueryClient(client), resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey)
        {
        }

        public DocumentQuery(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, string documentsFeedOrDatabaseLink, FeedOptions feedOptions, object partitionKey = null)
            : this(client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, (Expression)null, feedOptions, partitionKey)
        {
        }

        public DocumentQuery(DocumentClient client, ResourceType resourceTypeEnum, Type resourceType, string documentsFeedOrDatabaseLink, FeedOptions feedOptions, object partitionKey = null)
            : this((IDocumentQueryClient)new DocumentQueryClient(client), resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, (Expression)null, feedOptions, partitionKey)
        {
        }

        public void Dispose()
        {
            if (queryExecutionContext != null)
            {
                queryExecutionContext.Dispose();
                DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1} | Disposing DocumentQuery", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), CorrelatedActivityId));
            }
        }

        /// <summary>
        /// Executes the query to retrieve the next page of results.
        /// </summary>
        /// <returns></returns>        
        public Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ExecuteNextAsync<object>(cancellationToken);
        }

        /// <summary>
        /// Executes the query to retrieve the next page of results.
        /// </summary>
        /// <returns></returns>
        public Task<FeedResponse<TResponse>> ExecuteNextAsync<TResponse>(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (!tracedFirstExecution)
                {
                    DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1} | First ExecuteNextAsync", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), CorrelatedActivityId));
                    tracedFirstExecution = true;
                }
                executeNextAysncMetrics.Start();
                return TaskHelper.InlineIfPossible(() => ExecuteNextPrivateAsync<TResponse>(cancellationToken), null, cancellationToken);
            }
            finally
            {
                executeNextAysncMetrics.Stop();
                if (!HasMoreResults && !tracedLastExecution)
                {
                    DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1} | Last ExecuteNextAsync with ExecuteNextAsyncMetrics: [{2}]", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), CorrelatedActivityId, executeNextAysncMetrics));
                    tracedLastExecution = true;
                }
            }
        }

        /// <summary>
        /// Retrieves an object that can iterate through the individual results of the query.
        /// </summary>
        /// <remarks>
        /// This triggers a synchronous multi-page load.
        /// </remarks>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            IDocumentQueryExecutionContext localQueryExecutionContext = TaskHelper.InlineIfPossible(() => CreateDocumentQueryExecutionContextAsync(false, CancellationToken.None), null).Result;
            try
            {
                while (!localQueryExecutionContext.IsDone)
                {
                    IEnumerable<T> enumerable = (dynamic)TaskHelper.InlineIfPossible(() => localQueryExecutionContext.ExecuteNextAsync(CancellationToken.None), null).Result;
                    foreach (T item in enumerable)
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                if (localQueryExecutionContext != null)
                {
                    localQueryExecutionContext.Dispose();
                }
            }
        }

        /// <summary>
        /// Synchronous Multi-Page load
        /// </summary>
        /// <returns></returns>        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            SqlQuerySpec sqlQuerySpec = DocumentQueryEvaluator.Evaluate(expression);
            if (sqlQuerySpec != null)
            {
                return JsonConvert.SerializeObject(sqlQuerySpec);
            }
            return new Uri(client.ServiceEndpoint, documentsFeedOrDatabaseLink).ToString();
        }

        #pragma warning disable 612, 618
        private Task<IDocumentQueryExecutionContext> CreateDocumentQueryExecutionContextAsync(bool isContinuationExpected, CancellationToken cancellationToken)
        {
            IPartitionResolver value;
            if (documentsFeedOrDatabaseLink != null && client.PartitionResolvers.TryGetValue(documentsFeedOrDatabaseLink, out value) && (object)resourceType == typeof(Document))
            {
                return DocumentQueryExecutionContextFactory.CreateDocumentQueryExecutionContextAsync(client, resourceTypeEnum, resourceType, expression, feedOptions, value.ResolveForRead(partitionKey).ToArray(), isContinuationExpected, cancellationToken, CorrelatedActivityId);
            }
            return DocumentQueryExecutionContextFactory.CreateDocumentQueryExecutionContextAsync(client, resourceTypeEnum, resourceType, expression, feedOptions, documentsFeedOrDatabaseLink, isContinuationExpected, cancellationToken, CorrelatedActivityId);
        }
        #pragma warning restore 612, 618

        [AsyncStateMachine(typeof(DocumentQuery<>._ExecuteAllAsync_d__35))]
        internal Task<List<T>> ExecuteAllAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _ExecuteAllAsync_d__35 stateMachine = default(_ExecuteAllAsync_d__35);
            stateMachine.__4__this = this;
            stateMachine.cancellationToken = cancellationToken;
            stateMachine.__t__builder = AsyncTaskMethodBuilder<List<T>>.Create();
            stateMachine.__1__state = -1;
            AsyncTaskMethodBuilder<List<T>> __t__builder = stateMachine.__t__builder;
            __t__builder.Start(ref stateMachine);
            return stateMachine.__t__builder.Task;
        }

        [AsyncStateMachine(typeof(_ExecuteNextPrivateAsync_d__36<>))]
        private Task<FeedResponse<TResponse>> ExecuteNextPrivateAsync<TResponse>(CancellationToken cancellationToken)
        {
            _ExecuteNextPrivateAsync_d__36<TResponse> stateMachine = default(_ExecuteNextPrivateAsync_d__36<TResponse>);
            stateMachine.__4__this = this;
            stateMachine.cancellationToken = cancellationToken;
            stateMachine.__t__builder = AsyncTaskMethodBuilder<FeedResponse<TResponse>>.Create();
            stateMachine.__1__state = -1;
            AsyncTaskMethodBuilder<FeedResponse<TResponse>> __t__builder = stateMachine.__t__builder;
            __t__builder.Start(ref stateMachine);
            return stateMachine.__t__builder.Task;
        }
    }

}
