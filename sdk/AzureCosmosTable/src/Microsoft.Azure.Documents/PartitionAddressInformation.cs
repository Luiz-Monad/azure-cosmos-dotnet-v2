using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	internal sealed class PartitionAddressInformation
	{
		private static readonly int AllProtocolsCount = Enum.GetNames(typeof(Protocol)).Length;

		private readonly PerProtocolPartitionAddressInformation[] perProtocolAddressInformation;

		public IReadOnlyList<AddressInformation> AllAddresses
		{
			get;
		}

		public PartitionAddressInformation(AddressInformation[] replicaAddresses)
		{
			if (replicaAddresses == null)
			{
				throw new ArgumentNullException("replicaAddresses");
			}
			AllAddresses = (AddressInformation[])replicaAddresses.Clone();
			perProtocolAddressInformation = new PerProtocolPartitionAddressInformation[AllProtocolsCount];
			Protocol[] array = (Protocol[])Enum.GetValues(typeof(Protocol));
			foreach (Protocol protocol in array)
			{
				perProtocolAddressInformation[(int)protocol] = new PerProtocolPartitionAddressInformation(protocol, AllAddresses);
			}
		}

		public Uri GetPrimaryUri(DocumentServiceRequest request, Protocol protocol)
		{
			return perProtocolAddressInformation[(int)protocol].GetPrimaryUri(request);
		}

		public PerProtocolPartitionAddressInformation Get(Protocol protocol)
		{
			return perProtocolAddressInformation[(int)protocol];
		}
	}
}
