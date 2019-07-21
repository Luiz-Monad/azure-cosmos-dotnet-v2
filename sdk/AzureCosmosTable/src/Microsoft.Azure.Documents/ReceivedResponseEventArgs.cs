using System;
using System.Net.Http;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Event arguments on events raised after DocumentServiceResponse/HttpResponseMessage is received on ServerStoreModel or HttpRequestMessageHandler.
	/// </summary>
	internal sealed class ReceivedResponseEventArgs : EventArgs
	{
		/// <summary>
		/// The DocumentServiceResponse on which the RecievedResponse event is raised.
		/// </summary>
		public DocumentServiceResponse DocumentServiceResponse
		{
			get;
		}

		/// <summary>
		/// The HttpResponseMessage on which the RecievedResponse event is raised.
		/// </summary>
		public HttpResponseMessage HttpResponse
		{
			get;
		}

		/// <summary>
		/// The HttpRequestMessage on which corresponds to the response.
		/// </summary>
		public HttpRequestMessage HttpRequest
		{
			get;
		}

		/// <summary>
		/// The DocumentServiceRequest which yielded the response.
		/// </summary>
		public DocumentServiceRequest DocumentServiceRequest
		{
			get;
		}

		public ReceivedResponseEventArgs(DocumentServiceRequest request, DocumentServiceResponse response)
		{
			DocumentServiceResponse = response;
			DocumentServiceRequest = request;
		}

		public ReceivedResponseEventArgs(HttpRequestMessage request, HttpResponseMessage response)
		{
			HttpResponse = response;
			HttpRequest = request;
		}

		/// <summary>
		/// Checks if the SendingRequestEventArgs has HttpRespoonseMessage as its member.
		/// </summary>
		/// <remarks>Used to check if the message is HttpRespoonseMessage or DocumentServiceRequestMessage.</remarks>
		/// <returns>true if the message is HttpRespoonseMessage. otherwise, returns false.</returns>
		public bool IsHttpResponse()
		{
			return HttpResponse != null;
		}
	}
}
