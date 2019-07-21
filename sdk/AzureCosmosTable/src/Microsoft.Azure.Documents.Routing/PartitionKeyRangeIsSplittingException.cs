using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// This exception is thrown when DocumentServiceRequest reaches partition which is being split
	/// and was made unavailable for reads/writes.
	///
	/// Gateway/SDK can transparently refresh routing map and retry after some delay.
	/// </summary>
	[Serializable]
	internal sealed class PartitionKeyRangeIsSplittingException : DocumentClientException
	{
		public PartitionKeyRangeIsSplittingException()
			: this(RMResources.Gone)
		{
		}

		public PartitionKeyRangeIsSplittingException(string message)
			: this(message, null, null, null)
		{
		}

		public PartitionKeyRangeIsSplittingException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public PartitionKeyRangeIsSplittingException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public PartitionKeyRangeIsSplittingException(Exception innerException)
			: this(RMResources.Gone, innerException, null)
		{
		}

		public PartitionKeyRangeIsSplittingException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Gone, requestUri)
		{
			SetSubstatus();
			SetDescription();
		}

		public PartitionKeyRangeIsSplittingException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Gone, requestUri)
		{
			SetSubstatus();
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "InvalidPartition";
		}

		private void SetSubstatus()
		{
			base.Headers["x-ms-substatus"] = 1007u.ToString(CultureInfo.InvariantCulture);
		}
	}
}
