using Microsoft.Azure.Documents.Routing;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Represents the retry policy configuration assocated with a DocumentClient instance.
	/// </summary>
	internal sealed class RetryPolicy : IRetryPolicyFactory
	{
		private readonly GlobalEndpointManager globalEndpointManager;

		private readonly bool enableEndpointDiscovery;

		private readonly RetryOptions retryOptions;

		/// <summary>
		/// Initialize the instance of the RetryPolicy class
		/// </summary>
		public RetryPolicy(GlobalEndpointManager globalEndpointManager, ConnectionPolicy connectionPolicy)
		{
			enableEndpointDiscovery = connectionPolicy.EnableEndpointDiscovery;
			this.globalEndpointManager = globalEndpointManager;
			retryOptions = connectionPolicy.RetryOptions;
		}

		/// <summary>
		/// Creates a new instance of the ClientRetryPolicy class retrying request failures.
		/// </summary>
		public IDocumentClientRetryPolicy GetRequestPolicy()
		{
			return new ClientRetryPolicy(globalEndpointManager, enableEndpointDiscovery, retryOptions);
		}
	}
}
