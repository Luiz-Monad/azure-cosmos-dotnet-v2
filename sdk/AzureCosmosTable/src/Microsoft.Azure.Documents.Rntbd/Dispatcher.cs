using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class Dispatcher : IDisposable
	{
		public sealed class PrepareCallResult
		{
			public uint RequestId
			{
				get;
				private set;
			}

			public byte[] SerializedRequest
			{
				get;
				set;
			}

			public Uri Uri
			{
				get;
				private set;
			}

			public PrepareCallResult(uint requestId, Uri uri, byte[] serializedRequest)
			{
				RequestId = requestId;
				Uri = uri;
				SerializedRequest = serializedRequest;
			}
		}

		private sealed class CallInfo : IDisposable
		{
			private enum State
			{
				New,
				Sent,
				SendFailed
			}

			private readonly TaskCompletionSource<StoreResponse> completion = new TaskCompletionSource<StoreResponse>();

			private readonly ManualResetEventSlim sendComplete = new ManualResetEventSlim();

			private readonly Guid activityId;

			private readonly Uri uri;

			private readonly TaskScheduler scheduler;

			private bool disposed;

			private readonly object stateLock = new object();

			private State state;

			public CallInfo(Guid activityId, Uri uri, TaskScheduler scheduler)
			{
				this.activityId = activityId;
				this.uri = uri;
				this.scheduler = scheduler;
			}

			public Task<StoreResponse> ReadResponseAsync(ChannelCallArguments args)
			{
				ThrowIfDisposed();
				CompleteSend(State.Sent);
				args.CommonArguments.SetTimeoutCode(TransportErrorCode.ReceiveTimeout);
				return completion.Task;
			}

			public void SendFailed()
			{
				ThrowIfDisposed();
				CompleteSend(State.SendFailed);
			}

			public void SetResponse(RntbdConstants.Response rntbdResponse, TransportSerialization.RntbdHeader responseHeader, MemoryStream responseBody, string serverVersion)
			{
				ThrowIfDisposed();
				RunAsynchronously(delegate
				{
					Trace.CorrelationManager.ActivityId = activityId;
					StoreResponse storeResponse = null;
					try
					{
						storeResponse = TransportSerialization.MakeStoreResponse(responseHeader.Status, responseHeader.ActivityId, rntbdResponse, responseBody, serverVersion);
					}
					catch (Exception exception)
					{
						completion.SetException(exception);
						return;
					}
					completion.SetResult(storeResponse);
				});
			}

			public void SetConnectionBrokenException(Exception inner, string sourceDescription)
			{
				ThrowIfDisposed();
				RunAsynchronously(delegate
				{
					Trace.CorrelationManager.ActivityId = activityId;
					sendComplete.Wait();
					lock (stateLock)
					{
						if (state != State.Sent)
						{
							return;
						}
					}
					completion.SetException(new TransportException(TransportErrorCode.ConnectionBroken, inner, activityId, uri, sourceDescription, userPayload: true, payloadSent: true));
				});
			}

			public void Cancel()
			{
				ThrowIfDisposed();
				RunAsynchronously(delegate
				{
					Trace.CorrelationManager.ActivityId = activityId;
					completion.SetCanceled();
				});
			}

			public void Dispose()
			{
				ThrowIfDisposed();
				disposed = true;
				sendComplete.Dispose();
			}

			private void ThrowIfDisposed()
			{
				if (disposed)
				{
					throw new ObjectDisposedException("CallInfo");
				}
			}

			private void RunAsynchronously(Action action)
			{
				Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler).ContinueWith(delegate(Task failedTask)
				{
					DefaultTrace.TraceError("Unexpected: Rntbd asynchronous completion call failed. Consuming the task exception asynchronously. Exception: {0}", failedTask.Exception?.InnerException);
				}, TaskContinuationOptions.OnlyOnFaulted);
			}

			private void CompleteSend(State newState)
			{
				lock (stateLock)
				{
					if (sendComplete.IsSet)
					{
						throw new InvalidOperationException("Send may only complete once");
					}
					state = newState;
					sendComplete.Set();
				}
			}
		}

		private readonly Connection connection;

		private readonly UserAgentContainer userAgent;

		private readonly Uri serverUri;

		private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

		private readonly TimerPool idleTimerPool;

		private bool disposed;

		private ServerProperties serverProperties;

		private int nextRequestId;

		private readonly object callLock = new object();

		private Task receiveTask;

		private Dictionary<uint, CallInfo> calls = new Dictionary<uint, CallInfo>();

		private bool callsAllowed = true;

		private readonly object connectionLock = new object();

		private PooledTimer idleTimer;

		private Task idleTimerTask;

		internal bool TestIsIdle
		{
			get
			{
				lock (connectionLock)
				{
					if (connection.Disposed)
					{
						return true;
					}
					TimeSpan timeToIdle;
					return !connection.IsActive(out timeToIdle);
				}
			}
		}

		public bool Healthy
		{
			get
			{
				ThrowIfDisposed();
				if (cancellation.IsCancellationRequested)
				{
					return false;
				}
				lock (callLock)
				{
					if (!callsAllowed)
					{
						return false;
					}
				}
				bool flag;
				try
				{
					flag = connection.Healthy;
				}
				catch (ObjectDisposedException)
				{
					DefaultTrace.TraceWarning("RNTBD Dispatcher {0}: ObjectDisposedException from Connection.Healthy", this);
					flag = false;
				}
				if (flag)
				{
					return true;
				}
				lock (callLock)
				{
					callsAllowed = false;
				}
				return false;
			}
		}

		internal event Action TestOnConnectionClosed;

		public Dispatcher(Uri serverUri, UserAgentContainer userAgent, string hostNameCertificateOverride, TimeSpan receiveHangDetectionTime, TimeSpan sendHangDetectionTime, TimerPool idleTimerPool, TimeSpan idleTimeout)
		{
			connection = new Connection(serverUri, hostNameCertificateOverride, receiveHangDetectionTime, sendHangDetectionTime, idleTimeout);
			this.userAgent = userAgent;
			this.serverUri = serverUri;
			this.idleTimerPool = idleTimerPool;
		}

		public async Task OpenAsync(ChannelOpenArguments args)
		{
			ThrowIfDisposed();
			try
			{
				await connection.OpenAsync(args);
				await NegotiateRntbdContextAsync(args);
				lock (callLock)
				{
					receiveTask = Task.Factory.StartNew((Func<Task>)async delegate
					{
						await ReceiveLoopAsync();
					}, cancellation.Token, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
					receiveTask.ContinueWith(delegate(Task completedTask)
					{
						DefaultTrace.TraceWarning("RNTBD Dispatcher.ReceiveLoopAsync failed. Consuming the task exception asynchronously. Dispatcher: {0}. Exception: {1}", this, completedTask.Exception?.InnerException);
					}, default(CancellationToken), TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
				}
				if (idleTimerPool != null)
				{
					StartIdleTimer();
				}
			}
			catch (DocumentClientException)
			{
				DisallowInitialCalls();
				throw;
			}
			catch (TransportException)
			{
				DisallowInitialCalls();
				throw;
			}
		}

		public PrepareCallResult PrepareCall(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId)
		{
			uint requestId = (uint)Interlocked.Increment(ref nextRequestId);
			lock (request)
			{
				request.Headers.Set("x-ms-transport-request-id", requestId.ToString(CultureInfo.InvariantCulture));
				int headerSize;
				int bodySize;
				byte[] serializedRequest = TransportSerialization.BuildRequest(request, physicalAddress.PathAndQuery.TrimEnd(TransportSerialization.UrlTrim), resourceOperation, activityId, out headerSize, out bodySize);
				return new PrepareCallResult(requestId, physicalAddress, serializedRequest);
			}
		}

		public async Task<StoreResponse> CallAsync(ChannelCallArguments args)
		{
			ThrowIfDisposed();
			using (CallInfo callInfo = new CallInfo(args.CommonArguments.ActivityId, args.PreparedCall.Uri, TaskScheduler.Current))
			{
				uint requestId = args.PreparedCall.RequestId;
				lock (callLock)
				{
					if (!callsAllowed)
					{
						throw new TransportException(TransportErrorCode.ChannelMultiplexerClosed, null, args.CommonArguments.ActivityId, args.PreparedCall.Uri, ToString(), args.CommonArguments.UserPayload, args.CommonArguments.PayloadSent);
					}
					calls.Add(requestId, callInfo);
				}
				try
				{
					try
					{
						await connection.WriteRequestAsync(args.CommonArguments, args.PreparedCall.SerializedRequest);
						args.PreparedCall.SerializedRequest = null;
					}
					catch (Exception innerException)
					{
						callInfo.SendFailed();
						throw new TransportException(TransportErrorCode.SendFailed, innerException, args.CommonArguments.ActivityId, args.PreparedCall.Uri, ToString(), args.CommonArguments.UserPayload, args.CommonArguments.PayloadSent);
					}
					return await callInfo.ReadResponseAsync(args);
				}
				catch (DocumentClientException)
				{
					DisallowRuntimeCalls();
					throw;
				}
				catch (TransportException)
				{
					DisallowRuntimeCalls();
					throw;
				}
				finally
				{
					RemoveCall(requestId);
				}
			}
		}

		public void CancelCall(PrepareCallResult preparedCall)
		{
			ThrowIfDisposed();
			RemoveCall(preparedCall.RequestId)?.Cancel();
		}

		public override string ToString()
		{
			return connection.ToString();
		}

		public void Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			DefaultTrace.TraceInformation("Disposing RNTBD Dispatcher {0}", this);
			Task t = null;
			lock (connectionLock)
			{
				StartConnectionShutdown();
				t = StopIdleTimer();
			}
			WaitTask(t, "idle timer");
			Task t2 = null;
			lock (connectionLock)
			{
				t2 = CloseConnection();
			}
			WaitTask(t2, "receive loop");
			DefaultTrace.TraceInformation("RNTBD Dispatcher {0} is disposed", this);
		}

		private void StartIdleTimer()
		{
			DefaultTrace.TraceInformation("RNTBD idle connection monitor: Timer is starting...");
			TimeSpan timeToIdle = TimeSpan.MinValue;
			bool flag = false;
			try
			{
				lock (connectionLock)
				{
					if (!connection.IsActive(out timeToIdle))
					{
						DefaultTrace.TraceCritical("RNTBD Dispatcher {0}: New connection already idle.", this);
					}
					else
					{
						ScheduleIdleTimer(timeToIdle);
						flag = true;
					}
				}
			}
			finally
			{
				if (flag)
				{
					DefaultTrace.TraceInformation("RNTBD idle connection monitor {0}: Timer is scheduled to fire {1} seconds later at {2}.", this, timeToIdle.TotalSeconds, DateTime.UtcNow + timeToIdle);
				}
				else
				{
					DefaultTrace.TraceInformation("RNTBD idle connection monitor {0}: Timer is not scheduled.", this);
				}
			}
		}

		private void OnIdleTimer(Task precedentTask)
		{
			Task t = null;
			lock (connectionLock)
			{
				if (cancellation.IsCancellationRequested)
				{
					return;
				}
				TimeSpan timeToIdle;
				bool flag = connection.IsActive(out timeToIdle);
				if (flag)
				{
					ScheduleIdleTimer(timeToIdle);
					return;
				}
				lock (callLock)
				{
					if (calls.Count > 0)
					{
						DefaultTrace.TraceCritical("RNTBD Dispatcher {0}: Looks idle but still has {1} pending requests", this, calls.Count);
						flag = true;
					}
					else
					{
						callsAllowed = false;
					}
				}
				if (flag)
				{
					ScheduleIdleTimer(timeToIdle);
					return;
				}
				idleTimer = null;
				idleTimerTask = null;
				StartConnectionShutdown();
				t = CloseConnection();
			}
			WaitTask(t, "receive loop");
		}

		private void ScheduleIdleTimer(TimeSpan timeToIdle)
		{
			idleTimer = idleTimerPool.GetPooledTimer((int)timeToIdle.TotalSeconds);
			idleTimerTask = idleTimer.StartTimerAsync().ContinueWith(OnIdleTimer, TaskContinuationOptions.OnlyOnRanToCompletion);
			idleTimerTask.ContinueWith(delegate(Task failedTask)
			{
				DefaultTrace.TraceWarning("RNTBD Dispatcher {0} idle timer callback failed: {1}", this, failedTask.Exception?.InnerException);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private void StartConnectionShutdown()
		{
			if (!cancellation.IsCancellationRequested)
			{
				try
				{
					lock (callLock)
					{
						callsAllowed = false;
					}
					cancellation.Cancel();
				}
				catch (AggregateException ex)
				{
					DefaultTrace.TraceWarning("RNTBD Dispatcher {0}: Registered cancellation callbacks failed: {1}", this, ex);
				}
			}
		}

		private Task StopIdleTimer()
		{
			Task result = null;
			if (idleTimer != null)
			{
				if (idleTimer.CancelTimer())
				{
					idleTimer = null;
					idleTimerTask = null;
				}
				else
				{
					result = idleTimerTask;
				}
			}
			return result;
		}

		private Task CloseConnection()
		{
			Task result = null;
			if (!connection.Disposed)
			{
				lock (callLock)
				{
					result = receiveTask;
				}
				connection.Dispose();
				this.TestOnConnectionClosed?.Invoke();
			}
			return result;
		}

		private void WaitTask(Task t, string description)
		{
			if (t != null)
			{
				try
				{
					t.Wait();
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceWarning("RNTBD Dispatcher {0}: Parallel task failed: {1}. Exception: {2}", this, description, ex);
				}
			}
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException(string.Format("{0}:{1}", "Dispatcher", serverUri));
			}
		}

		private async Task NegotiateRntbdContextAsync(ChannelOpenArguments args)
		{
			byte[] messagePayload = TransportSerialization.BuildContextRequest(args.CommonArguments.ActivityId, userAgent);
			await connection.WriteRequestAsync(args.CommonArguments, messagePayload);
			Connection.ResponseMetadata responseMetadata = await connection.ReadResponseMetadataAsync(args.CommonArguments);
			StatusCodes status = (StatusCodes)BitConverter.ToUInt32(responseMetadata.Header, 4);
			byte[] array = new byte[16];
			Buffer.BlockCopy(responseMetadata.Header, 8, array, 0, 16);
			Guid activityId = new Guid(array);
			Trace.CorrelationManager.ActivityId = activityId;
			using (MemoryStream readStream = new MemoryStream(responseMetadata.Metadata))
			{
				RntbdConstants.ConnectionContextResponse response = null;
				using (BinaryReader reader = new BinaryReader(readStream))
				{
					response = new RntbdConstants.ConnectionContextResponse();
					response.ParseFrom(reader);
				}
				serverProperties = new ServerProperties(Encoding.UTF8.GetString(response.serverAgent.value.valueBytes), Encoding.UTF8.GetString(response.serverVersion.value.valueBytes));
				if ((uint)status < 200u || (uint)status >= 400u)
				{
					using (MemoryStream stream = new MemoryStream(await connection.ReadResponseBodyAsync(new ChannelCommonArguments(activityId, TransportErrorCode.TransportNegotiationTimeout, args.CommonArguments.UserPayload))))
					{
						Error error = JsonSerializable.LoadFrom<Error>(stream);
						DocumentClientException ex = new DocumentClientException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, error.ToString()), null, (HttpStatusCode)status, connection.ServerUri);
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
			args.OpenTimeline.RecordRntbdHandshakeFinishTime();
		}

		private async Task ReceiveLoopAsync()
		{
			CancellationToken cancellationToken = cancellation.Token;
			ChannelCommonArguments args = new ChannelCommonArguments(Guid.Empty, TransportErrorCode.ReceiveTimeout, userPayload: true);
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					args.ActivityId = Guid.Empty;
					RntbdConstants.Response response = new RntbdConstants.Response();
					Connection.ResponseMetadata responseMetadata = await connection.ReadResponseMetadataAsync(args);
					byte[] metadata = responseMetadata.Metadata;
					TransportSerialization.RntbdHeader header = TransportSerialization.DecodeRntbdHeader(responseMetadata.Header);
					args.ActivityId = header.ActivityId;
					using (MemoryStream input = new MemoryStream(metadata))
					{
						using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8))
						{
							response.ParseFrom(reader);
						}
					}
					MemoryStream responseBody = null;
					if (response.payloadPresent.value.valueByte != 0)
					{
						responseBody = new MemoryStream(await connection.ReadResponseBodyAsync(args));
					}
					DispatchRntbdResponse(response, header, responseBody);
				}
				DispatchCancellation();
			}
			catch (OperationCanceledException)
			{
				DispatchCancellation();
			}
			catch (ObjectDisposedException)
			{
				DispatchCancellation();
			}
			catch (Exception inner)
			{
				DispatchChannelFailureException(inner);
			}
		}

		private Dictionary<uint, CallInfo> StopCalls()
		{
			lock (callLock)
			{
				Dictionary<uint, CallInfo> result = new Dictionary<uint, CallInfo>(calls);
				calls.Clear();
				callsAllowed = false;
				return result;
			}
		}

		private void DispatchRntbdResponse(RntbdConstants.Response rntbdResponse, TransportSerialization.RntbdHeader responseHeader, MemoryStream responseBody)
		{
			if (!rntbdResponse.transportRequestID.isPresent || rntbdResponse.transportRequestID.GetTokenType() != RntbdTokenTypes.ULong)
			{
				throw TransportExceptions.GetInternalServerErrorException(serverUri, RMResources.ServerResponseTransportRequestIdMissingError);
			}
			RemoveCall(rntbdResponse.transportRequestID.value.valueULong)?.SetResponse(rntbdResponse, responseHeader, responseBody, serverProperties.Version);
		}

		private void DispatchChannelFailureException(Exception inner)
		{
			foreach (KeyValuePair<uint, CallInfo> item in StopCalls())
			{
				item.Value.SetConnectionBrokenException(inner, ToString());
			}
		}

		private void DispatchCancellation()
		{
			foreach (KeyValuePair<uint, CallInfo> item in StopCalls())
			{
				item.Value.Cancel();
			}
		}

		private CallInfo RemoveCall(uint requestId)
		{
			CallInfo value = null;
			lock (callLock)
			{
				calls.TryGetValue(requestId, out value);
				calls.Remove(requestId);
				return value;
			}
		}

		private void DisallowInitialCalls()
		{
			lock (callLock)
			{
				callsAllowed = false;
			}
		}

		private void DisallowRuntimeCalls()
		{
			lock (callLock)
			{
				callsAllowed = false;
			}
		}
	}
}
