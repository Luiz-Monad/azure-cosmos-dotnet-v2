using Microsoft.Azure.Documents.Sql;
using System;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// There are two types of collections: outer and inner.
	/// </summary>
	internal sealed class Collection
	{
		public bool isOuter;

		public SqlCollection inner;

		public string Name;

		/// <summary>
		/// Creates an outer collection.
		/// </summary>
		public Collection(string name)
		{
			isOuter = true;
			Name = name;
		}

		public Collection(SqlCollection collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			isOuter = false;
			inner = collection;
		}
	}
}
