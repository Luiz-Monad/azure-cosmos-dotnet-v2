using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class UnauthorizedException : DocumentClientException
	{
		public UnauthorizedException()
			: this(RMResources.Unauthorized)
		{
		}

		public UnauthorizedException(string message)
			: this(message, null, null, null)
		{
		}

		public UnauthorizedException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public UnauthorizedException(Exception innerException)
			: this(RMResources.Unauthorized, innerException, null)
		{
		}

		public UnauthorizedException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Unauthorized, requestUri)
		{
			SetDescription();
		}

		public UnauthorizedException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Unauthorized, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Unauthorized";
		}
	}
}
