using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a trigger in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks> 
	/// Azure Cosmos DB supports pre and post triggers written in JavaScript to be executed on creates, updates and deletes. 
	/// For additional details, refer to the server-side JavaScript API documentation.
	/// </remarks>
	public class Trigger : Resource
	{
		/// <summary>
		/// Gets or sets the body of the trigger for the Azure Cosmos DB service.
		/// </summary>
		/// <value>The body of the trigger.</value>
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

		/// <summary>
		/// Get or set the type of the trigger for the Azure Cosmos DB service.
		/// </summary>
		/// <value>The body of the trigger.</value>
		/// <seealso cref="P:Microsoft.Azure.Documents.Trigger.TriggerType" />
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty(PropertyName = "triggerType")]
		public TriggerType TriggerType
		{
			get
			{
				return GetValue("triggerType", TriggerType.Pre);
			}
			set
			{
				SetValue("triggerType", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the operation the trigger is associated with for the Azure Cosmos DB service.
		/// </summary>
		/// <value>The operation the trigger is associated with.</value>
		/// <seealso cref="P:Microsoft.Azure.Documents.Trigger.TriggerOperation" />
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty(PropertyName = "triggerOperation")]
		public TriggerOperation TriggerOperation
		{
			get
			{
				return GetValue("triggerOperation", TriggerOperation.All);
			}
			set
			{
				SetValue("triggerOperation", value.ToString());
			}
		}
	}
}
