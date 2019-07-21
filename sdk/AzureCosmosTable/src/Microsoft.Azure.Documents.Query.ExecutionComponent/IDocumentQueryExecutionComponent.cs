using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query.ExecutionComponent
{
	/// <summary>
	/// Interface for all DocumentQueryExecutionComponents
	/// </summary>
	internal interface IDocumentQueryExecutionComponent : IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether this component is done draining documents.
		/// </summary>
		bool IsDone
		{
			get;
		}

		/// <summary>
		/// Drains documents from this component.
		/// </summary>
		/// <param name="maxElements">The maximum number of documents to drain.</param>
		/// <param name="token">The cancellation token to cancel tasks.</param>
		/// <returns>A task that when awaited on returns a feed response.</returns>
		Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token);

		/// <summary>
		/// Stops this document query execution component.
		/// </summary>
		void Stop();

		/// <summary>
		/// Gets the QueryMetrics from this component.
		/// </summary>
		/// <returns>The QueryMetrics from this component.</returns>
		IReadOnlyDictionary<string, QueryMetrics> GetQueryMetrics();
	}
}
