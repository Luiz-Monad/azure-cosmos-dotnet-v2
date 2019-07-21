using Microsoft.Azure.Documents.Collections;

namespace Microsoft.Azure.Documents
{
	internal class NullSessionContainer : ISessionContainer
	{
		public string ResolveGlobalSessionToken(DocumentServiceRequest entity)
		{
			return string.Empty;
		}

		public ISessionToken ResolvePartitionLocalSessionToken(DocumentServiceRequest entity, string partitionKeyRangeId)
		{
			return null;
		}

		public void ClearTokenByResourceId(string resourceId)
		{
		}

		public void ClearTokenByCollectionFullname(string collectionFullname)
		{
		}

		public void SetSessionToken(DocumentServiceRequest request, INameValueCollection header)
		{
		}

		public void SetSessionToken(string collectionRid, string collectionFullname, INameValueCollection responseHeaders)
		{
		}
	}
}
