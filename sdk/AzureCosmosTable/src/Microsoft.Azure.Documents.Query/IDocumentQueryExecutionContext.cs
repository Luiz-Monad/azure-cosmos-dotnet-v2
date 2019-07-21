using Microsoft.Azure.Documents.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Interface for all document query execution contexts
	/// </summary>
	internal interface IDocumentQueryExecutionContext : IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether or not the context is done serving documents.
		/// </summary>
		bool IsDone
		{
			get;
		}

		/// <summary>
		/// Executes the context to feed the next page of results.
		/// </summary>
		/// <param name="token">The cancellation token.</param>
		/// <returns>A task to await on, which in return provides a FeedResponse of documents.</returns>
		Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token);
	}
}
