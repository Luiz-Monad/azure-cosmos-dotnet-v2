using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Collections.Generic
{
	internal sealed class CountableEnumerable<T> : IEnumerable<T>, IEnumerable
	{
		private readonly IEnumerable<T> enumerable;

		private readonly int count;

		public int Count => count;

		public CountableEnumerable(IEnumerable<T> enumerable, int count)
		{
			if (enumerable == null)
			{
				throw new ArgumentNullException("enumerable");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			this.enumerable = enumerable;
			this.count = count;
		}

		public IEnumerator<T> GetEnumerator()
		{
			int i = 0;
			foreach (T item in enumerable)
			{
				int num = i;
				i = num + 1;
				if (num >= count)
				{
					break;
				}
				yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
