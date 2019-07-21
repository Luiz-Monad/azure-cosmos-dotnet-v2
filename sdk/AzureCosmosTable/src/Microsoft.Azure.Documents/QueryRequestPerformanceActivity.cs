namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Dummy QueryRequestPerformanceActivity class as we don't support PerfCounters yet.
	/// </summary>
	internal class QueryRequestPerformanceActivity
	{
		public void ActivityComplete(bool markComplete)
		{
		}
	}
}
