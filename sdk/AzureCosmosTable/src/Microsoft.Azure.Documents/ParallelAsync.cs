using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal static class ParallelAsync
	{
		public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
		{
			return Task.WhenAll(from partition in Partitioner.Create(source).GetPartitions(dop)
			select Task.Factory.StartNewOnCurrentTaskSchedulerAsync((Func<Task>)async delegate
			{
				using (partition)
				{
					while (partition.MoveNext())
					{
						await body(partition.Current);
					}
				}
			}, TaskCreationOptions.DenyChildAttach).Unwrap());
		}
	}
}
