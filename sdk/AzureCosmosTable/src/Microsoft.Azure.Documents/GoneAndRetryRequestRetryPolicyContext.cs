using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class GoneAndRetryRequestRetryPolicyContext
	{
		public bool ForceRefresh
		{
			get;
			set;
		}

		public bool IsInRetry
		{
			get;
			set;
		}

		public TimeSpan RemainingTimeInMsOnClientRequest
		{
			get;
			set;
		}

		public int ClientRetryCount
		{
			get;
			set;
		}

		public int RegionRerouteAttemptCount
		{
			get;
			set;
		}

		public TimeSpan TimeoutForInBackoffRetryPolicy
		{
			get;
			set;
		}
	}
}
