using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// This exception is thrown when DocumentServiceRequest reaches partition which is being migrated
	/// and was made unavailable for reads/writes.
	///
	/// Gateway/SDK can transparently refresh routing map and retry after some delay.
	/// </summary>
	[Serializable]
	internal sealed class PartitionIsMigratingException : DocumentClientException
	{
		public PartitionIsMigratingException()
			: this(RMResources.Gone)
		{
		}

		public PartitionIsMigratingException(string message)
			: this(message, null, null, null)
		{
		}

		public PartitionIsMigratingException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public PartitionIsMigratingException(string message, Exception innerException)
			: this(message, innerException, null)
		{
		}

		public PartitionIsMigratingException(Exception innerException)
			: this(RMResources.Gone, innerException, null)
		{
		}

		public PartitionIsMigratingException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Gone, requestUri)
		{
			SetSubstatus();
			SetDescription();
		}

		public PartitionIsMigratingException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Gone, requestUri)
		{
			SetSubstatus();
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Partition is migrating";
		}

		private void SetSubstatus()
		{
			base.Headers["x-ms-substatus"] = 1008u.ToString(CultureInfo.InvariantCulture);
		}
	}
}
