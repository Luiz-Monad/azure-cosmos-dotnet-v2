using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class RetryWithException : DocumentClientException
	{
		public RetryWithException(string retryMessage)
			: this(retryMessage, (INameValueCollection)null, (Uri)null)
		{
		}

		public RetryWithException(Exception innerException)
			: base(RMResources.RetryWith, innerException, (HttpResponseHeaders)null, (HttpStatusCode?)(HttpStatusCode)449, (Uri)null, (SubStatusCodes?)null)
		{
		}

		public RetryWithException(string retryMessage, HttpResponseHeaders headers, Uri requestUri = null)
			: base(retryMessage, null, headers, (HttpStatusCode)449, requestUri)
		{
			SetDescription();
		}

		public RetryWithException(string retryMessage, INameValueCollection headers, Uri requestUri = null)
			: base(retryMessage, null, headers, (HttpStatusCode)449, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Retry the request";
		}
	}
}
