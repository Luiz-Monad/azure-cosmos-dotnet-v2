using System;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	internal interface ICommunicationEventSource
	{
		void Request(Guid activityId, Guid localId, string uri, string resourceType, HttpRequestHeaders requestHeaders);

		void Response(Guid activityId, Guid localId, short statusCode, double milliseconds, HttpResponseHeaders responseHeaders);
	}
}
