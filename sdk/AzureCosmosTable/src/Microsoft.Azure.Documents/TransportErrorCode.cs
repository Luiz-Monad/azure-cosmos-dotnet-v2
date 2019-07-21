namespace Microsoft.Azure.Documents
{
	internal enum TransportErrorCode
	{
		Unknown,
		ChannelOpenFailed,
		ChannelOpenTimeout,
		DnsResolutionFailed,
		DnsResolutionTimeout,
		ConnectFailed,
		ConnectTimeout,
		SslNegotiationFailed,
		SslNegotiationTimeout,
		TransportNegotiationTimeout,
		RequestTimeout,
		ChannelMultiplexerClosed,
		SendFailed,
		SendLockTimeout,
		SendTimeout,
		ReceiveFailed,
		ReceiveTimeout,
		ReceiveStreamClosed,
		ConnectionBroken
	}
}
