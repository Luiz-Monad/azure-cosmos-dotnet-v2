using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.Documents
{
	internal sealed class AddressInformation
	{
		public bool IsPublic
		{
			get;
			set;
		}

		public bool IsPrimary
		{
			get;
			set;
		}

		public Protocol Protocol
		{
			get;
			set;
		}

		public string PhysicalUri
		{
			get;
			set;
		}
	}
}
