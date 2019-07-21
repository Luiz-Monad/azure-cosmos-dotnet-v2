using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// <para>
	/// For cross partition order by queries a query like "SELECT c.id, c.field_0 ORDER BY r.field_7 gets rewritten as:
	/// SELECT r._rid, [{"item": r.field_7}] AS orderByItems, {"id": r.id, "field_0": r.field_0} AS payload
	/// FROM r
	/// WHERE({ document db - formattable order by query - filter})
	/// ORDER BY r.field_7
	/// </para>
	/// <para>
	/// This is so that the client can parse out the _rid, orderByItems from the actual data / payload,
	/// without scanning the entire document.
	/// </para>
	/// <para>
	/// This struct is used to strongly bind the results of that rewritten query.
	/// </para>
	/// </summary>
	internal sealed class OrderByQueryResult
	{
		/// <summary>
		/// Custom converter to serialize and deserialize the payload.
		/// </summary>
		private sealed class PayloadConverter : JsonConverter
		{
			/// <summary>
			/// Gets whether or not the object can be converted.
			/// </summary>
			/// <param name="objectType">The type of the object.</param>
			/// <returns>Whether or not the object can be converted.</returns>
			public override bool CanConvert(Type objectType)
			{
				return (object)objectType == typeof(object);
			}

			/// <summary>
			/// Reads a payload from a json reader.
			/// </summary>
			/// <param name="reader">The reader.</param>
			/// <param name="objectType">The object type.</param>
			/// <param name="existingValue">The existing value.</param>
			/// <param name="serializer">The serialized</param>
			/// <returns>The deserialized JSON.</returns>
			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JToken jToken = JToken.Load(reader);
				if (jToken.Type == JTokenType.Object || jToken.Type == JTokenType.Array)
				{
					return new QueryResult((JContainer)jToken, null, serializer);
				}
				return jToken;
			}

			/// <summary>
			/// Writes the json to a writer.
			/// </summary>
			/// <param name="writer">The writer to write to.</param>
			/// <param name="value">The value to serialize.</param>
			/// <param name="serializer">The serializer to use.</param>
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				serializer.Serialize(writer, value);
			}
		}

		/// <summary>
		/// Gets the rid of the document.
		/// </summary>
		[JsonProperty("_rid")]
		public string Rid
		{
			get;
		}

		/// <summary>
		/// Gets the order by items from the document.
		/// </summary>
		[JsonProperty("orderByItems")]
		public QueryItem[] OrderByItems
		{
			get;
		}

		/// <summary>
		/// Gets the actual document.
		/// </summary>
		[JsonProperty("payload")]
		[JsonConverter(typeof(PayloadConverter))]
		public object Payload
		{
			get;
		}

		/// <summary>
		/// Initializes a new instance of the OrderByQueryResult class.
		/// </summary>
		/// <param name="rid">The rid.</param>
		/// <param name="orderByItems">The order by items.</param>
		/// <param name="payload">The payload.</param>
		public OrderByQueryResult(string rid, QueryItem[] orderByItems, object payload)
		{
			if (string.IsNullOrEmpty(rid))
			{
				throw new ArgumentNullException(string.Format("{0} can not be null or empty.", "rid"));
			}
			if (orderByItems == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "orderByItems"));
			}
			if (orderByItems.Length == 0)
			{
				throw new ArgumentException(string.Format("{0} can not be empty.", "orderByItems"));
			}
			Rid = rid;
			OrderByItems = orderByItems;
			Payload = payload;
		}
	}
}
