using System;

namespace Microsoft.Azure.Documents
{
	internal struct RntbdConnectionOpenTimers
	{
		public DateTimeOffset CreationTimestamp;

		public DateTimeOffset TcpConnectCompleteTimestamp;

		public DateTimeOffset SslHandshakeCompleteTimestamp;

		public DateTimeOffset RntbdHandshakeCompleteTimestamp;
	}
}
