using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class ResourceThrottleRetryPolicy : IDocumentClientRetryPolicy, IRetryPolicy
	{
		private const int DefaultMaxWaitTimeInSeconds = 60;

		private const int DefaultRetryInSeconds = 5;

		private readonly uint backoffDelayFactor;

		private readonly int maxAttemptCount;

		private readonly TimeSpan maxWaitTimeInMilliseconds;

		private int currentAttemptCount;

		private TimeSpan cumulativeRetryDelay;

		public ResourceThrottleRetryPolicy(int maxAttemptCount, int maxWaitTimeInSeconds = 60, uint backoffDelayFactor = 1u)
		{
			if (maxWaitTimeInSeconds > 2147483)
			{
				throw new ArgumentException("maxWaitTimeInSeconds", "maxWaitTimeInSeconds must be less than " + 2147483);
			}
			this.maxAttemptCount = maxAttemptCount;
			this.backoffDelayFactor = backoffDelayFactor;
			maxWaitTimeInMilliseconds = TimeSpan.FromSeconds(maxWaitTimeInSeconds);
		}

		/// <summary> 
		/// Should the caller retry the operation.
		/// </summary>
		/// <param name="exception">Exception that occured when the operation was tried</param>
		/// <param name="cancellationToken"></param>
		/// <returns>True indicates caller should retry, False otherwise</returns>
		public Task<ShouldRetryResult> ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (exception is DocumentClientException)
			{
				DocumentClientException ex = (DocumentClientException)exception;
				if (!IsValidThrottleStatusCode(ex.StatusCode))
				{
					DefaultTrace.TraceError("Operation will NOT be retried. Current attempt {0}, Status Code: {1} ", currentAttemptCount, ex.StatusCode);
					return Task.FromResult(ShouldRetryResult.NoRetry());
				}
				return ShouldRetryInternalAsync(ex.RetryAfter);
			}
			DefaultTrace.TraceError("Operation will NOT be retried. Current attempt {0}, Exception: {1} ", currentAttemptCount, GetExceptionMessage(exception));
			return Task.FromResult(ShouldRetryResult.NoRetry());
		}

		private Task<ShouldRetryResult> ShouldRetryInternalAsync(TimeSpan? retryAfter)
		{
			TimeSpan retryDelay = TimeSpan.Zero;
			if (currentAttemptCount < maxAttemptCount && CheckIfRetryNeeded(retryAfter, out retryDelay))
			{
				currentAttemptCount++;
				DefaultTrace.TraceWarning("Operation will be retried after {0} milliseconds. Current attempt {1}, Cumulative delay {2}", retryDelay.TotalMilliseconds, currentAttemptCount, cumulativeRetryDelay);
				return Task.FromResult(ShouldRetryResult.RetryAfter(retryDelay));
			}
			DefaultTrace.TraceError("Operation will NOT be retried. Current attempt {0} maxAttempts {1} Cumulative delay {2} requested retryAfter {3} maxWaitTime {4}", currentAttemptCount, maxAttemptCount, cumulativeRetryDelay, retryAfter, maxWaitTimeInMilliseconds);
			return Task.FromResult(ShouldRetryResult.NoRetry());
		}

		private string GetExceptionMessage(Exception exception)
		{
			DocumentClientException ex = exception as DocumentClientException;
			if (ex != null && ex.StatusCode.HasValue && ex.StatusCode.Value < HttpStatusCode.InternalServerError)
			{
				return exception.Message;
			}
			return exception.ToString();
		}

		/// <summary>
		/// Method that is called before a request is sent to allow the retry policy implementation
		/// to modify the state of the request.
		/// </summary>
		/// <param name="request">The request being sent to the service.</param>
		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
		}

		/// <summary>
		/// Returns True if the given <paramref name="retryAfter" /> is within retriable bounds
		/// </summary>
		/// <param name="retryAfter">Value of x-ms-retry-after-ms header</param>
		/// <param name="retryDelay">retryDelay</param>
		/// <returns>True if the exception is retriable; False otherwise</returns>
		private bool CheckIfRetryNeeded(TimeSpan? retryAfter, out TimeSpan retryDelay)
		{
			retryDelay = TimeSpan.Zero;
			if (retryAfter.HasValue)
			{
				retryDelay = retryAfter.Value;
			}
			if (backoffDelayFactor > 1)
			{
				retryDelay = TimeSpan.FromTicks(retryDelay.Ticks * backoffDelayFactor);
			}
			if (retryDelay < maxWaitTimeInMilliseconds && maxWaitTimeInMilliseconds >= (cumulativeRetryDelay = retryDelay.Add(cumulativeRetryDelay)))
			{
				if (retryDelay == TimeSpan.Zero)
				{
					DefaultTrace.TraceInformation("Received retryDelay of 0 with Http 429: {0}", retryAfter);
					retryDelay = TimeSpan.FromSeconds(5.0);
				}
				return true;
			}
			return false;
		}

		private bool IsValidThrottleStatusCode(HttpStatusCode? statusCode)
		{
			if (statusCode.HasValue)
			{
				return statusCode.Value == (HttpStatusCode)429/*HttpStatusCode.TooManyRequests*/;
			}
			return false;
		}
	}
}
