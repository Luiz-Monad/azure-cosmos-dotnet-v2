using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Encapsulates error related details in the Azure Cosmos DB service.
	/// </summary>
	public class Error : Resource
	{
		/// <summary>
		/// Gets or sets the textual description of error code in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The textual description of error code.</value>
		[JsonProperty(PropertyName = "code")]
		public string Code
		{
			get
			{
				return GetValue<string>("code");
			}
			set
			{
				SetValue("code", value);
			}
		}

		/// <summary>
		/// Gets or sets the error message in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The error message.</value>
		[JsonProperty(PropertyName = "message")]
		public string Message
		{
			get
			{
				return GetValue<string>("message");
			}
			set
			{
				SetValue("message", value);
			}
		}

		[JsonProperty(PropertyName = "errorDetails")]
		internal string ErrorDetails
		{
			get
			{
				return GetValue<string>("errorDetails");
			}
			set
			{
				SetValue("errorDetails", value);
			}
		}

		[JsonProperty(PropertyName = "additionalErrorInfo")]
		internal string AdditionalErrorInfo
		{
			get
			{
				return GetValue<string>("additionalErrorInfo");
			}
			set
			{
				SetValue("additionalErrorInfo", value);
			}
		}
	}
}
