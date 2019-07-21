using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a stored procedure in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks> 
	/// Azure Cosmos DB allows application logic written entirely in JavaScript to be executed directly inside the database engine under the database transaction.
	/// For additional details, refer to the server-side JavaScript API documentation.
	/// </remarks>
	public class StoredProcedure : Resource
	{
		/// <summary>
		/// Gets or sets the body of the Azure Cosmos DB stored procedure.
		/// </summary>
		/// <value>The body of the stored procedure.</value>
		/// <remarks>Must be a valid JavaScript function. For e.g. "function () { getContext().getResponse().setBody('Hello World!'); }"</remarks>
		[JsonProperty(PropertyName = "body")]
		public string Body
		{
			get
			{
				return GetValue<string>("body");
			}
			set
			{
				SetValue("body", value);
			}
		}
	}
}
