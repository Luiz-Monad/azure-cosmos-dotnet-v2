using System;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelOpenArguments
	{
		private readonly ChannelCommonArguments commonArguments;

		private readonly ChannelOpenTimeline openTimeline;

		private readonly int openTimeoutSeconds;

		private readonly TcpPortReuse portReusePolicy;

		private readonly UserPortPool userPortPool;

		public ChannelCommonArguments CommonArguments => commonArguments;

		public ChannelOpenTimeline OpenTimeline => openTimeline;

		public int OpenTimeoutSeconds => openTimeoutSeconds;

		public TcpPortReuse PortReusePolicy => portReusePolicy;

		public UserPortPool PortPool => userPortPool;

		public ChannelOpenArguments(Guid activityId, ChannelOpenTimeline openTimeline, int openTimeoutSeconds, TcpPortReuse portReusePolicy, UserPortPool userPortPool)
		{
			commonArguments = new ChannelCommonArguments(activityId, TransportErrorCode.ChannelOpenTimeout, userPayload: false);
			this.openTimeline = openTimeline;
			this.openTimeoutSeconds = openTimeoutSeconds;
			this.portReusePolicy = portReusePolicy;
			this.userPortPool = userPortPool;
		}
	}
}
