using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a schema in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks> 
	/// A schema is a structured JSON document.
	/// </remarks>
	internal sealed class Schema : Resource
	{
		/// <summary>
		/// Gets the resource link for the schema from the Azure Cosmos DB service.
		/// </summary>
		public string ResourceLink => GetValue<string>("resource");

		internal static Schema FromObject(object schema)
		{
			if (schema != null)
			{
				if (typeof(Schema).IsAssignableFrom(schema.GetType()))
				{
					return (Schema)schema;
				}
				JObject propertyBag = JObject.FromObject(schema);
				return new Schema
				{
					propertyBag = propertyBag
				};
			}
			return null;
		}

		private object GetProperty(string propertyName, Type returnType)
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (jToken != null)
				{
					return jToken.ToObject(returnType);
				}
			}
			throw new DocumentClientException(string.Format(CultureInfo.CurrentUICulture, RMResources.PropertyNotFound, propertyName), null, null);
		}

		private object SetProperty(string propertyName, object value)
		{
			if (value != null)
			{
				if (propertyBag == null)
				{
					propertyBag = new JObject();
				}
				propertyBag[propertyName] = JToken.FromObject(value);
			}
			else if (propertyBag != null)
			{
				propertyBag.Remove(propertyName);
			}
			return value;
		}

		private T AsType<T>()
		{
			if ((object)typeof(T) == typeof(Schema) || (object)typeof(T) == typeof(object))
			{
				return (T)(object)this;
			}
			if (propertyBag == null)
			{
				return default(T);
			}
			return propertyBag.ToObject<T>();
		}
	}
}
