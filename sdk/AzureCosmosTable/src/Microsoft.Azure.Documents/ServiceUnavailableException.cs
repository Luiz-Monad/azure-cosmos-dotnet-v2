using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class ServiceUnavailableException : DocumentClientException
	{
		public ServiceUnavailableException()
			: this(RMResources.ServiceUnavailable)
		{
		}

		public ServiceUnavailableException(string message, SubStatusCodes subStatusCode)
			: base(message, HttpStatusCode.ServiceUnavailable, subStatusCode)
		{
		}

		public ServiceUnavailableException(string message, Uri requestUri = null)
			: this(message, null, null, requestUri)
		{
		}

		public ServiceUnavailableException(string message, Exception innerException, Uri requestUri = null)
			: this(message, innerException, null, requestUri)
		{
		}

		public ServiceUnavailableException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public ServiceUnavailableException(Exception innerException)
			: this(RMResources.ServiceUnavailable, innerException)
		{
		}

		public ServiceUnavailableException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.ServiceUnavailable, requestUri)
		{
			SetDescription();
		}

		public ServiceUnavailableException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.ServiceUnavailable, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Service Unavailable";
		}
	}
}
