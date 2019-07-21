using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the base class for Azure Cosmos DB database objects and provides methods for serializing and deserializing from JSON.
	/// </summary>
	public abstract class JsonSerializable
	{
		internal JObject propertyBag;

		private const string POCOSerializationOnly = "POCOSerializationOnly";

		internal static bool JustPocoSerialization;

		internal JsonSerializerSettings SerializerSettings
		{
			get;
			set;
		}

		static JsonSerializable()
		{
			if (int.TryParse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POCOSerializationOnly")) ? "0" : Environment.GetEnvironmentVariable("POCOSerializationOnly"), out int result) && result == 1)
			{
				JustPocoSerialization = true;
			}
			else
			{
				JustPocoSerialization = false;
			}
		}

		internal JsonSerializable()
		{
			propertyBag = new JObject();
		}

		/// <summary> 
		/// Saves the object to the specified stream in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="stream">Saves the object to this output stream.</param>
		/// <param name="formattingPolicy">Uses an optional serialization formatting policy when saving the object. The default policy is set to None.</param>
		public void SaveTo(Stream stream, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			SaveTo(stream, formattingPolicy, null);
		}

		/// <summary> 
		/// Saves the object to the specified stream in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="stream">Saves the object to this output stream.</param>
		/// <param name="formattingPolicy">Uses a custom serialization formatting policy when saving the object.</param>
		/// <param name="settings">The serializer settings to use.</param>
		public void SaveTo(Stream stream, SerializationFormattingPolicy formattingPolicy, JsonSerializerSettings settings)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			SerializerSettings = settings;
			JsonSerializer serializer = (settings == null) ? new JsonSerializer() : JsonSerializer.Create(settings);
			JsonTextWriter writer = new JsonTextWriter(new StreamWriter(stream));
			SaveTo(writer, serializer, formattingPolicy);
		}

		internal void SaveTo(JsonWriter writer, JsonSerializer serializer, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (serializer == null)
			{
				throw new ArgumentNullException("serializer");
			}
			if (formattingPolicy == SerializationFormattingPolicy.Indented)
			{
				writer.Formatting = Formatting.Indented;
			}
			else
			{
				writer.Formatting = Formatting.None;
			}
			OnSave();
			if ((typeof(Document).IsAssignableFrom(GetType()) && !GetType().Equals(typeof(Document))) || (typeof(Attachment).IsAssignableFrom(GetType()) && !GetType().Equals(typeof(Attachment))))
			{
				serializer.Serialize(writer, this);
			}
			else if (JustPocoSerialization)
			{
				propertyBag.WriteTo(writer);
			}
			else
			{
				serializer.Serialize(writer, propertyBag);
			}
			writer.Flush();
		}

		/// <summary> 
		/// Saves the object to the specified string builder
		/// </summary>
		/// <param name="stringBuilder">Saves the object to this output string builder.</param>
		/// <param name="formattingPolicy">Uses an optional serialization formatting policy when saving the object. The default policy is set to None.</param>
		internal void SaveTo(StringBuilder stringBuilder, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			if (stringBuilder == null)
			{
				throw new ArgumentNullException("stringBuilder");
			}
			SaveTo(new JsonTextWriter(new StringWriter(stringBuilder, CultureInfo.CurrentCulture)), new JsonSerializer(), formattingPolicy);
		}

		/// <summary>
		/// Loads the object from the specified JSON reader in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="reader">Loads the object from this JSON reader.</param>
		public virtual void LoadFrom(JsonReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			propertyBag = JObject.Load(reader);
		}

		/// <summary>
		/// Loads the object from the specified JSON reader in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="reader">Loads the object from this JSON reader.</param>
		/// <param name="serializerSettings">The JsonSerializerSettings to be used.</param>
		public virtual void LoadFrom(JsonReader reader, JsonSerializerSettings serializerSettings)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			Helpers.SetupJsonReader(reader, serializerSettings);
			propertyBag = JObject.Load(reader);
			SerializerSettings = serializerSettings;
		}

		/// <summary>
		/// Loads the object from the specified stream in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of the returning object.</typeparam>
		/// <param name="stream">The stream to load from.</param>
		/// <returns>The object loaded from the specified stream.</returns>
		public static T LoadFrom<T>(Stream stream) where T : JsonSerializable, new()
		{
			return LoadFrom<T>(stream, null);
		}

		/// <summary>
		/// Loads the object from the specified stream.
		/// </summary>
		/// <typeparam name="T">The type of the returning object.</typeparam>
		/// <param name="stream">The stream to load from.</param>
		/// <param name="typeResolver">Used to get a correct object from a stream.</param>
		/// <param name="settings">The JsonSerializerSettings to be used</param>
		/// <returns>The object loaded from the specified stream.</returns>
		internal static T LoadFrom<T>(Stream stream, ITypeResolver<T> typeResolver, JsonSerializerSettings settings = null) where T : JsonSerializable, new()
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			return LoadFrom(new JsonTextReader(new StreamReader(stream)), typeResolver, settings);
		}

		/// <summary>
		/// Loads the object from the specified stream.
		/// </summary>
		/// <typeparam name="T">The type of the returning object.</typeparam>
		/// <param name="serialized">Serialized payload.</param>
		/// <param name="typeResolver">Used to get a correct object from a stream.</param>
		/// <param name="settings">The JsonSerializerSettings to be used</param>
		/// <returns>The object loaded from the specified stream.</returns>
		internal static T LoadFrom<T>(string serialized, ITypeResolver<T> typeResolver, JsonSerializerSettings settings = null) where T : JsonSerializable, new()
		{
			if (serialized == null)
			{
				throw new ArgumentNullException("serialized");
			}
			return LoadFrom(new JsonTextReader(new StringReader(serialized)), typeResolver, settings);
		}

		/// <summary>
		/// Deserializes the specified stream using the given constructor in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="stream">The stream to load from.</param>
		/// <param name="constructorFunction">The constructor used for the returning object.</param>
		/// <returns>The object loaded from the specified stream.</returns>
		public static T LoadFromWithConstructor<T>(Stream stream, Func<T> constructorFunction)
		{
			return LoadFromWithConstructor(stream, constructorFunction, null);
		}

		/// <summary>
		/// Deserializes the specified stream using the given constructor in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="stream">The stream to load from.</param>
		/// <param name="constructorFunction">The constructor used for the returning object.</param>
		/// <param name="settings">The JsonSerializerSettings to be used.</param>
		/// <returns>The object loaded from the specified stream.</returns>
		public static T LoadFromWithConstructor<T>(Stream stream, Func<T> constructorFunction, JsonSerializerSettings settings)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!typeof(T).IsSubclassOf(typeof(JsonSerializable)))
			{
				throw new ArgumentException("type is not serializable");
			}
			T val = constructorFunction();
			JsonTextReader reader = new JsonTextReader(new StreamReader(stream));
			((JsonSerializable)(object)val).LoadFrom(reader, settings);
			return val;
		}

		/// <summary>
		/// Returns the string representation of the object in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>The string representation of the object.</returns>
		public override string ToString()
		{
			return propertyBag.ToString();
		}

		/// <summary>
		/// Get the value associated with the specified property name.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		internal T GetValue<T>(string propertyName)
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (jToken != null)
				{
					if (typeof(T).IsEnum() && jToken.Type == JTokenType.String)
					{
						return jToken.ToObject<T>(JsonSerializer.CreateDefault());
					}
					if (SerializerSettings != null)
					{
						return jToken.ToObject<T>(JsonSerializer.Create(SerializerSettings));
					}
					return jToken.ToObject<T>();
				}
			}
			return default(T);
		}

		/// <summary>
		/// Get the value associated with the specified property name.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyName"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		internal T GetValue<T>(string propertyName, T defaultValue)
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (jToken != null)
				{
					if (typeof(T).IsEnum() && jToken.Type == JTokenType.String)
					{
						return jToken.ToObject<T>(JsonSerializer.CreateDefault());
					}
					if (SerializerSettings != null)
					{
						return jToken.ToObject<T>(JsonSerializer.Create(SerializerSettings));
					}
					return jToken.ToObject<T>();
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Get the value associated with the specified property name.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fieldNames">Field names which compose a path to the property to be retrieved.</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		internal T GetValueByPath<T>(string[] fieldNames, T defaultValue)
		{
			if (fieldNames == null)
			{
				throw new ArgumentNullException("fieldNames");
			}
			if (fieldNames.Length == 0)
			{
				throw new ArgumentException("fieldNames is empty.");
			}
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[fieldNames[0]];
				for (int i = 1; i < fieldNames.Length; i++)
				{
					if (jToken == null)
					{
						break;
					}
					jToken = ((jToken is JObject) ? jToken[fieldNames[i]] : null);
				}
				if (jToken != null)
				{
					if (typeof(T).IsEnum() && jToken.Type == JTokenType.String)
					{
						return jToken.ToObject<T>(JsonSerializer.CreateDefault());
					}
					if (SerializerSettings != null)
					{
						return jToken.ToObject<T>(JsonSerializer.Create(SerializerSettings));
					}
					return jToken.ToObject<T>();
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Set the value associated with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		internal void SetValue(string name, object value)
		{
			if (propertyBag == null)
			{
				propertyBag = new JObject();
			}
			if (value != null)
			{
				propertyBag[name] = JToken.FromObject(value);
			}
			else
			{
				propertyBag.Remove(name);
			}
		}

		/// <summary>
		/// Set the value associated with the specified property name.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fieldNames">Field names which compose a path to the property to be retrieved.</param>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>This will overwrite the existing properties</remarks>
		internal void SetValueByPath<T>(string[] fieldNames, T value)
		{
			if (fieldNames == null)
			{
				throw new ArgumentNullException("fieldNames");
			}
			if (fieldNames.Length == 0)
			{
				throw new ArgumentException("fieldNames is empty.");
			}
			if (propertyBag == null)
			{
				propertyBag = new JObject();
			}
			JToken jToken = propertyBag;
			for (int i = 0; i < fieldNames.Length - 1; i++)
			{
				if (jToken[fieldNames[i]] == null)
				{
					jToken[fieldNames[i]] = new JObject();
				}
				jToken = jToken[fieldNames[i]];
			}
			JObject jObject = jToken as JObject;
			if (value == null && jObject != null)
			{
				jObject.Remove(fieldNames[fieldNames.Length - 1]);
			}
			else
			{
				jToken[fieldNames[fieldNames.Length - 1]] = ((value == null) ? null : JToken.FromObject(value));
			}
		}

		internal TSerializable GetObject<TSerializable>(string propertyName) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (jToken != null && jToken.HasValues)
				{
					TSerializable val = new TSerializable();
					val.propertyBag = JObject.FromObject(jToken);
					return val;
				}
			}
			return null;
		}

		internal void SetObject<TSerializable>(string propertyName, TSerializable value) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag == null)
			{
				propertyBag = new JObject();
			}
			propertyBag[propertyName] = value?.propertyBag;
		}

		internal Collection<TSerializable> GetObjectCollection<TSerializable>(string propertyName, Type resourceType = null, string ownerName = null, ITypeResolver<TSerializable> typeResolver = null) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (typeResolver == null)
				{
					typeResolver = GetTypeResolver<TSerializable>();
				}
				if (jToken != null)
				{
					Collection<JObject> collection = jToken.ToObject<Collection<JObject>>();
					Collection<TSerializable> collection2 = new Collection<TSerializable>();
					{
						foreach (JObject item in collection)
						{
							TSerializable val = (typeResolver != null) ? typeResolver.Resolve(item) : new TSerializable();
							val.propertyBag = item;
							if (PathsHelper.IsPublicResource(typeof(TSerializable)))
							{
								Resource resource = val as Resource;
								resource.AltLink = PathsHelper.GeneratePathForNameBased(resourceType, ownerName, resource.Id);
							}
							collection2.Add(val);
						}
						return collection2;
					}
				}
			}
			return null;
		}

		internal void SetObjectCollection<TSerializable>(string propertyName, Collection<TSerializable> value) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag == null)
			{
				propertyBag = new JObject();
			}
			if (value != null)
			{
				Collection<JObject> collection = new Collection<JObject>();
				foreach (TSerializable item in value)
				{
					item.OnSave();
					collection.Add(item.propertyBag ?? new JObject());
				}
				propertyBag[propertyName] = JToken.FromObject(collection);
			}
		}

		internal Dictionary<string, TSerializable> GetObjectDictionary<TSerializable>(string propertyName, ITypeResolver<TSerializable> typeResolver = null) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (typeResolver == null)
				{
					typeResolver = GetTypeResolver<TSerializable>();
				}
				if (jToken != null)
				{
					Dictionary<string, JObject> dictionary = jToken.ToObject<Dictionary<string, JObject>>();
					Dictionary<string, TSerializable> dictionary2 = new Dictionary<string, TSerializable>();
					{
						foreach (KeyValuePair<string, JObject> item in dictionary)
						{
							TSerializable val = (typeResolver != null) ? typeResolver.Resolve(item.Value) : new TSerializable();
							val.propertyBag = item.Value;
							dictionary2.Add(item.Key, val);
						}
						return dictionary2;
					}
				}
			}
			return null;
		}

		internal void SetObjectDictionary<TSerializable>(string propertyName, Dictionary<string, TSerializable> value) where TSerializable : JsonSerializable, new()
		{
			if (propertyBag == null)
			{
				propertyBag = new JObject();
			}
			if (value != null)
			{
				Dictionary<string, JObject> dictionary = new Dictionary<string, JObject>();
				foreach (KeyValuePair<string, TSerializable> item in value)
				{
					item.Value.OnSave();
					dictionary.Add(item.Key, item.Value.propertyBag ?? new JObject());
				}
				propertyBag[propertyName] = JToken.FromObject(dictionary);
			}
		}

		internal virtual void OnSave()
		{
		}

		internal static ITypeResolver<TResource> GetTypeResolver<TResource>() where TResource : JsonSerializable, new()
		{
			ITypeResolver<TResource> result = null;
			if ((object)typeof(TResource) == typeof(Offer))
			{
				result = (ITypeResolver<TResource>)OfferTypeResolver.ResponseOfferTypeResolver;
			}
			return result;
		}

		private static T LoadFrom<T>(JsonTextReader jsonReader, ITypeResolver<T> typeResolver, JsonSerializerSettings settings = null) where T : JsonSerializable, new()
		{
			T val = new T();
			val.LoadFrom(jsonReader, settings);
			return (typeResolver != null) ? typeResolver.Resolve(val.propertyBag) : val;
		}
	}
}
