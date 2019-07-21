using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal static class TransportExceptions
	{
		internal static GoneException GetGoneException(Uri targetAddress, Guid activityId, Exception inner = null)
		{
			Trace.CorrelationManager.ActivityId = activityId;
			GoneException ex = (inner == null) ? ((!RntbdConnection.AddSourceIpAddressInNetworkExceptionMessage) ? new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), inner, targetAddress) : new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), inner, targetAddress, RntbdConnection.LocalIpv4Address)) : ((!RntbdConnection.AddSourceIpAddressInNetworkExceptionMessage) ? new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), inner, targetAddress) : new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.Gone), inner, targetAddress, RntbdConnection.LocalIpv4Address));
			ex.Headers.Set("x-ms-activity-id", activityId.ToString());
			return ex;
		}

		internal static RequestTimeoutException GetRequestTimeoutException(Uri targetAddress, Guid activityId, Exception inner = null)
		{
			Trace.CorrelationManager.ActivityId = activityId;
			RequestTimeoutException ex = (inner == null) ? ((!RntbdConnection.AddSourceIpAddressInNetworkExceptionMessage) ? new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.RequestTimeout), inner, targetAddress) : new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.RequestTimeout), inner, targetAddress, RntbdConnection.LocalIpv4Address)) : ((!RntbdConnection.AddSourceIpAddressInNetworkExceptionMessage) ? new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.RequestTimeout), inner, targetAddress) : new RequestTimeoutException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.RequestTimeout), inner, targetAddress, RntbdConnection.LocalIpv4Address));
			ex.Headers.Add("x-ms-request-validation-failure", "1");
			return ex;
		}

		internal static ServiceUnavailableException GetServiceUnavailableException(Uri targetAddress, Guid activityId, Exception inner = null)
		{
			Trace.CorrelationManager.ActivityId = activityId;
			ServiceUnavailableException ex = (inner != null) ? new ServiceUnavailableException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.ChannelClosed), inner, targetAddress) : new ServiceUnavailableException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.ChannelClosed), targetAddress);
			ex.Headers.Add("x-ms-request-validation-failure", "1");
			return ex;
		}

		internal static InternalServerErrorException GetInternalServerErrorException(Uri targetAddress, Guid activityId, Exception inner = null)
		{
			Trace.CorrelationManager.ActivityId = activityId;
			InternalServerErrorException ex = (inner != null) ? new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.ChannelClosed), inner, targetAddress) : new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, RMResources.ChannelClosed), targetAddress);
			ex.Headers.Add("x-ms-request-validation-failure", "1");
			return ex;
		}

		internal static InternalServerErrorException GetInternalServerErrorException(Uri targetAddress, string exceptionMessage)
		{
			return new InternalServerErrorException(string.Format(CultureInfo.CurrentUICulture, RMResources.ExceptionMessage, exceptionMessage), targetAddress)
			{
				Headers = 
				{
					{
						"x-ms-request-validation-failure",
						"1"
					}
				}
			};
		}
	}
}
