using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Documents.Spatial.Converters
{
	/// <summary>
	/// Converter for <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> class.
	/// </summary>
	internal sealed class PositionJsonConverter : JsonConverter
	{
		/// <summary>
		/// Writes the JSON representation of the object.
		/// </summary>
		/// <param name="writer">
		/// The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.
		/// </param>
		/// <param name="value">The existingValue.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Position position = (Position)value;
			serializer.Serialize(writer, position.Coordinates);
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
			double[] array = serializer.Deserialize<double[]>(reader);
			if (array == null || array.Length < 2)
			{
				throw new JsonSerializationException(RMResources.SpatialInvalidPosition);
			}
			return new Position(array);
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
			return (object)objectType == typeof(Position);
		}
	}
}
