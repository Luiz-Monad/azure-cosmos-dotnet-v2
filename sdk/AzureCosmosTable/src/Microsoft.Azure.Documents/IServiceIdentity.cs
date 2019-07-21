using System;

namespace Microsoft.Azure.Documents
{
	internal interface IServiceIdentity
	{
		string GetFederationId();

		Uri GetServiceUri();

		long GetPartitionKey();
	}
}
