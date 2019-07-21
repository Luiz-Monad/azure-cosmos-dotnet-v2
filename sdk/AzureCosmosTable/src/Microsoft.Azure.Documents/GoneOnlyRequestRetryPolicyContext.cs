using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class GoneOnlyRequestRetryPolicyContext
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
	}
}
