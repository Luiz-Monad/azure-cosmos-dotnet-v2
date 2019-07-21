using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	///  Represents an abstract resource type in the Azure Cosmos DB service.
	///  All Azure Cosmos DB resources, such as <see cref="T:Microsoft.Azure.Documents.Database" />, <see cref="T:Microsoft.Azure.Documents.DocumentCollection" />, and <see cref="T:Microsoft.Azure.Documents.Document" /> extend this abstract type.
	/// </summary>
	public abstract class Resource : JsonSerializable
	{
		private static DateTime UnixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Gets or sets the Id of the resource in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The Id associated with the resource.</value>
		/// <remarks>
		/// <para>
		/// Every resource within an Azure Cosmos DB database account needs to have a unique identifier. 
		/// Unlike <see cref="P:Microsoft.Azure.Documents.Resource.ResourceId" />, which is set internally, this Id is settable by the user and is not immutable.
		/// </para>
		/// <para>
		/// When working with document resources, they too have this settable Id property. 
		/// If an Id is not supplied by the user the SDK will automatically generate a new GUID and assign its value to this property before
		/// persisting the document in the database. 
		/// You can override this auto Id generation by setting the disableAutomaticIdGeneration parameter on the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> instance to true.
		/// This will prevent the SDK from generating new Ids. 
		/// </para>
		/// <para>
		/// The following characters are restricted and cannot be used in the Id property:
		///  '/', '\\', '?', '#'
		/// </para>
		/// </remarks>
		[JsonProperty(PropertyName = "id")]
		public virtual string Id
		{
			get
			{
				return GetValue<string>("id");
			}
			set
			{
				SetValue("id", value);
			}
		}

		/// <summary>
		/// Gets or sets the Resource Id associated with the resource in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The Resource Id associated with the resource.
		/// </value>
		/// <remarks>
		/// A Resource Id is the unique, immutable, identifier assigned to each Azure Cosmos DB 
		/// resource whether that is a database, a collection or a document.
		/// These resource ids are used when building up SelfLinks, a static addressable Uri for each resource within a database account.
		/// </remarks>
		[JsonProperty(PropertyName = "_rid")]
		public virtual string ResourceId
		{
			get
			{
				return GetValue<string>("_rid");
			}
			set
			{
				SetValue("_rid", value);
			}
		}

		/// <summary>
		/// Gets the self-link associated with the resource from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The self-link associated with the resource.</value> 
		/// <remarks>
		/// A self-link is a static addressable Uri for each resource within a database account and follows the Azure Cosmos DB resource model.
		/// E.g. a self-link for a document could be dbs/db_resourceid/colls/coll_resourceid/documents/doc_resourceid
		/// </remarks>
		[JsonProperty(PropertyName = "_self")]
		public string SelfLink
		{
			get
			{
				return GetValue<string>("_self");
			}
			internal set
			{
				SetValue("_self", value);
			}
		}

		/// <summary>
		/// Gets the alt-link associated with the resource from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The alt-link associated with the resource.</value>
		[JsonIgnore]
		public string AltLink
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the last modified timestamp associated with the resource from the Azure Cosmos DB service.
		/// </summary>
		/// <value>The last modified timestamp associated with the resource.</value>
		[JsonProperty(PropertyName = "_ts")]
		[JsonConverter(typeof(UnixDateTimeConverter))]
		public virtual DateTime Timestamp
		{
			get
			{
				return UnixStartTime.AddSeconds(GetValue<double>("_ts"));
			}
			internal set
			{
				SetValue("_ts", (ulong)(value - UnixStartTime).TotalSeconds);
			}
		}

		/// <summary>
		/// Gets the entity tag associated with the resource from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The entity tag associated with the resource.
		/// </value>
		/// <remarks>
		/// ETags are used for concurrency checking when updating resources. 
		/// </remarks>
		[JsonProperty(PropertyName = "_etag")]
		public string ETag
		{
			get
			{
				return GetValue<string>("_etag");
			}
			internal set
			{
				SetValue("_etag", value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Resource" /> class for the Azure Cosmos DB service.
		/// </summary>
		protected Resource()
		{
		}

		/// <summary>
		/// Copy constructor for a <see cref="T:Microsoft.Azure.Documents.Resource" /> used in the Azure Cosmos DB service.
		/// </summary>
		protected Resource(Resource resource)
		{
			Id = resource.Id;
			ResourceId = resource.ResourceId;
			SelfLink = resource.SelfLink;
			AltLink = resource.AltLink;
			Timestamp = resource.Timestamp;
			ETag = resource.ETag;
		}

		/// <summary>
		/// Sets property value associated with the specified property name in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="propertyName">The name of the property.</param>
		/// <param name="propertyValue">The property value.</param>
		public void SetPropertyValue(string propertyName, object propertyValue)
		{
			SetValue(propertyName, propertyValue);
		}

		/// <summary>
		/// Gets property value associated with the specified property name from the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of the property.</typeparam>
		/// <param name="propertyName">The name of the property.</param>
		/// <returns>The property value.</returns>
		public T GetPropertyValue<T>(string propertyName)
		{
			return GetValue<T>(propertyName);
		}

		/// <summary>
		/// Validates the property, by calling it, in case of any errors exception is thrown
		/// </summary>
		internal virtual void Validate()
		{
			GetValue<string>("id");
			GetValue<string>("_rid");
			GetValue<string>("_self");
			GetValue<double>("_ts");
			GetValue<string>("_etag");
		}

		/// <summary>
		/// Serialize to a byte array via SaveTo for the Azure Cosmos DB service.
		/// </summary>
		public byte[] ToByteArray()
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				SaveTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
	}
}
