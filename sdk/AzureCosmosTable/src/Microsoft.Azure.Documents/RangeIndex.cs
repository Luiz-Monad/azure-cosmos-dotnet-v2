using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Specifies an instance of the <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> class in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// Can be used to serve queries like: SELECT * FROM docs d WHERE d.prop &gt; 5.
	/// </remarks>
	public sealed class RangeIndex : Index, ICloneable
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

		/// <summary>
		/// Gets or sets the precision for this particular index in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The precision for this particular index. Returns null, if not set.
		/// </value>
		/// <remarks>Refer to http://azure.microsoft.com/documentation/articles/documentdb-indexing-policies/#ConfigPolicy for valid ranges of values.</remarks>
		[JsonProperty(PropertyName = "precision", NullValueHandling = NullValueHandling.Ignore)]
		public short? Precision
		{
			get
			{
				short? result = null;
				string value = GetValue<string>("precision");
				if (!string.IsNullOrEmpty(value))
				{
					result = Convert.ToInt16(value, CultureInfo.InvariantCulture);
				}
				return result;
			}
			set
			{
				SetValue("precision", value);
			}
		}

		internal RangeIndex()
			: base(IndexKind.Range)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> class with specified DataType for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <seealso cref="P:Microsoft.Azure.Documents.RangeIndex.DataType" />
		/// <example>
		/// Here is an example to instantiate RangeIndex class passing in the DataType:
		/// <code language="c#">
		/// <![CDATA[
		/// RangeIndex rangeIndex = new RangeIndex(DataType.Number);
		/// ]]>
		/// </code>
		/// </example>
		public RangeIndex(DataType dataType)
			: this()
		{
			DataType = dataType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.RangeIndex" /> class with specified DataType and precision for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="dataType">Specifies the target data type for the index path specification.</param>
		/// <param name="precision">Specifies the precision to be used for the data type associated with this index.</param>
		/// <seealso cref="P:Microsoft.Azure.Documents.RangeIndex.DataType" />
		/// <example>
		/// Here is an example to instantiate RangeIndex class passing in the DataType and precision:
		/// <code language="c#">
		/// <![CDATA[
		/// RangeIndex rangeIndex = new RangeIndex(DataType.Number, -1);
		/// ]]>
		/// </code>
		/// </example>
		public RangeIndex(DataType dataType, short precision)
			: this(dataType)
		{
			Precision = precision;
		}

		/// <summary>
		/// Creates a copy of the range index for the Azure Cosmos DB service.
		/// </summary>
		/// <returns>A clone of the range index.</returns>
		public object Clone()
		{
			return new RangeIndex(DataType)
			{
				Precision = Precision
			};
		}
	}
}
