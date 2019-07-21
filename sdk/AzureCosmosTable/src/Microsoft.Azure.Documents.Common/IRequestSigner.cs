using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Common
{
	internal interface IRequestSigner
	{
		Task SignRequestAsync(DocumentServiceRequest request, CancellationToken cancellationToken);
	}
}
