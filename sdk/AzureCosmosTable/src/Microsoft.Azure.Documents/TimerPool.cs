using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This class reduces the overhead associated with creating and disposing timers created for shortlived activities
	/// It creates a PooledTimer which when started, returns a Task that you can await on and which will complete if the timeout expires
	/// This is preferred over DelayTaskTimer since it only creates a single timer which is used for the lifetime of the pool.
	/// It can *only* fire the timers at the minimum granularity configured.
	/// </summary>
	internal sealed class TimerPool : IDisposable
	{
		private readonly Timer timer;

		private readonly ConcurrentDictionary<int, ConcurrentQueue<PooledTimer>> pooledTimersByTimeout;

		private readonly TimeSpan minSupportedTimeout;

		private readonly object timerConcurrencyLock;

		private bool isRunning;

		private bool isDisposed;

		private readonly object subscriptionLock;

		internal ConcurrentDictionary<int, ConcurrentQueue<PooledTimer>> PooledTimersByTimeout => pooledTimersByTimeout;

		public TimerPool(int minSupportedTimerDelayInSeconds)
		{
			timerConcurrencyLock = new object();
			minSupportedTimeout = TimeSpan.FromSeconds((minSupportedTimerDelayInSeconds <= 0) ? 1 : minSupportedTimerDelayInSeconds);
			pooledTimersByTimeout = new ConcurrentDictionary<int, ConcurrentQueue<PooledTimer>>();
			TimerCallback callback = OnTimer;
			timer = new Timer(callback, null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(minSupportedTimerDelayInSeconds));
			DefaultTrace.TraceInformation("TimerPool Created with minSupportedTimerDelayInSeconds = {0}", minSupportedTimerDelayInSeconds);
			subscriptionLock = new object();
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				DisposeAllPooledTimers();
				isDisposed = true;
			}
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("TimerPool");
			}
		}

		private void DisposeAllPooledTimers()
		{
			DefaultTrace.TraceInformation("TimerPool Disposing");
			foreach (KeyValuePair<int, ConcurrentQueue<PooledTimer>> item in pooledTimersByTimeout)
			{
				ConcurrentQueue<PooledTimer> value = item.Value;
				PooledTimer result;
				while (value.TryDequeue(out result))
				{
					result.CancelTimer();
				}
			}
			timer.Dispose();
			DefaultTrace.TraceInformation("TimePool Disposed");
		}

		private void OnTimer(object stateInfo)
		{
			lock (timerConcurrencyLock)
			{
				if (isRunning)
				{
					return;
				}
				isRunning = true;
			}
			try
			{
				long ticks = DateTime.UtcNow.Ticks;
				foreach (KeyValuePair<int, ConcurrentQueue<PooledTimer>> item in pooledTimersByTimeout)
				{
					ConcurrentQueue<PooledTimer> value = item.Value;
					int count = item.Value.Count;
					long num = 0L;
					for (int i = 0; i < count; i++)
					{
						if (value.TryPeek(out PooledTimer result))
						{
							if (ticks < result.TimeoutTicks)
							{
								break;
							}
							if (result.TimeoutTicks < num)
							{
								DefaultTrace.TraceCritical("LastTicks: {0}, PooledTimer.Ticks: {1}", num, result.TimeoutTicks);
							}
							result.FireTimeout();
							num = result.TimeoutTicks;
							if (value.TryDequeue(out PooledTimer result2) && result2 != result)
							{
								DefaultTrace.TraceCritical("Timer objects peeked and dequeued are not equal");
								value.Enqueue(result2);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceCritical("Hit exception ex: {0}\n, stack: {1}", ex.Message, ex.StackTrace);
			}
			finally
			{
				lock (timerConcurrencyLock)
				{
					isRunning = false;
				}
			}
		}

		/// <summary>
		/// get a timer with timeout specified in seconds
		/// </summary>
		/// <param name="timeoutInSeconds"></param>
		/// <returns></returns>
		public PooledTimer GetPooledTimer(int timeoutInSeconds)
		{
			ThrowIfDisposed();
			return new PooledTimer(timeoutInSeconds, this);
		}

		/// <summary>
		/// Start the countdown for timeout
		/// </summary>
		/// <param name="pooledTimer"></param>
		/// <returns>the begin ticks of the timer</returns>
		public long SubscribeForTimeouts(PooledTimer pooledTimer)
		{
			ThrowIfDisposed();
			if (pooledTimer.Timeout < minSupportedTimeout)
			{
				DefaultTrace.TraceWarning("Timer timeoutinSeconds {0} is less than minSupportedTimeoutInSeconds {1}, will use the minsupported value", pooledTimer.Timeout.TotalSeconds, minSupportedTimeout.TotalSeconds);
				pooledTimer.Timeout = minSupportedTimeout;
			}
			lock (subscriptionLock)
			{
				if (pooledTimersByTimeout.TryGetValue((int)pooledTimer.Timeout.TotalSeconds, out ConcurrentQueue<PooledTimer> value))
				{
					value.Enqueue(pooledTimer);
				}
				else
				{
					value = pooledTimersByTimeout.GetOrAdd((int)pooledTimer.Timeout.TotalSeconds, (int _003Cp0_003E) => new ConcurrentQueue<PooledTimer>());
					value.Enqueue(pooledTimer);
				}
				return DateTime.UtcNow.Ticks;
			}
		}
	}
}
