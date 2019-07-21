using Microsoft.Azure.Documents.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal class NullRequestSigner : IRequestSigner
	{
		public Task SignRequestAsync(DocumentServiceRequest request, CancellationToken cancellationToken)
		{
			return Task.FromResult(0);
		}
	}
}
