using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a per-User permission to access a specific resource in the Azure Cosmos DB service, for example Document or Collection.
	/// </summary>
	public class Permission : Resource
	{
		/// <summary> 
		/// Gets or sets the self-link of resource to which the permission applies in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link of the resource to which the permission applies.
		/// </value>
		[JsonProperty(PropertyName = "resource")]
		public string ResourceLink
		{
			get
			{
				return GetValue<string>("resource");
			}
			set
			{
				SetValue("resource", value);
			}
		}

		/// <summary>
		/// Gets or sets optional partition key value for the permission in the Azure Cosmos DB service.
		/// A permission applies to resources when two conditions are met:
		///       1. <see cref="P:Microsoft.Azure.Documents.Permission.ResourceLink" /> is prefix of resource's link.
		///             For example "/dbs/mydatabase/colls/mycollection" applies to "/dbs/mydatabase/colls/mycollection" and "/dbs/mydatabase/colls/mycollection/docs/mydocument"
		///       2. <see cref="P:Microsoft.Azure.Documents.Permission.ResourcePartitionKey" /> is superset of resource's partition key.
		///             For example absent/empty partition key is superset of all partition keys.
		/// </summary>
		[JsonProperty(PropertyName = "resourcePartitionKey")]
		public PartitionKey ResourcePartitionKey
		{
			get
			{
				PartitionKeyInternal value = GetValue<PartitionKeyInternal>("resourcePartitionKey");
				if (value != null)
				{
					return new PartitionKey(value.ToObjectArray()[0]);
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					SetValue("resourcePartitionKey", value.InternalKey);
				}
			}
		}

		/// <summary>
		/// Gets or sets the permission mode in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The <see cref="P:Microsoft.Azure.Documents.Permission.PermissionMode" /> mode: Read or All.
		/// </value>
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty(PropertyName = "permissionMode")]
		public PermissionMode PermissionMode
		{
			get
			{
				return GetValue("permissionMode", PermissionMode.All);
			}
			set
			{
				SetValue("permissionMode", value.ToString());
			}
		}

		/// <summary>
		/// Gets the access token granting the defined permission from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The access token granting the defined permission.
		/// </value>
		[JsonProperty(PropertyName = "_token")]
		public string Token
		{
			get
			{
				return GetValue<string>("_token");
			}
		}
	}
}
