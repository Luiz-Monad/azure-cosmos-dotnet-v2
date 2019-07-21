namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies the operations on which a trigger should be executed in the Azure Cosmos DB service.
	/// </summary>
	public enum TriggerOperation : short
	{
		/// <summary>
		/// Specifies all operations.
		/// </summary>
		All,
		/// <summary>
		/// Specifies create operations only.
		/// </summary>
		Create,
		/// <summary>
		/// Specifies update operations only.
		/// </summary>
		Update,
		/// <summary>
		/// Specifies delete operations only.
		/// </summary>
		Delete,
		/// <summary>
		/// Specifies replace operations only.
		/// </summary>
		Replace
	}
}
