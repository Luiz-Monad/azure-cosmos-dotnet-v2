using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Query.ExecutionComponent;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// You can imagine the pipeline to be a directed acyclic graph where documents flow from multiple sources (the partitions) to a single sink (the client who calls on ExecuteNextAsync()).
	/// The pipeline will consist of individual implementations of <see cref="T:Microsoft.Azure.Documents.Query.IDocumentQueryExecutionContext" />. 
	/// Every member of the pipeline has a source of documents (another member of the pipeline or an actual partition),
	/// a method of draining documents (DrainAsync()) from said source, and a flag for whether that member of the pipeline is completely drained.
	/// <para>
	/// The following is a diagram of the pipeline:
	///     +--------------------------+    +--------------------------+    +--------------------------+
	///     |                          |    |                          |    |                          |
	///     | Document Producer Tree 0 |    | Document Producer Tree 1 |    | Document Producer Tree N |
	///     |                          |    |                          |    |                          |
	///     +--------------------------+    +--------------------------+    +--------------------------+
	///                   |                               |                               |           
	///                    \                              |                              /
	///                     \                             |                             /
	///                      +---------------------------------------------------------+
	///                      |                                                         |
	///                      |   Parallel / Order By Document Query Execution Context  |
	///                      |                                                         |
	///                      +---------------------------------------------------------+
	///                                                   |
	///                                                   |
	///                                                   |
	///                         +---------------------------------------------------+
	///                         |                                                   |
	///                         |    Aggregate Document Query Execution Component   |
	///                         |                                                   |
	///                         +---------------------------------------------------+
	///                                                   |
	///                                                   |
	///                                                   |
	///                             +------------------------------------------+
	///                             |                                          |
	///                             |  Top Document Query Execution Component  |
	///                             |                                          |
	///                             +------------------------------------------+
	///                                                   |
	///                                                   |
	///                                                   |
	///                                    +-----------------------------+
	///                                    |                             |
	///                                    |            Client           |
	///                                    |                             |
	///                                    +-----------------------------+
	/// </para>    
	/// <para>
	/// This class is responsible for constructing the pipelined described.
	/// Note that the pipeline will always have one of <see cref="T:Microsoft.Azure.Documents.Query.OrderByDocumentQueryExecutionContext" /> or <see cref="T:Microsoft.Azure.Documents.Query.ParallelDocumentQueryExecutionContext" />,
	/// which both derive from <see cref="T:Microsoft.Azure.Documents.Query.CrossPartitionQueryExecutionContext`1" /> as these are top level execution contexts.
	/// These top level execution contexts have <see cref="T:Microsoft.Azure.Documents.Query.DocumentProducerTree`1" /> that are responsible for hitting the backend
	/// and will optionally feed into <see cref="T:Microsoft.Azure.Documents.Query.ExecutionComponent.AggregateDocumentQueryExecutionComponent" /> and <see cref="T:Microsoft.Azure.Documents.Query.ExecutionComponent.TakeDocumentQueryExecutionComponent" />.
	/// How these components are picked is based on <see cref="T:Microsoft.Azure.Documents.Query.PartitionedQueryExecutionInfo" />,
	/// which is a serialized form of this class and serves as a blueprint for construction.
	/// </para>
	/// <para>
	/// Once the pipeline is constructed the client(sink of the graph) calls ExecuteNextAsync() which calls on DrainAsync(),
	/// which by definition grabs documents from the parent component of the pipeline.
	/// This bubbles down until you reach a component that has a DocumentProducer that fetches a document from the backend.
	/// </para>
	/// </summary>
	internal sealed class PipelinedDocumentQueryExecutionContext : IDocumentQueryExecutionContext, IDisposable
	{
		/// <summary>
		/// The root level component that all calls will be forwarded to.
		/// </summary>
		private readonly IDocumentQueryExecutionComponent component;

		/// <summary>
		/// The actual page size to drain.
		/// </summary>
		private readonly int actualPageSize;

		/// <summary>
		/// Gets a value indicating whether this execution context is done draining documents.
		/// </summary>
		public bool IsDone => component.IsDone;

		/// <summary>
		/// Initializes a new instance of the PipelinedDocumentQueryExecutionContext class.
		/// </summary>
		/// <param name="component">The root level component that all calls will be forwarded to.</param>
		/// <param name="actualPageSize">The actual page size to drain.</param>
		private PipelinedDocumentQueryExecutionContext(IDocumentQueryExecutionComponent component, int actualPageSize)
		{
			if (component == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "component"));
			}
			if (actualPageSize < 0)
			{
				throw new ArgumentException(string.Format("{0} can not be negative.", "actualPageSize"));
			}
			this.component = component;
			this.actualPageSize = actualPageSize;
		}

		/// <summary>
		/// Creates a PipelinedDocumentQueryExecutionContext.
		/// </summary>
		/// <param name="constructorParams">The parameters for constructing the base class.</param>
		/// <param name="collectionRid">The collection rid.</param>
		/// <param name="partitionedQueryExecutionInfo">The partitioned query execution info.</param>
		/// <param name="partitionKeyRanges">The partition key ranges.</param>
		/// <param name="initialPageSize">The initial page size.</param>
		/// <param name="requestContinuation">The request continuation.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A task to await on, which in turn returns a PipelinedDocumentQueryExecutionContext.</returns>
		public static async Task<IDocumentQueryExecutionContext> CreateAsync(DocumentQueryExecutionContextBase.InitParams constructorParams, string collectionRid, PartitionedQueryExecutionInfo partitionedQueryExecutionInfo, List<PartitionKeyRange> partitionKeyRanges, int initialPageSize, string requestContinuation, CancellationToken cancellationToken)
		{
			DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "{0}, CorrelatedActivityId: {1} | Pipelined~Context.CreateAsync", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), constructorParams.CorrelatedActivityId));
			QueryInfo queryInfo = partitionedQueryExecutionInfo.QueryInfo;
			Func<string, Task<IDocumentQueryExecutionComponent>> func = (!queryInfo.HasOrderBy) ? ((Func<string, Task<IDocumentQueryExecutionComponent>>)(async (string continuationToken) => await ParallelDocumentQueryExecutionContext.CreateAsync(initParams: new CrossPartitionQueryExecutionContext<object>.CrossPartitionInitParams(collectionRid, partitionedQueryExecutionInfo, partitionKeyRanges, initialPageSize, continuationToken), constructorParams: constructorParams, token: cancellationToken))) : ((Func<string, Task<IDocumentQueryExecutionComponent>>)(async (string continuationToken) => await OrderByDocumentQueryExecutionContext.CreateAsync(initParams: new CrossPartitionQueryExecutionContext<object>.CrossPartitionInitParams(collectionRid, partitionedQueryExecutionInfo, partitionKeyRanges, initialPageSize, continuationToken), constructorParams: constructorParams, token: cancellationToken)));
			if (queryInfo.HasAggregates)
			{
				Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback = func;
				func = (async (string continuationToken) => await AggregateDocumentQueryExecutionComponent.CreateAsync(queryInfo.Aggregates, continuationToken, createSourceCallback));
			}
			if (queryInfo.HasDistinct)
			{
				Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback2 = func;
				func = (async (string continuationToken) => await DistinctDocumentQueryExecutionComponent.CreateAsync(continuationToken, createSourceCallback2, queryInfo.DistinctType));
			}
			if (queryInfo.HasGroupBy)
			{
				if (!constructorParams.FeedOptions.EnableCrossPartitionGroupBy)
				{
					throw new ArgumentException("Cross Partition GROUP BY is not supported.");
				}
				throw new NotSupportedException("Cross Partition GROUP BY is not supported.");
			}
			if (queryInfo.HasOffset)
			{
				if (!constructorParams.FeedOptions.EnableCrossPartitionSkipTake)
				{
					throw new ArgumentException("Cross Partition OFFSET / LIMIT is not supported.");
				}
				Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback3 = func;
				func = (async (string continuationToken) => await SkipDocumentQueryExecutionComponent.CreateAsync(queryInfo.Offset.Value, continuationToken, createSourceCallback3));
			}
			if (queryInfo.HasLimit)
			{
				if (!constructorParams.FeedOptions.EnableCrossPartitionSkipTake)
				{
					throw new ArgumentException("Cross Partition OFFSET / LIMIT is not supported.");
				}
				Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback4 = func;
				func = (async (string continuationToken) => await TakeDocumentQueryExecutionComponent.CreateLimitDocumentQueryExecutionComponentAsync(queryInfo.Limit.Value, continuationToken, createSourceCallback4));
			}
			if (queryInfo.HasTop)
			{
				Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback5 = func;
				func = (async (string continuationToken) => await TakeDocumentQueryExecutionComponent.CreateTopDocumentQueryExecutionComponentAsync(queryInfo.Top.Value, continuationToken, createSourceCallback5));
			}
			return new PipelinedDocumentQueryExecutionContext(await func(requestContinuation), initialPageSize);
		}

		/// <summary>
		/// Disposes of this context.
		/// </summary>
		public void Dispose()
		{
			component.Dispose();
		}

		/// <summary>
		/// Gets the next page of results from this context.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on that in turn returns a FeedResponse of results.</returns>
		public async Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token)
		{
			try
			{
				return await component.DrainAsync(actualPageSize, token);
			}
			catch (Exception)
			{
				component.Stop();
				throw;
			}
		}
	}
}
