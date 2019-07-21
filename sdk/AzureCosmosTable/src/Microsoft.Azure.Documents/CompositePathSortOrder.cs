using System.Runtime.Serialization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Defines the target data type of an index path specification in the Azure Cosmos DB service.
	/// </summary>
	public enum CompositePathSortOrder
	{
		/// <summary>
		/// Ascending sort order for composite paths.
		/// </summary>
		[EnumMember(Value = "ascending")]
		Ascending,
		/// <summary>
		/// Descending sort order for composite paths.
		/// </summary>
		[EnumMember(Value = "descending")]
		Descending
	}
}
