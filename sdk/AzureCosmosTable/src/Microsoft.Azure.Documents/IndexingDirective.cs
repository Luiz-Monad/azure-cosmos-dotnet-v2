namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies whether or not the resource in the Azure Cosmos DB database is to be indexed.
	/// </summary>
	public enum IndexingDirective
	{
		/// <summary>
		/// Use any pre-defined/pre-configured defaults.
		/// </summary>
		Default,
		/// <summary>
		/// Index the resource.
		/// </summary>
		Include,
		/// <summary>
		///  Do not index the resource.
		/// </summary>
		Exclude
	}
}
