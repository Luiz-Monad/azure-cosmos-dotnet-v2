using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Microsoft.Azure.Documents.Collections
{
	internal interface INameValueCollection : IEnumerable
	{
		string this[string key]
		{
			get;
			set;
		}

		void Add(string key, string value);

		void Set(string key, string value);

		string Get(string key);

		void Remove(string key);

		void Clear();

		int Count();

		INameValueCollection Clone();

		void Add(INameValueCollection collection);

		string[] GetValues(string key);

		string[] AllKeys();

		IEnumerable<string> Keys();

		NameValueCollection ToNameValueCollection();
	}
}
