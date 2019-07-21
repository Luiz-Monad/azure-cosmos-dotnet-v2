using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Collections.Generic
{
	/// <summary> 
	/// An implementation of <a href="https://en.wikipedia.org/wiki/Binary_heap">Binary Heap</a>
	/// </summary>
	internal sealed class PriorityQueue<T> : IProducerConsumerCollection<T>, IEnumerable<T>, IEnumerable, ICollection
	{
		private const int DefaultInitialCapacity = 17;

		private readonly bool isSynchronized;

		private readonly List<T> queue;

		private readonly IComparer<T> comparer;

		public int Count => queue.Count;

		public IComparer<T> Comparer => comparer;

		public bool IsSynchronized => isSynchronized;

		public object SyncRoot => this;

		public PriorityQueue(bool isSynchronized = false)
			: this(17, isSynchronized)
		{
		}

		public PriorityQueue(int initialCapacity, bool isSynchronized = false)
			: this(initialCapacity, (IComparer<T>)Comparer<T>.Default, isSynchronized)
		{
		}

		public PriorityQueue(IComparer<T> comparer, bool isSynchronized = false)
			: this(17, comparer, isSynchronized)
		{
		}

		public PriorityQueue(IEnumerable<T> enumerable, bool isSynchronized = false)
			: this(enumerable, (IComparer<T>)Comparer<T>.Default, isSynchronized)
		{
		}

		public PriorityQueue(IEnumerable<T> enumerable, IComparer<T> comparer, bool isSynchronized = false)
			: this(new List<T>(enumerable), comparer, isSynchronized)
		{
			Heapify();
		}

		public PriorityQueue(int initialCapacity, IComparer<T> comparer, bool isSynchronized = false)
			: this(new List<T>(initialCapacity), comparer, isSynchronized)
		{
		}

		private PriorityQueue(List<T> queue, IComparer<T> comparer, bool isSynchronized)
		{
			if (queue == null)
			{
				throw new ArgumentNullException("queue");
			}
			if (comparer == null)
			{
				throw new ArgumentNullException("comparer");
			}
			this.isSynchronized = isSynchronized;
			this.queue = queue;
			this.comparer = comparer;
		}

		public void CopyTo(T[] array, int index)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					CopyToPrivate(array, index);
				}
			}
			else
			{
				CopyToPrivate(array, index);
			}
		}

		public bool TryAdd(T item)
		{
			Enqueue(item);
			return true;
		}

		public bool TryTake(out T item)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return TryTakePrivate(out item);
				}
			}
			return TryTakePrivate(out item);
		}

		public bool TryPeek(out T item)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return TryPeekPrivate(out item);
				}
			}
			return TryPeekPrivate(out item);
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					ClearPrivate();
				}
			}
			else
			{
				ClearPrivate();
			}
		}

		public bool Contains(T item)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return ContainsPrivate(item);
				}
			}
			return ContainsPrivate(item);
		}

		public T Dequeue()
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return DequeuePrivate();
				}
			}
			return DequeuePrivate();
		}

		public void Enqueue(T item)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					EnqueuePrivate(item);
				}
			}
			else
			{
				EnqueuePrivate(item);
			}
		}

		public void EnqueueRange(IEnumerable<T> items)
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					EnqueueRangePrivate(items);
				}
			}
			else
			{
				EnqueueRangePrivate(items);
			}
		}

		public T Peek()
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return PeekPrivate();
				}
			}
			return PeekPrivate();
		}

		public T[] ToArray()
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return ToArrayPrivate();
				}
			}
			return ToArrayPrivate();
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (isSynchronized)
			{
				lock (SyncRoot)
				{
					return GetEnumeratorPrivate();
				}
			}
			return GetEnumeratorPrivate();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private void CopyToPrivate(T[] array, int index)
		{
			queue.CopyTo(array, index);
		}

		private bool TryTakePrivate(out T item)
		{
			if (queue.Count <= 0)
			{
				item = default(T);
				return false;
			}
			item = DequeuePrivate();
			return true;
		}

		private bool TryPeekPrivate(out T item)
		{
			if (queue.Count <= 0)
			{
				item = default(T);
				return false;
			}
			item = PeekPrivate();
			return true;
		}

		private void ClearPrivate()
		{
			queue.Clear();
		}

		private bool ContainsPrivate(T item)
		{
			return queue.Contains(item);
		}

		private T DequeuePrivate()
		{
			if (queue.Count <= 0)
			{
				throw new InvalidOperationException("No more elements");
			}
			T result = queue[0];
			queue[0] = queue[queue.Count - 1];
			queue.RemoveAt(queue.Count - 1);
			DownHeap(0);
			return result;
		}

		private void EnqueuePrivate(T item)
		{
			queue.Add(item);
			UpHeap(queue.Count - 1);
		}

		private void EnqueueRangePrivate(IEnumerable<T> items)
		{
			queue.AddRange(items);
			Heapify();
		}

		private T PeekPrivate()
		{
			if (queue.Count <= 0)
			{
				throw new InvalidOperationException("No more elements");
			}
			return queue[0];
		}

		private T[] ToArrayPrivate()
		{
			return queue.ToArray();
		}

		private IEnumerator<T> GetEnumeratorPrivate()
		{
			return new List<T>(queue).GetEnumerator();
		}

		private void Heapify()
		{
			for (int num = GetParentIndex(Count); num >= 0; num--)
			{
				DownHeap(num);
			}
		}

		private void DownHeap(int itemIndex)
		{
			while (itemIndex < queue.Count)
			{
				int smallestChildIndex = GetSmallestChildIndex(itemIndex);
				if (smallestChildIndex != itemIndex)
				{
					T value = queue[itemIndex];
					queue[itemIndex] = queue[smallestChildIndex];
					itemIndex = smallestChildIndex;
					queue[itemIndex] = value;
					continue;
				}
				break;
			}
		}

		private void UpHeap(int itemIndex)
		{
			while (itemIndex > 0)
			{
				int parentIndex = GetParentIndex(itemIndex);
				T val = queue[parentIndex];
				T val2 = queue[itemIndex];
				if (comparer.Compare(val2, val) < 0)
				{
					queue[itemIndex] = val;
					itemIndex = parentIndex;
					queue[itemIndex] = val2;
					continue;
				}
				break;
			}
		}

		private int GetSmallestChildIndex(int parentIndex)
		{
			int num = parentIndex * 2 + 1;
			int num2 = num + 1;
			int num3 = parentIndex;
			if (num < queue.Count && comparer.Compare(queue[num3], queue[num]) > 0)
			{
				num3 = num;
			}
			if (num2 < queue.Count && comparer.Compare(queue[num3], queue[num2]) > 0)
			{
				num3 = num2;
			}
			return num3;
		}

		private int GetParentIndex(int childIndex)
		{
			return (childIndex - 1) / 2;
		}
	}
}
