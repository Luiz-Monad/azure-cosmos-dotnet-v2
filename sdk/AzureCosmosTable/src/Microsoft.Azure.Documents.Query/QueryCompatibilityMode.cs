namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// A client query compatibility mode when making query request.
	/// Can be used to force a specific query request format.
	/// </summary>
	internal enum QueryCompatibilityMode
	{
		/// <summary>
		/// Default (latest) query format.
		/// </summary>
		Default,
		/// <summary>
		/// Query (application/query+json).
		/// Default.
		/// </summary>
		Query,
		/// <summary>
		/// SqlQuery (application/sql).
		/// </summary>
		SqlQuery
	}
}
