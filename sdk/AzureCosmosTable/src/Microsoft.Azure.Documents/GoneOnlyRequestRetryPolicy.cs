using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class GoneOnlyRequestRetryPolicy<TResponse> : IRequestRetryPolicy<GoneOnlyRequestRetryPolicyContext, DocumentServiceRequest, TResponse>, IRequestRetryPolicy<DocumentServiceRequest, TResponse> where TResponse : IRetriableResponse
	{
		private const int backoffMultiplier = 2;

		private const int initialBackoffTimeInSeconds = 1;

		private Stopwatch durationTimer = new Stopwatch();

		private readonly TimeSpan retryTimeout;

		private int currentBackoffTimeInSeconds;

		private bool isInRetry;

		public GoneOnlyRequestRetryPolicyContext ExecuteContext
		{
			get;
		} = new GoneOnlyRequestRetryPolicyContext();


		public GoneOnlyRequestRetryPolicy(TimeSpan retryTimeout)
		{
			this.retryTimeout = retryTimeout;
			currentBackoffTimeInSeconds = 1;
			isInRetry = false;
			ExecuteContext.RemainingTimeInMsOnClientRequest = retryTimeout;
			durationTimer.Start();
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
		}

		public bool TryHandleResponseSynchronously(DocumentServiceRequest request, TResponse response, Exception exception, out ShouldRetryResult shouldRetryResult)
		{
			if ((response == null || response.StatusCode != HttpStatusCode.Gone) && !(exception is GoneException))
			{
				shouldRetryResult = ShouldRetryResult.NoRetry();
				return true;
			}
			TimeSpan elapsed = durationTimer.Elapsed;
			if (elapsed >= retryTimeout)
			{
				DefaultTrace.TraceInformation("GoneOnlyRequestRetryPolicy - timeout {0}, elapsed {1}", retryTimeout, elapsed);
				durationTimer.Stop();
				shouldRetryResult = ShouldRetryResult.NoRetry(new ServiceUnavailableException(exception));
				return true;
			}
			TimeSpan timeSpan = retryTimeout - elapsed;
			TimeSpan timeSpan2 = TimeSpan.Zero;
			if (isInRetry)
			{
				timeSpan2 = TimeSpan.FromSeconds(currentBackoffTimeInSeconds);
				currentBackoffTimeInSeconds *= 2;
				if (timeSpan2 > timeSpan)
				{
					DefaultTrace.TraceInformation("GoneOnlyRequestRetryPolicy - timeout {0}, elapsed {1}, backoffTime {2}", retryTimeout, elapsed, timeSpan2);
					durationTimer.Stop();
					shouldRetryResult = ShouldRetryResult.NoRetry(new ServiceUnavailableException(exception));
					return true;
				}
			}
			else
			{
				isInRetry = true;
			}
			DefaultTrace.TraceInformation("GoneOnlyRequestRetryPolicy - timeout {0}, elapsed {1}, backoffTime {2}, remainingTime {3}", retryTimeout, elapsed, timeSpan2, timeSpan);
			shouldRetryResult = ShouldRetryResult.RetryAfter(timeSpan2);
			ExecuteContext.IsInRetry = isInRetry;
			ExecuteContext.ForceRefresh = true;
			ExecuteContext.RemainingTimeInMsOnClientRequest = timeSpan;
			return true;
		}

		public Task<ShouldRetryResult> ShouldRetryAsync(DocumentServiceRequest request, TResponse response, Exception exception, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
