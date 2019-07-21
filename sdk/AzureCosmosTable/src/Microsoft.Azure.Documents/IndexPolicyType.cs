namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// These are the indexing types available for indexing all the paths in a Azure Cosmos DB policy.
	/// </summary> 
	/// <remarks>
	/// For additional details, refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy.
	/// </remarks>
	internal enum IndexPolicyType
	{
		/// <summary>
		/// Default value.
		/// </summary>
		None,
		/// <summary>
		/// The index entries are hashed to serve point look up queries.
		/// </summary>
		/// <remarks>
		/// Can be used to serve queries like: SELECT * FROM docs d WHERE d.prop = 5
		/// </remarks>
		Hash,
		/// <summary>
		/// The index entries are ordered. Range indexes are optimized for inequality predicate queries with efficient range scans.
		/// </summary>
		/// <remarks>
		/// Can be used to serve queries like: SELECT * FROM docs d WHERE d.prop &gt; 5
		/// </remarks>
		Range
	}
}
