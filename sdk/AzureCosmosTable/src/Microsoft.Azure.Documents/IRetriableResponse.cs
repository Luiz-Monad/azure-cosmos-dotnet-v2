using System.Net;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Service response that can be evaluated through an IRequestRetryPolicy and <see cref="T:Microsoft.Azure.Documents.RequestRetryUtility" />.
	/// </summary>
	internal interface IRetriableResponse
	{
		/// <summary>
		/// <see cref="T:System.Net.HttpStatusCode" /> in the service response.
		/// </summary>
		HttpStatusCode StatusCode
		{
			get;
		}

		/// <summary>
		/// <see cref="T:Microsoft.Azure.Documents.SubStatusCodes" /> in the service response.
		/// </summary>
		SubStatusCodes SubStatusCode
		{
			get;
		}
	}
}
