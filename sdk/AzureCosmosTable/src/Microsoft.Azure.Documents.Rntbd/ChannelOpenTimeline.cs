using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelOpenTimeline
	{
		public delegate void ConnectionTimerDelegate(Guid activityId, string connectionCreationTime, string tcpConnectCompleteTime, string sslHandshakeCompleteTime, string rntbdHandshakeCompleteTime, string openTaskCompletionTime);

		private readonly DateTimeOffset creationTime;

		private DateTimeOffset connectTime = DateTimeOffset.MinValue;

		private DateTimeOffset sslHandshakeTime = DateTimeOffset.MinValue;

		private DateTimeOffset rntbdHandshakeTime = DateTimeOffset.MinValue;

		public static ConnectionTimerDelegate TraceFunc
		{
			get;
			set;
		}

		public ChannelOpenTimeline()
		{
			creationTime = DateTimeOffset.UtcNow;
		}

		public void RecordConnectFinishTime()
		{
			connectTime = DateTimeOffset.UtcNow;
		}

		public void RecordSslHandshakeFinishTime()
		{
			sslHandshakeTime = DateTimeOffset.UtcNow;
		}

		public void RecordRntbdHandshakeFinishTime()
		{
			rntbdHandshakeTime = DateTimeOffset.UtcNow;
		}

		public void WriteTrace()
		{
			DateTimeOffset utcNow = DateTimeOffset.UtcNow;
			TraceFunc?.Invoke(Trace.CorrelationManager.ActivityId, InvariantString(creationTime), InvariantString(connectTime), InvariantString(sslHandshakeTime), InvariantString(rntbdHandshakeTime), InvariantString(utcNow));
		}

		public static void LegacyWriteTrace(RntbdConnectionOpenTimers timers)
		{
			DateTimeOffset utcNow = DateTimeOffset.UtcNow;
			TraceFunc?.Invoke(Trace.CorrelationManager.ActivityId, InvariantString(timers.CreationTimestamp), InvariantString(timers.TcpConnectCompleteTimestamp), InvariantString(timers.SslHandshakeCompleteTimestamp), InvariantString(timers.RntbdHandshakeCompleteTimestamp), InvariantString(utcNow));
		}

		private static string InvariantString(DateTimeOffset t)
		{
			return t.ToString("o", CultureInfo.InvariantCulture);
		}
	}
}
