using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.Documents.Spatial.Converters
{
	/// <summary>
	/// <see cref="T:Newtonsoft.Json.JsonConverter" /> for <see cref="T:Microsoft.Azure.Documents.Spatial.Crs" /> class and all its implementations.
	/// </summary>
	internal sealed class CrsJsonConverter : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Crs crs = (Crs)value;
			switch (crs.Type)
			{
			case CrsType.Linked:
			{
				LinkedCrs linkedCrs = (LinkedCrs)crs;
				writer.WriteStartObject();
				writer.WritePropertyName("type");
				writer.WriteValue("link");
				writer.WritePropertyName("properties");
				writer.WriteStartObject();
				writer.WritePropertyName("href");
				writer.WriteValue(linkedCrs.Href);
				if (linkedCrs.HrefType != null)
				{
					writer.WritePropertyName("type");
					writer.WriteValue(linkedCrs.HrefType);
				}
				writer.WriteEndObject();
				writer.WriteEndObject();
				break;
			}
			case CrsType.Named:
			{
				NamedCrs namedCrs = (NamedCrs)crs;
				writer.WriteStartObject();
				writer.WritePropertyName("type");
				writer.WriteValue("name");
				writer.WritePropertyName("properties");
				writer.WriteStartObject();
				writer.WritePropertyName("name");
				writer.WriteValue(namedCrs.Name);
				writer.WriteEndObject();
				writer.WriteEndObject();
				break;
			}
			case CrsType.Unspecified:
				writer.WriteNull();
				break;
			}
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>
		/// The object value.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JToken jToken = JToken.Load(reader);
			if (jToken.Type == JTokenType.Null)
			{
				return Crs.Unspecified;
			}
			if (jToken.Type != JTokenType.Object)
			{
				throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
			}
			JToken jToken2 = jToken["properties"];
			if (jToken2 == null || jToken2.Type != JTokenType.Object)
			{
				throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
			}
			JToken jToken3 = jToken["type"];
			if (jToken3 == null || jToken3.Type != JTokenType.String)
			{
				throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
			}
			string a = jToken3.Value<string>();
			if (!(a == "name"))
			{
				if (a == "link")
				{
					JToken jToken4 = jToken2["href"];
					JToken jToken5 = jToken2["type"];
					if (jToken4 == null || jToken4.Type != JTokenType.String || (jToken5 != null && jToken4.Type != JTokenType.String))
					{
						throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
					}
					return new LinkedCrs(jToken4.Value<string>(), jToken5.Value<string>());
				}
				throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
			}
			JToken jToken6 = jToken2["name"];
			if (jToken6 == null || jToken6.Type != JTokenType.String)
			{
				throw new JsonSerializationException(RMResources.SpatialFailedToDeserializeCrs);
			}
			return new NamedCrs(jToken6.Value<string>());
		}

		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanConvert(Type objectType)
		{
			return typeof(Crs).IsAssignableFrom(objectType);
		}
	}
}
