using Microsoft.Azure.Documents.Routing;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Policy to perform backoff retry on GoneException, InvalidPartitionException, RetryWithException, PartitionKeyRangeIsSplittingException, and PartitionKeyRangeGoneException, including their associated
	/// </summary>
	internal sealed class GoneAndRetryWithRequestRetryPolicy<TResponse> : IRequestRetryPolicy<GoneAndRetryRequestRetryPolicyContext, DocumentServiceRequest, TResponse>, IRequestRetryPolicy<DocumentServiceRequest, TResponse> where TResponse : IRetriableResponse
	{
		private const int defaultWaitTimeInSeconds = 30;

		private const int minExecutionTimeInSeconds = 5;

		private const int initialBackoffSeconds = 1;

		private const int backoffMultiplier = 2;

		private const int maximumBackoffTimeInSeconds = 15;

		private const int minFailedReplicaCountToConsiderConnectivityIssue = 3;

		private Stopwatch durationTimer = new Stopwatch();

		private TimeSpan minBackoffForRegionReroute;

		private int attemptCount = 1;

		private int attemptCountInvalidPartition = 1;

		private int regionRerouteAttemptCount;

		private int currentBackoffSeconds = 1;

		private RetryWithException lastRetryWithException;

		private readonly int waitTimeInSeconds;

		private readonly bool detectConnectivityIssues;

		public GoneAndRetryRequestRetryPolicyContext ExecuteContext
		{
			get;
		} = new GoneAndRetryRequestRetryPolicyContext();


		public GoneAndRetryWithRequestRetryPolicy(int? waitTimeInSecondsOverride = default(int?), TimeSpan minBackoffForRegionReroute = default(TimeSpan), bool detectConnectivityIssues = false)
		{
			if (waitTimeInSecondsOverride.HasValue)
			{
				waitTimeInSeconds = waitTimeInSecondsOverride.Value;
			}
			else
			{
				waitTimeInSeconds = 30;
			}
			this.detectConnectivityIssues = detectConnectivityIssues;
			this.minBackoffForRegionReroute = minBackoffForRegionReroute;
			ExecuteContext.RemainingTimeInMsOnClientRequest = TimeSpan.FromSeconds(waitTimeInSeconds);
			ExecuteContext.TimeoutForInBackoffRetryPolicy = TimeSpan.Zero;
			durationTimer.Start();
		}

		public void OnBeforeSendRequest(DocumentServiceRequest request)
		{
		}

		public bool TryHandleResponseSynchronously(DocumentServiceRequest request, TResponse response, Exception exception, out ShouldRetryResult shouldRetryResult)
		{
			Exception exception2 = null;
			TimeSpan timeSpan = TimeSpan.FromSeconds(0.0);
			TimeSpan timeSpan2 = TimeSpan.FromSeconds(0.0);
			bool flag = false;
			if (!IsBaseGone(response, exception) && !(exception is RetryWithException) && (!IsPartitionIsMigrating(response, exception) || request.ServiceIdentity != null) && (!IsInvalidPartition(response, exception) || (request.PartitionKeyRangeIdentity != null && request.PartitionKeyRangeIdentity.CollectionRid != null)) && (!IsPartitionKeySplitting(response, exception) || request.ServiceIdentity != null))
			{
				durationTimer.Stop();
				shouldRetryResult = ShouldRetryResult.NoRetry();
				return true;
			}
			if (exception is RetryWithException)
			{
				lastRetryWithException = (exception as RetryWithException);
			}
			int num = waitTimeInSeconds - Convert.ToInt32(durationTimer.Elapsed.TotalSeconds);
			num = ((num > 0) ? num : 0);
			int clientRetryCount = attemptCount;
			if (attemptCount++ > 1)
			{
				if (num <= 0)
				{
					if (IsBaseGone(response, exception) || IsPartitionIsMigrating(response, exception) || IsInvalidPartition(response, exception) || IsPartitionKeyRangeGone(response, exception) || IsPartitionKeySplitting(response, exception))
					{
						string text = $"Received {((object)exception)?.GetType().Name ?? response?.StatusCode.ToString()} after backoff/retry";
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
						DefaultTrace.TraceError("Received retrywith exception after backoff/retry. Will fail the request. {0}", ((object)exception)?.GetType().Name ?? response?.StatusCode.ToString());
					}
					durationTimer.Stop();
					shouldRetryResult = ShouldRetryResult.NoRetry(exception2);
					return true;
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
			if (IsBaseGone(response, exception))
			{
				flag = true;
			}
			else if (IsPartitionIsMigrating(response, exception))
			{
				ClearRequestContext(request);
				request.ForceCollectionRoutingMapRefresh = true;
				request.ForceMasterRefresh = true;
				flag = false;
			}
			else if (IsInvalidPartition(response, exception))
			{
				ClearRequestContext(request);
				request.RequestContext.GlobalCommittedSelectedLSN = -1L;
				if (attemptCountInvalidPartition++ > 2)
				{
					DefaultTrace.TraceCritical("Received second InvalidPartitionException after backoff/retry. Will fail the request. {0}", exception);
					shouldRetryResult = ShouldRetryResult.NoRetry(new ServiceUnavailableException(exception));
					return true;
				}
				if (request == null)
				{
					DefaultTrace.TraceCritical("Received unexpected invalid collection exception, request should be non-null.", exception);
					shouldRetryResult = ShouldRetryResult.NoRetry(new InternalServerErrorException(exception));
					return true;
				}
				request.ForceNameCacheRefresh = true;
				flag = false;
			}
			else if (IsPartitionKeySplitting(response, exception))
			{
				ClearRequestContext(request);
				request.ForcePartitionKeyRangeRefresh = true;
				flag = false;
			}
			else
			{
				flag = false;
			}
			DefaultTrace.TraceWarning("GoneAndRetryWithRequestRetryPolicy Received exception, will retry, attempt: {0}, regionRerouteAttempt: {1}, backoffTime: {2}, Timeout: {3}, Exception: {4}", attemptCount, regionRerouteAttemptCount, timeSpan, timeSpan2, exception);
			shouldRetryResult = ShouldRetryResult.RetryAfter(timeSpan);
			ExecuteContext.ForceRefresh = flag;
			ExecuteContext.IsInRetry = true;
			ExecuteContext.RemainingTimeInMsOnClientRequest = timeSpan2;
			ExecuteContext.ClientRetryCount = clientRetryCount;
			ExecuteContext.RegionRerouteAttemptCount = regionRerouteAttemptCount;
			ExecuteContext.TimeoutForInBackoffRetryPolicy = timeSpan;
			return true;
		}

		public Task<ShouldRetryResult> ShouldRetryAsync(DocumentServiceRequest request, TResponse response, Exception exception, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		private static bool IsBaseGone(TResponse response, Exception exception)
		{
			if (!(exception is GoneException))
			{
				if (response != null && response.StatusCode == HttpStatusCode.Gone)
				{
					if (response == null)
					{
						return false;
					}
					return response.SubStatusCode == SubStatusCodes.Unknown;
				}
				return false;
			}
			return true;
		}

		private static bool IsPartitionIsMigrating(TResponse response, Exception exception)
		{
			if (!(exception is PartitionIsMigratingException))
			{
				if (response != null && response.StatusCode == HttpStatusCode.Gone)
				{
					if (response == null)
					{
						return false;
					}
					return response.SubStatusCode == SubStatusCodes.CompletingPartitionMigration;
				}
				return false;
			}
			return true;
		}

		private static bool IsInvalidPartition(TResponse response, Exception exception)
		{
			if (!(exception is InvalidPartitionException))
			{
				if (response != null && response.StatusCode == HttpStatusCode.Gone)
				{
					if (response == null)
					{
						return false;
					}
					return response.SubStatusCode == SubStatusCodes.NameCacheIsStale;
				}
				return false;
			}
			return true;
		}

		private static bool IsPartitionKeySplitting(TResponse response, Exception exception)
		{
			if (!(exception is PartitionKeyRangeIsSplittingException))
			{
				if (response != null && response.StatusCode == HttpStatusCode.Gone)
				{
					if (response == null)
					{
						return false;
					}
					return response.SubStatusCode == SubStatusCodes.CompletingSplit;
				}
				return false;
			}
			return true;
		}

		private static bool IsPartitionKeyRangeGone(TResponse response, Exception exception)
		{
			if (!(exception is PartitionKeyRangeGoneException))
			{
				if (response != null && response.StatusCode == HttpStatusCode.Gone)
				{
					if (response == null)
					{
						return false;
					}
					return response.SubStatusCode == SubStatusCodes.PartitionKeyRangeGone;
				}
				return false;
			}
			return true;
		}

		private static void ClearRequestContext(DocumentServiceRequest request)
		{
			request.RequestContext.TargetIdentity = null;
			request.RequestContext.ResolvedPartitionKeyRange = null;
			request.RequestContext.QuorumSelectedLSN = -1L;
			request.RequestContext.QuorumSelectedStoreResponse = null;
		}
	}
}
