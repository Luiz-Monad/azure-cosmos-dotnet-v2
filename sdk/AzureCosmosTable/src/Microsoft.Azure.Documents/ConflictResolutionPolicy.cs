using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the conflict resolution policy configuration for specifying how to resolve conflicts 
	/// in case writes from different regions result in conflicts on documents in the collection in the Azure Cosmos DB service.
	/// </summary>
	/// <example>
	/// A collection with custom conflict resolution with no user-registered stored procedure.
	/// <![CDATA[
	/// var collectionSpec = new DocumentCollection
	/// {
	///     Id = "Multi-master collection",
	///     ConflictResolutionPolicy policy = new ConflictResolutionPolicy
	///     {
	///         Mode = ConflictResolutionMode.Custom
	///     }
	/// };
	/// DocumentCollection collection = await client.CreateDocumentCollectionAsync(databaseLink, collectionSpec });
	/// ]]>
	/// </example>
	/// <example>
	/// A collection with custom conflict resolution with a user-registered stored procedure.
	/// <![CDATA[
	/// var collectionSpec = new DocumentCollection
	/// {
	///     Id = "Multi-master collection",
	///     ConflictResolutionPolicy policy = new ConflictResolutionPolicy
	///     {
	///         Mode = ConflictResolutionMode.Custom,
	///         ConflictResolutionProcedure = "conflictResolutionSprocName"
	///     }
	/// };
	/// DocumentCollection collection = await client.CreateDocumentCollectionAsync(databaseLink, collectionSpec });
	/// ]]>
	/// </example>
	/// <example>
	/// A collection with last writer wins conflict resolution, based on a path in the conflicting documents.
	/// <![CDATA[
	/// var collectionSpec = new DocumentCollection
	/// {
	///     Id = "Multi-master collection",
	///     ConflictResolutionPolicy policy = new ConflictResolutionPolicy
	///     {
	///         Mode = ConflictResolutionMode.LastWriterWins,
	///         ConflictResolutionPath = "/path/for/conflict/resolution"
	///     }
	/// };
	/// DocumentCollection collection = await client.CreateDocumentCollectionAsync(databaseLink, collectionSpec });
	/// ]]>
	/// </example>
	public sealed class ConflictResolutionPolicy : JsonSerializable
	{
		/// <summary>
		/// Gets or sets the <see cref="T:Microsoft.Azure.Documents.ConflictResolutionMode" /> in the Azure Cosmos DB service. By default it is <see cref="F:Microsoft.Azure.Documents.ConflictResolutionMode.LastWriterWins" />.
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.ConflictResolutionMode" /> enumeration.
		/// </value>
		[JsonProperty(PropertyName = "mode")]
		[JsonConverter(typeof(StringEnumConverter))]
		public ConflictResolutionMode Mode
		{
			get
			{
				ConflictResolutionMode result = ConflictResolutionMode.LastWriterWins;
				string value = GetValue<string>("mode");
				if (!string.IsNullOrEmpty(value))
				{
					result = (ConflictResolutionMode)Enum.Parse(typeof(ConflictResolutionMode), value, ignoreCase: true);
				}
				return result;
			}
			set
			{
				SetValue("mode", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the path which is present in each document in the Azure Cosmos DB service for last writer wins conflict-resolution.
		/// This path must be present in each document and must be an integer value.
		/// In case of a conflict occuring on a document, the document with the higher integer value in the specified path will be picked.
		/// If the path is unspecified, by default the <see cref="P:Microsoft.Azure.Documents.Resource.Timestamp" /> path will be used.
		/// </summary>
		/// <remarks>
		/// This value should only be set when using <see cref="F:Microsoft.Azure.Documents.ConflictResolutionMode.LastWriterWins" />
		/// </remarks>
		/// <value>
		/// <![CDATA[The path to check values for last-writer wins conflict resolution. That path is a rooted path of the property in the document, such as "/name/first".]]>
		/// </value>
		/// <example>
		/// <![CDATA[
		/// conflictResolutionPolicy.ConflictResolutionPath = "/name/first";
		/// ]]>
		/// </example>
		[JsonProperty(PropertyName = "conflictResolutionPath")]
		public string ConflictResolutionPath
		{
			get
			{
				return GetValue<string>("conflictResolutionPath");
			}
			set
			{
				SetValue("conflictResolutionPath", value);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> which is used for conflict resolution in the Azure Cosmos DB service.
		/// This stored procedure may be created after the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> is created and can be changed as required. 
		/// </summary>
		/// <remarks>
		/// 1. This value should only be set when using <see cref="F:Microsoft.Azure.Documents.ConflictResolutionMode.Custom" />
		/// 2. In case the stored procedure fails or throws an exception, the conflict resolution will default to registering conflicts in the conflicts feed"/&gt;.
		/// 3. The user can provide the stored procedure <see cref="P:Microsoft.Azure.Documents.Resource.Id" /> or <see cref="P:Microsoft.Azure.Documents.Resource.ResourceId" />.
		/// </remarks>
		/// <value>
		/// <![CDATA[The stored procedure to perform conflict resolution.]]>
		/// </value>
		/// <example>
		/// <![CDATA[
		/// conflictResolutionPolicy.ConflictResolutionProcedure = "/name/first";
		/// ]]>
		/// </example>
		[JsonProperty(PropertyName = "conflictResolutionProcedure")]
		public string ConflictResolutionProcedure
		{
			get
			{
				return GetValue<string>("conflictResolutionProcedure");
			}
			set
			{
				SetValue("conflictResolutionProcedure", value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.ConflictResolutionPolicy" /> class for the Azure Cosmos DB service.
		/// </summary>
		public ConflictResolutionPolicy()
		{
			Mode = ConflictResolutionMode.LastWriterWins;
		}
	}
}
