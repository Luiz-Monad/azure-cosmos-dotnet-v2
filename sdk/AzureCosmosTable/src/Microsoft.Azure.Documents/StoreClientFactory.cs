using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Rntbd;
using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class StoreClientFactory : IStoreClientFactory, IDisposable
	{
		private bool isDisposed;

		private readonly Protocol protocol;

		private TransportClient transportClient;

		private TransportClient fallbackClient;

		public StoreClientFactory(Protocol protocol, int requestTimeoutInSeconds, int maxConcurrentConnectionOpenRequests, UserAgentContainer userAgent = null, ICommunicationEventSource eventSource = null, string overrideHostNameInCertificate = null, int openTimeoutInSeconds = 0, int idleTimeoutInSeconds = -1, int timerPoolGranularityInSeconds = 0, int maxRntbdChannels = 65535, int rntbdPartitionCount = 1, int maxRequestsPerRntbdChannel = 30, TcpPortReuse rntbdPortReusePolicy = TcpPortReuse.ReuseUnicastPort, int rntbdPortPoolReuseThreshold = 256, int rntbdPortPoolBindAttempts = 5, int receiveHangDetectionTimeSeconds = 65, int sendHangDetectionTimeSeconds = 10, bool enableCpuMonitor = true)
		{
			if (idleTimeoutInSeconds > 0 && idleTimeoutInSeconds < 600)
			{
				throw new ArgumentOutOfRangeException("idleTimeoutInSeconds");
			}
			switch (protocol)
			{
			case Protocol.Https:
				if (eventSource == null)
				{
					throw new ArgumentOutOfRangeException("eventSource");
				}
				transportClient = new HttpTransportClient(requestTimeoutInSeconds, eventSource, userAgent, idleTimeoutInSeconds);
				break;
			case Protocol.Tcp:
				if (maxRntbdChannels <= 0)
				{
					throw new ArgumentOutOfRangeException("maxRntbdChannels");
				}
				if (rntbdPartitionCount < 1 || rntbdPartitionCount > 8)
				{
					throw new ArgumentOutOfRangeException("rntbdPartitionCount");
				}
				if (maxRequestsPerRntbdChannel <= 0)
				{
					throw new ArgumentOutOfRangeException("maxRequestsPerRntbdChannel");
				}
				if (maxRntbdChannels > 65535)
				{
					DefaultTrace.TraceWarning("The value of {0} is unreasonably large. Received: {1}. Use {2} to represent \"effectively infinite\".", "maxRntbdChannels", maxRntbdChannels, ushort.MaxValue);
				}
				if (maxRequestsPerRntbdChannel < 6)
				{
					DefaultTrace.TraceWarning("The value of {0} is unreasonably small. Received: {1}. Small values of {0} can cause a large number of RNTBD channels to be opened to the same back-end. Reasonable values are between {2} and {3}", "maxRequestsPerRntbdChannel", maxRequestsPerRntbdChannel, 6, 256);
				}
				if (maxRequestsPerRntbdChannel > 256)
				{
					DefaultTrace.TraceWarning("The value of {0} is unreasonably large. Received: {1}. Large values of {0} can cause significant head-of-line blocking over RNTBD channels. Reasonable values are between {2} and {3}", "maxRequestsPerRntbdChannel", maxRequestsPerRntbdChannel, 6, 256);
				}
				if (checked(maxRntbdChannels * maxRequestsPerRntbdChannel) < 512)
				{
					DefaultTrace.TraceWarning("The number of simultaneous requests allowed per backend is unreasonably small. Received {0} = {1}, {2} = {3}. Reasonable values are at least {4}", "maxRntbdChannels", maxRntbdChannels, "maxRequestsPerRntbdChannel", maxRequestsPerRntbdChannel, 512);
				}
				ValidatePortPoolReuseThreshold(ref rntbdPortPoolReuseThreshold);
				ValidatePortPoolBindAttempts(ref rntbdPortPoolBindAttempts);
				if (rntbdPortPoolBindAttempts > rntbdPortPoolReuseThreshold)
				{
					DefaultTrace.TraceWarning("Raising the value of {0} from {1} to {2} to match the value of {3}", "rntbdPortPoolReuseThreshold", rntbdPortPoolReuseThreshold, rntbdPortPoolBindAttempts + 1, "rntbdPortPoolBindAttempts");
					rntbdPortPoolReuseThreshold = rntbdPortPoolBindAttempts;
				}
				if (receiveHangDetectionTimeSeconds < 65)
				{
					DefaultTrace.TraceWarning("The value of {0} is too small. Received {1}. Adjusting to {2}", "receiveHangDetectionTimeSeconds", receiveHangDetectionTimeSeconds, 65);
					receiveHangDetectionTimeSeconds = 65;
				}
				if (receiveHangDetectionTimeSeconds > 180)
				{
					DefaultTrace.TraceWarning("The value of {0} is too large. Received {1}. Adjusting to {2}", "receiveHangDetectionTimeSeconds", receiveHangDetectionTimeSeconds, 180);
					receiveHangDetectionTimeSeconds = 180;
				}
				if (sendHangDetectionTimeSeconds < 2)
				{
					DefaultTrace.TraceWarning("The value of {0} is too small. Received {1}. Adjusting to {2}", "sendHangDetectionTimeSeconds", sendHangDetectionTimeSeconds, 2);
					sendHangDetectionTimeSeconds = 2;
				}
				if (sendHangDetectionTimeSeconds > 60)
				{
					DefaultTrace.TraceWarning("The value of {0} is too large. Received {1}. Adjusting to {2}", "sendHangDetectionTimeSeconds", sendHangDetectionTimeSeconds, 60);
					sendHangDetectionTimeSeconds = 60;
				}
				fallbackClient = new RntbdTransportClient(requestTimeoutInSeconds, maxConcurrentConnectionOpenRequests, userAgent, overrideHostNameInCertificate, openTimeoutInSeconds, idleTimeoutInSeconds, timerPoolGranularityInSeconds);
				transportClient = new Microsoft.Azure.Documents.Rntbd.TransportClient(new Microsoft.Azure.Documents.Rntbd.TransportClient.Options(TimeSpan.FromSeconds(requestTimeoutInSeconds))
				{
					MaxChannels = maxRntbdChannels,
					PartitionCount = rntbdPartitionCount,
					MaxRequestsPerChannel = maxRequestsPerRntbdChannel,
					PortReusePolicy = rntbdPortReusePolicy,
					PortPoolReuseThreshold = rntbdPortPoolReuseThreshold,
					PortPoolBindAttempts = rntbdPortPoolBindAttempts,
					ReceiveHangDetectionTime = TimeSpan.FromSeconds(receiveHangDetectionTimeSeconds),
					SendHangDetectionTime = TimeSpan.FromSeconds(sendHangDetectionTimeSeconds),
					UserAgent = userAgent,
					CertificateHostNameOverride = overrideHostNameInCertificate,
					OpenTimeout = TimeSpan.FromSeconds(openTimeoutInSeconds),
					TimerPoolResolution = TimeSpan.FromSeconds(timerPoolGranularityInSeconds),
					IdleTimeout = TimeSpan.FromSeconds(idleTimeoutInSeconds),
					EnableCpuMonitor = enableCpuMonitor
				});
				break;
			default:
				throw new ArgumentOutOfRangeException("protocol", protocol, "Invalid protocol value");
			}
			this.protocol = protocol;
		}

		internal void WithTransportInterceptor(Func<TransportClient, TransportClient> transportClientHandlerFactory)
		{
			if (transportClientHandlerFactory == null)
			{
				throw new ArgumentNullException("transportClientHandlerFactory");
			}
			transportClient = transportClientHandlerFactory(transportClient);
		}

		public StoreClient CreateStoreClient(IAddressResolver addressResolver, ISessionContainer sessionContainer, IServiceConfigurationReader serviceConfigurationReader, IAuthorizationTokenProvider authorizationTokenProvider, bool enableRequestDiagnostics = false, bool enableReadRequestsFallback = false, bool useFallbackClient = true, bool useMultipleWriteLocations = false, bool detectClientConnectivityIssues = false)
		{
			ThrowIfDisposed();
			if (useFallbackClient && fallbackClient != null)
			{
				return new StoreClient(addressResolver, sessionContainer, serviceConfigurationReader, authorizationTokenProvider, protocol, fallbackClient, enableRequestDiagnostics, enableReadRequestsFallback, useMultipleWriteLocations, detectClientConnectivityIssues);
			}
			return new StoreClient(addressResolver, sessionContainer, serviceConfigurationReader, authorizationTokenProvider, protocol, transportClient, enableRequestDiagnostics, enableReadRequestsFallback, useMultipleWriteLocations, detectClientConnectivityIssues);
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				if (transportClient != null)
				{
					transportClient.Dispose();
					transportClient = null;
				}
				if (fallbackClient != null)
				{
					fallbackClient.Dispose();
					fallbackClient = null;
				}
				isDisposed = true;
			}
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("StoreClientFactory");
			}
		}

		private static void ValidatePortPoolReuseThreshold(ref int rntbdPortPoolReuseThreshold)
		{
			if (rntbdPortPoolReuseThreshold < 32)
			{
				DefaultTrace.TraceWarning("The value of {0} is too small. Received {1}. Adjusting to {2}", "rntbdPortPoolReuseThreshold", rntbdPortPoolReuseThreshold, 32);
				rntbdPortPoolReuseThreshold = 32;
			}
			else if (rntbdPortPoolReuseThreshold > 2048)
			{
				DefaultTrace.TraceWarning("The value of {0} is too large. Received {1}. Adjusting to {2}", "rntbdPortPoolReuseThreshold", rntbdPortPoolReuseThreshold, 2048);
				rntbdPortPoolReuseThreshold = 2048;
			}
		}

		private static void ValidatePortPoolBindAttempts(ref int rntbdPortPoolBindAttempts)
		{
			if (rntbdPortPoolBindAttempts < 3)
			{
				DefaultTrace.TraceWarning("The value of {0} is too small. Received {1}. Adjusting to {2}", "rntbdPortPoolBindAttempts", rntbdPortPoolBindAttempts, 3);
				rntbdPortPoolBindAttempts = 3;
			}
			else if (rntbdPortPoolBindAttempts > 32)
			{
				DefaultTrace.TraceWarning("The value of {0} is too large. Received {1}. Adjusting to {2}", "rntbdPortPoolBindAttempts", rntbdPortPoolBindAttempts, 32);
				rntbdPortPoolBindAttempts = 32;
			}
		}
	}
}
