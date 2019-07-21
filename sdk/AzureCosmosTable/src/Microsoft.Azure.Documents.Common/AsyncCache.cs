using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Common
{
	/// <summary>
	/// Cache which supports asynchronous value initialization.
	/// It ensures that for given key only single inintialization funtion is running at any point in time.
	/// </summary>
	/// <typeparam name="TKey">Type of keys.</typeparam>
	/// <typeparam name="TValue">Type of values.</typeparam>
	internal sealed class AsyncCache<TKey, TValue>
	{
		private ConcurrentDictionary<TKey, AsyncLazy<TValue>> values;

		private readonly IEqualityComparer<TValue> valueEqualityComparer;

		private readonly IEqualityComparer<TKey> keyEqualityComparer;

		public ICollection<TKey> Keys => values.Keys;

		public AsyncCache(IEqualityComparer<TValue> valueEqualityComparer, IEqualityComparer<TKey> keyEqualityComparer = null)
		{
			this.keyEqualityComparer = (keyEqualityComparer ?? EqualityComparer<TKey>.Default);
			values = new ConcurrentDictionary<TKey, AsyncLazy<TValue>>(this.keyEqualityComparer);
			this.valueEqualityComparer = valueEqualityComparer;
		}

		public AsyncCache()
			: this((IEqualityComparer<TValue>)EqualityComparer<TValue>.Default, (IEqualityComparer<TKey>)null)
		{
		}

		public void Set(TKey key, TValue value)
		{
			AsyncLazy<TValue> lazyValue = new AsyncLazy<TValue>(() => value, CancellationToken.None);
			TValue result = lazyValue.Value.Result;
			values.AddOrUpdate(key, lazyValue, delegate(TKey k, AsyncLazy<TValue> existingValue)
			{
				if (existingValue.IsValueCreated)
				{
					existingValue.Value.ContinueWith((Task<TValue> c) => c.Exception, TaskContinuationOptions.OnlyOnFaulted);
				}
				return lazyValue;
			});
		}

		/// <summary>
		/// <para>
		/// Gets value corresponding to <paramref name="key" />.
		/// </para>
		/// <para>
		/// If another initialization function is already running, new initialization function will not be started.
		/// The result will be result of currently running initialization function.
		/// </para>
		/// <para>
		/// If previous initialization function is successfully completed - value returned by it will be returned unless
		/// it is equal to <paramref name="obsoleteValue" />, in which case new initialization function will be started.
		/// </para>
		/// <para>
		/// If previous initialization function failed - new one will be launched.
		/// </para>
		/// </summary>
		/// <param name="key">Key for which to get a value.</param>
		/// <param name="obsoleteValue">Value which is obsolete and needs to be refreshed.</param>
		/// <param name="singleValueInitFunc">Initialization function.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <param name="forceRefresh">Skip cached value and generate new value.</param>
		/// <returns>Cached value or value returned by initialization function.</returns>
		public async Task<TValue> GetAsync(TKey key, TValue obsoleteValue, Func<Task<TValue>> singleValueInitFunc, CancellationToken cancellationToken, bool forceRefresh = false)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (values.TryGetValue(key, out AsyncLazy<TValue> initialLazyValue))
			{
				if (!initialLazyValue.IsValueCreated || !initialLazyValue.Value.IsCompleted)
				{
					try
					{
						return await initialLazyValue.Value;
					}
					catch
					{
					}
				}
				else if (initialLazyValue.Value.Exception == null && !initialLazyValue.Value.IsCanceled)
				{
					TValue val = await initialLazyValue.Value;
					if (!forceRefresh && !valueEqualityComparer.Equals(val, obsoleteValue))
					{
						return val;
					}
				}
			}
			AsyncLazy<TValue> newLazyValue = new AsyncLazy<TValue>(singleValueInitFunc, cancellationToken);
			Task<TValue> value = values.AddOrUpdate(key, newLazyValue, delegate(TKey existingKey, AsyncLazy<TValue> existingValue)
			{
				if (existingValue != initialLazyValue)
				{
					return existingValue;
				}
				return newLazyValue;
			}).Value;
			#pragma warning disable 4014
            value.ContinueWith((Task<TValue> c) => c.Exception, TaskContinuationOptions.OnlyOnFaulted);
			#pragma warning restore 4014
			return await value;
		}

		public void Remove(TKey key)
		{
			if (values.TryRemove(key, out AsyncLazy<TValue> value) && value.IsValueCreated)
			{
				value.Value.ContinueWith((Task<TValue> c) => c.Exception, TaskContinuationOptions.OnlyOnFaulted);
			}
		}

		public bool TryRemoveIfCompleted(TKey key)
		{
			AsyncLazy<TValue> value;
			if (values.TryGetValue(key, out value) && value.IsValueCreated && value.Value.IsCompleted)
			{
				AggregateException exception = value.Value.Exception;
				return ((ICollection<KeyValuePair<TKey, AsyncLazy<TValue>>>)values)?.Remove(new KeyValuePair<TKey, AsyncLazy<TValue>>(key, value)) ?? false;
			}
			return false;
		}

		/// <summary>
		/// Remove value from cache and return it if present.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>Value if present, default value if not present.</returns>
		public async Task<TValue> RemoveAsync(TKey key)
		{
			if (values.TryRemove(key, out AsyncLazy<TValue> value))
			{
				try
				{
					return await value.Value;
				}
				catch
				{
				}
			}
			return default(TValue);
		}

		public void Clear()
		{
			ConcurrentDictionary<TKey, AsyncLazy<TValue>> value = new ConcurrentDictionary<TKey, AsyncLazy<TValue>>(keyEqualityComparer);
			ConcurrentDictionary<TKey, AsyncLazy<TValue>> concurrentDictionary = Interlocked.Exchange(ref values, value);
			foreach (AsyncLazy<TValue> value2 in concurrentDictionary.Values)
			{
				if (value2.IsValueCreated)
				{
					value2.Value.ContinueWith((Task<TValue> c) => c.Exception, TaskContinuationOptions.OnlyOnFaulted);
				}
			}
			concurrentDictionary.Clear();
		}

		/// <summary>
		/// Runs a background task that will started refreshing the cached value for a given key.
		/// This observes the same logic as GetAsync - a running value will still take precedence over a call to this.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="singleValueInitFunc">Generator function.</param>
		public void BackgroundRefreshNonBlocking(TKey key, Func<Task<TValue>> singleValueInitFunc)
		{
			Task.Factory.StartNewOnCurrentTaskSchedulerAsync((Func<Task>)async delegate
			{
				try
				{
					if (!values.TryGetValue(key, out AsyncLazy<TValue> value) || (value.IsValueCreated && value.Value.IsCompleted))
					{
						await GetAsync(key, default(TValue), singleValueInitFunc, CancellationToken.None, forceRefresh: true);
					}
				}
				catch
				{
				}
			}).Unwrap();
		}
	}
}
