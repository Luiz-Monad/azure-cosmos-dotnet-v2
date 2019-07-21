using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class PartitionKeyInternalJsonConverter : JsonConverter
	{
		private const string Type = "type";

		private const string MinNumber = "MinNumber";

		private const string MaxNumber = "MaxNumber";

		private const string MinString = "MinString";

		private const string MaxString = "MaxString";

		private const string Infinity = "Infinity";

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			PartitionKeyInternal partitionKeyInternal = (PartitionKeyInternal)value;
			if (partitionKeyInternal.Equals(PartitionKeyInternal.ExclusiveMaximum))
			{
				writer.WriteValue("Infinity");
				return;
			}
			writer.WriteStartArray();
			foreach (IPartitionKeyComponent component in partitionKeyInternal.Components)
			{
				component.JsonEncode(writer);
			}
			writer.WriteEndArray();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JToken jToken = JToken.Load(reader);
			if (jToken.Type == JTokenType.String && jToken.Value<string>() == "Infinity")
			{
				return PartitionKeyInternal.ExclusiveMaximum;
			}
			List<object> list = new List<object>();
			if (jToken.Type == JTokenType.Array)
			{
				foreach (JToken item in (JArray)jToken)
				{
					if (item is JObject)
					{
						JObject jObject = (JObject)item;
						if (!jObject.Properties().Any())
						{
							list.Add(Undefined.Value);
						}
						else
						{
							bool flag = false;
							if (jObject.TryGetValue("type", out JToken value) && value.Type == JTokenType.String)
							{
								flag = true;
								if (value.Value<string>() == "MinNumber")
								{
									list.Add(Microsoft.Azure.Documents.Routing.MinNumber.Value);
								}
								else if (value.Value<string>() == "MaxNumber")
								{
									list.Add(Microsoft.Azure.Documents.Routing.MaxNumber.Value);
								}
								else if (value.Value<string>() == "MinString")
								{
									list.Add(Microsoft.Azure.Documents.Routing.MinString.Value);
								}
								else if (value.Value<string>() == "MaxString")
								{
									list.Add(Microsoft.Azure.Documents.Routing.MaxString.Value);
								}
								else
								{
									flag = false;
								}
							}
							if (!flag)
							{
								throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, RMResources.UnableToDeserializePartitionKeyValue, jToken));
							}
						}
					}
					else
					{
						if (!(item is JValue))
						{
							throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, RMResources.UnableToDeserializePartitionKeyValue, jToken));
						}
						list.Add(((JValue)item).Value);
					}
				}
				return PartitionKeyInternal.FromObjectArray(list, strict: true);
			}
			throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, RMResources.UnableToDeserializePartitionKeyValue, jToken));
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(PartitionKeyInternal).IsAssignableFrom(objectType);
		}

		public static void JsonEncode(MinNumberPartitionKeyComponent component, JsonWriter writer)
		{
			JsonEncodeLimit(writer, "MinNumber");
		}

		public static void JsonEncode(MaxNumberPartitionKeyComponent component, JsonWriter writer)
		{
			JsonEncodeLimit(writer, "MaxNumber");
		}

		public static void JsonEncode(MinStringPartitionKeyComponent component, JsonWriter writer)
		{
			JsonEncodeLimit(writer, "MinString");
		}

		public static void JsonEncode(MaxStringPartitionKeyComponent component, JsonWriter writer)
		{
			JsonEncodeLimit(writer, "MaxString");
		}

		private static void JsonEncodeLimit(JsonWriter writer, string value)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("type");
			writer.WriteValue(value);
			writer.WriteEndObject();
		}
	}
}
