using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.Documents.Query
{
	internal sealed class AggregateItem
	{
		private static readonly JsonSerializerSettings NoDateParseHandlingJsonSerializerSettings = new JsonSerializerSettings
		{
			DateParseHandling = DateParseHandling.None
		};

		private readonly Lazy<object> item;

		[JsonProperty("item")]
		private JRaw RawItem
		{
			get;
			set;
		}

		[JsonProperty("item2")]
		private JRaw RawItem2
		{
			get;
			set;
		}

		public AggregateItem(JRaw rawItem, JRaw rawItem2)
		{
			RawItem = rawItem;
			RawItem2 = rawItem2;
			item = new Lazy<object>(InitLazy);
		}

		private object InitLazy()
		{
			object result = (RawItem != null) ? JsonConvert.DeserializeObject((string)RawItem.Value, NoDateParseHandlingJsonSerializerSettings) : Undefined.Value;
			if (RawItem2 != null)
			{
				result = JsonConvert.DeserializeObject((string)RawItem2.Value, NoDateParseHandlingJsonSerializerSettings);
			}
			return result;
		}

		public object GetItem()
		{
			return item.Value;
		}
	}
}
