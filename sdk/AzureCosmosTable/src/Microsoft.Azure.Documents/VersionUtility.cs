using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal static class VersionUtility
	{
		private const string versionDateTimeFormat = "yyyy-MM-dd";

		internal static bool IsLaterThan(string compareVersion, string baseVersion)
		{
			if (!DateTime.TryParseExact(baseVersion, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidVersionFormat, "base", baseVersion));
			}
			if (!DateTime.TryParseExact(compareVersion, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result2))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidVersionFormat, "compare", compareVersion));
			}
			return result2.CompareTo(result) >= 0;
		}
	}
}
