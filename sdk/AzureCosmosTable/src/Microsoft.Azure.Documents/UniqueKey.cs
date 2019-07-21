using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a unique key on that enforces uniqueness constraint on documents in the collection in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// 1) For partitioned collections, the value of partition key is implicitly a part of each unique key.
	/// 2) Uniqueness constraint is also enforced for missing values.
	/// For instance, if unique key policy defines a unique key with single property path, there could be only one document that has missing value for this property.
	/// </remarks>
	/// <seealso cref="T:Microsoft.Azure.Documents.UniqueKeyPolicy" />
	public sealed class UniqueKey : JsonSerializable
	{
		private Collection<string> paths;

		/// <summary>
		/// Gets or sets the paths, a set of which must be unique for each document in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// <![CDATA[The paths to enforce uniqueness on. Each path is a rooted path of the unique property in the document, such as "/name/first".]]>
		/// </value>
		/// <example>
		/// <![CDATA[
		/// uniqueKey.Paths = new Collection<string> { "/name/first", "/name/last" };
		/// ]]>
		/// </example>
		[JsonProperty(PropertyName = "paths")]
		public Collection<string> Paths
		{
			get
			{
				if (paths == null)
				{
					paths = GetValue<Collection<string>>("paths");
					if (paths == null)
					{
						paths = new Collection<string>();
					}
				}
				return paths;
			}
			set
			{
				paths = value;
				SetValue("paths", value);
			}
		}

		internal override void OnSave()
		{
			if (paths != null)
			{
				SetValue("paths", paths);
			}
		}
	}
}
