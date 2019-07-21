using System;
using System.Net;

namespace Microsoft.Azure.Documents
{
	internal static class WebExceptionUtility
	{
		public static bool IsWebExceptionRetriable(Exception ex)
		{
			for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
			{
				if (IsWebExceptionRetriableInternal(ex2))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsWebExceptionRetriableInternal(Exception ex)
		{
			WebException ex2 = ex as WebException;
			if (ex2 == null)
			{
				return false;
			}
			if (ex2.Status == WebExceptionStatus.ConnectFailure || ex2.Status == WebExceptionStatus.NameResolutionFailure || ex2.Status == WebExceptionStatus.ProxyNameResolutionFailure || ex2.Status == WebExceptionStatus.SecureChannelFailure || ex2.Status == WebExceptionStatus.TrustFailure)
			{
				return true;
			}
			return false;
		}
	}
}
