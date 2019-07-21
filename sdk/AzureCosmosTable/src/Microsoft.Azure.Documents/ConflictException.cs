using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class ConflictException : DocumentClientException
	{
		public ConflictException()
			: this(RMResources.EntityAlreadyExists)
		{
		}

		public ConflictException(string message)
			: this(message, null, null, null, null)
		{
		}

		public ConflictException(string message, SubStatusCodes subStatusCode)
			: base(message, HttpStatusCode.Conflict, subStatusCode)
		{
		}

		public ConflictException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public ConflictException(Exception innerException)
			: this(RMResources.EntityAlreadyExists, innerException, null)
		{
		}

		public ConflictException(Exception innerException, SubStatusCodes subStatusCode)
			: this(RMResources.EntityAlreadyExists, innerException, null, null, subStatusCode)
		{
		}

		public ConflictException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public ConflictException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Conflict, requestUri)
		{
			SetDescription();
		}

		public ConflictException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null, SubStatusCodes? subStatusCode = default(SubStatusCodes?))
			: base(message, innerException, headers, HttpStatusCode.Conflict, requestUri, subStatusCode)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Conflict";
		}
	}
}
