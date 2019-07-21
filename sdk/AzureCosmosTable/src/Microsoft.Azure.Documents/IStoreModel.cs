using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IStoreModel : IDisposable
	{
		Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, CancellationToken cancellationToken = default(CancellationToken));
	}
}
