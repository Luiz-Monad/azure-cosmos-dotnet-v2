using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class ActivityScope : IDisposable
	{
		private readonly Guid ambientActivityId;

		public ActivityScope(Guid activityId)
		{
			ambientActivityId = Trace.CorrelationManager.ActivityId;
			Trace.CorrelationManager.ActivityId = activityId;
		}

		public void Dispose()
		{
			Trace.CorrelationManager.ActivityId = ambientActivityId;
		}
	}
}
