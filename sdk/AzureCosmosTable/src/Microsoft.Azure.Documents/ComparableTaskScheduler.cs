using Microsoft.Azure.Documents.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class ComparableTaskScheduler : IDisposable
	{
		private const int MinimumBatchSize = 1;

		private readonly AsyncCollection<IComparableTask> taskQueue;

		private readonly ConcurrentDictionary<IComparableTask, Task> delayedTasks;

		private readonly CancellationTokenSource tokenSource;

		private readonly SemaphoreSlim canRunTaskSemaphoreSlim;

		private readonly Task schedulerTask;

		private int maximumConcurrencyLevel;

		private volatile bool isStopped;

		public int MaximumConcurrencyLevel => maximumConcurrencyLevel;

		public int CurrentRunningTaskCount => maximumConcurrencyLevel - Math.Max(0, canRunTaskSemaphoreSlim.CurrentCount);

		public bool IsStopped => isStopped;

		private CancellationToken CancellationToken => tokenSource.Token;

		public ComparableTaskScheduler()
			: this(Environment.ProcessorCount)
		{
		}

		public ComparableTaskScheduler(int maximumConcurrencyLevel)
			: this(Enumerable.Empty<IComparableTask>(), maximumConcurrencyLevel)
		{
		}

		public ComparableTaskScheduler(IEnumerable<IComparableTask> tasks, int maximumConcurrencyLevel)
		{
			taskQueue = new AsyncCollection<IComparableTask>(new PriorityQueue<IComparableTask>(tasks, isSynchronized: true));
			delayedTasks = new ConcurrentDictionary<IComparableTask, Task>();
			this.maximumConcurrencyLevel = maximumConcurrencyLevel;
			tokenSource = new CancellationTokenSource();
			canRunTaskSemaphoreSlim = new SemaphoreSlim(maximumConcurrencyLevel);
			schedulerTask = ScheduleAsync();
		}

		public void IncreaseMaximumConcurrencyLevel(int delta)
		{
			if (delta <= 0)
			{
				throw new ArgumentOutOfRangeException("delta must be a positive number.");
			}
			canRunTaskSemaphoreSlim.Release(delta);
			maximumConcurrencyLevel += delta;
		}

		public void Dispose()
		{
			Stop();
		}

		public void Stop()
		{
			isStopped = true;
			tokenSource.Cancel();
			delayedTasks.Clear();
		}

		public bool TryQueueTask(IComparableTask comparableTask, TimeSpan delay = default(TimeSpan))
		{
			if (comparableTask == null)
			{
				throw new ArgumentNullException("task");
			}
			if (isStopped)
			{
				return false;
			}
			Task task = new Task<Task>(() => QueueDelayedTaskAsync(comparableTask, delay), CancellationToken);
			if (delayedTasks.TryAdd(comparableTask, task))
			{
				task.Start();
				return true;
			}
			return false;
		}

		private async Task QueueDelayedTaskAsync(IComparableTask comparableTask, TimeSpan delay)
		{
			if (delayedTasks.TryRemove(comparableTask, out Task value) && !value.IsCanceled)
			{
				if (delay > default(TimeSpan))
				{
					await Task.Delay(delay, CancellationToken);
				}
				if (!taskQueue.TryPeek(out IComparableTask item) || comparableTask.CompareTo(item) > 0)
				{
					await taskQueue.AddAsync(comparableTask, CancellationToken);
				}
				else
				{
					await ExecuteComparableTaskAsync(comparableTask);
				}
			}
		}

		private async Task ScheduleAsync()
		{
			while (!isStopped)
			{
				await ExecuteComparableTaskAsync(await taskQueue.TakeAsync(CancellationToken));
			}
		}

		private async Task ExecuteComparableTaskAsync(IComparableTask comparableTask)
		{
			await canRunTaskSemaphoreSlim.WaitAsync(CancellationToken);
			#pragma warning disable 4014
			Task.Factory.StartNewOnCurrentTaskSchedulerAsync(() => comparableTask.StartAsync(CancellationToken).ContinueWith(delegate
			{
				canRunTaskSemaphoreSlim.Release();
			}, TaskScheduler.Current), CancellationToken);
			#pragma warning restore 4014
		}
	}
}
