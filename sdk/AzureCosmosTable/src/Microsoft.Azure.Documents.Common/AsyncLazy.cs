using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Common
{
	internal sealed class AsyncLazy<T> : Lazy<Task<T>>
	{
		public AsyncLazy(Func<T> valueFactory, CancellationToken cancellationToken)
			: base((Func<Task<T>>)(() => Task.Factory.StartNewOnCurrentTaskSchedulerAsync(valueFactory, cancellationToken)))
		{
		}

		public AsyncLazy(Func<Task<T>> taskFactory, CancellationToken cancellationToken)
			: base((Func<Task<T>>)(() => Task.Factory.StartNewOnCurrentTaskSchedulerAsync(taskFactory, cancellationToken).Unwrap()))
		{
		}
	}
}
