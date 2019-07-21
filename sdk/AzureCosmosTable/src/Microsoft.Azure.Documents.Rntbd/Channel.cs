using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class Channel : IChannel, IDisposable
	{
		private enum State
		{
			New,
			Opening,
			Open,
			Closed
		}

		private readonly Dispatcher dispatcher;

		private readonly TimerPool timerPool;

		private readonly int requestTimeoutSeconds;

		private readonly Uri serverUri;

		private bool disposed;

		private readonly ReaderWriterLockSlim stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		private State state;

		private Task initializationTask;

		private ChannelOpenArguments openArguments;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool Healthy
		{
			get
			{
				ThrowIfDisposed();
				Dispatcher dispatcher = null;
				stateLock.EnterReadLock();
				try
				{
					switch (state)
					{
					case State.Open:
						dispatcher = this.dispatcher;
						break;
					case State.Opening:
						return true;
					case State.Closed:
						return false;
					case State.New:
						return false;
					default:
						return false;
					}
				}
				finally
				{
					stateLock.ExitReadLock();
				}
				return dispatcher.Healthy;
			}
		}

		internal bool TestIsIdle => dispatcher.TestIsIdle;

		internal event Action TestOnInitializeComplete;

		internal event Action TestOnConnectionClosed
		{
			add
			{
				dispatcher.TestOnConnectionClosed += value;
			}
			remove
			{
				dispatcher.TestOnConnectionClosed -= value;
			}
		}

		public Channel(Guid activityId, Uri serverUri, ChannelProperties channelProperties)
		{
			dispatcher = new Dispatcher(serverUri, channelProperties.UserAgent, channelProperties.CertificateHostNameOverride, channelProperties.ReceiveHangDetectionTime, channelProperties.SendHangDetectionTime, channelProperties.IdleTimerPool, channelProperties.IdleTimeout);
			timerPool = channelProperties.RequestTimerPool;
			requestTimeoutSeconds = (int)channelProperties.RequestTimeout.TotalSeconds;
			this.serverUri = serverUri;
			openArguments = new ChannelOpenArguments(activityId, new ChannelOpenTimeline(), (int)channelProperties.OpenTimeout.TotalSeconds, channelProperties.PortReusePolicy, channelProperties.UserPortPool);
		}

		public void Initialize()
		{
			ThrowIfDisposed();
			stateLock.EnterWriteLock();
			try
			{
				state = State.Opening;
				initializationTask = Task.Run(async delegate
				{
					Trace.CorrelationManager.ActivityId = openArguments.CommonArguments.ActivityId;
					await InitializeAsync();
				});
			}
			finally
			{
				stateLock.ExitWriteLock();
			}
		}

		public async Task<StoreResponse> RequestAsync(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId)
		{
			ThrowIfDisposed();
			Task task = null;
			stateLock.EnterReadLock();
			try
			{
				if (state != State.Open)
				{
					task = initializationTask;
				}
			}
			finally
			{
				stateLock.ExitReadLock();
			}
			if (task != null)
			{
				DefaultTrace.TraceInformation("Awaiting RNTBD channel initialization. Request URI: {0}", physicalAddress);
				await task;
			}
			ChannelCallArguments callArguments = new ChannelCallArguments(activityId);
			try
			{
				callArguments.PreparedCall = dispatcher.PrepareCall(request, physicalAddress, resourceOperation, activityId);
			}
			catch (DocumentClientException ex)
			{
				ex.Headers.Add("x-ms-request-validation-failure", "1");
				throw;
			}
			catch (Exception ex2)
			{
				DefaultTrace.TraceError("Failed to serialize request. Assuming malformed request payload: {0}", ex2);
				throw new BadRequestException(ex2)
				{
					Headers = 
					{
						{
							"x-ms-request-validation-failure",
							"1"
						}
					}
				};
			}
			PooledTimer timer = timerPool.GetPooledTimer(requestTimeoutSeconds);
			Task[] tasks = new Task[2]
			{
				timer.StartTimerAsync(),
				null
			};
			Task<StoreResponse> dispatcherCall = dispatcher.CallAsync(callArguments);
			TransportPerformanceCounters transportPerformanceCounters = TransportClient.GetTransportPerformanceCounters();
			ResourceType resourceType = resourceOperation.resourceType;
			OperationType operationType = resourceOperation.operationType;
			Dispatcher.PrepareCallResult preparedCall = callArguments.PreparedCall;
			int? obj;
			if (preparedCall == null)
			{
				obj = null;
			}
			else
			{
				byte[] serializedRequest = preparedCall.SerializedRequest;
				obj = ((serializedRequest != null) ? new int?(serializedRequest.Length) : null);
			}
			transportPerformanceCounters.LogRntbdBytesSentCount(resourceType, operationType, obj);
			tasks[1] = dispatcherCall;
			Task task2 = await Task.WhenAny(tasks);
			if (task2 == tasks[0])
			{
				callArguments.CommonArguments.SnapshotCallState(out TransportErrorCode timeoutCode, out bool payloadSent);
				dispatcher.CancelCall(callArguments.PreparedCall);
				HandleTaskTimeout(tasks[1], activityId);
				Exception innerException = task2.Exception?.InnerException;
				DefaultTrace.TraceWarning("RNTBD call timed out on channel {0}. Error: {1}", this, timeoutCode);
				throw new TransportException(timeoutCode, innerException, activityId, physicalAddress, ToString(), callArguments.CommonArguments.UserPayload, payloadSent);
			}
			timer.CancelTimer();
			if (task2.IsFaulted)
			{
				await task2;
			}
			StoreResponse result = dispatcherCall.Result;
			TransportClient.GetTransportPerformanceCounters().LogRntbdBytesReceivedCount(resourceOperation.resourceType, resourceOperation.operationType, result?.ResponseBody?.Length);
			return result;
		}

		public override string ToString()
		{
			return dispatcher.ToString();
		}

		public void Close()
		{
			((IDisposable)this).Dispose();
		}

		void IDisposable.Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			DefaultTrace.TraceInformation("Disposing RNTBD Channel {0}", this);
			Task task = null;
			stateLock.EnterWriteLock();
			try
			{
				if (state != State.Closed)
				{
					task = initializationTask;
				}
				state = State.Closed;
			}
			finally
			{
				stateLock.ExitWriteLock();
			}
			if (task != null)
			{
				try
				{
					task.Wait();
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceWarning("{0} initialization failed. Consuming the task exception in {1}. Server URI: {2}. Exception: {3}", "Channel", "Dispose", serverUri, ex.Message);
				}
			}
			dispatcher.Dispose();
			stateLock.Dispose();
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("Channel");
			}
		}

		private async Task InitializeAsync()
		{
			try
			{
				PooledTimer timer = timerPool.GetPooledTimer(openArguments.OpenTimeoutSeconds);
				Task[] tasks = new Task[2]
				{
					timer.StartTimerAsync(),
					dispatcher.OpenAsync(openArguments)
				};
				Task task = await Task.WhenAny(tasks);
				if (task == tasks[0])
				{
					openArguments.CommonArguments.SnapshotCallState(out TransportErrorCode timeoutCode, out bool payloadSent);
					HandleTaskTimeout(tasks[1], openArguments.CommonArguments.ActivityId);
					Exception innerException = task.Exception?.InnerException;
					DefaultTrace.TraceWarning("RNTBD open timed out on channel {0}. Error: {1}", this, timeoutCode);
					throw new TransportException(timeoutCode, innerException, openArguments.CommonArguments.ActivityId, serverUri, ToString(), openArguments.CommonArguments.UserPayload, payloadSent);
				}
				timer.CancelTimer();
				if (task.IsFaulted)
				{
					await task;
				}
				FinishInitialization(State.Open);
			}
			catch (DocumentClientException ex)
			{
				FinishInitialization(State.Closed);
				ex.Headers.Set("x-ms-activity-id", openArguments.CommonArguments.ActivityId.ToString());
				DefaultTrace.TraceWarning("Channel.InitializeAsync failed. Channel: {0}. DocumentClientException: {1}", this, ex);
				throw;
			}
			catch (TransportException ex2)
			{
				FinishInitialization(State.Closed);
				DefaultTrace.TraceWarning("Channel.InitializeAsync failed. Channel: {0}. TransportException: {1}", this, ex2);
				throw;
			}
			catch (Exception ex3)
			{
				FinishInitialization(State.Closed);
				DefaultTrace.TraceWarning("Channel.InitializeAsync failed. Wrapping exception in TransportException. Channel: {0}. Inner exception: {1}", this, ex3);
				throw new TransportException(TransportErrorCode.ChannelOpenFailed, ex3, openArguments.CommonArguments.ActivityId, serverUri, ToString(), openArguments.CommonArguments.UserPayload, openArguments.CommonArguments.PayloadSent);
			}
			finally
			{
				openArguments.OpenTimeline.WriteTrace();
				openArguments = null;
			}
			this.TestOnInitializeComplete?.Invoke();
		}

		private void FinishInitialization(State nextState)
		{
			Task task = null;
			stateLock.EnterWriteLock();
			try
			{
				if (state != State.Closed)
				{
					state = nextState;
					task = initializationTask;
				}
			}
			finally
			{
				stateLock.ExitWriteLock();
			}
			if (nextState == State.Closed)
			{
				task?.ContinueWith(delegate(Task completedTask)
				{
					DefaultTrace.TraceWarning("{0} initialization failed. Consuming the task exception asynchronously. Server URI: {1}. Exception: {2}", "Channel", serverUri, completedTask.Exception.InnerException?.Message);
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
		}

		private static void HandleTaskTimeout(Task runawayTask, Guid activityId)
		{
			runawayTask.ContinueWith(delegate(Task task)
			{
				Trace.CorrelationManager.ActivityId = activityId;
				Exception innerException = task.Exception.InnerException;
				DefaultTrace.TraceInformation("Timed out task completed. Activity ID = {0}. HRESULT = {1:X}. Exception: {2}", activityId, innerException.HResult, innerException);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}
