using System;
using System.Net.Http;

namespace Microsoft.Azure.Documents
{
	internal sealed class SendingRequestEventArgs : EventArgs
	{
		/// <summary>
		/// The HttpRequestMessage on which the SendingRequest event is raised.
		/// </summary>
		public HttpRequestMessage HttpRequest
		{
			get;
		}

		/// <summary>
		/// The DocumentServiceRequest on which the SendingRequest event is raised.
		/// </summary>
		public DocumentServiceRequest DocumentServiceRequest
		{
			get;
		}

		public SendingRequestEventArgs(DocumentServiceRequest request)
		{
			DocumentServiceRequest = request;
		}

		public SendingRequestEventArgs(HttpRequestMessage request)
		{
			HttpRequest = request;
		}

		/// <summary>
		/// Checks if the SendingRequestEventArgs has HttpRequestMessage as its member.
		/// </summary>
		/// <remarks>Used to check if the message is HttpRequestMessage or DocumentServiceRequestMessage.</remarks>
		/// <returns>true if the message is HttpRequestMessage. otherwise, returns false.</returns>
		public bool IsHttpRequest()
		{
			return HttpRequest != null;
		}
	}
}
