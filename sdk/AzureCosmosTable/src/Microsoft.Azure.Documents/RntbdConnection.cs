using Microsoft.Azure.Documents.Rntbd;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class RntbdConnection : IConnection
	{
		private static readonly TimeSpan MaxIdleConnectionTimeout = TimeSpan.FromHours(1.0);

		private static readonly TimeSpan MinIdleConnectionTimeout = TimeSpan.FromSeconds(100.0);

		private static readonly TimeSpan DefaultIdleConnectionTimeout = TimeSpan.FromSeconds(100.0);

		private static readonly TimeSpan DefaultUnauthenticatedTimeout = TimeSpan.FromSeconds(10.0);

		private const uint MinimumUnauthenticatedTimeoutInSeconds = 1u;

		private const uint UnauthenticatedTimeoutBufferInSeconds = 5u;

		private const SslProtocols EnabledTLSProtocols = SslProtocols.Tls12;

		private static readonly char[] UrlTrim = new char[1]
		{
			'/'
		};

		private static readonly byte[] KeepAliveOn = BitConverter.GetBytes(1u);

		private static readonly byte[] KeepAliveTimeInMilliseconds = BitConverter.GetBytes(30000u);

		private static readonly byte[] KeepAliveIntervalInMilliseconds = BitConverter.GetBytes(1000u);

		internal static string LocalIpv4Address;

		private static bool AddSourceIpAddressInNetworkExceptionMessagePrivate = false;

		private const int MaxContextResponse = 8000;

		private const int MaxResponse = int.MaxValue;

		private readonly Uri initialOpenUri;

		private readonly string poolKey;

		private Uri targetPhysicalAddress;

		private Stream stream;

		private Socket socket;

		private TcpClient tcpClient;

		private double requestTimeoutInSeconds;

		private bool isOpen;

		private string serverAgent;

		private string serverVersion;

		private TimeSpan idleTimeout;

		private TimeSpan unauthenticatedTimeout;

		private string overrideHostNameInCertificate;

		private double openTimeoutInSeconds;

		private DateTime lastUsed;

		private DateTime opened;

		private UserAgentContainer userAgent;

		private bool hasIssuedSuccessfulRequest;

		private RntbdConnectionOpenTimers connectionTimers;

		private readonly TimerPool timerPool;

		public string PoolKey => poolKey;

		public RntbdConnectionOpenTimers ConnectionTimers => connectionTimers;

		public static bool AddSourceIpAddressInNetworkExceptionMessage
		{
			get
			{
				return AddSourceIpAddressInNetworkExceptionMessagePrivate;
			}
			set
			{
				if (value && !AddSourceIpAddressInNetworkExceptionMessagePrivate)
				{
					LocalIpv4Address = (NetUtil.GetNonLoopbackIpV4Address() ?? string.Empty);
				}
				AddSourceIpAddressInNetworkExceptionMessagePrivate = value;
			}
		}

		public RntbdConnection(Uri address, double requestTimeoutInSeconds, string overrideHostNameInCertificate, double openTimeoutInSeconds, double idleConnectionTimeoutInSeconds, string poolKey, UserAgentContainer userAgent, TimerPool pool)
		{
			connectionTimers.CreationTimestamp = DateTimeOffset.Now;
			initialOpenUri = address;
			this.poolKey = poolKey;
			this.requestTimeoutInSeconds = requestTimeoutInSeconds;
			this.overrideHostNameInCertificate = overrideHostNameInCertificate;
			this.openTimeoutInSeconds = openTimeoutInSeconds;
			if (TimeSpan.FromSeconds(idleConnectionTimeoutInSeconds) < MaxIdleConnectionTimeout && TimeSpan.FromSeconds(idleConnectionTimeoutInSeconds) > MinIdleConnectionTimeout)
			{
				idleTimeout = TimeSpan.FromSeconds(idleConnectionTimeoutInSeconds);
			}
			else
			{
				idleTimeout = DefaultIdleConnectionTimeout;
			}
			serverVersion = null;
			opened = DateTime.UtcNow;
			lastUsed = opened;
			this.userAgent = (userAgent ?? new UserAgentContainer());
			timerPool = pool;
		}

		public void Close()
		{
			if (stream != null)
			{
				DefaultTrace.TraceVerbose("Closing connection stream for TargetAddress: {0}, creationTime: {1}, lastUsed: {2}, poolKey: {3}", targetPhysicalAddress, connectionTimers.CreationTimestamp.ToString("o", CultureInfo.InvariantCulture), lastUsed.ToString("o", CultureInfo.InvariantCulture), poolKey);
				CustomTypeExtensions.Close(stream);
				stream = null;
			}
			if (tcpClient != null)
			{
				CustomTypeExtensions.Close(tcpClient);
			}
			if (socket != null)
			{
				socket = null;
			}
			if (isOpen)
			{
				isOpen = false;
			}
		}

		public async Task Open(Guid activityId, Uri fullTargetAddress)
		{
			targetPhysicalAddress = fullTargetAddress;
			DateTimeOffset openStartTime = DateTimeOffset.Now;
			Task[] awaitTasks = new Task[2];
			PooledTimer delayTaskTimer = (openTimeoutInSeconds == 0.0) ? timerPool.GetPooledTimer((int)requestTimeoutInSeconds) : timerPool.GetPooledTimer((int)openTimeoutInSeconds);
			Task task = awaitTasks[0] = delayTaskTimer.StartTimerAsync();
			awaitTasks[1] = OpenSocket(activityId);
			Task task2 = await Task.WhenAny(awaitTasks);
			if (task2 == awaitTasks[0])
			{
				CleanupWorkTask(awaitTasks[1], activityId, openStartTime);
				if (!awaitTasks[0].IsFaulted)
				{
					throw GetGoneException(fullTargetAddress, activityId);
				}
				throw GetGoneException(fullTargetAddress, activityId, task2.Exception.InnerException);
			}
			if (task2.IsFaulted)
			{
				delayTaskTimer.CancelTimer();
				if (!(task2.Exception.InnerException is DocumentClientException))
				{
					throw GetGoneException(fullTargetAddress, activityId, task2.Exception.InnerException);
				}
				((DocumentClientException)task2.Exception.InnerException).Headers.Set("x-ms-activity-id", activityId.ToString());
				await task2;
			}
			connectionTimers.TcpConnectCompleteTimestamp = DateTimeOffset.Now;
			RntbdResponseState state = new RntbdResponseState();
			awaitTasks[1] = PerformHandshakes(activityId, state);
			task2 = await Task.WhenAny(awaitTasks);
			if (task2 == awaitTasks[0])
			{
				CleanupWorkTask(awaitTasks[1], activityId, openStartTime);
				if (!awaitTasks[0].IsFaulted)
				{
					throw GetGoneException(fullTargetAddress, activityId);
				}
				throw GetGoneException(fullTargetAddress, activityId, task2.Exception.InnerException);
			}
			delayTaskTimer.CancelTimer();
			if (task2.IsFaulted)
			{
				if (!(task2.Exception.InnerException is DocumentClientException))
				{
					throw GetGoneException(fullTargetAddress, activityId, task2.Exception.InnerException);
				}
				((DocumentClientException)task2.Exception.InnerException).Headers.Set("x-ms-activity-id", activityId.ToString());
				await task2;
			}
		}

		/// <summary>
		///  Async method to makes request to backend using the rntbd protocol
		/// </summary>
		/// <param name="request"> a DocumentServiceRequest object that has the state for all the headers </param>
		/// <param name="physicalAddress"> physical address of the replica </param>
		/// <param name="resourceOperation"> Resource Type + Operation Type Pair </param>
		/// <param name="activityId"> ActivityId of the request </param>
		/// <returns> StoreResponse </returns>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException"> exception that was caught while building the request byte buffer for sending over wire </exception>
		/// <exception cref="T:Microsoft.Azure.Documents.BadRequestException"> any other exception encountered while building the request </exception>
		/// <exception cref="T:Microsoft.Azure.Documents.GoneException"> All timeouts for read-only requests are converted to Gone exception </exception>
		/// <exception cref="T:Microsoft.Azure.Documents.RequestTimeoutException"> Request Times out (if there's no response until the timeout duration </exception>
		/// <exception cref="T:Microsoft.Azure.Documents.ServiceUnavailableException"> Any other exception that is not from our code (exception SocketException, or Connection Close 
		/// </exception>
		public async Task<StoreResponse> RequestAsync(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId)
		{
			targetPhysicalAddress = physicalAddress;
			int headerAndMetadataSize = 0;
			int bodySize = 0;
			byte[] requestPayload;
			try
			{
				requestPayload = BuildRequest(request, physicalAddress.PathAndQuery.TrimEnd(UrlTrim), resourceOperation, out headerAndMetadataSize, out bodySize, activityId);
			}
			catch (Exception ex)
			{
				DocumentClientException ex2 = ex as DocumentClientException;
				if (ex2 != null)
				{
					ex2.Headers.Add("x-ms-request-validation-failure", "1");
					throw;
				}
				DefaultTrace.TraceError("RntbdConnection.BuildRequest failure due to assumed malformed request payload: {0}", ex);
				ex2 = new BadRequestException(ex);
				ex2.Headers.Add("x-ms-request-validation-failure", "1");
				throw ex2;
			}
			PooledTimer delayTaskTimer = timerPool.GetPooledTimer((int)requestTimeoutInSeconds);
			Task task = delayTaskTimer.StartTimerAsync();
			DateTimeOffset requestStartTime = DateTimeOffset.Now;
			Task[] awaitTasks = new Task[2]
			{
				task,
				SendRequestAsyncInternal(requestPayload, activityId)
			};
			Task task2 = await Task.WhenAny(awaitTasks);
			if (task2 == awaitTasks[0])
			{
				DateTimeOffset now = DateTimeOffset.Now;
				CleanupWorkTask(awaitTasks[1], activityId, requestStartTime);
				DefaultTrace.TraceError("Throwing RequestTimeoutException while awaiting request send. Task start time {0}. Task end time {1}. Request message size: {2}", requestStartTime, now, requestPayload.Length);
				if (!awaitTasks[0].IsFaulted)
				{
					if (request.IsReadOnlyRequest)
					{
						DefaultTrace.TraceVerbose("Converting RequestTimeout to GoneException for ReadOnlyRequest");
						throw GetGoneException(physicalAddress, activityId);
					}
					throw GetRequestTimeoutException(physicalAddress, activityId);
				}
				if (request.IsReadOnlyRequest)
				{
					DefaultTrace.TraceVerbose("Converting RequestTimeout to GoneException for ReadOnlyRequest");
					throw GetGoneException(physicalAddress, activityId, task2.Exception.InnerException);
				}
				throw GetRequestTimeoutException(physicalAddress, activityId, task2.Exception.InnerException);
			}
			if (task2.IsFaulted)
			{
				delayTaskTimer.CancelTimer();
				if (!(task2.Exception.InnerException is DocumentClientException))
				{
					throw GetServiceUnavailableException(physicalAddress, activityId, task2.Exception.InnerException);
				}
				((DocumentClientException)task2.Exception.InnerException).Headers.Set("x-ms-activity-id", activityId.ToString());
				await task2;
			}
			DateTimeOffset requestSendDoneTime = DateTimeOffset.Now;
			RntbdResponseState state = new RntbdResponseState();
			Task<StoreResponse> responseTask = (Task<StoreResponse>)(awaitTasks[1] = GetResponseAsync(activityId, request.IsReadOnlyRequest, state));
			task2 = await Task.WhenAny(awaitTasks);
			if (task2 == awaitTasks[0])
			{
				DateTimeOffset now2 = DateTimeOffset.Now;
				CleanupWorkTask(awaitTasks[1], activityId, requestStartTime);
				DefaultTrace.TraceError("Throwing RequestTimeoutException while awaiting response receive. Task start time {0}. Request Send End time: {1}. Request header size: {2}. Request body size: {3}. Request size: {4}. Task end time {5}. State {6}.", requestStartTime.ToString("o", CultureInfo.InvariantCulture), requestSendDoneTime.ToString("o", CultureInfo.InvariantCulture), headerAndMetadataSize, bodySize, requestPayload.Length, now2.ToString("o", CultureInfo.InvariantCulture), state.ToString());
				if (!awaitTasks[0].IsFaulted)
				{
					if (request.IsReadOnlyRequest)
					{
						DefaultTrace.TraceVerbose("Converting RequestTimeout to GoneException for ReadOnlyRequest");
						throw GetGoneException(physicalAddress, activityId);
					}
					throw GetRequestTimeoutException(physicalAddress, activityId);
				}
				if (request.IsReadOnlyRequest)
				{
					DefaultTrace.TraceVerbose("Converting RequestTimeout to GoneException for ReadOnlyRequest");
					throw GetGoneException(physicalAddress, activityId, task2.Exception.InnerException);
				}
				throw GetRequestTimeoutException(physicalAddress, activityId, task2.Exception.InnerException);
			}
			delayTaskTimer.CancelTimer();
			if (task2.IsFaulted)
			{
				if (!(task2.Exception.InnerException is DocumentClientException))
				{
					throw GetServiceUnavailableException(physicalAddress, activityId, task2.Exception.InnerException);
				}
				((DocumentClientException)task2.Exception.InnerException).Headers.Set("x-ms-activity-id", activityId.ToString());
				await task2;
			}
			if (responseTask.Result.Status >= 200 && responseTask.Result.Status != 410 && responseTask.Result.Status != 401 && responseTask.Result.Status != 403)
			{
				hasIssuedSuccessfulRequest = true;
			}
			lastUsed = DateTime.UtcNow;
			return responseTask.Result;
		}

		private void CleanupWorkTask(Task workTask, Guid activityId, DateTimeOffset requestStartTime)
		{
			workTask.ContinueWith(delegate(Task t)
			{
				if (t.Exception != null)
				{
					ObjectDisposedException ex = t.Exception.InnerException as ObjectDisposedException;
					if (ex == null || (ex.ObjectName != null && string.Compare(ex.ObjectName, "SslStream", StringComparison.Ordinal) != 0))
					{
						DefaultTrace.TraceError("Ignoring exception {0} on ActivityId {1}. Task start time {2} Hresult {3}", t.Exception, activityId.ToString(), requestStartTime, t.Exception.HResult);
					}
					else
					{
						DefaultTrace.TraceVerbose("Ignoring exception {0} on ActivityId {1}. Task start time {2} Hresult {3}", ex, activityId.ToString(), requestStartTime, ex.HResult);
					}
				}
			});
		}

		private async Task OpenSocket(Guid activityId)
		{
			TcpClient client = null;
			try
			{
				IPAddress[] array = await Dns.GetHostAddressesAsync(initialOpenUri.DnsSafeHost);
				if (array.Length > 1)
				{
					DefaultTrace.TraceWarning("Found multiple addresses for host, choosing the first. Host: {0}. Addresses: {1}", initialOpenUri.DnsSafeHost, array);
				}
				IPAddress iPAddress = array[0];
				client = new TcpClient(iPAddress.AddressFamily);
				client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, optionValue: true);
				client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, optionValue: true);
				SetKeepAlive(client.Client);
				await client.ConnectAsync(iPAddress, initialOpenUri.Port);
			}
			catch (Exception ex)
			{
				TcpClient obj = client;
				if (obj != null)
				{
					CustomTypeExtensions.Close(obj);
				}
				SocketError? socketError = (ex as SocketException)?.SocketErrorCode;
				throw GetGoneException(targetPhysicalAddress, activityId, ex);
			}
			tcpClient = client;
			socket = client.Client;
			stream = client.GetStream();
		}

		private async Task PerformHandshakes(Guid activityId, RntbdResponseState state)
		{
			string targetHost = (overrideHostNameInCertificate != null) ? overrideHostNameInCertificate : initialOpenUri.Host;
			SslStream sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
			try
			{
				await sslStream.AuthenticateAsClientAsync(targetHost, null, SslProtocols.Tls12, checkCertificateRevocation: false);
			}
			catch (Exception inner)
			{
				throw GetGoneException(targetPhysicalAddress, activityId, inner);
			}
			connectionTimers.SslHandshakeCompleteTimestamp = DateTimeOffset.Now;
			stream = sslStream;
			try
			{
				await NegotiateRntbdContextAsync(sslStream, activityId, state);
			}
			catch (Exception ex)
			{
				if (ex is DocumentClientException)
				{
					throw;
				}
				throw GetGoneException(targetPhysicalAddress, activityId, ex);
			}
			connectionTimers.RntbdHandshakeCompleteTimestamp = DateTimeOffset.Now;
			isOpen = true;
		}

		public bool HasExpired()
		{
			TimeSpan t = DateTime.UtcNow - lastUsed;
			TimeSpan t2 = DateTime.UtcNow - opened;
			if (!(t > idleTimeout))
			{
				if (!hasIssuedSuccessfulRequest)
				{
					return t2 > unauthenticatedTimeout;
				}
				return false;
			}
			return true;
		}

		public bool ConfirmOpen()
		{
			return CustomTypeExtensions.ConfirmOpen(socket);
		}

		protected virtual byte[] BuildContextRequest(Guid activityId)
		{
			return TransportSerialization.BuildContextRequest(activityId, userAgent);
		}

		private async Task NegotiateRntbdContextAsync(Stream negotiatingStream, Guid activityId, RntbdResponseState state)
		{
			byte[] array = BuildContextRequest(activityId);
			await negotiatingStream.WriteAsync(array, 0, array.Length);
			Tuple<byte[], byte[]> obj = await ReadHeaderAndMetadata(8000, throwGoneOnChannelFailure: true, activityId, state);
			byte[] item = obj.Item1;
			byte[] item2 = obj.Item2;
			StatusCodes status = (StatusCodes)BitConverter.ToUInt32(item, 4);
			byte[] array2 = new byte[16];
			Buffer.BlockCopy(item, 8, array2, 0, 16);
			Guid responseActivityId = new Guid(array2);
			using (MemoryStream outerReadStream = new MemoryStream(item2))
			{
				RntbdConstants.ConnectionContextResponse response = null;
				using (BinaryReader readStream = new BinaryReader(outerReadStream))
				{
					response = new RntbdConstants.ConnectionContextResponse();
					response.ParseFrom(readStream);
				}
				serverAgent = Encoding.UTF8.GetString(response.serverAgent.value.valueBytes);
				serverVersion = Encoding.UTF8.GetString(response.serverVersion.value.valueBytes);
				SetIdleTimers(response);
				if ((uint)status < 200u || (uint)status >= 400u)
				{
					using (MemoryStream memoryStream = new MemoryStream(await ReadBody(throwGoneOnChannelFailure: true, responseActivityId, state)))
					{
						Error error = JsonSerializable.LoadFrom<Error>(memoryStream);
						Trace.CorrelationManager.ActivityId = responseActivityId;
						DocumentClientException ex = new DocumentClientException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, error.ToString()), null, (HttpStatusCode)status, targetPhysicalAddress);
						if (response.clientVersion.isPresent)
						{
							ex.Headers.Add("RequiredClientVersion", Encoding.UTF8.GetString(response.clientVersion.value.valueBytes));
						}
						if (response.protocolVersion.isPresent)
						{
							ex.Headers.Add("RequiredProtocolVersion", response.protocolVersion.value.valueULong.ToString());
						}
						if (response.serverAgent.isPresent)
						{
							ex.Headers.Add("ServerAgent", Encoding.UTF8.GetString(response.serverAgent.value.valueBytes));
						}
						if (response.serverVersion.isPresent)
						{
							ex.Headers.Add("x-ms-serviceversion", Encoding.UTF8.GetString(response.serverVersion.value.valueBytes));
						}
						throw ex;
					}
				}
			}
		}

		private void SetIdleTimers(RntbdConstants.ConnectionContextResponse response)
		{
			if (response.unauthenticatedTimeoutInSeconds.isPresent)
			{
				uint num = (response.unauthenticatedTimeoutInSeconds.value.valueULong <= 5) ? 1u : (response.unauthenticatedTimeoutInSeconds.value.valueULong - 5);
				unauthenticatedTimeout = TimeSpan.FromSeconds(num);
			}
			else
			{
				unauthenticatedTimeout = DefaultUnauthenticatedTimeout;
			}
		}

		private async Task SendRequestAsyncInternal(byte[] requestPayload, Guid activityId)
		{
			try
			{
				await stream.WriteAsync(requestPayload, 0, requestPayload.Length);
			}
			catch (SocketException inner)
			{
				throw GetServiceUnavailableException(targetPhysicalAddress, activityId, inner);
			}
			catch (IOException inner2)
			{
				throw GetServiceUnavailableException(targetPhysicalAddress, activityId, inner2);
			}
		}

		/// <summary>
		/// Given DocumentServiceRequest object, creates a byte array of the request that is to be sent over wire to
		/// backend per rntbd protocol
		/// </summary>
		/// <param name="request"> DocumentService Request</param>
		/// <param name="replicaPath"> path to the replica, as extracted from the replica uri </param>
		/// <param name="resourceOperation"> ResourceType + OperationType pair </param>
		/// <param name="headerAndMetadataSize"></param>
		/// <param name="bodySize"></param>
		/// <param name="activityId"></param>
		/// <exception cref="T:Microsoft.Azure.Documents.InternalServerErrorException"> 
		/// This is of type DocumentClientException. Thrown 
		/// if there is a bug in rntbd token serialization 
		/// </exception>
		/// <returns> byte array that is the request body to be sent over wire </returns>
		protected virtual byte[] BuildRequest(DocumentServiceRequest request, string replicaPath, ResourceOperation resourceOperation, out int headerAndMetadataSize, out int bodySize, Guid activityId)
		{
			return TransportSerialization.BuildRequest(request, replicaPath, resourceOperation, activityId, out headerAndMetadataSize, out bodySize);
		}

		private async Task<StoreResponse> GetResponseAsync(Guid requestActivityId, bool isReadOnlyRequest, RntbdResponseState state)
		{
			state.SetState(RntbdResponseStateEnum.Called);
			Tuple<byte[], byte[]> obj = await ReadHeaderAndMetadata(int.MaxValue, isReadOnlyRequest, requestActivityId, state);
			byte[] item = obj.Item1;
			byte[] item2 = obj.Item2;
			StatusCodes status = (StatusCodes)BitConverter.ToUInt32(item, 4);
			byte[] array = new byte[16];
			Buffer.BlockCopy(item, 8, array, 0, 16);
			Guid responseActivityId = new Guid(array);
			RntbdConstants.Response response = null;
			using (MemoryStream input = new MemoryStream(item2))
			{
				using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8))
				{
					response = new RntbdConstants.Response();
					response.ParseFrom(reader);
				}
			}
			MemoryStream body = null;
			if (response.payloadPresent.value.valueByte != 0)
			{
				byte[] buffer;
				try
				{
					buffer = await ReadBody(throwGoneOnChannelFailure: false, responseActivityId, state);
				}
				catch (Exception ex)
				{
					if (ex is DocumentClientException)
					{
						throw;
					}
					throw GetServiceUnavailableException(targetPhysicalAddress, responseActivityId, ex);
				}
				body = new MemoryStream(buffer);
			}
			state.SetState(RntbdResponseStateEnum.Done);
			return TransportSerialization.MakeStoreResponse(status, responseActivityId, response, body, serverVersion);
		}

		private async Task<Tuple<byte[], byte[]>> ReadHeaderAndMetadata(int maxAllowed, bool throwGoneOnChannelFailure, Guid activityId, RntbdResponseState state)
		{
			state.SetState(RntbdResponseStateEnum.StartHeader);
			byte[] header = new byte[24];
			int headerRead;
			int read;
			for (headerRead = 0; headerRead < header.Length; headerRead += read)
			{
				read = 0;
				try
				{
					read = await stream.ReadAsync(header, headerRead, header.Length - headerRead);
				}
				catch (IOException innerException)
				{
					DefaultTrace.TraceError("Hit IOException while reading header on connection with last used time {0}", lastUsed.ToString("o", CultureInfo.InvariantCulture));
					ThrowOnFailure(throwGoneOnChannelFailure, activityId, innerException);
				}
				if (read == 0)
				{
					DefaultTrace.TraceError("Read 0 bytes while reading header");
					ThrowOnFailure(throwGoneOnChannelFailure, activityId);
				}
				state.SetState(RntbdResponseStateEnum.BufferingHeader);
				state.AddHeaderMetadataRead(read);
			}
			state?.SetState(RntbdResponseStateEnum.DoneBufferingHeader);
			uint num = BitConverter.ToUInt32(header, 0);
			if (num > maxAllowed)
			{
				DefaultTrace.TraceCritical("RNTBD header length says {0} but expected at most {1} bytes", num, maxAllowed);
				throw GetInternalServerErrorException(targetPhysicalAddress, activityId);
			}
			if (num < header.Length)
			{
				DefaultTrace.TraceCritical("RNTBD header length says {0} but expected at least {1} bytes and read {2} bytes from wire", num, header.Length, headerRead);
				throw GetInternalServerErrorException(targetPhysicalAddress, activityId);
			}
			int metadataLength = (int)num - header.Length;
			byte[] metadata = new byte[metadataLength];
			for (int responseMetadataRead = 0; responseMetadataRead < metadataLength; responseMetadataRead += read)
			{
				read = 0;
				try
				{
					read = await stream.ReadAsync(metadata, responseMetadataRead, metadataLength - responseMetadataRead);
				}
				catch (IOException innerException2)
				{
					DefaultTrace.TraceError("Hit IOException while reading metadata on connection with last used time {0}", lastUsed.ToString("o", CultureInfo.InvariantCulture));
					ThrowOnFailure(throwGoneOnChannelFailure, activityId, innerException2);
				}
				if (read == 0)
				{
					DefaultTrace.TraceError("Read 0 bytes while reading metadata");
					ThrowOnFailure(throwGoneOnChannelFailure, activityId);
				}
				state.SetState(RntbdResponseStateEnum.BufferingMetadata);
				state.AddHeaderMetadataRead(read);
			}
			state.SetState(RntbdResponseStateEnum.DoneBufferingMetadata);
			return new Tuple<byte[], byte[]>(header, metadata);
		}

		private async Task<byte[]> ReadBody(bool throwGoneOnChannelFailure, Guid activityId, RntbdResponseState state)
		{
			byte[] bodyLengthHeader = new byte[4];
			int read;
			for (int bodyLengthRead = 0; bodyLengthRead < 4; bodyLengthRead += read)
			{
				read = 0;
				try
				{
					read = await stream.ReadAsync(bodyLengthHeader, bodyLengthRead, bodyLengthHeader.Length - bodyLengthRead);
				}
				catch (IOException innerException)
				{
					DefaultTrace.TraceError("Hit IOException while reading BodyLengthHeader on connection with last used time {0}", lastUsed.ToString("o", CultureInfo.InvariantCulture));
					ThrowOnFailure(throwGoneOnChannelFailure, activityId, innerException);
				}
				if (read == 0)
				{
					DefaultTrace.TraceError("Read 0 bytes while reading BodyLengthHeader");
					ThrowOnFailure(throwGoneOnChannelFailure, activityId);
				}
				state.SetState(RntbdResponseStateEnum.BufferingBodySize);
				state.AddBodyRead(read);
			}
			state.SetState(RntbdResponseStateEnum.DoneBufferingBodySize);
			uint bodyRead = 0u;
			uint length = BitConverter.ToUInt32(bodyLengthHeader, 0);
			byte[] body = new byte[length];
			for (; bodyRead < length; bodyRead = (uint)((int)bodyRead + read))
			{
				read = 0;
				try
				{
					read = await stream.ReadAsync(body, (int)bodyRead, body.Length - (int)bodyRead);
				}
				catch (IOException innerException2)
				{
					DefaultTrace.TraceError("Hit IOException while reading Body on connection with last used time {0}", lastUsed.ToString("o", CultureInfo.InvariantCulture));
					ThrowOnFailure(throwGoneOnChannelFailure, activityId, innerException2);
				}
				if (read == 0)
				{
					DefaultTrace.TraceError("Read 0 bytes while reading Body");
					ThrowOnFailure(throwGoneOnChannelFailure, activityId);
				}
				state.SetState(RntbdResponseStateEnum.BufferingBody);
				state.AddBodyRead(read);
			}
			state.SetState(RntbdResponseStateEnum.DoneBufferingBody);
			return body;
		}

		private void ThrowOnFailure(bool throwGoneOnChannelFailure, Guid activityId, Exception innerException = null)
		{
			if (throwGoneOnChannelFailure)
			{
				throw GetGoneException(targetPhysicalAddress, activityId, innerException);
			}
			throw GetServiceUnavailableException(targetPhysicalAddress, activityId, innerException);
		}

		private static GoneException GetGoneException(Uri fullTargetAddress, Guid activityId, Exception inner = null)
		{
			return TransportExceptions.GetGoneException(fullTargetAddress, activityId, inner);
		}

		private static RequestTimeoutException GetRequestTimeoutException(Uri fullTargetAddress, Guid activityId, Exception inner = null)
		{
			return TransportExceptions.GetRequestTimeoutException(fullTargetAddress, activityId, inner);
		}

		private static ServiceUnavailableException GetServiceUnavailableException(Uri fullTargetAddress, Guid activityId, Exception inner = null)
		{
			return TransportExceptions.GetServiceUnavailableException(fullTargetAddress, activityId, inner);
		}

		private static InternalServerErrorException GetInternalServerErrorException(Uri fullTargetAddress, Guid activityId, Exception inner = null)
		{
			return TransportExceptions.GetInternalServerErrorException(fullTargetAddress, activityId, inner);
		}

		private static void SetKeepAlive(Socket socket)
		{
		}
	}
}
