namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Represents a user in the Azure Cosmos DB service.
	/// </summary>
	public class User : Resource
	{
		/// <summary>
		/// Gets the self-link of the permissions associated with the user for the Azure Cosmos DB service.
		/// </summary>
		/// <value>The self-link of the permissions associated with the user.</value> 
		public string PermissionsLink => base.SelfLink.TrimEnd(new char[1]
		{
			'/'
		}) + "/" + GetValue<string>("_permissions");
	}
}
