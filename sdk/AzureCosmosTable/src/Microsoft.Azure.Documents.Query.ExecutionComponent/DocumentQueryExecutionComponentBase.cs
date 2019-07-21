using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query.ExecutionComponent
{
	/// <summary>
	/// Base class for all DocumentQueryExecutionComponents that implements and IDocumentQueryExecutionComponent
	/// </summary>
	internal abstract class DocumentQueryExecutionComponentBase : IDocumentQueryExecutionComponent, IDisposable
	{
		/// <summary>
		/// Source DocumentQueryExecutionComponent that this component will drain from.
		/// </summary>
		protected readonly IDocumentQueryExecutionComponent Source;

		/// <summary>
		/// Gets a value indicating whether or not this component is done draining documents.
		/// </summary>
		public virtual bool IsDone => Source.IsDone;

		/// <summary>
		/// Initializes a new instance of the DocumentQueryExecutionComponentBase class.
		/// </summary>
		/// <param name="source">The source to drain documents from.</param>
		protected DocumentQueryExecutionComponentBase(IDocumentQueryExecutionComponent source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source for a component can not be null.");
			}
			Source = source;
		}

		/// <summary>
		/// Disposes this context.
		/// </summary>
		public virtual void Dispose()
		{
			Source.Dispose();
		}

		/// <summary>
		/// Drains documents from this execution context.
		/// </summary>
		/// <param name="maxElements">Upper bound for the number of documents you wish to receive.</param>
		/// <param name="token">The cancellation token to use.</param>
		/// <returns>A FeedResponse of documents.</returns>
		public virtual Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token)
		{
			return Source.DrainAsync(maxElements, token);
		}

		/// <summary>
		/// Stops the execution component.
		/// </summary>
		public void Stop()
		{
			Source.Stop();
		}

		/// <summary>
		/// Gets the query metrics from this component.
		/// </summary>
		/// <returns>The partitioned query metrics from this component.</returns>
		public IReadOnlyDictionary<string, QueryMetrics> GetQueryMetrics()
		{
			return Source.GetQueryMetrics();
		}
	}
}
