using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// This exception is thrown when DocumentServiceRequest contains x-ms-documentdb-partitionkeyrangeid
	/// header and such range id doesn't exist.
	/// No retries should be made in this case, as either split or merge might have happened and query/readfeed
	/// must take appropriate actions.
	/// </summary>
	[Serializable]
	internal sealed class PartitionKeyRangeGoneException : DocumentClientException
	{
		public PartitionKeyRangeGoneException()
			: this(RMResources.Gone)
		{
		}

		public PartitionKeyRangeGoneException(string message)
			: this(message, null, null, null)
		{
		}

		public PartitionKeyRangeGoneException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public PartitionKeyRangeGoneException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public PartitionKeyRangeGoneException(Exception innerException)
			: this(RMResources.Gone, innerException, null)
		{
		}

		public PartitionKeyRangeGoneException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Gone, requestUri)
		{
			SetSubstatus();
			SetDescription();
		}

		public PartitionKeyRangeGoneException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
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
			base.Headers["x-ms-substatus"] = 1002u.ToString(CultureInfo.InvariantCulture);
		}
	}
}
