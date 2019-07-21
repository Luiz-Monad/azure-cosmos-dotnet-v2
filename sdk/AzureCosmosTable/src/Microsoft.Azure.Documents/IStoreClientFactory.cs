using System;

namespace Microsoft.Azure.Documents
{
	internal interface IStoreClientFactory : IDisposable
	{
		StoreClient CreateStoreClient(IAddressResolver addressResolver, ISessionContainer sessionContainer, IServiceConfigurationReader serviceConfigurationReader, IAuthorizationTokenProvider authorizationTokenProvider, bool enableRequestDiagnostics = false, bool enableReadRequestsFallback = false, bool useFallbackClient = true, bool useMultipleWriteLocations = false, bool detectClientConnectivityIssues = false);
	}
}
