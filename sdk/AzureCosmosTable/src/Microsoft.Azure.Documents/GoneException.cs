using Microsoft.Azure.Documents.Collections;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	[Serializable]
	internal sealed class GoneException : DocumentClientException
	{
		internal string LocalIp
		{
			get;
			set;
		}

		/// <summary>
		///
		/// Summary:
		///     Gets a message that describes the current exception.
		///
		/// </summary>
		public override string Message
		{
			get
			{
				if (!string.IsNullOrEmpty(LocalIp))
				{
					return string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessageAddIpAddress, base.Message, LocalIp);
				}
				return base.Message;
			}
		}

		public GoneException()
			: this(RMResources.Gone)
		{
		}

		public GoneException(string message, Uri requestUri = null)
			: this(message, null, null, requestUri)
		{
		}

		public GoneException(string message, HttpResponseHeaders headers, Uri requestUri = null)
			: this(message, null, headers, requestUri)
		{
		}

		public GoneException(string message, Exception innerException, Uri requestUri = null, string localIpAddress = null)
			: this(message, innerException, null, requestUri)
		{
			LocalIp = localIpAddress;
		}

		public GoneException(Exception innerException)
			: this(RMResources.Gone, innerException, (HttpResponseHeaders)null, (Uri)null)
		{
		}

		public GoneException(string message, INameValueCollection headers, Uri requestUri = null)
			: base(message, null, headers, HttpStatusCode.Gone, requestUri)
		{
			SetDescription();
		}

		public GoneException(string message, Exception innerException, HttpResponseHeaders headers, Uri requestUri = null)
			: base(message, innerException, headers, HttpStatusCode.Gone, requestUri)
		{
			SetDescription();
		}

		private void SetDescription()
		{
			base.StatusDescription = "Gone";
		}
	}
}
