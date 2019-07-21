using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies a path within a JSON document to be included in the Azure Cosmos DB service.
	/// </summary>
	public sealed class IncludedPath : JsonSerializable, ICloneable
	{
		private Collection<Index> indexes;

		/// <summary>
		/// Gets or sets the path to be indexed in the Azure Cosmos DB service.
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
		/// Gets or sets the collection of <see cref="T:Microsoft.Azure.Documents.Index" /> objects to be applied for this included path in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The collection of the <see cref="T:Microsoft.Azure.Documents.Index" /> objects to be applied for this included path.
		/// </value>
		[JsonProperty(PropertyName = "indexes")]
		public Collection<Index> Indexes
		{
			get
			{
				if (indexes == null)
				{
					indexes = GetValue<Collection<Index>>("indexes");
					if (indexes == null)
					{
						indexes = new Collection<Index>();
					}
				}
				return indexes;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, RMResources.PropertyCannotBeNull, "Indexes"));
				}
				indexes = value;
				SetValue("indexes", value);
			}
		}

		internal override void OnSave()
		{
			if (indexes != null)
			{
				foreach (Index index in indexes)
				{
					index.OnSave();
				}
				SetValue("indexes", indexes);
			}
		}

		/// <summary>
		/// Creates a copy of the included path in the Azure Cosmos DB service. 
		/// </summary>
		/// <returns>A clone of the included path.</returns>
		public object Clone()
		{
			IncludedPath includedPath = new IncludedPath
			{
				Path = Path
			};
			foreach (Index index in Indexes)
			{
				includedPath.Indexes.Add(index);
			}
			return includedPath;
		}
	}
}
