using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial.Converters
{
	/// <summary>
	/// <see cref="T:Newtonsoft.Json.JsonConverter" /> for <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" />.
	/// </summary>
	internal sealed class PolygonCoordinatesJsonConverter : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The existingValue.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			PolygonCoordinates obj = (PolygonCoordinates)value;
			writer.WriteStartArray();
			foreach (LinearRing ring in obj.Rings)
			{
				serializer.Serialize(writer, ring.Positions);
			}
			writer.WriteEndArray();
		}

		/// <summary>
		/// Reads the JSON representation of the object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing existingValue of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>
		/// Deserialized object.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return new PolygonCoordinates((from c in serializer.Deserialize<Position[][]>(reader)
			select new LinearRing(c)).ToList());
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
			return typeof(IEnumerable<LinearRing>).IsAssignableFrom(objectType);
		}
	}
}
