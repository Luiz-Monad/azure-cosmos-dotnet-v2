using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;

namespace Microsoft.Azure.Documents
{
	internal static class Helpers
	{
		internal static int ValidateNonNegativeInteger(string name, int value)
		{
			if (value < 0)
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.NegativeInteger, name));
			}
			return value;
		}

		internal static int ValidatePositiveInteger(string name, int value)
		{
			if (value <= 0)
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.PositiveInteger, name));
			}
			return value;
		}

		/// <summary>
		/// Gets the byte value for a header. If header not present, returns the defaultValue.
		/// </summary>
		/// <param name="headerValues"></param>
		/// <param name="headerName"></param>
		/// <param name="defaultValue">Pls do not set defaultValue to MinValue as MinValue carries valid meaning in some place</param>
		/// <returns></returns>
		public static byte GetHeaderValueByte(INameValueCollection headerValues, string headerName, byte defaultValue = byte.MaxValue)
		{
			byte result = defaultValue;
			string text = headerValues[headerName];
			if (!string.IsNullOrWhiteSpace(text) && !byte.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out result))
			{
				result = defaultValue;
			}
			return result;
		}

		public static string GetDateHeader(INameValueCollection headerValues)
		{
			if (headerValues == null)
			{
				return string.Empty;
			}
			string text = headerValues["x-ms-date"];
			if (string.IsNullOrEmpty(text))
			{
				text = headerValues["date"];
			}
			return text ?? string.Empty;
		}

		public static long GetHeaderValueLong(INameValueCollection headerValues, string headerName, long defaultValue = -1L)
		{
			long result = defaultValue;
			string text = headerValues[headerName];
			if (!string.IsNullOrEmpty(text) && !long.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
			{
				result = defaultValue;
			}
			return result;
		}

		public static double GetHeaderValueDouble(INameValueCollection headerValues, string headerName, double defaultValue = -1.0)
		{
			double result = defaultValue;
			string text = headerValues[headerName];
			if (!string.IsNullOrEmpty(text) && !double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				result = defaultValue;
			}
			return result;
		}

		internal static string[] ExtractValuesFromHTTPHeaders(HttpHeaders httpHeaders, string[] keys)
		{
			string[] array = Enumerable.Repeat("", keys.Length).ToArray();
			if (httpHeaders == null)
			{
				return array;
			}
			foreach (KeyValuePair<string, IEnumerable<string>> pair in httpHeaders)
			{
				int num = Array.FindIndex(keys, (string t) => t.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
				if (num >= 0 && pair.Value.Count() > 0)
				{
					array[num] = pair.Value.First();
				}
			}
			return array;
		}

		/// <summary>
		/// Helper method to set application specific user agent suffix for internal telemetry purposes
		/// </summary>
		/// <param name="appName"></param>
		/// <param name="appVersion"></param>
		/// <returns></returns>
		internal static string GetAppSpecificUserAgentSuffix(string appName, string appVersion)
		{
			if (string.IsNullOrEmpty(appName))
			{
				throw new ArgumentNullException("appName");
			}
			if (string.IsNullOrEmpty(appVersion))
			{
				throw new ArgumentNullException("appVersion");
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", appName, appVersion);
		}

		internal static void SetupJsonReader(JsonReader reader, JsonSerializerSettings serializerSettings)
		{
			if (serializerSettings != null)
			{
				if (serializerSettings.Culture != null)
				{
					reader.Culture = serializerSettings.Culture;
				}
				reader.DateTimeZoneHandling = serializerSettings.DateTimeZoneHandling;
				reader.DateParseHandling = serializerSettings.DateParseHandling;
				reader.FloatParseHandling = serializerSettings.FloatParseHandling;
				if (serializerSettings.MaxDepth.HasValue)
				{
					reader.MaxDepth = serializerSettings.MaxDepth;
				}
				if (serializerSettings.DateFormatString != null)
				{
					reader.DateFormatString = serializerSettings.DateFormatString;
				}
			}
		}

		internal static string GetScriptLogHeader(INameValueCollection headerValues)
		{
			string text = headerValues?["x-ms-documentdb-script-log-results"];
			if (!string.IsNullOrEmpty(text))
			{
				return Uri.UnescapeDataString(text);
			}
			return text;
		}

		internal static long ToUnixTime(DateTimeOffset dt)
		{
			return (long)(dt - new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0L))).TotalSeconds;
		}
	}
}
