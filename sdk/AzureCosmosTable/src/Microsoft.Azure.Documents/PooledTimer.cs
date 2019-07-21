using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class PooledTimer
	{
		/// <summary>
		/// keeps track of the timer was started, timeout time is calculated using this
		/// </summary>
		private long beginTicks;

		/// <summary>
		/// TimeSpan to timeout
		/// </summary>
		private TimeSpan timeoutPeriod;

		/// <summary>
		/// PooledTimer subscribes to the TimerPool to get notified when the timeout has expired
		/// </summary>
		private TimerPool timerPool;

		/// <summary>
		/// tcs is set to completed state if the timeout occurs else its set to cancelled state 
		/// </summary>
		private readonly TaskCompletionSource<object> tcs;

		private readonly object memberLock;

		private bool timerStarted;

		/// <summary>
		/// this is the expected ticks when this timer should be fired
		/// </summary>
		public long TimeoutTicks => beginTicks + Timeout.Ticks;

		/// <summary>
		/// amount of time in seconds after which the timeout should be fired
		/// </summary>
		public TimeSpan Timeout
		{
			get
			{
				return timeoutPeriod;
			}
			set
			{
				timeoutPeriod = value;
			}
		}

		public PooledTimer(int timeout, TimerPool timerPool)
		{
			timeoutPeriod = TimeSpan.FromSeconds(timeout);
			tcs = new TaskCompletionSource<object>();
			this.timerPool = timerPool;
			memberLock = new object();
		}

		/// <summary>
		/// Starts the timer for the timeout period specfied in constructor
		/// </summary>
		/// <returns>Returns the Task upon which you can await on until completion</returns>
		public Task StartTimerAsync()
		{
			lock (memberLock)
			{
				if (timerStarted)
				{
					throw new InvalidOperationException("Timer Already Started");
				}
				beginTicks = timerPool.SubscribeForTimeouts(this);
				timerStarted = true;
				return tcs.Task;
			}
		}

		/// <summary>
		/// Cancels the timer by setting the tcs state to cancelled
		/// </summary>
		public bool CancelTimer()
		{
			return tcs.TrySetCanceled();
		}

		/// <summary>
		/// Invoked by the TimerPool when the timeout period has elapsed
		/// If the state is already cancelled, its a noop else it changes the state to completed
		/// signalling a timeout on the TaskWaiter
		/// </summary>
		internal bool FireTimeout()
		{
			return tcs.TrySetResult(null);
		}
	}
}
