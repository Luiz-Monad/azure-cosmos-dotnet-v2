using System;
using System.Threading;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Tracks remaining timespan.
	/// </summary>
	internal sealed class TimeoutHelper
	{
		private readonly DateTime startTime;

		private readonly TimeSpan timeOut;

		private readonly CancellationToken cancellationToken;

		public TimeoutHelper(TimeSpan timeOut, CancellationToken cancellationToken = default(CancellationToken))
		{
			startTime = DateTime.UtcNow;
			this.timeOut = timeOut;
			this.cancellationToken = cancellationToken;
		}

		public bool IsElapsed()
		{
			return DateTime.UtcNow.Subtract(startTime) >= timeOut;
		}

		public TimeSpan GetRemainingTime()
		{
			TimeSpan ts = DateTime.UtcNow.Subtract(startTime);
			return timeOut.Subtract(ts);
		}

		public void ThrowTimeoutIfElapsed()
		{
			if (IsElapsed())
			{
				throw new RequestTimeoutException(RMResources.RequestTimeout);
			}
		}

		public void ThrowGoneIfElapsed()
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (IsElapsed())
			{
				throw new GoneException(RMResources.Gone);
			}
		}
	}
}
