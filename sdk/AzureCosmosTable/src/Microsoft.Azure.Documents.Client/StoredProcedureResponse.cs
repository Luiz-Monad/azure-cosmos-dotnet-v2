using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Represents the response returned from a database stored procedure in the Azure Cosmos DB service. Wraps the response body and headers.
	/// </summary> 
	/// <typeparam name="TValue">The returned value type of the stored procedure.</typeparam>
	/// <remarks>
	/// Stored procedures can return any string output via the getContext().getResponse().setBody() method.
	/// This response body could be a serialized JSON object, or any other type.
	/// Within the .NET SDK, you can deserialize the response into a corresponding TValue type.
	/// </remarks>
	public class StoredProcedureResponse<TValue> : IStoredProcedureResponse<TValue>
	{
		private DocumentServiceResponse response;

		private TValue responseBody;

		private JsonSerializerSettings serializerSettings;

		/// <summary>
		/// Gets the Activity ID of the request from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The Activity ID of the request.
		/// </value>
		/// <remarks>Every request is traced with a globally unique ID. Include activity ID in tracing application failures and when contacting Azure Cosmos DB support</remarks>
		public string ActivityId => response.Headers["x-ms-activity-id"];

		/// <summary>
		/// Gets the token for use with session consistency requests from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The token for use with session consistency requests.
		/// </value>
		public string SessionToken => response.Headers["x-ms-session-token"];

		/// <summary>
		/// Gets the output from stored procedure console.log() statements.
		/// </summary>
		/// <value>
		/// Output from console.log() statements in a stored procedure.
		/// </value>
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.RequestOptions.EnableScriptLogging" />
		public string ScriptLog => Helpers.GetScriptLogHeader(response.Headers);

		/// <summary>
		/// Gets the request completion status code from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The request completion status code</value>
		public HttpStatusCode StatusCode => response.StatusCode;

		/// <summary>
		/// Gets the delimited string containing the quota of each resource type within the collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The delimited string containing the number of used units per resource type within the collection.</value>
		public string MaxResourceQuota => response.Headers["x-ms-resource-quota"];

		/// <summary>
		/// Gets the delimited string containing the usage of each resource type within the collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The delimited string containing the number of used units per resource type within the collection.</value>
		public string CurrentResourceQuotaUsage => response.Headers["x-ms-resource-usage"];

		/// <summary>
		/// Gets the number of normalized Azure Cosmos DB request units (RUs) charged from Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The number of normalized Azure Cosmos DB request units (RUs) charged.
		/// </value>
		public double RequestCharge => Helpers.GetHeaderValueDouble(response.Headers, "x-ms-request-charge", 0.0);

		/// <summary>
		/// Gets the flag associated with the response from the Azure Cosmos DB service whether this stored procedure request is served from Request Units(RUs)/minute capacity or not.
		/// </summary>
		/// <value>
		/// True if this request is served from RUs/minute capacity. Otherwise, false.
		/// </value>
		public bool IsRUPerMinuteUsed
		{
			get
			{
				if (Helpers.GetHeaderValueByte(response.Headers, "x-ms-documentdb-is-ru-per-minute-used", 0) != 0)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the headers associated with the response from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Headers associated with the response.
		/// </value>
		/// <remarks>
		/// Provides access to all HTTP response headers returned from the 
		/// Azure Cosmos DB API.
		/// </remarks>
		public NameValueCollection ResponseHeaders => response.ResponseHeaders;

		internal INameValueCollection Headers => response.Headers;

		/// <summary>
		/// Gets the response of a stored procedure, serialized into the given type from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The response of a stored procedure, serialized into the given type.</value>
		public TValue Response => responseBody;

		/// <summary>
		/// Gets the clientside request statics for execution of stored procedure.
		/// </summary>
		/// <value>The clientside request statics for execution of stored procedure.</value>
		internal ClientSideRequestStatistics RequestStatistics => response.RequestStats;

		/// <summary>
		/// Constructor exposed for mocking purposes in Azure Cosmos DB service.
		/// </summary>
		public StoredProcedureResponse()
		{
		}

		internal StoredProcedureResponse(DocumentServiceResponse response, JsonSerializerSettings serializerSettings = null)
		{
			//IL_014f: Expected O, but got Unknown
			this.response = response;
			this.serializerSettings = serializerSettings;
			if (typeof(TValue).IsSubclassOf(typeof(JsonSerializable)))
			{
				if ((object)typeof(TValue) == typeof(Document) || typeof(Document).IsAssignableFrom(typeof(TValue)))
				{
					responseBody = JsonSerializable.LoadFromWithConstructor(response.ResponseBody, () => (TValue)(object)new Document(), this.serializerSettings);
					return;
				}
				if ((object)typeof(TValue) != typeof(Attachment) && !typeof(Attachment).IsAssignableFrom(typeof(TValue)))
				{
					throw new ArgumentException("Cannot serialize object if it is not document or attachment");
				}
				responseBody = JsonSerializable.LoadFromWithConstructor(response.ResponseBody, () => (TValue)(object)new Attachment(), this.serializerSettings);
			}
			else
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (StreamReader streamReader = new StreamReader(response.ResponseBody ?? memoryStream))
					{
						string text = streamReader.ReadToEnd();
						try
						{
							responseBody = (TValue)JsonConvert.DeserializeObject(text, typeof(TValue), this.serializerSettings);
						}
						catch (JsonException val)
						{
							JsonException val2 = val;
							throw new SerializationException(string.Format(CultureInfo.InvariantCulture, "Failed to deserialize stored procedure response or convert it to type '{0}': {1}", typeof(TValue).FullName, ((Exception)val2).Message), (Exception)val2);
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the resource implicitly from Azure Cosmos DB service.
		/// </summary>
		/// <param name="source">Stored procedure response.</param>
		/// <returns>The returned resource.</returns>
		public static implicit operator TValue(StoredProcedureResponse<TValue> source)
		{
			return source.responseBody;
		}
	}
}
