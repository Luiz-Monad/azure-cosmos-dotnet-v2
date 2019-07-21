using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the schema discovery policy configuration for a collection.
	/// </summary> 
	/// <remarks>
	/// The schema discovery policy is used to control the schema builder through a collection configuration.
	/// </remarks>
	/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
	internal sealed class SchemaDiscoveryPolicy : JsonSerializable, ICloneable
	{
		/// <summary>
		/// Gets or sets the indexing mode (consistent or lazy).
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.SchemaBuilderMode" /> enumeration.
		/// </value>
		[JsonProperty(PropertyName = "mode")]
		[JsonConverter(typeof(StringEnumConverter))]
		public SchemaBuilderMode SchemaBuilderMode
		{
			get
			{
				SchemaBuilderMode result = SchemaBuilderMode.Lazy;
				string value = GetValue<string>("mode");
				if (!string.IsNullOrEmpty(value))
				{
					result = (SchemaBuilderMode)Enum.Parse(typeof(SchemaBuilderMode), value, ignoreCase: true);
				}
				return result;
			}
			set
			{
				SetValue("mode", value.ToString());
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.SchemaDiscoveryPolicy" /> class.
		/// </summary>
		/// <remarks>
		/// Schema mode is set to none
		/// </remarks>
		public SchemaDiscoveryPolicy()
		{
			SchemaBuilderMode = SchemaBuilderMode.None;
		}

		/// <summary>
		/// Performs a deep copy of the schema discovery policy.
		/// </summary>
		/// <returns>
		/// A clone of the schema discovery policy.
		/// </returns>
		public object Clone()
		{
			return new SchemaDiscoveryPolicy();
		}
	}
}
