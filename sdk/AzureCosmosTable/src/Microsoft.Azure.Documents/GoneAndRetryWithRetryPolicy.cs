using Microsoft.Azure.Documents.Routing;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Policy to perform backoff retry on GoneException, InvalidPartitionException and RetryWithException
	/// TArg1: Perform force refresh.
	/// TArg2: TimeSpan for completing the work in the callback
	/// </summary>
	internal sealed class GoneAndRetryWithRetryPolicy : IRetryPolicy<bool>, IRetryPolicy<Tuple<bool, bool, TimeSpan>>, IRetryPolicy<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>
	{
		private const int defaultWaitTimeInSeconds = 30;

		private const int minExecutionTimeInSeconds = 5;

		private const int initialBackoffSeconds = 1;

		private const int backoffMultiplier = 2;

		private const int maximumBackoffTimeInSeconds = 15;

		private const int minFailedReplicaCountToConsiderConnectivityIssue = 3;

		private Stopwatch durationTimer = new Stopwatch();

		private int attemptCount = 1;

		private int attemptCountInvalidPartition = 1;

		private int regionRerouteAttemptCount;

		private TimeSpan minBackoffForRegionReroute;

		private RetryWithException lastRetryWithException;

		private readonly int waitTimeInSeconds;

		private readonly bool detectConnectivityIssues;

		private int currentBackoffSeconds = 1;

		private DocumentServiceRequest request;

		bool IRetryPolicy<bool>.InitialArgumentValue
		{
			get
			{
				return false;
			}
		}

		Tuple<bool, bool, TimeSpan> IRetryPolicy<Tuple<bool, bool, TimeSpan>>.InitialArgumentValue
		{
			get
			{
				return Tuple.Create(item1: false, item2: false, TimeSpan.FromSeconds(waitTimeInSeconds));
			}
		}

		Tuple<bool, bool, TimeSpan, int, int, TimeSpan> IRetryPolicy<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.InitialArgumentValue
		{
			get
			{
				return Tuple.Create(item1: false, item2: false, TimeSpan.FromSeconds(waitTimeInSeconds), 0, 0, TimeSpan.Zero);
			}
		}

		public GoneAndRetryWithRetryPolicy(DocumentServiceRequest request = null, int? waitTimeInSecondsOverride = default(int?), TimeSpan minBackoffForRegionReroute = default(TimeSpan), bool detectConnectivityIssues = false)
		{
			if (waitTimeInSecondsOverride.HasValue)
			{
				waitTimeInSeconds = waitTimeInSecondsOverride.Value;
			}
			else
			{
				waitTimeInSeconds = 30;
			}
			this.request = request;
			this.detectConnectivityIssues = detectConnectivityIssues;
			this.minBackoffForRegionReroute = minBackoffForRegionReroute;
			durationTimer.Start();
		}

		/// <summary>
		/// ShouldRetry method
		/// </summary>
		/// <param name="exception">Exception thrown by callback</param>
		/// <param name="cancellationToken"></param>
		/// <returns>Is the retry helper should retry</returns>
		async Task<ShouldRetryResult<bool>> IRetryPolicy<bool>.ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>> shouldRetryResult = await((IRetryPolicy<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>)this).ShouldRetryAsync(exception, cancellationToken);
			if (shouldRetryResult.ShouldRetry)
			{
				return ShouldRetryResult<bool>.RetryAfter(shouldRetryResult.BackoffTime, shouldRetryResult.PolicyArg1.Item1);
			}
			return ShouldRetryResult<bool>.NoRetry(shouldRetryResult.ExceptionToThrow);
		}

		async Task<ShouldRetryResult<Tuple<bool, bool, TimeSpan>>> IRetryPolicy<Tuple<bool, bool, TimeSpan>>.ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>> shouldRetryResult = await((IRetryPolicy<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>)this).ShouldRetryAsync(exception, cancellationToken);
			if (shouldRetryResult.ShouldRetry)
			{
				return ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.RetryAfter(shouldRetryResult.BackoffTime, Tuple.Create(shouldRetryResult.PolicyArg1.Item1, shouldRetryResult.PolicyArg1.Item2, shouldRetryResult.PolicyArg1.Item3));
			}
			return ShouldRetryResult<Tuple<bool, bool, TimeSpan>>.NoRetry(shouldRetryResult.ExceptionToThrow);
		}

		/// <summary>
		/// ShouldRetry method
		/// </summary>
		/// <param name="exception">Exception thrown by callback</param>
		/// <param name="cancellationToken"></param>
		/// <returns>Is the retry helper should retry</returns>
		Task<ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>> IRetryPolicy<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.ShouldRetryAsync(Exception exception, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Exception exception2 = null;
			TimeSpan timeSpan = TimeSpan.FromSeconds(0.0);
			TimeSpan timeSpan2 = TimeSpan.FromSeconds(0.0);
			bool flag = false;
			if (!(exception is GoneException) && !(exception is RetryWithException) && (!(exception is PartitionIsMigratingException) || request.ServiceIdentity != null) && (!(exception is InvalidPartitionException) || (request.PartitionKeyRangeIdentity != null && request.PartitionKeyRangeIdentity.CollectionRid != null)) && (!(exception is PartitionKeyRangeIsSplittingException) || request.ServiceIdentity != null))
			{
				durationTimer.Stop();
				return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.NoRetry());
			}
			if (exception is RetryWithException)
			{
				lastRetryWithException = (exception as RetryWithException);
			}
			int num = waitTimeInSeconds - Convert.ToInt32(durationTimer.Elapsed.TotalSeconds);
			num = ((num > 0) ? num : 0);
			int item = attemptCount;
			if (attemptCount++ > 1)
			{
				if (num <= 0)
				{
					if (exception is GoneException || exception is PartitionIsMigratingException || exception is InvalidPartitionException || exception is PartitionKeyRangeGoneException || exception is PartitionKeyRangeIsSplittingException)
					{
						string text = $"Received {((object)exception).GetType().Name} after backoff/retry";
						if (lastRetryWithException != null)
						{
							DefaultTrace.TraceError("{0} including at least one RetryWithException. Will fail the request with RetryWithException. Exception: {1}. RetryWithException: {2}", text, exception, lastRetryWithException);
							exception2 = lastRetryWithException;
						}
						else
						{
							DefaultTrace.TraceError("{0}. Will fail the request. {1}", text, exception);
							exception2 = ((detectConnectivityIssues && request.RequestContext.ClientRequestStatistics != null && request.RequestContext.ClientRequestStatistics.IsCpuOverloaded) ? new ServiceUnavailableException(string.Format(RMResources.ClientCpuOverload, request.RequestContext.ClientRequestStatistics.FailedReplicas.Count, (request.RequestContext.ClientRequestStatistics.RegionsContacted.Count == 0) ? 1 : request.RequestContext.ClientRequestStatistics.RegionsContacted.Count)) : ((!detectConnectivityIssues || request.RequestContext.ClientRequestStatistics == null || request.RequestContext.ClientRequestStatistics.FailedReplicas.Count < 3) ? new ServiceUnavailableException(exception) : new ServiceUnavailableException(string.Format(RMResources.ClientUnavailable, request.RequestContext.ClientRequestStatistics.FailedReplicas.Count, (request.RequestContext.ClientRequestStatistics.RegionsContacted.Count == 0) ? 1 : request.RequestContext.ClientRequestStatistics.RegionsContacted.Count), exception)));
						}
					}
					else
					{
						DefaultTrace.TraceError("Received retrywith exception after backoff/retry. Will fail the request. {0}", exception);
					}
					durationTimer.Stop();
					return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.NoRetry(exception2));
				}
				timeSpan = TimeSpan.FromSeconds(Math.Min(Math.Min(currentBackoffSeconds, num), 15));
				currentBackoffSeconds *= 2;
			}
			double num2 = (double)num - timeSpan.TotalSeconds;
			timeSpan2 = ((num2 > 0.0) ? TimeSpan.FromSeconds(num2) : TimeSpan.FromSeconds(5.0));
			if (timeSpan >= minBackoffForRegionReroute)
			{
				regionRerouteAttemptCount++;
			}
			if (exception is GoneException)
			{
				flag = true;
			}
			else if (exception is PartitionIsMigratingException)
			{
				ClearRequestContext();
				request.ForceCollectionRoutingMapRefresh = true;
				request.ForceMasterRefresh = true;
				flag = false;
			}
			else if (exception is InvalidPartitionException)
			{
				ClearRequestContext();
				request.RequestContext.GlobalCommittedSelectedLSN = -1L;
				if (attemptCountInvalidPartition++ > 2)
				{
					DefaultTrace.TraceCritical("Received second InvalidPartitionException after backoff/retry. Will fail the request. {0}", exception);
					return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.NoRetry(new ServiceUnavailableException(exception)));
				}
				if (request == null)
				{
					DefaultTrace.TraceCritical("Received unexpected invalid collection exception, request should be non-null.", exception);
					return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.NoRetry(new InternalServerErrorException(exception)));
				}
				request.ForceNameCacheRefresh = true;
				flag = false;
			}
			else if (exception is PartitionKeyRangeIsSplittingException)
			{
				ClearRequestContext();
				request.ForcePartitionKeyRangeRefresh = true;
				flag = false;
			}
			else
			{
				flag = false;
			}
			DefaultTrace.TraceWarning("GoneAndRetryWithRetryPolicy Received exception, will retry, attempt: {0}, regionRerouteAttempt: {1}, backoffTime: {2}, Timeout: {3}, Exception: {4}", attemptCount, regionRerouteAttemptCount, timeSpan, timeSpan2, exception);
			return Task.FromResult(ShouldRetryResult<Tuple<bool, bool, TimeSpan, int, int, TimeSpan>>.RetryAfter(timeSpan, Tuple.Create(flag, item2: true, timeSpan2, item, regionRerouteAttemptCount, timeSpan)));
		}

		private void ClearRequestContext()
		{
			request.RequestContext.TargetIdentity = null;
			request.RequestContext.ResolvedPartitionKeyRange = null;
			request.RequestContext.QuorumSelectedLSN = -1L;
			request.RequestContext.QuorumSelectedStoreResponse = null;
		}
	}
}
