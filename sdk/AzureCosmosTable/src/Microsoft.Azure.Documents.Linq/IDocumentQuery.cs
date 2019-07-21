using Microsoft.Azure.Documents.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Provides methods to support query pagination and asynchronous execution in the Azure Cosmos DB service.
	/// </summary> 
	/// <remarks>
	/// Untyped interface with no methods.
	/// </remarks>
	public interface IDocumentQuery : IDisposable
	{
	}
	/// <summary>
	/// Provides methods to support query pagination and asynchronous execution in the Azure Cosmos DB service.
	/// </summary>
	/// <typeparam name="T">Source Query Type</typeparam>
	public interface IDocumentQuery<T> : IDocumentQuery, IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether there are potentially additional results that can be 
		/// returned from the query in the Azure Cosmos DB service.
		/// </summary>
		/// <value>Boolean value representing if there are potentially additional results that can be 
		/// returned from the query.</value>
		/// <remarks>Initially returns true. This value is set based on whether the last execution returned a continuation token.</remarks>
		bool HasMoreResults
		{
			get;
		}

		/// <summary>
		/// Executes the query and retrieves the next page of results in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TResult">The type of the object returned in the query result.</typeparam>
		/// <param name="token">(Optional) The <see cref="T:System.Threading.CancellationToken" /> allows for notification that operations should be cancelled.</param>
		/// <returns>The Task object for the asynchronous response from query execution.</returns>
		Task<FeedResponse<TResult>> ExecuteNextAsync<TResult>(CancellationToken token = default(CancellationToken));

		/// <summary>
		/// Executes the query and retrieves the next page of results as dynamic objects in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="token">(Optional) The <see cref="T:System.Threading.CancellationToken" /> allows for notification that operations should be cancelled.</param>
		/// <returns>The Task object for the asynchronous response from query execution.</returns>
		Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token = default(CancellationToken));
	}
}
