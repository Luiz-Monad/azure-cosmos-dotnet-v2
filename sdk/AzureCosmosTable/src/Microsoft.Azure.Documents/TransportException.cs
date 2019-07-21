using Microsoft.Azure.Documents.Rntbd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class TransportException : Exception
	{
		private static readonly Lazy<Dictionary<TransportErrorCode, string>> lazyMessageMap = new Lazy<Dictionary<TransportErrorCode, string>>(GetErrorTextMap, LazyThreadSafetyMode.ExecutionAndPublication);

		private static TransportExceptionCounters transportExceptionCounters = new TransportExceptionCounters();

		private readonly object mutex = new object();

		private CpuLoadHistory cpuHistory;

		public override string Message
		{
			get
			{
				Exception baseException = GetBaseException();
				SocketException ex = baseException as SocketException;
				string text;
				if (ex != null)
				{
					text = string.Format(CultureInfo.InvariantCulture, "socket error {0} [0x{1:X8}]", ex.SocketErrorCode, (int)ex.SocketErrorCode);
				}
				else
				{
					Win32Exception ex2 = baseException as Win32Exception;
					text = ((ex2 == null) ? string.Format(CultureInfo.InvariantCulture, "HRESULT 0x{0:X8}", baseException.HResult) : string.Format(CultureInfo.InvariantCulture, "Windows error 0x{0:X8}", ex2.NativeErrorCode));
				}
				string text2 = "not available";
				CpuLoadHistory cpuLoadHistory = CpuHistory;
				if (cpuLoadHistory != null)
				{
					text2 = cpuLoadHistory.ToString();
				}
				return string.Format(CultureInfo.InvariantCulture, "{0} (Time: {1:o}, activity ID: {2}, error code: {3} [0x{4:X4}], base error: {5}, URI: {6}, connection: {7}, payload sent: {8}, CPU history: {9}, CPU count: {10})", base.Message, Timestamp, ActivityId, ErrorCode, (int)ErrorCode, text, RequestUri, Source, UserRequestSent, text2, Environment.ProcessorCount);
			}
		}

		public DateTime Timestamp
		{
			get;
			private set;
		}

		public DateTime? RequestStartTime
		{
			get;
			set;
		}

		public DateTime? RequestEndTime
		{
			get;
			set;
		}

		public ResourceType ResourceType
		{
			get;
			set;
		}

		public OperationType OperationType
		{
			get;
			set;
		}

		public TransportErrorCode ErrorCode
		{
			get;
			private set;
		}

		public Guid ActivityId
		{
			get;
			private set;
		}

		public Uri RequestUri
		{
			get;
			private set;
		}

		public bool UserRequestSent
		{
			get;
			private set;
		}

		public bool IsClientCpuOverloaded => CpuHistory?.IsCpuOverloaded ?? false;

		private CpuLoadHistory CpuHistory
		{
			get
			{
				lock (mutex)
				{
					return cpuHistory;
				}
			}
		}

		public TransportException(TransportErrorCode errorCode, Exception innerException, Guid activityId, Uri requestUri, string sourceDescription, bool userPayload, bool payloadSent)
			: base(LoadMessage(errorCode), innerException)
		{
			Timestamp = DateTime.UtcNow;
			ErrorCode = errorCode;
			ActivityId = activityId;
			RequestUri = requestUri;
			Source = sourceDescription;
			UserRequestSent = IsUserRequestSent(errorCode, userPayload, payloadSent);
			UpdateCounters(requestUri, innerException);
		}

		public static bool IsTimeout(TransportErrorCode errorCode)
		{
			if (errorCode != TransportErrorCode.ChannelOpenTimeout && errorCode != TransportErrorCode.DnsResolutionTimeout && errorCode != TransportErrorCode.ConnectTimeout && errorCode != TransportErrorCode.SslNegotiationTimeout && errorCode != TransportErrorCode.TransportNegotiationTimeout && errorCode != TransportErrorCode.RequestTimeout && errorCode != TransportErrorCode.SendLockTimeout && errorCode != TransportErrorCode.SendTimeout)
			{
				return errorCode == TransportErrorCode.ReceiveTimeout;
			}
			return true;
		}

		internal void SetCpuLoad(CpuLoadHistory cpuHistory)
		{
			lock (mutex)
			{
				if (this.cpuHistory == null)
				{
					this.cpuHistory = cpuHistory;
				}
			}
		}

		private static bool IsUserRequestSent(TransportErrorCode errorCode, bool userPayload, bool payloadSent)
		{
			if (!userPayload)
			{
				return false;
			}
			if (!payloadSent)
			{
				return IsTimeout(errorCode);
			}
			return true;
		}

		private static string LoadMessage(TransportErrorCode errorCode)
		{
			return string.Format(CultureInfo.CurrentUICulture, RMResources.TransportExceptionMessage, GetErrorText(errorCode));
		}

		private static string GetErrorText(TransportErrorCode errorCode)
		{
			if (lazyMessageMap.Value.TryGetValue(errorCode, out string value))
			{
				return value;
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}", errorCode);
		}

		private static Dictionary<TransportErrorCode, string> GetErrorTextMap()
		{
			return new Dictionary<TransportErrorCode, string>
			{
				{
					TransportErrorCode.ChannelMultiplexerClosed,
					RMResources.ChannelMultiplexerClosedTransportError
				},
				{
					TransportErrorCode.ChannelOpenFailed,
					RMResources.ChannelOpenFailedTransportError
				},
				{
					TransportErrorCode.ChannelOpenTimeout,
					RMResources.ChannelOpenTimeoutTransportError
				},
				{
					TransportErrorCode.ConnectFailed,
					RMResources.ConnectFailedTransportError
				},
				{
					TransportErrorCode.ConnectTimeout,
					RMResources.ConnectTimeoutTransportError
				},
				{
					TransportErrorCode.ConnectionBroken,
					RMResources.ConnectionBrokenTransportError
				},
				{
					TransportErrorCode.DnsResolutionFailed,
					RMResources.DnsResolutionFailedTransportError
				},
				{
					TransportErrorCode.DnsResolutionTimeout,
					RMResources.DnsResolutionTimeoutTransportError
				},
				{
					TransportErrorCode.ReceiveFailed,
					RMResources.ReceiveFailedTransportError
				},
				{
					TransportErrorCode.ReceiveStreamClosed,
					RMResources.ReceiveStreamClosedTransportError
				},
				{
					TransportErrorCode.ReceiveTimeout,
					RMResources.ReceiveTimeoutTransportError
				},
				{
					TransportErrorCode.RequestTimeout,
					RMResources.RequestTimeoutTransportError
				},
				{
					TransportErrorCode.SendFailed,
					RMResources.SendFailedTransportError
				},
				{
					TransportErrorCode.SendLockTimeout,
					RMResources.SendLockTimeoutTransportError
				},
				{
					TransportErrorCode.SendTimeout,
					RMResources.SendTimeoutTransportError
				},
				{
					TransportErrorCode.SslNegotiationFailed,
					RMResources.SslNegotiationFailedTransportError
				},
				{
					TransportErrorCode.SslNegotiationTimeout,
					RMResources.SslNegotiationTimeoutTransportError
				},
				{
					TransportErrorCode.TransportNegotiationTimeout,
					RMResources.TransportNegotiationTimeoutTransportError
				},
				{
					TransportErrorCode.Unknown,
					RMResources.UnknownTransportError
				}
			};
		}

		private static void UpdateCounters(Uri requestUri, Exception innerException)
		{
			if (innerException == null || innerException is TransportException)
			{
				return;
			}
			innerException = innerException.GetBaseException();
			SocketException ex = innerException as SocketException;
			if (ex != null)
			{
				SocketError socketErrorCode = ex.SocketErrorCode;
				if (socketErrorCode == SocketError.NoBufferSpaceAvailable)
				{
					transportExceptionCounters.IncrementEphemeralPortExhaustion();
				}
				return;
			}
			Win32Exception ex2 = innerException as Win32Exception;
			if (ex2 != null)
			{
				int nativeErrorCode = ex2.NativeErrorCode;
				if (nativeErrorCode == -2146893008)
				{
					DefaultTrace.TraceWarning("Decryption failure. Exception text: {0}. Native error code: 0x{1:X8}. Remote endpoint: {2}", ex2.Message, ex2.NativeErrorCode, string.Format(CultureInfo.InvariantCulture, "{0}:{1}", requestUri.DnsSafeHost, requestUri.Port));
					transportExceptionCounters.IncrementDecryptionFailures();
				}
			}
		}

		internal static void SetCounters(TransportExceptionCounters transportExceptionCounters)
		{
			if (transportExceptionCounters == null)
			{
				throw new ArgumentNullException("transportExceptionCounters");
			}
			TransportException.transportExceptionCounters = transportExceptionCounters;
		}
	}
}
