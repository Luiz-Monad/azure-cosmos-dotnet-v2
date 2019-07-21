using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class SessionTokenMismatchRetryPolicy : IRetryPolicy
	{
		private const string sessionRetryInitialBackoff = "AZURE_COSMOS_SESSION_RETRY_INITIAL_BACKOFF";

		private const string sessionRetryMaximumBackoff = "AZURE_COSMOS_SESSION_RETRY_MAXIMUM_BACKOFF";

		private const int defaultWaitTimeInMilliSeconds = 5000;

		private const int defaultInitialBackoffTimeInMilliseconds = 5;

		private const int defaultMaximumBackoffTimeInMilliseconds = 50;

		private const int backoffMultiplier = 2;

		private int retryCount;

		private Stopwatch durationTimer = new Stopwatch();

		private int waitTimeInMilliSeconds;

		private int initialBackoffTimeInMilliseconds = 5;

		private int maximumBackoffTimeInMilliseconds = 50;

		private int currentBackoffInMilliSeconds;

		public SessionTokenMismatchRetryPolicy(int waitTimeInMilliSeconds = 5000)
		{
			durationTimer.Start();
			retryCount = 0;
			this.waitTimeInMilliSeconds = waitTimeInMilliSeconds;
			string environmentVariable = Environment.GetEnvironmentVariable("AZURE_COSMOS_SESSION_RETRY_INITIAL_BACKOFF");
			if (!string.IsNullOrWhiteSpace(environmentVariable))
			{
				if (int.TryParse(environmentVariable, out int result) && result >= 0)
				{
					initialBackoffTimeInMilliseconds = result;
				}
				else
				{
					DefaultTrace.TraceCritical("The value of AZURE_COSMOS_SESSION_RETRY_INITIAL_BACKOFF is invalid.  Value: {0}", result);
				}
			}
			string environmentVariable2 = Environment.GetEnvironmentVariable("AZURE_COSMOS_SESSION_RETRY_MAXIMUM_BACKOFF");
			if (!string.IsNullOrWhiteSpace(environmentVariable2))
			{
				if (int.TryParse(environmentVariable2, out int result2) && result2 >= 0)
				{
					maximumBackoffTimeInMilliseconds = result2;
				}
				else
				{
					DefaultTrace.TraceCritical("The value of AZURE_COSMOS_SESSION_RETRY_MAXIMUM_BACKOFF is invalid.  Value: {0}", result2);
				}
			}
			currentBackoffInMilliSeconds = initialBackoffTimeInMilliseconds;
		}

		public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ShouldRetryResult result = ShouldRetryResult.NoRetry();
			DocumentClientException ex = exception as DocumentClientException;
			if (ex != null)
			{
				result = ShouldRetryInternalAsync(ex?.StatusCode, ex?.GetSubStatus());
			}
			return Task.FromResult(result);
		}

		private ShouldRetryResult ShouldRetryInternalAsync(HttpStatusCode? statusCode, SubStatusCodes? subStatusCode)
		{
			if (statusCode.HasValue && statusCode.Value == HttpStatusCode.NotFound && subStatusCode.HasValue && subStatusCode.Value == SubStatusCodes.PartitionKeyRangeGone)
			{
				int num = waitTimeInMilliSeconds - Convert.ToInt32(durationTimer.Elapsed.TotalMilliseconds);
				if (num <= 0)
				{
					durationTimer.Stop();
					DefaultTrace.TraceInformation("SessionTokenMismatchRetryPolicy not retrying because it has exceeded the time limit. Retry count = {0}", retryCount);
					return ShouldRetryResult.NoRetry();
				}
				TimeSpan backoffTime = TimeSpan.Zero;
				if (retryCount > 0)
				{
					backoffTime = TimeSpan.FromMilliseconds(Math.Min(currentBackoffInMilliSeconds, num));
					currentBackoffInMilliSeconds = Math.Min(currentBackoffInMilliSeconds * 2, maximumBackoffTimeInMilliseconds);
				}
				retryCount++;
				DefaultTrace.TraceInformation("SessionTokenMismatchRetryPolicy will retry. Retry count = {0}.  Backoff time = {1} ms", retryCount, backoffTime.Milliseconds);
				return ShouldRetryResult.RetryAfter(backoffTime);
			}
			durationTimer.Stop();
			DefaultTrace.TraceInformation("SessionTokenMismatchRetryPolicy not retrying because StatusCode or SubStatusCode not found.");
			return ShouldRetryResult.NoRetry();
		}
	}
}
