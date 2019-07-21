using Microsoft.Azure.Documents.Rntbd;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class RntbdConnectionDispenser : IConnectionDispenser, IDisposable
	{
		private readonly int requestTimeoutInSeconds;

		private readonly int idleConnectionTimeoutInSeconds;

		private readonly string overrideHostNameInCertificate;

		private readonly int openTimeoutInSeconds;

		private readonly UserAgentContainer userAgent;

		private bool isDisposed;

		private TimerPool timerPool;

		public RntbdConnectionDispenser(int requestTimeoutInSeconds, string overrideHostNameInCertificate, int openTimeoutInSeconds, int idleConnectionTimeoutInSeconds, int timerPoolGranularityInSeconds, UserAgentContainer userAgent)
		{
			this.requestTimeoutInSeconds = requestTimeoutInSeconds;
			this.overrideHostNameInCertificate = overrideHostNameInCertificate;
			this.idleConnectionTimeoutInSeconds = idleConnectionTimeoutInSeconds;
			this.openTimeoutInSeconds = openTimeoutInSeconds;
			this.userAgent = userAgent;
			int num = 0;
			if (timerPoolGranularityInSeconds > 0 && timerPoolGranularityInSeconds < openTimeoutInSeconds && timerPoolGranularityInSeconds < requestTimeoutInSeconds)
			{
				num = timerPoolGranularityInSeconds;
			}
			else if (openTimeoutInSeconds > 0 && requestTimeoutInSeconds > 0)
			{
				num = Math.Min(openTimeoutInSeconds, requestTimeoutInSeconds);
			}
			else if (openTimeoutInSeconds > 0)
			{
				num = openTimeoutInSeconds;
			}
			else if (requestTimeoutInSeconds > 0)
			{
				num = requestTimeoutInSeconds;
			}
			timerPool = new TimerPool(num);
			DefaultTrace.TraceInformation("RntbdConnectionDispenser: requestTimeoutInSeconds: {0}, openTimeoutInSeconds: {1}, timerValueInSeconds: {2}", requestTimeoutInSeconds, openTimeoutInSeconds, num);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					timerPool.Dispose();
					timerPool = null;
					DefaultTrace.TraceInformation("RntbdConnectionDispenser Disposed");
				}
				isDisposed = true;
			}
		}

		public async Task<IConnection> OpenNewConnection(Guid activityId, Uri fullUri, string poolKey)
		{
			RntbdConnection connection = new RntbdConnection(fullUri, requestTimeoutInSeconds, overrideHostNameInCertificate, openTimeoutInSeconds, idleConnectionTimeoutInSeconds, poolKey, userAgent, timerPool);
			DateTimeOffset now = DateTimeOffset.Now;
			try
			{
				await connection.Open(activityId, fullUri);
				return connection;
			}
			finally
			{
				ChannelOpenTimeline.LegacyWriteTrace(connection.ConnectionTimers);
			}
		}
	}
}
