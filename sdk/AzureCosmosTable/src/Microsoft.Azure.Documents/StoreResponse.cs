using System;
using System.Globalization;
using System.IO;
using System.Net;

namespace Microsoft.Azure.Documents
{
	internal sealed class StoreResponse : IRetriableResponse
	{
		private Lazy<SubStatusCodes> subStatusCode;

		public int Status
		{
			get;
			set;
		}

		public string[] ResponseHeaderNames
		{
			get;
			set;
		}

		public string[] ResponseHeaderValues
		{
			get;
			set;
		}

		public Stream ResponseBody
		{
			get;
			set;
		}

		public long LSN
		{
			get
			{
				long result = -1L;
				if (TryGetHeaderValue("lsn", out string value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
				{
					return result;
				}
				return -1L;
			}
		}

		public string PartitionKeyRangeId
		{
			get
			{
				if (TryGetHeaderValue("x-ms-documentdb-partitionkeyrangeid", out string value))
				{
					return value;
				}
				return null;
			}
		}

		public long CollectionPartitionIndex
		{
			get
			{
				long result = -1L;
				if (TryGetHeaderValue("collection-partition-index", out string value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
				{
					return result;
				}
				return -1L;
			}
		}

		public long CollectionServiceIndex
		{
			get
			{
				long result = -1L;
				if (TryGetHeaderValue("collection-service-index", out string value) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
				{
					return result;
				}
				return -1L;
			}
		}

		public string Continuation
		{
			get
			{
				if (TryGetHeaderValue("x-ms-continuation", out string value))
				{
					return value;
				}
				return null;
			}
		}

		public SubStatusCodes SubStatusCode => subStatusCode.Value;

		public HttpStatusCode StatusCode => (HttpStatusCode)Status;

		public StoreResponse()
		{
			subStatusCode = new Lazy<SubStatusCodes>(GetSubStatusCode);
		}

		public bool TryGetHeaderValue(string attribute, out string value, StringComparison comparator = StringComparison.OrdinalIgnoreCase)
		{
			int? attributePosition;
			return TryGetHeaderValue(attribute, out value, out attributePosition);
		}

		public void UpsertHeaderValue(string headerName, string headerValue)
		{
			if (TryGetHeaderValue(headerName, out string _, out int? attributePosition))
			{
				ResponseHeaderValues[attributePosition.Value] = headerValue;
				return;
			}
			string[] array = ResponseHeaderNames;
			Array.Resize(ref array, ResponseHeaderNames.Length + 1);
			array[array.Length - 1] = headerName;
			string[] array2 = ResponseHeaderValues;
			Array.Resize(ref array2, ResponseHeaderValues.Length + 1);
			array2[array2.Length - 1] = headerValue;
			ResponseHeaderNames = array;
			ResponseHeaderValues = array2;
		}

		private bool TryGetHeaderValue(string attribute, out string value, out int? attributePosition, StringComparison comparator = StringComparison.OrdinalIgnoreCase)
		{
			value = null;
			attributePosition = null;
			if (ResponseHeaderNames == null || ResponseHeaderValues == null || ResponseHeaderNames.Length != ResponseHeaderValues.Length)
			{
				return false;
			}
			for (int i = 0; i < ResponseHeaderNames.Length; i++)
			{
				if (string.Equals(ResponseHeaderNames[i], attribute, comparator))
				{
					value = ResponseHeaderValues[i];
					attributePosition = i;
					return true;
				}
			}
			return false;
		}

		private SubStatusCodes GetSubStatusCode()
		{
			SubStatusCodes result = SubStatusCodes.Unknown;
			if (TryGetHeaderValue("x-ms-substatus", out string value))
			{
				uint result2 = 0u;
				if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2))
				{
					result = (SubStatusCodes)result2;
				}
			}
			return result;
		}
	}
}
