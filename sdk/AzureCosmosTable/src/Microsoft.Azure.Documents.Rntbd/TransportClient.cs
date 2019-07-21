using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class TransportClient : Microsoft.Azure.Documents.TransportClient, IDisposable
	{
		private enum TransportResponseStatusCode
		{
			Success = 0,
			DocumentClientException = -1,
			UnknownException = -2
		}

		public sealed class Options
		{
			private UserAgentContainer userAgent;

			private TimeSpan openTimeout = TimeSpan.Zero;

			private TimeSpan timerPoolResolution = TimeSpan.Zero;

			public TimeSpan RequestTimeout
			{
				get;
				private set;
			}

			public int MaxChannels
			{
				get;
				set;
			}

			public int PartitionCount
			{
				get;
				set;
			}

			public int MaxRequestsPerChannel
			{
				get;
				set;
			}

			public TimeSpan ReceiveHangDetectionTime
			{
				get;
				set;
			}

			public TimeSpan SendHangDetectionTime
			{
				get;
				set;
			}

			public TimeSpan IdleTimeout
			{
				get;
				set;
			}

			public bool EnableCpuMonitor
			{
				get;
				set;
			}

			public UserAgentContainer UserAgent
			{
				get
				{
					if (userAgent != null)
					{
						return userAgent;
					}
					userAgent = new UserAgentContainer();
					return userAgent;
				}
				set
				{
					userAgent = value;
				}
			}

			public string CertificateHostNameOverride
			{
				get;
				set;
			}

			public TimeSpan OpenTimeout
			{
				get
				{
					if (openTimeout > TimeSpan.Zero)
					{
						return openTimeout;
					}
					return RequestTimeout;
				}
				set
				{
					openTimeout = value;
				}
			}

			public TcpPortReuse PortReusePolicy
			{
				get;
				set;
			}

			public int PortPoolReuseThreshold
			{
				get;
				internal set;
			}

			public int PortPoolBindAttempts
			{
				get;
				internal set;
			}

			public TimeSpan TimerPoolResolution
			{
				get
				{
					return GetTimerPoolResolutionSeconds(timerPoolResolution, RequestTimeout, openTimeout);
				}
				set
				{
					timerPoolResolution = value;
				}
			}

			public Options(TimeSpan requestTimeout)
			{
				RequestTimeout = requestTimeout;
				MaxChannels = 65535;
				PartitionCount = 1;
				MaxRequestsPerChannel = 30;
				PortReusePolicy = TcpPortReuse.ReuseUnicastPort;
				PortPoolReuseThreshold = 256;
				PortPoolBindAttempts = 5;
				ReceiveHangDetectionTime = TimeSpan.FromSeconds(65.0);
				SendHangDetectionTime = TimeSpan.FromSeconds(10.0);
				IdleTimeout = TimeSpan.FromSeconds(1800.0);
				EnableCpuMonitor = true;
			}

			private static TimeSpan GetTimerPoolResolutionSeconds(TimeSpan timerPoolResolution, TimeSpan requestTimeout, TimeSpan openTimeout)
			{
				if (timerPoolResolution > TimeSpan.Zero && timerPoolResolution < openTimeout && timerPoolResolution < requestTimeout)
				{
					return timerPoolResolution;
				}
				if (openTimeout > TimeSpan.Zero && requestTimeout > TimeSpan.Zero)
				{
					if (!(openTimeout < requestTimeout))
					{
						return requestTimeout;
					}
					return openTimeout;
				}
				if (!(openTimeout > TimeSpan.Zero))
				{
					return requestTimeout;
				}
				return openTimeout;
			}
		}

		private readonly ChannelDictionary channelDictionary;

		private readonly CpuMonitor cpuMonitor;

		private bool disposed;

		private static TransportPerformanceCounters transportPerformanceCounters = new TransportPerformanceCounters();

		private readonly object disableRntbdChannelLock = new object();

		private bool disableRntbdChannel;

		public event Action OnDisableRntbdChannel;

		public TransportClient(Options clientOptions)
		{
			if (clientOptions == null)
			{
				throw new ArgumentNullException("clientOptions");
			}
			UserPortPool userPortPool = null;
			if (clientOptions.PortReusePolicy == TcpPortReuse.PrivatePortPool)
			{
				userPortPool = new UserPortPool(clientOptions.PortPoolReuseThreshold, clientOptions.PortPoolBindAttempts);
			}
			channelDictionary = new ChannelDictionary(new ChannelProperties(clientOptions.UserAgent, clientOptions.CertificateHostNameOverride, new TimerPool((int)clientOptions.TimerPoolResolution.TotalSeconds), clientOptions.RequestTimeout, clientOptions.OpenTimeout, clientOptions.PortReusePolicy, userPortPool, clientOptions.MaxChannels, clientOptions.PartitionCount, clientOptions.MaxRequestsPerChannel, clientOptions.ReceiveHangDetectionTime, clientOptions.SendHangDetectionTime, clientOptions.IdleTimeout, (clientOptions.IdleTimeout > TimeSpan.Zero) ? new TimerPool(30) : null));
			if (clientOptions.EnableCpuMonitor)
			{
				cpuMonitor = new CpuMonitor();
				cpuMonitor.Start();
			}
		}

		internal override async Task<StoreResponse> InvokeStoreAsync(Uri physicalAddress, ResourceOperation resourceOperation, DocumentServiceRequest request)
		{
			ThrowIfDisposed();
			Guid activityId = Trace.CorrelationManager.ActivityId;
			if (!request.IsBodySeekableClonableAndCountable)
			{
				throw new InternalServerErrorException();
			}
			StoreResponse storeResponse = null;
			string operation = "Unknown operation";
			DateTime requestStartTime = DateTime.UtcNow;
			int transportResponseStatusCode = 0;
			try
			{
				IncrementCounters();
				operation = "GetChannel";
				IChannel channel = channelDictionary.GetChannel(physicalAddress);
				operation = "RequestAsync";
				GetTransportPerformanceCounters().IncrementRntbdRequestCount(resourceOperation.resourceType, resourceOperation.operationType);
				storeResponse = await channel.RequestAsync(request, physicalAddress, resourceOperation, activityId);
			}
			catch (TransportException ex)
			{
				if (cpuMonitor != null)
				{
					ex.SetCpuLoad(cpuMonitor.GetCpuLoad());
				}
				transportResponseStatusCode = (int)ex.ErrorCode;
				ex.RequestStartTime = requestStartTime;
				ex.RequestEndTime = DateTime.UtcNow;
				ex.OperationType = resourceOperation.operationType;
				ex.ResourceType = resourceOperation.resourceType;
				GetTransportPerformanceCounters().IncrementRntbdResponseCount(resourceOperation.resourceType, resourceOperation.operationType, (int)ex.ErrorCode);
				DefaultTrace.TraceInformation("{0} failed: RID: {1}, Resource Type: {2}, Op: {3}, Address: {4}, Exception: {5}", operation, request.ResourceAddress, request.ResourceType, resourceOperation, physicalAddress, ex);
				if (request.IsReadOnlyRequest)
				{
					DefaultTrace.TraceInformation("Converting to Gone (read-only request)");
					throw TransportExceptions.GetGoneException(physicalAddress, activityId, ex);
				}
				if (!ex.UserRequestSent)
				{
					DefaultTrace.TraceInformation("Converting to Gone (write request, not sent)");
					throw TransportExceptions.GetGoneException(physicalAddress, activityId, ex);
				}
				if (TransportException.IsTimeout(ex.ErrorCode))
				{
					DefaultTrace.TraceInformation("Converting to RequestTimeout");
					throw TransportExceptions.GetRequestTimeoutException(physicalAddress, activityId, ex);
				}
				DefaultTrace.TraceInformation("Converting to ServiceUnavailable");
				throw TransportExceptions.GetServiceUnavailableException(physicalAddress, activityId, ex);
			}
			catch (DocumentClientException ex2)
			{
				transportResponseStatusCode = -1;
				DefaultTrace.TraceInformation("{0} failed: RID: {1}, Resource Type: {2}, Op: {3}, Address: {4}, Exception: {5}", operation, request.ResourceAddress, request.ResourceType, resourceOperation, physicalAddress, ex2);
				throw;
			}
			catch (Exception ex3)
			{
				transportResponseStatusCode = -2;
				DefaultTrace.TraceInformation("{0} failed: RID: {1}, Resource Type: {2}, Op: {3}, Address: {4}, Exception: {5}", operation, request.ResourceAddress, request.ResourceType, resourceOperation, physicalAddress, ex3);
				throw;
			}
			finally
			{
				DecrementCounters();
				GetTransportPerformanceCounters().IncrementRntbdResponseCount(resourceOperation.resourceType, resourceOperation.operationType, transportResponseStatusCode);
				RaiseProtocolDowngradeRequest(storeResponse);
			}
			Microsoft.Azure.Documents.TransportClient.ThrowServerException(request.ResourceAddress, storeResponse, physicalAddress, activityId, request);
			return storeResponse;
		}

		public override void Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			channelDictionary.Dispose();
			if (cpuMonitor != null)
			{
				cpuMonitor.Stop();
				cpuMonitor.Dispose();
			}
			base.Dispose();
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("TransportClient");
			}
		}

		private static void IncrementCounters()
		{
		}

		private static void DecrementCounters()
		{
		}

		private void RaiseProtocolDowngradeRequest(StoreResponse storeResponse)
		{
			if (storeResponse == null)
			{
				return;
			}
			string value = null;
			if (storeResponse.TryGetHeaderValue("x-ms-disable-rntbd-channel", out value) && string.Equals(value, "true"))
			{
				bool flag = false;
				lock (disableRntbdChannelLock)
				{
					if (disableRntbdChannel)
					{
						return;
					}
					disableRntbdChannel = true;
					flag = true;
				}
				if (flag)
				{
					Task.Factory.StartNewOnCurrentTaskSchedulerAsync(delegate
					{
						this.OnDisableRntbdChannel?.Invoke();
					}).ContinueWith(delegate(Task failedTask)
					{
						DefaultTrace.TraceError("RNTBD channel callback failed: {0}", failedTask.Exception);
					}, default(CancellationToken), TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
				}
			}
		}

		internal static void SetTransportPerformanceCounters(TransportPerformanceCounters transportPerformanceCounters)
		{
			if (transportPerformanceCounters == null)
			{
				throw new ArgumentNullException("transportPerformanceCounters");
			}
			TransportClient.transportPerformanceCounters = transportPerformanceCounters;
		}

		internal static TransportPerformanceCounters GetTransportPerformanceCounters()
		{
			return transportPerformanceCounters;
		}
	}
}
