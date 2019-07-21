using Microsoft.Azure.Documents.Routing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Common
{
	internal interface ICollectionRoutingMapCache
	{
		Task<CollectionRoutingMap> TryLookupAsync(string collectionRid, CollectionRoutingMap previousValue, DocumentServiceRequest request, CancellationToken cancellationToken);
	}
}
