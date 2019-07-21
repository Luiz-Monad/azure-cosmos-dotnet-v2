using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Used to lazily bind a item from a query.
	/// </summary>
	internal sealed class QueryItem
	{
		private static readonly JsonSerializerSettings NoDateParseHandlingJsonSerializerSettings = new JsonSerializerSettings
		{
			DateParseHandling = DateParseHandling.None
		};

		/// <summary>
		/// whether or not the item has been deserizalized yet.
		/// </summary>
		private bool isItemDeserialized;

		/// <summary>
		/// The actual item.
		/// </summary>
		private object item;

		/// <summary>
		/// The raw value of the item.
		/// </summary>
		[JsonProperty("item")]
		private JRaw RawItem
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the item and deserializes it if it hasn't already been.
		/// </summary>
		/// <remarks>This can be replaced with Lazy of T</remarks>
		/// <returns>The item.</returns>
		public object GetItem()
		{
			if (!isItemDeserialized)
			{
				if (RawItem == null)
				{
					item = Undefined.Value;
				}
				else
				{
					item = JsonConvert.DeserializeObject((string)RawItem.Value, NoDateParseHandlingJsonSerializerSettings);
				}
				isItemDeserialized = true;
			}
			return item;
		}
	}
}
