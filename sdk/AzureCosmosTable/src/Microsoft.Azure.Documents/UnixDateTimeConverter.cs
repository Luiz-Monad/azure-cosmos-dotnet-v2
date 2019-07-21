using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Converts a DateTime object to and from JSON.
	/// DateTime is represented as the total number of seconds
	/// that have elapsed since January 1, 1970 (midnight UTC/GMT), 
	/// not counting leap seconds (in ISO 8601: 1970-01-01T00:00:00Z).
	/// </summary>
	public sealed class UnixDateTimeConverter : DateTimeConverterBase
	{
		private static DateTime UnixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Writes the JSON representation of the DateTime object.
		/// </summary>
		/// <param name="writer">The Newtonsoft.Json.JsonWriter to write to.</param>
		/// <param name="value">The value.</param>
		/// <param name="serializer">The calling serializer.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is DateTime)
			{
				long value2 = (long)((DateTime)value - UnixStartTime).TotalSeconds;
				writer.WriteValue(value2);
				return;
			}
			throw new ArgumentException(RMResources.DateTimeConverterInvalidDateTime, "value");
		}

		/// <summary>
		/// Reads the JSON representation of the DateTime object.
		/// </summary>
		/// <param name="reader">The Newtonsoft.Json.JsonReader to read from.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="existingValue">The existing value of object being read.</param>
		/// <param name="serializer">The calling serializer.</param>
		/// <returns>
		/// The DateTime object value.
		/// </returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.Integer)
			{
				throw new Exception(RMResources.DateTimeConverterInvalidReaderValue);
			}
			double num = 0.0;
			try
			{
				num = Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
			}
			catch
			{
				throw new Exception(RMResources.DateTimeConveterInvalidReaderDoubleValue);
			}
			return UnixStartTime.AddSeconds(num);
		}
	}
}
