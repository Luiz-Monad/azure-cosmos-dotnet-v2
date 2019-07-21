using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class ForbiddenException : DocumentClientException
	{
		public ForbiddenException()
			: this(RMResources.Forbidden)
		{
		}

		public ForbiddenException(string message)
			: this(message, null, null, null)
		{
		}

		public ForbiddenException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public ForbiddenException(Exception innerException)
			: this(RMResources.Forbidden, innerException, null)
		{
		}

		public ForbiddenException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public ForbiddenException(string message, SubStatusCodes subStatusCode)
			: base(message, HttpStatusCode.Forbidden, subStatusCode)
		{
			SetDescription();
		}

		public ForbiddenException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Forbidden, requestUri)
		{
			SetDescription();
		}

		public ForbiddenException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Forbidden, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Forbidden";
		}
	}
}
