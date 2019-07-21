using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies the indexing policy for a JSON path within documents in a Azure Cosmos DB collection.
	/// </summary>
	/// <remarks>
	/// Within documents, you can choose which paths must be included or excluded from indexing. This can offer improved write 
	/// performance and lower index storage for scenarios when the query patterns are known beforehand. Index paths start with 
	/// the root (/) and typically end with the ? wildcard operator, denoting that there are multiple possible values for the 
	/// prefix. 
	/// For e.g., to serve SELECT * FROM Families F WHERE F.familyName = "Andersen", you must include an index path 
	/// for /"familyName"/?in the collection’s index policy.
	///
	/// Index paths can also use the * wildcard operator to specify the behavior for paths recursively under the prefix. 
	/// For e.g., /"payload"/* can be used to exclude everything under the payload property from indexing.
	///
	/// For additional details, refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy
	/// </remarks>
	/// <example>
	/// The following example indexes a collection for queries like "SELECT * FROM docs d WHERE d.CreatedTimestamp &gt; 5555".
	/// <code language="c#">
	/// <![CDATA[
	/// var collection = new DocumentCollection { Id = "myCollection" };
	/// collection.IndexingPolicy.IncludedPaths.Add(
	///     new IndexingPath 
	///     {
	///         IndexType = IndexType.Range,
	///         Path = "/\"CreatedTimestamp\"/?",
	///         NumericPrecision = 7
	///     });
	/// collection.IndexingPolicy.IncludedPaths.Add(
	///     new IndexingPath 
	///     {
	///         Path = "/"
	///     });
	///
	/// //create collection.
	/// ]]>
	/// </code>
	/// </example>
	/// <seealso cref="T:Microsoft.Azure.Documents.IndexingPolicy" />
	internal sealed class IndexingPath : JsonSerializable, ICloneable
	{
		/// <summary>
		/// Gets or sets the path to be indexed.
		/// </summary>
		/// <value>
		/// The path to be indexed.
		/// </value>
		/// <remarks>
		/// Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy for how to specify paths.
		/// Some valid examples: /"prop"/?, /"prop"/**, /"prop"/"subprop"/?, /"prop"/[]/?
		/// </remarks>
		[JsonProperty(PropertyName = "path")]
		public string Path
		{
			get
			{
				return GetValue<string>("path");
			}
			set
			{
				SetValue("path", value);
			}
		}

		/// <summary>
		/// Gets or sets the type of indexing to be applied.
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.IndexType" /> enumeration.
		/// </value>
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty(PropertyName = "IndexType")]
		public IndexType IndexType
		{
			get
			{
				IndexType result = IndexType.Hash;
				string value = GetValue<string>("IndexType");
				if (!string.IsNullOrEmpty(value))
				{
					Enum.TryParse(value, ignoreCase: true, out result);
				}
				return result;
			}
			set
			{
				SetValue("IndexType", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets the precision for this particular index path for numeric data.
		/// </summary>
		/// <value>
		/// The precision for this particular index path for numeric data.
		/// </value>
		/// <remarks>Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy for valid ranges of values.</remarks>
		[JsonProperty(PropertyName = "NumericPrecision")]
		public int? NumericPrecision
		{
			get
			{
				int? result = null;
				string value = GetValue<string>("NumericPrecision");
				if (!string.IsNullOrEmpty(value))
				{
					result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
				}
				return result;
			}
			set
			{
				SetValue("NumericPrecision", value);
			}
		}

		/// <summary>
		/// Gets or sets the precision for this particular index path for string data.
		/// </summary>
		/// <value>
		/// The precision for this particular index path for string data.
		/// </value>
		/// <remarks>Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy for valid ranges of values.</remarks>
		[JsonProperty(PropertyName = "StringPrecision")]
		public int? StringPrecision
		{
			get
			{
				int? result = null;
				string value = GetValue<string>("StringPrecision");
				if (!string.IsNullOrEmpty(value))
				{
					result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
				}
				return result;
			}
			set
			{
				SetValue("StringPrecision", value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.IndexingPath" /> class.
		/// </summary>
		public IndexingPath()
		{
			IndexType = IndexType.Hash;
		}

		/// <summary>
		/// Creates a copy of the indexing path.
		/// </summary>
		/// <returns>A clone of the indexing path.</returns>
		public object Clone()
		{
			return new IndexingPath
			{
				IndexType = IndexType,
				NumericPrecision = NumericPrecision,
				StringPrecision = StringPrecision,
				Path = Path
			};
		}
	}
}
