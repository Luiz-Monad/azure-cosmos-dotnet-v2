using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IStoreClient
	{
		Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, IRetryPolicy retryPolicy = null, Func<DocumentServiceRequest, Task> prepareRequestAsyncDelegate = null);
	}
}
