using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class AddressSelector
	{
		private readonly IAddressResolver addressResolver;

		private readonly Protocol protocol;

		public AddressSelector(IAddressResolver addressResolver, Protocol protocol)
		{
			this.addressResolver = addressResolver;
			this.protocol = protocol;
		}

		public async Task<IReadOnlyList<Uri>> ResolveAllUriAsync(DocumentServiceRequest request, bool includePrimary, bool forceRefresh)
		{
			PerProtocolPartitionAddressInformation perProtocolPartitionAddressInformation = await ResolveAddressesAsync(request, forceRefresh);
			return includePrimary ? perProtocolPartitionAddressInformation.ReplicaUris : perProtocolPartitionAddressInformation.NonPrimaryReplicaUris;
		}

		public async Task<Uri> ResolvePrimaryUriAsync(DocumentServiceRequest request, bool forceAddressRefresh)
		{
			return (await ResolveAddressesAsync(request, forceAddressRefresh)).GetPrimaryUri(request);
		}

		public async Task<PerProtocolPartitionAddressInformation> ResolveAddressesAsync(DocumentServiceRequest request, bool forceAddressRefresh)
		{
			return (await addressResolver.ResolveAsync(request, forceAddressRefresh, CancellationToken.None)).Get(protocol);
		}
	}
}
