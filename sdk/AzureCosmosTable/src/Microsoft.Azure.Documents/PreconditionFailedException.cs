using Microsoft.Azure.Documents.Collections;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class PreconditionFailedException : DocumentClientException
	{
		public PreconditionFailedException()
			: this(RMResources.PreconditionFailed)
		{
		}

		public PreconditionFailedException(string message, SubStatusCodes? substatusCode = default(SubStatusCodes?))
			: this(message, null, null, null, substatusCode)
		{
		}

		public PreconditionFailedException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public PreconditionFailedException(Exception innerException)
			: this(RMResources.PreconditionFailed, innerException, null)
		{
		}

		public PreconditionFailedException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public PreconditionFailedException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.PreconditionFailed, requestUri)
		{
			SetDescription();
		}

		public PreconditionFailedException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null, SubStatusCodes? substatusCode = default(SubStatusCodes?))
			: base(message, innerException, headers, HttpStatusCode.PreconditionFailed, requestUri, substatusCode)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Precondition Failed";
		}
	}
}
