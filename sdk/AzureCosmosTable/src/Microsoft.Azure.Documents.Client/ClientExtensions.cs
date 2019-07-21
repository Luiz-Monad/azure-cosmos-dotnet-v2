using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Client
{
	internal static class ClientExtensions
	{
		public static async Task<HttpResponseMessage> GetAsync(this HttpClient client, Uri uri, INameValueCollection additionalHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
			{
				if (additionalHeaders != null)
				{
					foreach (string additionalHeader in additionalHeaders)
					{
						if (GatewayStoreClient.IsAllowedRequestHeader(additionalHeader))
						{
							requestMessage.Headers.TryAddWithoutValidation(additionalHeader, additionalHeaders[additionalHeader]);
						}
					}
				}
				return await client.SendHttpAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			}
		}

		public static Task<DocumentServiceResponse> ParseResponseAsync(HttpResponseMessage responseMessage, JsonSerializerSettings serializerSettings = null, DocumentServiceRequest request = null)
		{
			return GatewayStoreClient.ParseResponseAsync(responseMessage, serializerSettings, request);
		}

		public static async Task<DocumentServiceResponse> ParseMediaResponseAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (responseMessage.StatusCode < HttpStatusCode.BadRequest)
			{
				return new DocumentServiceResponse(headers: GatewayStoreClient.ExtractResponseHeaders(responseMessage), body: new MediaStream(responseMessage, await responseMessage.Content.ReadAsStreamAsync()), statusCode: responseMessage.StatusCode);
			}
			throw await GatewayStoreClient.CreateDocumentClientException(responseMessage);
		}
	}
}
