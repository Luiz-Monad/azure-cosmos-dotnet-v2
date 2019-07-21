namespace Microsoft.Azure.Documents
{
	internal interface IRetryPolicyFactory
	{
		/// <summary>
		/// Method that is called to get the retry policy for a non-query request.
		/// </summary>
		IDocumentClientRetryPolicy GetRequestPolicy();
	}
}
