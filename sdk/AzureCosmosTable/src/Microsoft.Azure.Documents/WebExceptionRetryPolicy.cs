using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class WebExceptionRetryPolicy : IRetryPolicy
	{
		private const int waitTimeInSeconds = 30;

		private const int initialBackoffSeconds = 1;

		private const int backoffMultiplier = 2;

		private Stopwatch durationTimer = new Stopwatch();

		private int attemptCount = 1;

		private int currentBackoffSeconds = 1;

		public WebExceptionRetryPolicy()
		{
			durationTimer.Start();
		}

		public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			TimeSpan backoffTime = TimeSpan.FromSeconds(0.0);
			if (!WebExceptionUtility.IsWebExceptionRetriable(exception))
			{
				durationTimer.Stop();
				return Task.FromResult(ShouldRetryResult.NoRetry());
			}
			if (attemptCount++ > 1)
			{
				int num = 30 - durationTimer.Elapsed.Seconds;
				if (num <= 0)
				{
					durationTimer.Stop();
					return Task.FromResult(ShouldRetryResult.NoRetry());
				}
				backoffTime = TimeSpan.FromSeconds(Math.Min(currentBackoffSeconds, num));
				currentBackoffSeconds *= 2;
			}
			DefaultTrace.TraceWarning("Received retriable web exception, will retry, {0}", exception);
			return Task.FromResult(ShouldRetryResult.RetryAfter(backoffTime));
		}
	}
}
