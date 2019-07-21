using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelCommonArguments
	{
		private readonly object mutex = new object();

		private TransportErrorCode timeoutCode;

		private bool payloadSent;

		public Guid ActivityId
		{
			get;
			set;
		}

		public bool UserPayload
		{
			get;
			private set;
		}

		public bool PayloadSent
		{
			get
			{
				lock (mutex)
				{
					return payloadSent;
				}
			}
		}

		public ChannelCommonArguments(Guid activityId, TransportErrorCode initialTimeoutCode, bool userPayload)
		{
			ActivityId = activityId;
			UserPayload = userPayload;
			SetTimeoutCode(initialTimeoutCode);
		}

		public void SnapshotCallState(out TransportErrorCode timeoutCode, out bool payloadSent)
		{
			lock (mutex)
			{
				timeoutCode = this.timeoutCode;
				payloadSent = this.payloadSent;
			}
		}

		public void SetTimeoutCode(TransportErrorCode errorCode)
		{
			if (!TransportException.IsTimeout(errorCode))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is not a timeout error code", errorCode), "errorCode");
			}
			lock (mutex)
			{
				timeoutCode = errorCode;
			}
		}

		public void SetPayloadSent()
		{
			lock (mutex)
			{
				if (payloadSent)
				{
					throw new InvalidOperationException("TransportException.SetPayloadSent cannot be called more than once.");
				}
				payloadSent = true;
			}
		}
	}
}
