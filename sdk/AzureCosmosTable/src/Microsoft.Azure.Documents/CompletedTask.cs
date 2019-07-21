using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal static class CompletedTask
	{
		private static Task instance;

		public static Task Instance
		{
			get
			{
				if (instance == null)
				{
					TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
					taskCompletionSource.SetResult(result: true);
					instance = taskCompletionSource.Task;
				}
				return instance;
			}
		}

		public static Task<T> SetException<T>(Exception exception)
		{
			TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
			taskCompletionSource.SetException(exception);
			return taskCompletionSource.Task;
		}
	}
}
