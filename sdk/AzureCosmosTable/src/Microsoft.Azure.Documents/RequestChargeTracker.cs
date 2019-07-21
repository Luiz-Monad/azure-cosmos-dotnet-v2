using System.Threading;

namespace Microsoft.Azure.Documents
{
	internal sealed class RequestChargeTracker
	{
		/// <summary>
		/// Total accumulated Charge that would be presented to the client in the next FeedResponse
		/// </summary>
		private long totalRUsNotServedToClient;

		private long totalRUs;

		/// <summary>
		/// 100 preserves 2 decimal points, 1000 3 and so on. This is because Interlocked operations are not supported for doubles.
		/// </summary>
		private const int numberOfDecimalPointToReserveFactor = 1000;

		public double TotalRequestCharge => (double)totalRUs / 1000.0;

		public void AddCharge(double ruUsage)
		{
			Interlocked.Add(ref totalRUsNotServedToClient, (long)(ruUsage * 1000.0));
			Interlocked.Add(ref totalRUs, (long)(ruUsage * 1000.0));
		}

		/// <summary>
		/// Gets the Charge incurred so far in a thread-safe manner, and resets the value to zero. The function effectively returns
		/// all charges accumulated so far, which will be returned to client as a part of the feedResponse. And the value is reset to 0 
		/// so that we can keep on accumulating any new charges incurred by any new backend calls which returened after the current feedreposnse 
		/// is served to the user. 
		/// </summary>
		/// <returns></returns>
		public double GetAndResetCharge()
		{
			return (double)Interlocked.Exchange(ref totalRUsNotServedToClient, 0L) / 1000.0;
		}
	}
}
