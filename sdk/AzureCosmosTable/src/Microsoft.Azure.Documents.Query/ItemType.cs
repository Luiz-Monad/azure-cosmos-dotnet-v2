namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Enum of all item types that are returned by queries.
	/// </summary>
	internal enum ItemType
	{
		/// <summary>
		/// NoValue / Undefined item type.
		/// </summary>
		NoValue = 0,
		/// <summary>
		/// Null item type.
		/// </summary>
		Null = 1,
		/// <summary>
		/// Boolean item type.
		/// </summary>
		Bool = 2,
		/// <summary>
		/// Number item type.
		/// </summary>
		Number = 4,
		/// <summary>
		/// String item type.
		/// </summary>
		String = 5
	}
}
