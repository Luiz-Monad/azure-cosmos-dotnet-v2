using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal static class RequestHelper
	{
		public static ConsistencyLevel GetConsistencyLevelToUse(IServiceConfigurationReader serviceConfigReader, DocumentServiceRequest request)
		{
			ConsistencyLevel result = serviceConfigReader.DefaultConsistencyLevel;
			string text = request.Headers["x-ms-consistency-level"];
			if (!string.IsNullOrEmpty(text))
			{
				if (!Enum.TryParse(text, out ConsistencyLevel result2))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, "x-ms-consistency-level"));
				}
				result = result2;
			}
			return result;
		}
	}
}
