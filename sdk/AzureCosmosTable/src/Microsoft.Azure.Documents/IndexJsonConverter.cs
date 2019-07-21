using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal sealed class IndexJsonConverter : JsonConverter
	{
		public override bool CanWrite => false;

		public override bool CanConvert(Type objectType)
		{
			return typeof(Index).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if ((object)objectType != typeof(Index))
			{
				return null;
			}
			JToken jToken = JToken.Load(reader);
			if (jToken.Type == JTokenType.Null)
			{
				return null;
			}
			if (jToken.Type != JTokenType.Object)
			{
				throw new JsonSerializationException(string.Format(CultureInfo.CurrentCulture, RMResources.InvalidIndexSpecFormat));
			}
			JToken jToken2 = jToken["kind"];
			if (jToken2 == null || jToken2.Type != JTokenType.String)
			{
				throw new JsonSerializationException(string.Format(CultureInfo.CurrentCulture, RMResources.InvalidIndexSpecFormat));
			}
			IndexKind result = IndexKind.Hash;
			if (Enum.TryParse(jToken2.Value<string>(), out result))
			{
				object obj = null;
				switch (result)
				{
				case IndexKind.Hash:
					obj = new HashIndex();
					break;
				case IndexKind.Range:
					obj = new RangeIndex();
					break;
				case IndexKind.Spatial:
					obj = new SpatialIndex();
					break;
				default:
					throw new JsonSerializationException(string.Format(CultureInfo.CurrentCulture, RMResources.InvalidIndexKindValue, result));
				}
				serializer.Populate(jToken.CreateReader(), obj);
				return obj;
			}
			throw new JsonSerializationException(string.Format(CultureInfo.CurrentCulture, RMResources.InvalidIndexKindValue, jToken2.Value<string>()));
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
