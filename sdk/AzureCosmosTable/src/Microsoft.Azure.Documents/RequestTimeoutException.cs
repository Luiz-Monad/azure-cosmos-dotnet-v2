using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class RequestTimeoutException : DocumentClientException
	{
		/// <summary>
		///
		/// Summary:
		///     Gets a message that describes the current exception.
		///
		/// </summary>
		public override string Message
		{
			get
			{
				if (!string.IsNullOrEmpty(LocalIp))
				{
					return string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessageAddIpAddress, base.Message, LocalIp);
				}
				return base.Message;
			}
		}

		internal string LocalIp
		{
			get;
			set;
		}

		public RequestTimeoutException()
			: this(RMResources.RequestTimeout)
		{
		}

		public RequestTimeoutException(string message, Uri requestUri = null)
			: this(message, null, null, requestUri)
		{
		}

		public RequestTimeoutException(string message, Exception innerException, Uri requestUri = null)
			: this(message, innerException, null, requestUri)
		{
		}

		public RequestTimeoutException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public RequestTimeoutException(Exception innerException, Uri requestUri = null)
			: this(RMResources.RequestTimeout, innerException, requestUri)
		{
		}

		public RequestTimeoutException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.RequestTimeout, requestUri)
		{
			SetDescription();
		}

		public RequestTimeoutException(string message, Exception innerException, Uri requestUri = null, string localIpAddress = null)
			: this(message, innerException, null, requestUri)
		{
			LocalIp = localIpAddress;
		}

		public RequestTimeoutException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.RequestTimeout, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Request timed out";
		}
	}
}
