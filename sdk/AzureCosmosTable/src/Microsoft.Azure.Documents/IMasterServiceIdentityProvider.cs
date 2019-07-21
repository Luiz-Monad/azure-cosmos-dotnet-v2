using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IMasterServiceIdentityProvider
	{
		ServiceIdentity MasterServiceIdentity
		{
			get;
		}

		Task RefreshAsync(ServiceIdentity previousMasterService, CancellationToken cancellationToken);
	}
}
