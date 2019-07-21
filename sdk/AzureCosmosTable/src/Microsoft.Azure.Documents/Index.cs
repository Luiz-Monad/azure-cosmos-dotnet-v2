using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Base class for IndexingPolicy Indexes in the Azure Cosmos DB service, you should use a concrete Index like HashIndex or RangeIndex.
	/// </summary> 
	[JsonConverter(typeof(IndexJsonConverter))]
	public abstract class Index : JsonSerializable
	{
		/// <summary>
		/// Gets or sets the kind of indexing to be applied in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.IndexKind" /> enumeration.
		/// </value>
		[JsonProperty(PropertyName = "kind")]
		[JsonConverter(typeof(StringEnumConverter))]
		public IndexKind Kind
		{
			get
			{
				IndexKind result = IndexKind.Hash;
				string value = GetValue<string>("kind");
				if (!string.IsNullOrEmpty(value))
				{
					result = (IndexKind)Enum.Parse(typeof(IndexKind), value, ignoreCase: true);
				}
				return result;
			}
			private set
			{
				SetValue("kind", value.ToString());
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Index" /> class for the Azure Cosmos DB service.
		/// </summary>
		protected Index(IndexKind kind)
		{
			Kind = kind;
		}

		/// <summary>
		/// Returns an instance of the <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> class with specified DataType for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <returns>An instance of <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> type.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.DataType" />
		/// <example>
		/// Here is an example to create RangeIndex instance passing in the DataType:
		/// <code language="c#">
		/// <![CDATA[
		/// RangeIndex rangeIndex = Index.Range(DataType.Number);
		/// ]]>
		/// </code>
		/// </example>
		public static RangeIndex Range(DataType dataType)
		{
			return new RangeIndex(dataType);
		}

		/// <summary>
		/// Returns an instance of the <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> class with specified DataType and precision for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <param name="precision">Specifies the precision to be used for the data type associated with this index.</param>
		/// <returns>An instance of <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> type.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.DataType" />
		/// <example>
		/// Here is an example to create RangeIndex instance passing in the DataType and precision:
		/// <code language="c#">
		/// <![CDATA[
		/// RangeIndex rangeIndex = Index.Range(DataType.Number, -1);
		/// ]]>
		/// </code>
		/// </example>
		public static RangeIndex Range(DataType dataType, short precision)
		{
			return new RangeIndex(dataType, precision);
		}

		/// <summary>
		/// Returns an instance of the <see cref="T:Microsoft.Azure.Documents.HashIndex" /> class with specified DataType for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <returns>An instance of <see cref="T:Microsoft.Azure.Documents.HashIndex" /> type.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.DataType" />
		/// <example>
		/// Here is an example to create HashIndex instance passing in the DataType:
		/// <code language="c#">
		/// <![CDATA[
		/// HashIndex hashIndex = Index.Hash(DataType.String);
		/// ]]>
		/// </code>
		/// </example>
		public static HashIndex Hash(DataType dataType)
		{
			return new HashIndex(dataType);
		}

		/// <summary>
		/// Returns an instance of the <see cref="T:Microsoft.Azure.Documents.HashIndex" /> class with specified DataType and precision for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <param name="precision">Specifies the precision to be used for the data type associated with this index.</param>
		/// <returns>An instance of <see cref="T:Microsoft.Azure.Documents.HashIndex" /> type.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.DataType" />
		/// <example>
		/// Here is an example to create HashIndex instance passing in the DataType and precision:
		/// <code language="c#">
		/// <![CDATA[
		/// HashIndex hashIndex = Index.Hash(DataType.String, 3);
		/// ]]>
		/// </code>
		/// </example>
		public static HashIndex Hash(DataType dataType, short precision)
		{
			return new HashIndex(dataType, precision);
		}

		/// <summary>
		/// Returns an instance of the <see cref="T:Microsoft.Azure.Documents.SpatialIndex" /> class with specified DataType for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <returns>An instance of <see cref="T:Microsoft.Azure.Documents.SpatialIndex" /> type.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.DataType" />
		/// <example>
		/// Here is an example to create SpatialIndex instance passing in the DataType:
		/// <code language="c#">
		/// <![CDATA[
		/// SpatialIndex spatialIndex = Index.Spatial(DataType.Point);
		/// ]]>
		/// </code>
		/// </example>
		public static SpatialIndex Spatial(DataType dataType)
		{
			return new SpatialIndex(dataType);
		}
	}
}
