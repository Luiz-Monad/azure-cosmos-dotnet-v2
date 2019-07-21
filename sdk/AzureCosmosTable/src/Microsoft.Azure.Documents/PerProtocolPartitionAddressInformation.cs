using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	internal sealed class PerProtocolPartitionAddressInformation
	{
		public Protocol Protocol
		{
			get;
		}

		public IReadOnlyList<Uri> NonPrimaryReplicaUris
		{
			get;
		}

		public IReadOnlyList<Uri> ReplicaUris
		{
			get;
		}

		public Uri PrimaryReplicaUri
		{
			get;
		}

		public IReadOnlyList<AddressInformation> ReplicaAddresses
		{
			get;
		}

		public PerProtocolPartitionAddressInformation(Protocol protocol, IReadOnlyList<AddressInformation> replicaAddresses)
		{
			if (replicaAddresses == null)
			{
				throw new ArgumentNullException("replicaAddresses");
			}
			IEnumerable<AddressInformation> source = replicaAddresses.Where(delegate(AddressInformation address)
			{
				if (!string.IsNullOrEmpty(address.PhysicalUri))
				{
					return address.Protocol == protocol;
				}
				return false;
			});
			IEnumerable<AddressInformation> source2 = from address in source
			where !address.IsPublic
			select address;
			ReplicaAddresses = (source2.Any() ? source2.ToArray() : (from address in source
			where address.IsPublic
			select address).ToArray());
			ReplicaUris = (from e in ReplicaAddresses
			select new Uri(e.PhysicalUri)).ToArray();
			NonPrimaryReplicaUris = (from e in ReplicaAddresses
			where !e.IsPrimary
			select new Uri(e.PhysicalUri)).ToArray();
			AddressInformation addressInformation = ReplicaAddresses.SingleOrDefault(delegate(AddressInformation address)
			{
				if (address.IsPrimary)
				{
					return !Enumerable.Contains(address.PhysicalUri, '[');
				}
				return false;
			});
			if (addressInformation != null)
			{
				PrimaryReplicaUri = new Uri(addressInformation.PhysicalUri);
			}
			Protocol = protocol;
		}

		public Uri GetPrimaryUri(DocumentServiceRequest request)
		{
			Uri uri = null;
			if (!request.DefaultReplicaIndex.HasValue || request.DefaultReplicaIndex.Value == 0)
			{
				uri = PrimaryReplicaUri;
			}
			else if (request.DefaultReplicaIndex.Value != 0 && request.DefaultReplicaIndex.Value < ReplicaUris.Count)
			{
				uri = ReplicaUris[(int)request.DefaultReplicaIndex.Value];
			}
			if (uri == null)
			{
				throw new GoneException(string.Format(CultureInfo.CurrentUICulture, "The requested resource is no longer available at the server. Returned addresses are {0}", string.Join(",", (from address in ReplicaAddresses
				select address.PhysicalUri).ToList())));
			}
			return uri;
		}
	}
}
