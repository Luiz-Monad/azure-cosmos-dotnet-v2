using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class GoneOnlyRetryPolicy : IRetryPolicy<Tuple<bool, bool, TimeSpan>>
	{
		private const int backoffMultiplier = 2;

		private const int initialBackoffTimeInSeconds = 1;

		private Stopwatch durationTimer = new Stopwatch();

		private readonly TimeSpan retryTimeout;

		private DocumentServiceRequest request;

		private int currentBackoffTimeInSeconds;

		private bool isInRetry;

		Tuple<bool, bool, TimeSpan> IRetryPolicy<Tuple<bool, bool, TimeSpan>>.InitialArgumentValue
		{
			get
			{
				return Tuple.Create(item1: false, item2: false, retryTimeout);
			}
		}

		public GoneOnlyRetryPolicy(DocumentServiceRequest request, TimeSpan retryTimeout)
		{
			this.request = request;
			this.retryTimeout = retryTimeout;
			currentBackoffTimeInSeconds = 1;
			isInRetry = false;
			durationTimer.Start();
		}

		Task<ShouldRetryResult<Tuple<bool, bool, TimeSpan>>> IRetryPolicy<Tuple<bool, bool, TimeSpan>>.ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!(exception is GoneException))
			{
				durationTimer.Stop();
				return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.NoRetry());
			}
			TimeSpan elapsed = durationTimer.Elapsed;
			if (elapsed >= retryTimeout)
			{
				DefaultTrace.TraceInformation("GoneOnlyRetryPolicy - timeout {0}, elapsed {1}", retryTimeout, elapsed);
				durationTimer.Stop();
				return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.NoRetry(new ServiceUnavailableException(exception)));
			}
			TimeSpan timeSpan = retryTimeout - elapsed;
			TimeSpan timeSpan2 = TimeSpan.Zero;
			if (isInRetry)
			{
				timeSpan2 = TimeSpan.FromSeconds(currentBackoffTimeInSeconds);
				currentBackoffTimeInSeconds *= 2;
				if (timeSpan2 > timeSpan)
				{
					DefaultTrace.TraceInformation("GoneOnlyRetryPolicy - timeout {0}, elapsed {1}, backoffTime {2}", retryTimeout, elapsed, timeSpan2);
					durationTimer.Stop();
					return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.NoRetry(new ServiceUnavailableException(exception)));
				}
			}
			else
			{
				isInRetry = true;
			}
			DefaultTrace.TraceInformation("GoneOnlyRetryPolicy - timeout {0}, elapsed {1}, backoffTime {2}, remainingTime {3}", retryTimeout, elapsed, timeSpan2, timeSpan);
			return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.RetryAfter(timeSpan2, Tuple.Create(item1: true, item2: true, timeSpan)));
		}
	}
}
