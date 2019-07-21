using Microsoft.Azure.Documents.Collections;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IAuthorizationTokenProvider
	{
		string GetUserAuthorizationToken(string resourceAddress, string resourceType, string requestVerb, INameValueCollection headers, AuthorizationTokenType tokenType);

		Task<string> GetSystemAuthorizationTokenAsync(string federationName, string resourceAddress, string resourceType, string requestVerb, INameValueCollection headers, AuthorizationTokenType tokenType);
	}
}
