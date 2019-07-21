using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal static class HttpClientExtension
	{
		internal static void AddUserAgentHeader(this HttpClient httpClient, UserAgentContainer userAgent)
		{
			httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent.UserAgent);
		}

		internal static void AddApiTypeHeader(this HttpClient httpClient, ApiType apitype)
		{
			if (!apitype.Equals(ApiType.None))
			{
				httpClient.DefaultRequestHeaders.Add("x-ms-cosmos-apitype", apitype.ToString());
			}
		}

		internal static Task<HttpResponseMessage> SendHttpAsync(this HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				return httpClient.SendAsync(requestMessage, cancellationToken);
			}
			catch (HttpRequestException innerException)
			{
				throw new ServiceUnavailableException(innerException);
			}
		}

		internal static Task<HttpResponseMessage> SendHttpAsync(this HttpClient httpClient, HttpRequestMessage requestMessage, HttpCompletionOption options, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				return httpClient.SendAsync(requestMessage, options, cancellationToken);
			}
			catch (HttpRequestException innerException)
			{
				throw new ServiceUnavailableException(innerException);
			}
		}

		internal static Task<HttpResponseMessage> GetHttpAsync(this HttpClient httpClient, Uri serviceEndpoint, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				return httpClient.GetAsync(serviceEndpoint, cancellationToken);
			}
			catch (HttpRequestException innerException)
			{
				throw new ServiceUnavailableException(innerException);
			}
		}
	}
}
