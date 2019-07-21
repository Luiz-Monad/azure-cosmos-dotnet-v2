using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IComparableTask : IComparable<IComparableTask>, IEquatable<IComparableTask>
	{
		Task StartAsync(CancellationToken token);
	}
}
