namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Specifies the type of the trigger in the Azure Cosmos DB service.
	/// </summary> 
	public enum TriggerType : byte
	{
		/// <summary>
		/// Trigger should be executed before the associated operation(s).
		/// </summary>
		Pre,
		/// <summary>
		/// Trigger should be executed after the associated operation(s).
		/// </summary>
		Post
	}
}
