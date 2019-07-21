using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.Documents.Spatial.Converters
{
	/// <summary>
	/// Converter for <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> class.
	/// </summary>
	internal sealed class GeometryJsonConverter : JsonConverter
	{
		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
		/// </summary>
		/// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.</value>
		public override bool CanWrite => false;

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
			throw new NotImplementedException();
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
			JToken jToken = JToken.Load(reader);
			if (jToken.Type == JTokenType.Null)
			{
				return null;
			}
			if (jToken.Type != JTokenType.Object)
			{
				throw new JsonSerializationException(RMResources.SpatialInvalidGeometryType);
			}
			JToken jToken2 = jToken["type"];
			if (jToken2.Type != JTokenType.String)
			{
				throw new JsonSerializationException(RMResources.SpatialInvalidGeometryType);
			}
			Geometry geometry;
			switch (jToken2.Value<string>())
			{
			case "Point":
				geometry = new Point();
				break;
			case "MultiPoint":
				geometry = new MultiPoint();
				break;
			case "LineString":
				geometry = new LineString();
				break;
			case "MultiLineString":
				geometry = new MultiLineString();
				break;
			case "Polygon":
				geometry = new Polygon();
				break;
			case "MultiPolygon":
				geometry = new MultiPolygon();
				break;
			case "GeometryCollection":
				geometry = new GeometryCollection();
				break;
			default:
				throw new JsonSerializationException(RMResources.SpatialInvalidGeometryType);
			}
			serializer.Populate(jToken.CreateReader(), geometry);
			return geometry;
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
			return (object)typeof(Geometry) == objectType;
		}
	}
}
