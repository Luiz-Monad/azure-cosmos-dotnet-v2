using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Microsoft.Azure.Documents
{
	internal sealed class SerializableNameValueCollection : JsonSerializable
	{
		private Lazy<NameValueCollection> lazyCollection;

		[JsonIgnore]
		public NameValueCollection Collection
		{
			get
			{
				return lazyCollection.Value;
			}
		}

		public SerializableNameValueCollection()
		{
			lazyCollection = new Lazy<NameValueCollection>(Init);
		}

		public SerializableNameValueCollection(NameValueCollection collection)
		{
			lazyCollection = new Lazy<NameValueCollection>(Init);
			Collection.Add(collection);
		}

		public static string SaveToString(SerializableNameValueCollection nameValueCollection)
		{
			if (nameValueCollection == null)
			{
				return string.Empty;
			}
			using (MemoryStream memoryStream = new MemoryStream())
			{
				nameValueCollection.SaveTo(memoryStream);
				memoryStream.Position = 0L;
				using (StreamReader streamReader = new StreamReader(memoryStream))
				{
					return streamReader.ReadToEnd();
				}
			}
		}

		public static SerializableNameValueCollection LoadFromString(string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (StreamWriter streamWriter = new StreamWriter(memoryStream))
					{
						streamWriter.Write(value);
						streamWriter.Flush();
						memoryStream.Position = 0L;
						return JsonSerializable.LoadFrom<SerializableNameValueCollection>(memoryStream);
					}
				}
			}
			return new SerializableNameValueCollection();
		}

		internal override void OnSave()
		{
			foreach (string item in Collection)
			{
				SetValue(item, Collection[item]);
			}
		}

		private NameValueCollection Init()
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			if (propertyBag != null)
			{
				foreach (KeyValuePair<string, JToken> item in propertyBag)
				{
					JValue jValue = item.Value as JValue;
					if (jValue != null)
					{
						nameValueCollection.Add(item.Key, jValue.ToString());
					}
				}
				return nameValueCollection;
			}
			return nameValueCollection;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as SerializableNameValueCollection);
		}

		public bool Equals(SerializableNameValueCollection collection)
		{
			if (collection == null)
			{
				return false;
			}
			if (this == collection)
			{
				return true;
			}
			return IsEqual(collection);
		}

		private bool IsEqual(SerializableNameValueCollection serializableNameValueCollection)
		{
			if (Collection.Count != serializableNameValueCollection.Collection.Count)
			{
				return false;
			}
			string[] allKeys = Collection.AllKeys;
			foreach (string name in allKeys)
			{
				if (Collection[name] != serializableNameValueCollection.Collection[name])
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = 0;
			foreach (string item in Collection)
			{
				num = ((num * 397) ^ item.GetHashCode());
				num = ((num * 397) ^ ((Collection.Get(item) != null) ? Collection.Get(item).GetHashCode() : 0));
			}
			return num;
		}
	}
}
