using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Collections.Generic
{
	internal sealed class PartialReadOnlyList<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
	{
		private readonly IReadOnlyList<T> list;

		private readonly int startIndex;

		private readonly int count;

		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return list[checked(startIndex + index)];
			}
		}

		public int Count => count;

		public PartialReadOnlyList(IReadOnlyList<T> list, int count)
			: this(list, 0, count)
		{
		}

		public PartialReadOnlyList(IReadOnlyList<T> list, int startIndex, int count)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			if (count <= 0 || count > list.Count)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (startIndex < 0 || startIndex + count > list.Count)
			{
				throw new ArgumentOutOfRangeException("startIndex");
			}
			this.list = list;
			this.startIndex = startIndex;
			this.count = count;
		}

		public IEnumerator<T> GetEnumerator()
		{
			int num;
			for (int i = 0; i < count; i = num)
			{
				yield return list[i + startIndex];
				num = i + 1;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
