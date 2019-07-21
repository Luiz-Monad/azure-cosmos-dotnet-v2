using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies a path within a JSON document to be excluded while indexing data for the Azure Cosmos DB service.
	/// </summary>
	public sealed class ExcludedPath : JsonSerializable, ICloneable
	{
		/// <summary>
		/// Gets or sets the path to be excluded from indexing in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The path to be excluded from indexing.
		/// </value>
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
		/// Creates a copy of the excluded path in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>A clone of the excluded path.</returns>
		public object Clone()
		{
			return new ExcludedPath
			{
				Path = Path
			};
		}
	}
}
