using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IAddressResolver
	{
		Task<PartitionAddressInformation> ResolveAsync(DocumentServiceRequest request, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken);
	}
}
