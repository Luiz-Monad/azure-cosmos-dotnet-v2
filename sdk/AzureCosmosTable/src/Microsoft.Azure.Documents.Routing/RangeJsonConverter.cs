using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class RangeJsonConverter : JsonConverter
	{
		private static readonly string MinProperty = "min";

		private static readonly string MaxProperty = "max";

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			try
			{
				Range<string> range = (Range<string>)value;
				writer.WriteStartObject();
				writer.WritePropertyName(MinProperty);
				serializer.Serialize(writer, range.Min);
				writer.WritePropertyName(MaxProperty);
				serializer.Serialize(writer, range.Max);
				writer.WriteEndObject();
			}
			catch (Exception innerException)
			{
				throw new JsonSerializationException(string.Empty, innerException);
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				JObject jObject = JObject.Load(reader);
				return new Range<string>(jObject[MinProperty].Value<string>(), jObject[MaxProperty].Value<string>(), isMinInclusive: true, isMaxInclusive: false);
			}
			catch (Exception innerException)
			{
				throw new JsonSerializationException(string.Empty, innerException);
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(PartitionKeyRange).IsAssignableFrom(objectType);
		}
	}
}
