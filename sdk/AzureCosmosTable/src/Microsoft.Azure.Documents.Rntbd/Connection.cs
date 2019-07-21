using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class Connection : IDisposable
	{
		public struct ResponseMetadata
		{
			public byte[] Header;

			public byte[] Metadata;

			public ResponseMetadata(byte[] header, byte[] metadata)
			{
				Header = header;
				Metadata = metadata;
			}
		}

		private const int ResponseLengthByteLimit = int.MaxValue;

		private const SslProtocols TlsProtocols = SslProtocols.Tls12;

		private static readonly Lazy<ConcurrentPrng> rng = new Lazy<ConcurrentPrng>(LazyThreadSafetyMode.ExecutionAndPublication);

		private static readonly Lazy<byte[]> keepAliveConfiguration = new Lazy<byte[]>(GetWindowsKeepAliveConfiguration, LazyThreadSafetyMode.ExecutionAndPublication);

		private static readonly byte[] healthCheckBuffer = new byte[1];

		private static readonly TimeSpan recentReceiveWindow = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan sendHangGracePeriod = TimeSpan.FromSeconds(2.0);

		private static readonly TimeSpan receiveHangGracePeriod = TimeSpan.FromSeconds(10.0);

		private readonly Uri serverUri;

		private readonly string hostNameCertificateOverride;

		private readonly TimeSpan receiveDelayLimit;

		private readonly TimeSpan sendDelayLimit;

		private bool disposed;

		private TcpClient tcpClient;

		private UserPortPool portPool;

		/// <summary>
		/// a connection is defined as idle if (now - lastReceiveTime &gt;= idleConnectionTimeout)
		/// </summary>
		private readonly TimeSpan idleConnectionTimeout;

		/// <summary>
		/// Due to race condition, requests may enter a connection when it's evaluated as idle
		/// The value is idleConnectionTimeout plus a reasonably adequate buffer for pending requests to complete send and receive.
		/// </summary>
		private readonly TimeSpan idleConnectionClosureTimeout;

		private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1);

		private Stream stream;

		private readonly object timestampLock = new object();

		private DateTime lastSendAttemptTime;

		private DateTime lastSendTime;

		private DateTime lastReceiveTime;

		private readonly object nameLock = new object();

		private string name;

		public Uri ServerUri => serverUri;

		public bool Healthy
		{
			get
			{
				ThrowIfDisposed();
				if (tcpClient == null)
				{
					return false;
				}
				SnapshotConnectionTimestamps(out DateTime lastSendAttempt, out DateTime lastSend, out DateTime lastReceive);
				DateTime utcNow = DateTime.UtcNow;
				if (utcNow - lastReceive < recentReceiveWindow)
				{
					return true;
				}
				if (lastSendAttempt - lastSend > sendDelayLimit && utcNow - lastSendAttempt > sendHangGracePeriod)
				{
					DefaultTrace.TraceWarning("Unhealthy RNTBD connection: Hung send: {0}. Last send attempt: {1:o}. Last send: {2:o}. Tolerance {3:c}", this, lastSendAttempt, lastSend, sendDelayLimit);
					return false;
				}
				if (lastSend - lastReceive > receiveDelayLimit && utcNow - lastSend > receiveHangGracePeriod)
				{
					DefaultTrace.TraceWarning("Unhealthy RNTBD connection: Replies not getting back: {0}. Last send: {1:o}. Last receive: {2:o}. Tolerance: {3:c}", this, lastSend, lastReceive, receiveDelayLimit);
					return false;
				}
				if (idleConnectionTimeout > TimeSpan.Zero && utcNow - lastReceive > idleConnectionTimeout)
				{
					return false;
				}
				try
				{
					Socket client = tcpClient.Client;
					if (client == null || !client.Connected)
					{
						return false;
					}
					client.Send(healthCheckBuffer, 0, SocketFlags.None);
					return true;
				}
				catch (SocketException ex)
				{
					bool flag = ex.SocketErrorCode == SocketError.WouldBlock;
					if (!flag)
					{
						DefaultTrace.TraceWarning("Unhealthy RNTBD connection. Socket error code: {0}", ex.SocketErrorCode.ToString());
					}
					return flag;
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		public bool Disposed => disposed;

		internal TimeSpan TestIdleConnectionClosureTimeout => idleConnectionClosureTimeout;

		public Connection(Uri serverUri, string hostNameCertificateOverride, TimeSpan receiveHangDetectionTime, TimeSpan sendHangDetectionTime, TimeSpan idleTimeout)
		{
			this.serverUri = serverUri;
			this.hostNameCertificateOverride = hostNameCertificateOverride;
			if (receiveHangDetectionTime <= receiveHangGracePeriod)
			{
				throw new ArgumentOutOfRangeException("receiveHangDetectionTime", receiveHangDetectionTime, string.Format(CultureInfo.InvariantCulture, "{0} must be greater than {1} ({2})", "receiveHangDetectionTime", "receiveHangGracePeriod", receiveHangGracePeriod));
			}
			receiveDelayLimit = receiveHangDetectionTime;
			if (sendHangDetectionTime <= sendHangGracePeriod)
			{
				throw new ArgumentOutOfRangeException("sendHangDetectionTime", sendHangDetectionTime, string.Format(CultureInfo.InvariantCulture, "{0} must be greater than {1} ({2})", "sendHangDetectionTime", "sendHangGracePeriod", sendHangGracePeriod));
			}
			sendDelayLimit = sendHangDetectionTime;
			lastSendAttemptTime = DateTime.MinValue;
			lastSendTime = DateTime.MinValue;
			lastReceiveTime = DateTime.MinValue;
			if (idleTimeout > TimeSpan.Zero)
			{
				idleConnectionTimeout = idleTimeout;
				idleConnectionClosureTimeout = idleConnectionTimeout + TimeSpan.FromTicks(2 * (sendHangDetectionTime.Ticks + receiveHangDetectionTime.Ticks));
			}
			name = string.Format(CultureInfo.InvariantCulture, "<not connected> -> {0}", this.serverUri);
		}

		public async Task OpenAsync(ChannelOpenArguments args)
		{
			ThrowIfDisposed();
			await OpenSocketAsync(args);
			await NegotiateSslAsync(args);
		}

		public async Task WriteRequestAsync(ChannelCommonArguments args, byte[] messagePayload)
		{
			ThrowIfDisposed();
			args.SetTimeoutCode(TransportErrorCode.SendLockTimeout);
			await writeSemaphore.WaitAsync();
			try
			{
				args.SetTimeoutCode(TransportErrorCode.SendTimeout);
				args.SetPayloadSent();
				UpdateLastSendAttemptTime();
				await stream.WriteAsync(messagePayload, 0, messagePayload.Length);
			}
			finally
			{
				writeSemaphore.Release();
			}
			UpdateLastSendTime();
		}

		[SuppressMessage("", "AvoidMultiLineComments", Justification = "Multi line business logic")]
		public async Task<ResponseMetadata> ReadResponseMetadataAsync(ChannelCommonArguments args)
		{
			ThrowIfDisposed();
			Trace.CorrelationManager.ActivityId = args.ActivityId;
			byte[] header = await ReadPayloadAsync(24, "header", args);
			uint num = BitConverter.ToUInt32(header, 0);
			if (num > int.MaxValue)
			{
				DefaultTrace.TraceCritical("RNTBD header length says {0} but expected at most {1} bytes. Connection: {2}", num, int.MaxValue, this);
				throw TransportExceptions.GetInternalServerErrorException(serverUri, string.Format(CultureInfo.CurrentUICulture, RMResources.ServerResponseHeaderTooLargeError, num, this));
			}
			if (num < header.Length)
			{
				DefaultTrace.TraceCritical("Invalid RNTBD header length {0} bytes. Expected at least {1} bytes. Connection: {2}", num, header.Length, this);
				throw TransportExceptions.GetInternalServerErrorException(serverUri, string.Format(CultureInfo.CurrentUICulture, RMResources.ServerResponseInvalidHeaderLengthError, header.Length, num, this));
			}
			int length = (int)num - header.Length;
			return new ResponseMetadata(header, await ReadPayloadAsync(length, "metadata", args));
		}

		public async Task<byte[]> ReadResponseBodyAsync(ChannelCommonArguments args)
		{
			ThrowIfDisposed();
			Trace.CorrelationManager.ActivityId = args.ActivityId;
			uint num = BitConverter.ToUInt32(await ReadPayloadAsync(4, "body length header", args), 0);
			if (num > int.MaxValue)
			{
				DefaultTrace.TraceCritical("Invalid RNTBD response body length {0} bytes. Connection: {1}", num, this);
				throw TransportExceptions.GetInternalServerErrorException(serverUri, string.Format(CultureInfo.CurrentUICulture, RMResources.ServerResponseBodyTooLargeError, num, this));
			}
			return await ReadPayloadAsync((int)num, "body", args);
		}

		public override string ToString()
		{
			lock (nameLock)
			{
				return name;
			}
		}

		public void Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			string connectionTimestampsText = GetConnectionTimestampsText();
			if (tcpClient != null)
			{
				DefaultTrace.TraceInformation("Disposing RNTBD connection {0} -> {1} to server {2}. {3}", tcpClient.Client.LocalEndPoint, tcpClient.Client.RemoteEndPoint, serverUri, connectionTimestampsText);
				string text = string.Format(CultureInfo.InvariantCulture, "<disconnected> {0} -> {1}", tcpClient.Client.LocalEndPoint, tcpClient.Client.RemoteEndPoint);
				lock (nameLock)
				{
					name = text;
				}
			}
			else
			{
				DefaultTrace.TraceInformation("Disposing unused RNTBD connection to server {0}. {1}", serverUri, connectionTimestampsText);
			}
			if (tcpClient != null)
			{
				if (portPool != null)
				{
					IPEndPoint iPEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
					portPool.RemoveReference(iPEndPoint.AddressFamily, checked((ushort)iPEndPoint.Port));
				}
				CustomTypeExtensions.Close(tcpClient);
				tcpClient = null;
				CustomTypeExtensions.Close(stream);
				TransportClient.GetTransportPerformanceCounters().IncrementRntbdConnectionClosedCount();
			}
		}

		public bool IsActive(out TimeSpan timeToIdle)
		{
			ThrowIfDisposed();
			SnapshotConnectionTimestamps(out DateTime _, out DateTime _, out DateTime lastReceive);
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow - lastReceive > idleConnectionTimeout)
			{
				timeToIdle = idleConnectionClosureTimeout;
				return false;
			}
			timeToIdle = lastReceive + idleConnectionClosureTimeout - utcNow;
			return true;
		}

		internal void TestSetLastReceiveTime(DateTime lrt)
		{
			lock (timestampLock)
			{
				lastReceiveTime = lrt;
			}
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException(string.Format("{0}:{1}", "Connection", serverUri));
			}
		}

		private async Task OpenSocketAsync(ChannelOpenArguments args)
		{
			if (this.tcpClient != null)
			{
				throw new InvalidOperationException("Attempting to call Connection.OpenSocketAsync on an " + $"already initialized connection {this}");
			}
			TcpClient tcpClient = null;
			TransportErrorCode errorCode = TransportErrorCode.Unknown;
			try
			{
				errorCode = TransportErrorCode.DnsResolutionFailed;
				args.CommonArguments.SetTimeoutCode(TransportErrorCode.DnsResolutionTimeout);
				IPAddress iPAddress = await ResolveHostAsync(serverUri.DnsSafeHost);
				errorCode = TransportErrorCode.ConnectFailed;
				args.CommonArguments.SetTimeoutCode(TransportErrorCode.ConnectTimeout);
				UpdateLastSendAttemptTime();
				switch (args.PortReusePolicy)
				{
				case TcpPortReuse.ReuseUnicastPort:
					tcpClient = await ConnectUnicastPortAsync(serverUri, iPAddress);
					break;
				case TcpPortReuse.PrivatePortPool:
				{
					Tuple<TcpClient, bool> tuple = await ConnectUserPortAsync(serverUri, iPAddress, args.PortPool);
					tcpClient = tuple.Item1;
					if (tuple.Item2)
					{
						portPool = args.PortPool;
					}
					break;
				}
				default:
					throw new ArgumentException($"Unsupported port reuse policy {args.PortReusePolicy.ToString()}");
				}
				UpdateLastSendTime();
				UpdateLastReceiveTime();
				args.OpenTimeline.RecordConnectFinishTime();
				DefaultTrace.TraceInformation("RNTBD connection established {0} -> {1}", tcpClient.Client.LocalEndPoint, tcpClient.Client.RemoteEndPoint);
				TransportClient.GetTransportPerformanceCounters().IncrementRntbdConnectionEstablishedCount();
				string text = string.Format(CultureInfo.InvariantCulture, "{0} -> {1}", tcpClient.Client.LocalEndPoint, tcpClient.Client.RemoteEndPoint);
				lock (nameLock)
				{
					name = text;
				}
			}
			catch (Exception ex)
			{
				TcpClient obj = tcpClient;
				if (obj != null)
				{
					CustomTypeExtensions.Close(obj);
				}
				DefaultTrace.TraceInformation("Connection.OpenSocketAsync failed. Converting to TransportException. Connection: {0}. Inner exception: {1}", this, ex);
				throw new TransportException(errorCode, ex, args.CommonArguments.ActivityId, serverUri, ToString(), args.CommonArguments.UserPayload, args.CommonArguments.PayloadSent);
			}
			this.tcpClient = tcpClient;
			stream = tcpClient.GetStream();
			this.tcpClient.Client.Blocking = false;
		}

		private async Task NegotiateSslAsync(ChannelOpenArguments args)
		{
			string targetHost = hostNameCertificateOverride ?? serverUri.DnsSafeHost;
			SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
			try
			{
				args.CommonArguments.SetTimeoutCode(TransportErrorCode.SslNegotiationTimeout);
				UpdateLastSendAttemptTime();
				await sslStream.AuthenticateAsClientAsync(targetHost, null, SslProtocols.Tls12, checkCertificateRevocation: false);
				UpdateLastSendTime();
				UpdateLastReceiveTime();
				args.OpenTimeline.RecordSslHandshakeFinishTime();
				stream = sslStream;
				DefaultTrace.TraceInformation("RNTBD SSL handshake complete {0} -> {1}", tcpClient.Client.LocalEndPoint, tcpClient.Client.RemoteEndPoint);
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceInformation("Connection.NegotiateSslAsync failed. Converting to TransportException. Connection: {0}. Inner exception: {1}", this, ex);
				throw new TransportException(TransportErrorCode.SslNegotiationFailed, ex, args.CommonArguments.ActivityId, serverUri, ToString(), args.CommonArguments.UserPayload, args.CommonArguments.PayloadSent);
			}
		}

		private async Task<byte[]> ReadPayloadAsync(int length, string type, ChannelCommonArguments args)
		{
			byte[] payload = new byte[length];
			int num;
			for (int bytesRead = 0; bytesRead < length; bytesRead += num)
			{
				try
				{
					num = await stream.ReadAsync(payload, bytesRead, length - bytesRead);
				}
				catch (IOException innerException)
				{
					DefaultTrace.TraceError("Hit IOException while reading {0} on connection {1}. {2}", type, this, GetConnectionTimestampsText());
					throw new TransportException(TransportErrorCode.ReceiveFailed, innerException, args.ActivityId, serverUri, ToString(), args.UserPayload, payloadSent: true);
				}
				if (num == 0)
				{
					DefaultTrace.TraceError("Reached end of stream. Read 0 bytes while reading {0} on connection {1}. {2}", type, this, GetConnectionTimestampsText());
					throw new TransportException(TransportErrorCode.ReceiveStreamClosed, null, args.ActivityId, serverUri, ToString(), args.UserPayload, payloadSent: true);
				}
				UpdateLastReceiveTime();
			}
			return payload;
		}

		private void SnapshotConnectionTimestamps(out DateTime lastSendAttempt, out DateTime lastSend, out DateTime lastReceive)
		{
			lock (timestampLock)
			{
				lastSendAttempt = lastSendAttemptTime;
				lastSend = lastSendTime;
				lastReceive = lastReceiveTime;
			}
		}

		private string GetConnectionTimestampsText()
		{
			SnapshotConnectionTimestamps(out DateTime lastSendAttempt, out DateTime lastSend, out DateTime lastReceive);
			return string.Format(CultureInfo.InvariantCulture, "Last send attempt time: {0:o}. Last send time: {1:o}. Last receive time: {2:o}", lastSendAttempt, lastSend, lastReceive);
		}

		private void UpdateLastSendAttemptTime()
		{
			lock (timestampLock)
			{
				lastSendAttemptTime = DateTime.UtcNow;
			}
		}

		private void UpdateLastSendTime()
		{
			lock (timestampLock)
			{
				lastSendTime = DateTime.UtcNow;
			}
		}

		private void UpdateLastReceiveTime()
		{
			lock (timestampLock)
			{
				lastReceiveTime = DateTime.UtcNow;
			}
		}

		private static async Task<TcpClient> ConnectUnicastPortAsync(Uri serverUri, IPAddress resolvedAddress)
		{
			TcpClient tcpClient = new TcpClient(resolvedAddress.AddressFamily);
			SetCommonSocketOptions(tcpClient.Client);
			SetReuseUnicastPort(tcpClient.Client);
			DefaultTrace.TraceInformation("RNTBD: {0} connecting to {1} (address {2})", "ConnectUnicastPortAsync", serverUri, resolvedAddress);
			await tcpClient.ConnectAsync(resolvedAddress, serverUri.Port);
			return tcpClient;
		}

		private static async Task<Tuple<TcpClient, bool>> ConnectReuseAddrAsync(Uri serverUri, IPAddress address, ushort candidatePort)
		{
			TcpClient candidateClient = new TcpClient(address.AddressFamily);
			TcpClient item = null;
			try
			{
				SetCommonSocketOptions(candidateClient.Client);
				candidateClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
				EndPoint endPoint;
				switch (address.AddressFamily)
				{
				case AddressFamily.InterNetwork:
					endPoint = new IPEndPoint(IPAddress.Any, candidatePort);
					break;
				case AddressFamily.InterNetworkV6:
					endPoint = new IPEndPoint(IPAddress.IPv6Any, candidatePort);
					break;
				default:
					throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Address family {0} not supported", address.AddressFamily));
				}
				DefaultTrace.TraceInformation("RNTBD: {0} binding local endpoint {1}", "ConnectReuseAddrAsync", endPoint);
				try
				{
					candidateClient.Client.Bind(endPoint);
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.AccessDenied)
					{
						return Tuple.Create<TcpClient, bool>(null, item2: false);
					}
					throw;
				}
				DefaultTrace.TraceInformation("RNTBD: {0} connecting to {1} (address {2})", "ConnectReuseAddrAsync", serverUri, address);
				try
				{
					await candidateClient.ConnectAsync(address, serverUri.Port);
				}
				catch (SocketException ex2)
				{
					if (ex2.SocketErrorCode == SocketError.AddressAlreadyInUse)
					{
						return Tuple.Create<TcpClient, bool>(null, item2: true);
					}
					throw;
				}
				item = candidateClient;
				candidateClient = null;
			}
			finally
			{
				if (candidateClient != null)
				{
					CustomTypeExtensions.Close(candidateClient);
				}
			}
			return Tuple.Create(item, item2: true);
		}

		private static async Task<Tuple<TcpClient, bool>> ConnectUserPortAsync(Uri serverUri, IPAddress address, UserPortPool portPool)
		{
			ushort[] candidatePorts = portPool.GetCandidatePorts(address.AddressFamily);
			checked
			{
				if (candidatePorts != null)
				{
					ushort[] array = candidatePorts;
					foreach (ushort candidatePort in array)
					{
						Tuple<TcpClient, bool> obj = await ConnectReuseAddrAsync(serverUri, address, candidatePort);
						TcpClient item = obj.Item1;
						bool item2 = obj.Item2;
						if (item != null)
						{
							ushort port = (ushort)((IPEndPoint)item.Client.LocalEndPoint).Port;
							portPool.AddReference(address.AddressFamily, port);
							return Tuple.Create(item, item2: true);
						}
						if (!item2)
						{
							portPool.MarkUnusable(address.AddressFamily, candidatePort);
						}
					}
				}
				TcpClient item3 = (await ConnectReuseAddrAsync(serverUri, address, 0)).Item1;
				if (item3 != null)
				{
					portPool.AddReference(address.AddressFamily, (ushort)((IPEndPoint)item3.Client.LocalEndPoint).Port);
					return Tuple.Create(item3, item2: true);
				}
				return Tuple.Create(await ConnectUnicastPortAsync(serverUri, address), item2: false);
			}
		}

		private static async Task<IPAddress> ResolveHostAsync(string hostName)
		{
			IPAddress[] array = await Dns.GetHostAddressesAsync(hostName);
			int num = 0;
			if (array.Length > 1)
			{
				num = rng.Value.Next(array.Length);
			}
			return array[num];
		}

		private static void SetCommonSocketOptions(Socket clientSocket)
		{
			clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, optionValue: true);
			EnableTcpKeepAlive(clientSocket);
		}

		private static void EnableTcpKeepAlive(Socket clientSocket)
		{
			clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, optionValue: true);
		}

		private static byte[] GetWindowsKeepAliveConfiguration()
		{
			byte[] array = new byte[12];
			BitConverter.GetBytes(1u).CopyTo(array, 0);
			BitConverter.GetBytes(30000u).CopyTo(array, 4);
			BitConverter.GetBytes(1000u).CopyTo(array, 8);
			return array;
		}

		private static void SetReuseUnicastPort(Socket clientSocket)
		{
		}
	}
}
