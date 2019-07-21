using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Interface for resolving the service settings.
	/// Implementations:
	/// NamingServiceConfigurationReader : Multi tenant Cloud implementation.
	/// GatewayServiceConfigurationReader: Client implementation.
	/// </summary>
	internal interface IServiceConfigurationReader
	{
		string DatabaseAccountId
		{
			get;
		}

		Uri DatabaseAccountApiEndpoint
		{
			get;
		}

		ReplicationPolicy UserReplicationPolicy
		{
			get;
		}

		ReplicationPolicy SystemReplicationPolicy
		{
			get;
		}

		ConsistencyLevel DefaultConsistencyLevel
		{
			get;
		}

		ReadPolicy ReadPolicy
		{
			get;
		}

		string PrimaryMasterKey
		{
			get;
		}

		string SecondaryMasterKey
		{
			get;
		}

		string PrimaryReadonlyMasterKey
		{
			get;
		}

		string SecondaryReadonlyMasterKey
		{
			get;
		}

		string ResourceSeedKey
		{
			get;
		}

		bool EnableAuthorization
		{
			get;
		}

		string SubscriptionId
		{
			get;
		}

		Task InitializeAsync();
	}
}
