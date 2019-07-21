namespace Microsoft.Azure.Documents
{
	internal interface IDocumentClientRetryPolicy : IRetryPolicy
	{
		/// <summary>
		/// Method that is called before a request is sent to allow the retry policy implementation
		/// to modify the state of the request.
		/// </summary>
		/// <param name="request">The request being sent to the service.</param>
		/// <remarks>
		/// Currently only read operations will invoke this method. There is no scenario for write
		/// operations to modify requests before retrying.
		/// </remarks>
		void OnBeforeSendRequest(DocumentServiceRequest request);
	}
}
