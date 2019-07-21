using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal abstract class ComparableTask : IComparableTask, IComparable<IComparableTask>, IEquatable<IComparableTask>
	{
		protected readonly int schedulePriority;

		protected ComparableTask(int schedulePriority)
		{
			this.schedulePriority = schedulePriority;
		}

		public abstract Task StartAsync(CancellationToken token);

		public virtual int CompareTo(IComparableTask other)
		{
			if (other == null)
			{
				throw new ArgumentNullException("other");
			}
			return CompareToByPriority(other as ComparableTask);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IComparableTask);
		}

		public abstract bool Equals(IComparableTask other);

		public abstract override int GetHashCode();

		protected int CompareToByPriority(ComparableTask other)
		{
			if (other == null)
			{
				return 1;
			}
			if (this == other)
			{
				return 0;
			}
			return schedulePriority.CompareTo(other.schedulePriority);
		}
	}
}
