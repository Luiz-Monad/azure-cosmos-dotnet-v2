using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// DOM for a composite path.
	/// A composite path is used in a composite index.
	/// For example if you want to run a query like "SELECT * FROM c ORDER BY c.age, c.height",
	/// then you need to add "/age" and "/height" as composite paths to your composite index.
	/// </summary>
	public sealed class CompositePath : JsonSerializable, ICloneable
	{
		/// <summary>
		/// Gets or sets the full path in a document used for composite indexing.
		/// We do not support wildcards in the path.
		/// </summary>
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
		/// Gets or sets the sort order for the composite path.
		/// For example if you want to run the query "SELECT * FROM c ORDER BY c.age asc, c.height desc",
		/// then you need to make the order for "/age" "ascending" and the order for "/height" "descending".
		/// </summary>
		[JsonProperty(PropertyName = "order")]
		[JsonConverter(typeof(StringEnumConverter))]
		public CompositePathSortOrder Order
		{
			get
			{
				CompositePathSortOrder result = CompositePathSortOrder.Ascending;
				string value = GetValue<string>("order");
				if (!string.IsNullOrEmpty(value))
				{
					result = (CompositePathSortOrder)Enum.Parse(typeof(CompositePathSortOrder), value, ignoreCase: true);
				}
				return result;
			}
			set
			{
				SetValue("order", value);
			}
		}

		/// <summary>
		/// Clones the composite path.
		/// </summary>
		/// <returns>The cloned composite path.</returns>
		public object Clone()
		{
			return new CompositePath
			{
				Path = Path,
				Order = Order
			};
		}
	}
}
