using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Azure.Documents.Collections
{
	/// <summary>
	/// NameValueCollectionWrapper provides an implementation of INameValueCollection and maintains the behavior of NameValueCollection type.
	/// All operations are delegated to an instance of NameValueCollection internally.
	/// </summary>
	internal class NameValueCollectionWrapper : INameValueCollection, IEnumerable
	{
		private NameValueCollection collection;

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string this[string key]
		{
			get
			{
				return collection[key];
			}
			set
			{
				collection[key] = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public NameValueCollectionWrapper()
		{
			collection = new NameValueCollection();
		}

		public NameValueCollectionWrapper(int capacity)
		{
			collection = new NameValueCollection(capacity);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="comparer"></param>
		public NameValueCollectionWrapper(StringComparer comparer)
		{
			collection = new NameValueCollection(comparer);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="values"></param>
		public NameValueCollectionWrapper(NameValueCollectionWrapper values)
		{
			collection = new NameValueCollection(values.collection);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		public NameValueCollectionWrapper(NameValueCollection collection)
		{
			this.collection = new NameValueCollection(collection);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		public NameValueCollectionWrapper(INameValueCollection collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this.collection = new NameValueCollection();
			foreach (string item in collection)
			{
				this.collection.Add(item, collection[item]);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		public static NameValueCollectionWrapper Create(NameValueCollection collection)
		{
			return new NameValueCollectionWrapper
			{
				collection = collection
			};
		}

		public void Add(INameValueCollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			NameValueCollectionWrapper nameValueCollectionWrapper = c as NameValueCollectionWrapper;
			if (nameValueCollectionWrapper != null)
			{
				collection.Add(nameValueCollectionWrapper.collection);
			}
			else
			{
				foreach (string item in c)
				{
					string[] values = c.GetValues(item);
					foreach (string value in values)
					{
						collection.Add(item, value);
					}
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(string key, string value)
		{
			collection.Add(key, value);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public INameValueCollection Clone()
		{
			return new NameValueCollectionWrapper(this);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string Get(string key)
		{
			return collection.Get(key);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return collection.GetEnumerator();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string[] GetValues(string key)
		{
			return collection.GetValues(key);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		public void Remove(string key)
		{
			collection.Remove(key);
		}

		/// <summary>
		///
		/// </summary>
		public void Clear()
		{
			collection.Clear();
		}

		public int Count()
		{
			return collection.Count;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Set(string key, string value)
		{
			collection.Set(key, value);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string[] AllKeys()
		{
			return collection.AllKeys;
		}

		public IEnumerable<string> Keys()
		{
			foreach (string key in collection.Keys)
			{
				yield return key;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public NameValueCollection ToNameValueCollection()
		{
			return collection;
		}
	}
}
