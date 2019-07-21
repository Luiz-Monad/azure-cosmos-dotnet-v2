using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class NotFoundException : DocumentClientException
	{
		public NotFoundException()
			: this(RMResources.NotFound)
		{
		}

		public NotFoundException(string message)
			: this(message, null, null, null, null)
		{
		}

		public NotFoundException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public NotFoundException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public NotFoundException(Exception innerException)
			: this(RMResources.NotFound, innerException, null)
		{
		}

		public NotFoundException(Exception innerException, SubStatusCodes subStatusCode)
			: this(RMResources.NotFound, innerException, null, null, subStatusCode)
		{
		}

		public NotFoundException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.NotFound, requestUri)
		{
			SetDescription();
		}

		public NotFoundException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null, SubStatusCodes? subStatusCode = default(SubStatusCodes?))
			: base(message, innerException, headers, HttpStatusCode.NotFound, requestUri, subStatusCode)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Not Found";
		}
	}
}
