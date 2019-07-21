using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class InvalidPartitionException : DocumentClientException
	{
		public InvalidPartitionException()
			: this(RMResources.Gone)
		{
		}

		public InvalidPartitionException(string message)
			: this(message, null, null, null)
		{
		}

		public InvalidPartitionException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public InvalidPartitionException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public InvalidPartitionException(Exception innerException)
			: this(RMResources.Gone, innerException, null)
		{
		}

		public InvalidPartitionException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Gone, requestUri)
		{
			SetDescription();
			SetSubStatus();
		}

		public InvalidPartitionException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Gone, requestUri)
		{
			SetDescription();
			SetSubStatus();
		}

		private void SetSubStatus()
		{
			base.Headers["x-ms-substatus"] = 1000u.ToString(CultureInfo.InvariantCulture);
		}

		private void SetDescription()
		{
			base.StatusDescription = "InvalidPartition";
		}
	}
}
