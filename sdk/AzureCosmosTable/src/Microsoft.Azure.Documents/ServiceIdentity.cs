using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class ServiceIdentity : IServiceIdentity
	{
		public string FederationId
		{
			get;
			private set;
		}

		public Uri ServiceName
		{
			get;
			private set;
		}

		public bool IsMasterService
		{
			get;
			private set;
		}

		public string ApplicationName => ServiceName.AbsoluteUri.Substring(0, ServiceName.AbsoluteUri.LastIndexOf('/'));

		/// <summary>
		/// Needed for TestForWhiteListedPersistedTypes to succeed 
		/// </summary>
		private ServiceIdentity()
		{
		}

		public ServiceIdentity(string federationId, Uri serviceName, bool isMasterService)
		{
			FederationId = federationId;
			ServiceName = serviceName;
			IsMasterService = isMasterService;
		}

		public string GetFederationId()
		{
			return FederationId;
		}

		public Uri GetServiceUri()
		{
			return ServiceName;
		}

		public long GetPartitionKey()
		{
			return 0L;
		}

		public override bool Equals(object obj)
		{
			ServiceIdentity serviceIdentity = obj as ServiceIdentity;
			if (serviceIdentity != null && string.Compare(FederationId, serviceIdentity.FederationId, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return Uri.Compare(ServiceName, serviceIdentity.ServiceName, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((FederationId != null) ? FederationId.GetHashCode() : 0) ^ ((!(ServiceName == null)) ? ServiceName.GetHashCode() : 0);
		}

		public override string ToString()
		{
			return $"FederationId:{FederationId},ServiceName:{ServiceName},IsMasterService:{IsMasterService}";
		}
	}
}
