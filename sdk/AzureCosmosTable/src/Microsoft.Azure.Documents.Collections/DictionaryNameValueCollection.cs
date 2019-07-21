using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Azure.Documents.Collections
{
	internal sealed class DictionaryNameValueCollection : INameValueCollection, IEnumerable
	{
		/// <summary>
		/// This class represent the value of the key-value entry.
		/// It can represent a null value as well as multiple values for a single key.
		/// </summary>
		private class CompositeValue
		{
			private List<string> values;

			public string[] Values
			{
				get
				{
					if (values.Count <= 0)
					{
						return null;
					}
					return values.ToArray();
				}
			}

			public string Value
			{
				get
				{
					if (values.Count <= 0)
					{
						return null;
					}
					return Convert(values);
				}
			}

			internal CompositeValue()
			{
				values = new List<string>();
			}

			private static string Convert(List<string> values)
			{
				return string.Join(",", values);
			}

			public CompositeValue(string value)
				: this()
			{
				Add(value);
			}

			public void Add(string value)
			{
				if (value != null)
				{
					values.Add(value);
				}
			}

			public void Reset(string value)
			{
				values.Clear();
				Add(value);
			}

			public void Add(CompositeValue cv)
			{
				values.AddRange(cv.values);
			}
		}

		private static StringComparer defaultStringComparer = StringComparer.OrdinalIgnoreCase;

		private readonly Dictionary<string, CompositeValue> dictionary;

		private CompositeValue nullValue;

		private NameValueCollection nvc;

		public IEnumerable<string> Keys
		{
			get
			{
				foreach (string key in dictionary.Keys)
				{
					yield return key;
				}
				if (nullValue != null)
				{
					yield return null;
				}
			}
		}

		public string this[string key]
		{
			get
			{
				return Get(key);
			}
			set
			{
				Set(key, value);
			}
		}

		public DictionaryNameValueCollection()
		{
			dictionary = new Dictionary<string, CompositeValue>(defaultStringComparer);
		}

		public DictionaryNameValueCollection(StringComparer comparer)
		{
			dictionary = new Dictionary<string, CompositeValue>(comparer);
		}

		public DictionaryNameValueCollection(int capacity)
			: this(capacity, defaultStringComparer)
		{
		}

		private DictionaryNameValueCollection(int capacity, StringComparer comparer)
		{
			dictionary = new Dictionary<string, CompositeValue>(capacity, (comparer == null) ? defaultStringComparer : comparer);
		}

		public DictionaryNameValueCollection(INameValueCollection c)
			: this(c.Count())
		{
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			Add(c);
		}

		public DictionaryNameValueCollection(NameValueCollection c)
			: this(c.Count)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			foreach (string item in c)
			{
				string[] values = c.GetValues(item);
				foreach (string value in values)
				{
					Add(item, value);
				}
			}
		}

		public void Add(string key, string value)
		{
			if (key == null)
			{
				if (nullValue == null)
				{
					nullValue = new CompositeValue(value);
				}
				else
				{
					nullValue.Add(value);
				}
				return;
			}
			dictionary.TryGetValue(key, out CompositeValue value2);
			if (value2 != null)
			{
				value2.Add(value);
			}
			else
			{
				dictionary.Add(key, new CompositeValue(value));
			}
		}

		public void Add(INameValueCollection c)
		{
			if (c == null)
			{
				throw new ArgumentNullException("c");
			}
			DictionaryNameValueCollection dictionaryNameValueCollection = c as DictionaryNameValueCollection;
			if (dictionaryNameValueCollection != null)
			{
				foreach (string key2 in dictionaryNameValueCollection.dictionary.Keys)
				{
					if (!dictionary.ContainsKey(key2))
					{
						dictionary[key2] = new CompositeValue();
					}
					dictionary[key2].Add(dictionaryNameValueCollection.dictionary[key2]);
				}
				if (dictionaryNameValueCollection.nullValue != null)
				{
					if (nullValue == null)
					{
						nullValue = new CompositeValue();
					}
					nullValue.Add(dictionaryNameValueCollection.nullValue);
				}
			}
			else
			{
				foreach (string item in c)
				{
					string[] values = c.GetValues(item);
					foreach (string value in values)
					{
						Add(item, value);
					}
				}
			}
		}

		public void Set(string key, string value)
		{
			if (key == null)
			{
				if (nullValue == null)
				{
					nullValue = new CompositeValue(value);
				}
				else
				{
					nullValue.Reset(value);
				}
				return;
			}
			dictionary.TryGetValue(key, out CompositeValue value2);
			if (value2 != null)
			{
				value2.Reset(value);
			}
			else
			{
				dictionary.Add(key, new CompositeValue(value));
			}
		}

		public string Get(string key)
		{
			CompositeValue value = null;
			if (key == null)
			{
				value = nullValue;
			}
			else
			{
				dictionary.TryGetValue(key, out value);
			}
			return value?.Value;
		}

		public string[] GetValues(string key)
		{
			CompositeValue value = null;
			if (key == null)
			{
				value = nullValue;
			}
			else
			{
				dictionary.TryGetValue(key, out value);
			}
			return value?.Values;
		}

		public void Remove(string key)
		{
			if (key == null)
			{
				nullValue = null;
			}
			else
			{
				dictionary.Remove(key);
			}
		}

		public void Clear()
		{
			nullValue = null;
			dictionary.Clear();
		}

		public int Count()
		{
			return dictionary.Count + ((nullValue != null) ? 1 : 0);
		}

		public IEnumerator GetEnumerator()
		{
			return Keys.GetEnumerator();
		}

		public INameValueCollection Clone()
		{
			return new DictionaryNameValueCollection(this);
		}

		public string[] AllKeys()
		{
			string[] array = new string[Count()];
			int num = 0;
			foreach (string key in dictionary.Keys)
			{
				array[num++] = key;
			}
			if (nullValue != null)
			{
				array[num++] = null;
			}
			return array;
		}

		IEnumerable<string> INameValueCollection.Keys()
		{
			return Keys;
		}

		public NameValueCollection ToNameValueCollection()
		{
			if (nvc == null)
			{
				lock (this)
				{
					if (nvc == null)
					{
						nvc = new NameValueCollection(dictionary.Count, (StringComparer)dictionary.Comparer);
						{
							IEnumerator enumerator = GetEnumerator();
							try
							{
								while (enumerator.MoveNext())
								{
									string text = (string)enumerator.Current;
									if (GetValues(text) == null)
									{
										nvc.Add(text, null);
									}
									else
									{
										string[] values = GetValues(text);
										foreach (string value in values)
										{
											nvc.Add(text, value);
										}
									}
								}
							}
							finally
							{
								IDisposable disposable = enumerator as IDisposable;
								if (disposable != null)
								{
									disposable.Dispose();
								}
							}
						}
					}
				}
			}
			return nvc;
		}
	}
}
