using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IDocumentClientInternal : IDocumentClient
	{
		Task<DatabaseAccount> GetDatabaseAccountInternalAsync(Uri serviceEndpoint, CancellationToken cancellationToken = default(CancellationToken));
	}
}
