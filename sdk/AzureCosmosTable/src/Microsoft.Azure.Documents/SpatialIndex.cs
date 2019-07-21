using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Specifies an instance of the <see cref="T:Microsoft.Azure.Documents.SpatialIndex" /> class in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// Can be used to serve spatial queries.
	/// </remarks>
	public sealed class SpatialIndex : Index, ICloneable
	{
		/// <summary>
		/// Gets or sets the data type for which this index should be applied in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The data type for which this index should be applied.
		/// </value>
		/// <remarks>Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy for valid ranges of values.</remarks>
		[JsonProperty(PropertyName = "dataType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public DataType DataType
		{
			get
			{
				DataType result = DataType.Number;
				string value = GetValue<string>("dataType");
				if (!string.IsNullOrEmpty(value))
				{
					result = (DataType)Enum.Parse(typeof(DataType), value, ignoreCase: true);
				}
				return result;
			}
			set
			{
				SetValue("dataType", value.ToString());
			}
		}

		internal SpatialIndex()
			: base(IndexKind.Spatial)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.SpatialIndex" /> class for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification</param>
		/// <seealso cref="P:Microsoft.Azure.Documents.SpatialIndex.DataType" />
		/// <example>
		/// Here is an example to instantiate SpatialIndex class passing in the DataType
		/// <code language="c#">
		/// <![CDATA[
		/// SpatialIndex spatialIndex = new SpatialIndex(DataType.Point);
		/// ]]>
		/// </code>
		/// </example>
		public SpatialIndex(DataType dataType)
			: this()
		{
			DataType = dataType;
		}

		/// <summary>
		/// Creates a copy of the spatial index for the Azure Cosmos DB service.
		/// </summary>
		/// <returns>A clone of the spatial index.</returns>
		public object Clone()
		{
			return new SpatialIndex(DataType);
		}
	}
}
