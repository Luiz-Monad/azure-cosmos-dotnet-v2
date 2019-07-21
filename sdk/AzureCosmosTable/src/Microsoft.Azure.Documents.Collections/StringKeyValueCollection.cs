using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Azure.Documents.Collections
{
	internal sealed class StringKeyValueCollection : INameValueCollection, IEnumerable
	{
		internal static volatile INameValueCollectionFactory factory = DefaultNameValueCollectionFactory();

		internal readonly INameValueCollection internalCollection;

		public string this[string key]
		{
			get
			{
				return internalCollection[key];
			}
			set
			{
				internalCollection[key] = value;
			}
		}

		public StringKeyValueCollection()
		{
			internalCollection = factory.CreateNewNameValueCollection();
		}

		public StringKeyValueCollection(int capacity)
		{
			internalCollection = factory.CreateNewNameValueCollection(capacity);
		}

		public StringKeyValueCollection(StringComparer ordinal)
		{
			internalCollection = factory.CreateNewNameValueCollection(ordinal);
		}

		public StringKeyValueCollection(INameValueCollection collection)
		{
			internalCollection = factory.CreateNewNameValueCollection(collection);
		}

		public StringKeyValueCollection(NameValueCollection collection)
		{
			internalCollection = factory.CreateNewNameValueCollection(collection);
		}

		public static void UseDictionaryNameValueCollection()
		{
			SetNameValueCollectionFactory(new DictionaryNameValueCollectionFactory());
		}

		internal static void SetNameValueCollectionFactory(INameValueCollectionFactory factory)
		{
			StringKeyValueCollection.factory = factory;
		}

		internal static INameValueCollectionFactory DefaultNameValueCollectionFactory()
		{
			return new NameValueCollectionWrapperFactory();
		}

		public void Add(string key, string value)
		{
			internalCollection.Add(key, value);
		}

		public void Set(string key, string value)
		{
			internalCollection.Set(key, value);
		}

		public string Get(string key)
		{
			return internalCollection.Get(key);
		}

		public IEnumerator GetEnumerator()
		{
			return internalCollection.GetEnumerator();
		}

		public INameValueCollection Clone()
		{
			return new StringKeyValueCollection(internalCollection);
		}

		public void Remove(string key)
		{
			internalCollection.Remove(key);
		}

		public void Clear()
		{
			internalCollection.Clear();
		}

		public int Count()
		{
			return internalCollection.Count();
		}

		public void Add(INameValueCollection collection)
		{
			StringKeyValueCollection stringKeyValueCollection = collection as StringKeyValueCollection;
			internalCollection.Add((stringKeyValueCollection != null) ? stringKeyValueCollection.internalCollection : collection);
		}

		public string[] GetValues(string key)
		{
			return internalCollection.GetValues(key);
		}

		public string[] AllKeys()
		{
			return internalCollection.AllKeys();
		}

		public IEnumerable<string> Keys()
		{
			return internalCollection.Keys();
		}

		public NameValueCollection ToNameValueCollection()
		{
			return internalCollection.ToNameValueCollection();
		}
	}
}
