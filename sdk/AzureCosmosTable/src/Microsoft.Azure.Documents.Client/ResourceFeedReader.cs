using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// The Azure Cosmos DB ResourceFeedReader class can be used to iterate through the resources of the specified type under a 
	/// feed, e.g., collections under a database or documents under a collection. Supports paginated read of results.
	/// </summary>
	/// <typeparam name="T">Resource type</typeparam>
	/// <remarks>
	/// <para>
	/// The database entities that Azure Cosmos DB manages like databases, collections and documents are referred to as resources, and each set 
	/// of resources is referred to as a feed. For example, a collection has a feed of documents, as well as a feed of stored procedures.
	/// <see cref="T:Microsoft.Azure.Documents.Client.ResourceFeedReader`1" /> objects can be used to perform a "read feed", i.e, enumerate the specified resources under the 
	/// specified Azure Cosmos DB feed link. For more details, refer to <a href="http://azure.microsoft.com/documentation/articles/documentdb-resources/">
	/// Azure Cosmos DB resource model and concepts</a>.
	/// </para>
	/// </remarks>
	/// <example>
	/// The following example shows how to create a ResourceFeedReader to read all documents in a collection using the default page size.
	/// <code style="c#">
	/// <![CDATA[
	/// var feedReader = client.CreateDocumentFeedReader(collection1.SelfLink);
	/// var count = 0;
	/// while (feedReader.HasMoreResults)
	/// {
	///     count += feedReader.ExecuteNextAsync().Result.Count;
	/// }
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The following example shows how to create a ResourceFeedReader for documents against a partitioned database using a Range partition key, and 
	/// a custom page size.
	/// <code style="c#">
	/// <![CDATA[
	/// feedCount = 0;
	/// ResourceFeedReader<Document> feedReader = client.CreateDocumentFeedReader(
	///     databaseLink, 
	///     new FeedOptions() { MaxItemCount = 1 }, 
	///     new Range<long>(0, 800));
	///
	/// while (feedReader.HasMoreResults)
	/// {
	///     var feed = feedReader.ExecuteNextAsync().Result;
	///     feedCount += feed.Count;
	/// }
	/// ]]>
	/// </code>
	/// </example>
	/// <seealso cref="T:Microsoft.Azure.Documents.Resource" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Client.DocumentClient" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceFeedReaderClientExtensions" />
	internal sealed class ResourceFeedReader<T> : IEnumerable<T>, IEnumerable where T : JsonSerializable, new()
	{
		private readonly DocumentQuery<T> documentQuery;

		/// <summary>
		/// Gets a value indicating whether there are additional results to retrieve from the Azure Cosmos DB service.
		/// </summary>
		/// <returns>Returns true if there are additional results to retrieve. Returns false otherwise.</returns>
		public bool HasMoreResults => documentQuery.HasMoreResults;

		internal ResourceFeedReader(DocumentClient client, ResourceType resourceType, FeedOptions options, string resourceLink, object partitionKey = null)
		{
			documentQuery = new DocumentQuery<T>(client, resourceType, typeof(T), resourceLink, options, partitionKey);
		}

		/// <summary>
		/// Retrieves an <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate over the resources from the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This call results in all pages for the feed being fetched synchronously.
		/// </remarks>
		/// <returns>An enumerator for the feed.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			return documentQuery.GetEnumerator();
		}

		/// <summary>
		/// Retrieves an <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate over the resources from the Azure Cosmos DB service.
		/// </summary>
		/// <returns>An enumerator for the feed.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return documentQuery.GetEnumerator();
		}

		/// <summary>
		/// Retrieves the next page of results from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="cancellationToken">(Optional) The <see cref="T:System.Threading.CancellationToken" /> allows for notification that operations should be cancelled.</param>
		/// <returns>The response from a single call to ReadFeed for the specified resource.</returns>
		public Task<FeedResponse<T>> ExecuteNextAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => ExecuteNextAsyncInternal(cancellationToken), null, cancellationToken);
		}

		private Task<FeedResponse<T>> ExecuteNextAsyncInternal(CancellationToken cancellationToken)
		{
			return documentQuery.ExecuteNextAsync<T>(cancellationToken);
		}
	}
}
