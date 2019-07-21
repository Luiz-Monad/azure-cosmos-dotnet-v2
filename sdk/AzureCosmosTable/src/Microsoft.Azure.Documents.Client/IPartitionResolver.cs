using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// This represents a partition resolver for a database. Given a partition key, return the collection link(s) matching the partition key
	/// in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Support for IPartitionResolver is now obsolete. It's recommended that you use 
	/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
	/// </para>
	/// <para>
	/// DocumentClient allows you to create and register IPartitionResolvers implementations for each database. Once registered, you can perform 
	/// document operations directly against a database instead of a collection. IPartitionResolvers has just three methods 
	/// ExtractPartitionKey, ResolveForCreate and ResolveForRead.
	/// </para>
	/// <para>
	/// LINQ queries and ReadFeed iterators use the ResolveForRead internally to iterate over all the collections that match the partition key for 
	/// the request. Similarly, create operations use the ResolveForCreate to route creates to the right partition. There are no changes required for Replace,
	/// Delete and Read since they use the Document, which already contains the reference to the collection that holds the document.
	/// </para>
	/// </remarks>
	[Obsolete("Support for IPartitionResolver is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
	public interface IPartitionResolver
	{
		/// <summary>
		/// Extracts the partition key from a document in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="document">A document object.</param>
		/// <returns>The partition key for the document.</returns>
		/// <remarks>
		/// Typical implementations will get the value of a single property from the document (e.g., user ID) or 
		/// extract a compound property, for e.g., version ID, device #) or implement custom logic based on the 
		/// type of the document, for e.g., extract value of id for users but extract userId for userMessages.
		/// </remarks>
		object GetPartitionKey(object document);

		/// <summary>
		/// Given a partition key, this returns the collection self-link for creating a document
		/// in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collection for create operations.</param>
		/// <returns>A self-link for the collection to create documents in for the specified partition key.</returns>
		/// <remarks>
		/// The return value must be a valid collection self-link string in the format dbs/db_rid/colls/col_rid.
		/// </remarks>
		string ResolveForCreate(object partitionKey);

		/// <summary>
		/// Given a partition key, this returns a list of collection self-links to read from.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collections for reads, i.e., query or read-feed.</param>
		/// <returns>The self-links for the collections to perform read requests for the specified partition key.</returns>
		/// <remarks>
		/// The return value must be an IEnumerable of collection self-link strings in the format dbs/db_rid/colls/col_rid.
		/// Unlike ResolveForCreate, this is a 1:N as a single partition key might be created in different collections over 
		/// time or because you are performing data migration of partition key between collections 
		/// in the Azure Cosmos DB service.
		/// </remarks>
		IEnumerable<string> ResolveForRead(object partitionKey);
	}
}
