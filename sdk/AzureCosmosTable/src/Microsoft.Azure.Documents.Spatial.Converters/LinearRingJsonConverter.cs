using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Spatial.Converters
{
	/// <summary>
	/// <see cref="T:Newtonsoft.Json.JsonConverter" /> for <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" />.
	/// </summary>
	internal sealed class LinearRingJsonConverter : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
		/// <param name="value">The existingValue.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			LinearRing linearRing = (LinearRing)value;
			serializer.Serialize(writer, linearRing.Positions);
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
			return new LinearRing(serializer.Deserialize<Position[]>(reader));
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
