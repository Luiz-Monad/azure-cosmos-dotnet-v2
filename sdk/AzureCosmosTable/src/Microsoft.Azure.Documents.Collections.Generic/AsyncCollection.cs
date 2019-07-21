using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Collections.Generic
{
	/// <summary> 
	/// Provides awaitable and bounding capabilities for thread-safe collections that implement IProducerConsumerCollection&lt;T&gt;.
	/// </summary>
	internal sealed class AsyncCollection<T>
	{
		private delegate bool TryPeekDelegate(out T item);

		private readonly IProducerConsumerCollection<T> collection;

		private readonly int boundingCapacity;

		private readonly SemaphoreSlim notFull;

		private readonly SemaphoreSlim notEmpty;

		private readonly TryPeekDelegate tryPeekDelegate;

		public int Count => collection.Count;

		public bool IsUnbounded => boundingCapacity >= int.MaxValue;

		public AsyncCollection()
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), int.MaxValue)
		{
		}

		public AsyncCollection(int boundingCapacity)
			: this((IProducerConsumerCollection<T>)new ConcurrentQueue<T>(), boundingCapacity)
		{
		}

		public AsyncCollection(IProducerConsumerCollection<T> collection)
			: this(collection, int.MaxValue)
		{
		}

		public AsyncCollection(IProducerConsumerCollection<T> collection, int boundingCapacity)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (boundingCapacity < 1)
			{
				throw new ArgumentOutOfRangeException("boundedCapacity is not a positive value.");
			}
			int count = collection.Count;
			if (boundingCapacity < count)
			{
				throw new ArgumentOutOfRangeException("boundedCapacity is less than the size of collection.");
			}
			this.collection = collection;
			this.boundingCapacity = boundingCapacity;
			notFull = (IsUnbounded ? null : new SemaphoreSlim(boundingCapacity - count, boundingCapacity));
			notEmpty = new SemaphoreSlim(count);
			MethodInfo method = CustomTypeExtensions.GetMethod(this.collection.GetType(), "TryPeek", BindingFlags.Instance | BindingFlags.Public);
			tryPeekDelegate = (((object)method == null) ? null : ((TryPeekDelegate)CustomTypeExtensions.CreateDelegate(typeof(TryPeekDelegate), this.collection, method)));
		}

		public async Task AddAsync(T item, CancellationToken token = default(CancellationToken))
		{
			if (!IsUnbounded)
			{
				await notFull.WaitAsync(token);
			}
			if (collection.TryAdd(item))
			{
				notEmpty.Release();
			}
		}

		public async Task AddRangeAsync(IEnumerable<T> items, CancellationToken token = default(CancellationToken))
		{
			if (!IsUnbounded)
			{
				foreach (T item in items)
				{
					await AddAsync(item);
				}
				return;
			}
			int num = 0;
			foreach (T item2 in items)
			{
				if (collection.TryAdd(item2))
				{
					num++;
				}
			}
			if (num > 0)
			{
				notEmpty.Release(num);
			}
		}

		public async Task<T> TakeAsync(CancellationToken token = default(CancellationToken))
		{
			await notEmpty.WaitAsync(token);
			if (collection.TryTake(out T item) && !IsUnbounded)
			{
				notFull.Release();
			}
			return item;
		}

		public async Task<T> PeekAsync(CancellationToken token = default(CancellationToken))
		{
			if (tryPeekDelegate == null)
			{
				throw new NotImplementedException();
			}
			await notEmpty.WaitAsync(token);
			tryPeekDelegate(out T item);
			notEmpty.Release();
			return item;
		}

		public bool TryPeek(out T item)
		{
			if (tryPeekDelegate == null)
			{
				throw new NotImplementedException();
			}
			return tryPeekDelegate(out item);
		}

		public async Task<IReadOnlyList<T>> DrainAsync(int maxElements = int.MaxValue, TimeSpan timeout = default(TimeSpan), Func<T, bool> callback = null, CancellationToken token = default(CancellationToken))
		{
			if (maxElements < 1)
			{
				throw new ArgumentOutOfRangeException("maxElements is not a positive value.");
			}
			List<T> elements = new List<T>();
			Stopwatch stopWatch = Stopwatch.StartNew();
			while (true)
			{
				bool flag = elements.Count < maxElements;
				if (flag)
				{
					flag = await notEmpty.WaitAsync(timeout, token);
				}
				T item;
				if (!flag || !collection.TryTake(out item) || (callback != null && !callback(item)))
				{
					break;
				}
				elements.Add(item);
				timeout.Subtract(TimeSpan.FromTicks(Math.Min(stopWatch.ElapsedTicks, timeout.Ticks)));
				stopWatch.Restart();
			}
			if (!IsUnbounded && elements.Count > 0)
			{
				notFull.Release(elements.Count);
			}
			return elements;
		}
	}
}
