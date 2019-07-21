using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Internal class created for overriding ToString method for List of generic type T.
	/// </summary>
	internal sealed class JsonSerializableList<T> : List<T>
	{
		public JsonSerializableList(IEnumerable<T> list)
			: base(list)
		{
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}

		public static List<T> LoadFrom(string serialized)
		{
			if (serialized == null)
			{
				throw new ArgumentNullException("serialized");
			}
			return JArray.Parse(serialized).ToObject<List<T>>();
		}
	}
}
