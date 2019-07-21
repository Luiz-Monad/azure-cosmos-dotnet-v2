namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// The DatabaseAccountLocation class represents an Azure Cosmos DB database account in a specific region.
	/// </summary>
	public class DatabaseAccountLocation : JsonSerializable
	{
		/// <summary>
		/// Gets the name of the database account location in the Azure Cosmos DB service. For example,
		/// "West US" as the name of the database account location in the West US region.
		/// </summary>
		public string Name
		{
			get
			{
				return GetValue<string>("name", null);
			}
			internal set
			{
				SetValue("name", value);
			}
		}

		/// <summary>
		/// Gets the Url of the database account location in the Azure Cosmos DB service. For example,
		/// "https://contoso-WestUS.documents.azure.com:443/" as the Url of the 
		/// database account location in the West US region.
		/// </summary>
		public string DatabaseAccountEndpoint
		{
			get
			{
				return GetValue<string>("databaseAccountEndpoint", null);
			}
			internal set
			{
				SetValue("databaseAccountEndpoint", value);
			}
		}
	}
}
