using System;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelProperties
	{
		public UserAgentContainer UserAgent
		{
			get;
			private set;
		}

		public string CertificateHostNameOverride
		{
			get;
			private set;
		}

		/// <summary>
		/// timer pool to track request timeout
		/// </summary>
		public TimerPool RequestTimerPool
		{
			get;
			private set;
		}

		/// <summary>
		/// timer pool to track idle channels
		/// </summary>
		public TimerPool IdleTimerPool
		{
			get;
			private set;
		}

		public TimeSpan RequestTimeout
		{
			get;
			private set;
		}

		public TimeSpan OpenTimeout
		{
			get;
			private set;
		}

		public TcpPortReuse PortReusePolicy
		{
			get;
			private set;
		}

		public int MaxChannels
		{
			get;
			private set;
		}

		public int PartitionCount
		{
			get;
			private set;
		}

		public int MaxRequestsPerChannel
		{
			get;
			private set;
		}

		public TimeSpan ReceiveHangDetectionTime
		{
			get;
			private set;
		}

		public TimeSpan SendHangDetectionTime
		{
			get;
			private set;
		}

		public TimeSpan IdleTimeout
		{
			get;
			private set;
		}

		public UserPortPool UserPortPool
		{
			get;
			private set;
		}

		public ChannelProperties(UserAgentContainer userAgent, string certificateHostNameOverride, TimerPool requestTimerPool, TimeSpan requestTimeout, TimeSpan openTimeout, TcpPortReuse portReusePolicy, UserPortPool userPortPool, int maxChannels, int partitionCount, int maxRequestsPerChannel, TimeSpan receiveHangDetectionTime, TimeSpan sendHangDetectionTime, TimeSpan idleTimeout, TimerPool idleTimerPool)
		{
			UserAgent = userAgent;
			CertificateHostNameOverride = certificateHostNameOverride;
			RequestTimerPool = requestTimerPool;
			RequestTimeout = requestTimeout;
			OpenTimeout = openTimeout;
			PortReusePolicy = portReusePolicy;
			UserPortPool = userPortPool;
			MaxChannels = maxChannels;
			PartitionCount = partitionCount;
			MaxRequestsPerChannel = maxRequestsPerChannel;
			ReceiveHangDetectionTime = receiveHangDetectionTime;
			SendHangDetectionTime = sendHangDetectionTime;
			IdleTimeout = idleTimeout;
			IdleTimerPool = idleTimerPool;
		}
	}
}
