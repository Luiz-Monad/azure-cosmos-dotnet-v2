using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class MethodNotAllowedException : DocumentClientException
	{
		public MethodNotAllowedException()
			: this(RMResources.MethodNotAllowed)
		{
		}

		public MethodNotAllowedException(string message)
			: this(message, null, null, null)
		{
		}

		public MethodNotAllowedException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public MethodNotAllowedException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.MethodNotAllowed, requestUri)
		{
			SetDescription();
		}

		public MethodNotAllowedException(string message, Exception innerException)
			: base(message, innerException, HttpStatusCode.MethodNotAllowed)
		{
			SetDescription();
		}

		public MethodNotAllowedException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.MethodNotAllowed, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "MethodNotAllowed";
		}
	}
}
