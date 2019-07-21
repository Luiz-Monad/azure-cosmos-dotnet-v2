namespace Microsoft.Azure.Documents
{
	internal static class DocumentServiceRequestExtensions
	{
		/// <summary>
		/// This method is used to define if a particular response to the current <see cref="T:Microsoft.Azure.Documents.DocumentServiceRequest" /> needs to be processed without exceptions based on the status code and substatus code.
		/// </summary>
		/// <param name="request">Current <see cref="T:Microsoft.Azure.Documents.DocumentServiceRequest" /> instance.</param>
		/// <param name="statusCode">Status code of the response.</param>
		/// <param name="subStatusCode"><see cref="T:Microsoft.Azure.Documents.SubStatusCodes" /> of the response of any.</param>
		/// <returns>Whether the response should be processed without exceptions (true) or not (false).</returns>
		public static bool IsValidStatusCodeForExceptionlessRetry(this DocumentServiceRequest request, int statusCode, SubStatusCodes subStatusCode = SubStatusCodes.Unknown)
		{
			if (request.UseStatusCodeForFailures && (statusCode == 412 || statusCode == 409 || (statusCode == 404 && subStatusCode != SubStatusCodes.PartitionKeyRangeGone)))
			{
				return true;
			}
			if (request.UseStatusCodeFor429 && statusCode == 429)
			{
				return true;
			}
			return false;
		}
	}
}
