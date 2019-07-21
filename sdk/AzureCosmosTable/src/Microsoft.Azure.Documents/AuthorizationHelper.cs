using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class AuthorizationHelper
	{
		public const int MaxAuthorizationHeaderSize = 1024;

		public const int DefaultAllowedClockSkewInSeconds = 900;

		public const int DefaultMasterTokenExpiryInSeconds = 900;

		public static string GenerateGatewayAuthSignatureWithAddressResolution(string verb, Uri uri, INameValueCollection headers, IComputeHash stringHMACSHA256Helper, string clientVersion = "")
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (uri.AbsolutePath.Equals("//addresses/", StringComparison.OrdinalIgnoreCase))
			{
				uri = GenerateUriFromAddressRequestUri(uri);
			}
			return GenerateKeyAuthorizationSignature(verb, uri, headers, stringHMACSHA256Helper, clientVersion);
		}

		public static string GenerateKeyAuthorizationSignature(string verb, Uri uri, INameValueCollection headers, string key, string clientVersion = "")
		{
			if (string.IsNullOrEmpty(verb))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "verb");
			}
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "key");
			}
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			string resourceType = string.Empty;
			string resourceId = string.Empty;
			bool isNameBased = false;
			GetResourceTypeAndIdOrFullName(uri, out isNameBased, out resourceType, out resourceId, clientVersion);
			return GenerateKeyAuthorizationSignature(verb, resourceId, resourceType, headers, key);
		}

		public static string GenerateKeyAuthorizationSignature(string verb, Uri uri, INameValueCollection headers, IComputeHash stringHMACSHA256Helper, string clientVersion = "")
		{
			if (string.IsNullOrEmpty(verb))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "verb");
			}
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (stringHMACSHA256Helper == null)
			{
				throw new ArgumentNullException("stringHMACSHA256Helper");
			}
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			string resourceType = string.Empty;
			string resourceId = string.Empty;
			bool isNameBased = false;
			GetResourceTypeAndIdOrFullName(uri, out isNameBased, out resourceType, out resourceId, clientVersion);
			return GenerateKeyAuthorizationSignature(verb, resourceId, resourceType, headers, stringHMACSHA256Helper);
		}

		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "HTTP Headers are ASCII")]
		public static string GenerateKeyAuthorizationSignature(string verb, string resourceId, string resourceType, INameValueCollection headers, string key, bool bUseUtcNowForMissingXDate = false)
		{
			if (string.IsNullOrEmpty(verb))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "verb");
			}
			if (resourceType == null)
			{
				throw new ArgumentNullException("resourceType");
			}
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "key");
			}
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			using (HMACSHA256 hMACSHA = new HMACSHA256(Convert.FromBase64String(key)))
			{
				string verb2 = verb ?? string.Empty;
				string resourceIdOrFullName = resourceId ?? string.Empty;
				string resourceType2 = resourceType ?? string.Empty;
				string authorizationResourceIdOrFullName = GetAuthorizationResourceIdOrFullName(resourceType2, resourceIdOrFullName);
				string s = GenerateMessagePayload(verb2, authorizationResourceIdOrFullName, resourceType2, headers, bUseUtcNowForMissingXDate);
				string arg = Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(s)));
				return HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "type={0}&ver={1}&sig={2}", "master", "1.0", arg));
			}
		}

		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "HTTP Headers are ASCII")]
		public static string GenerateKeyAuthorizationSignature(string verb, string resourceId, string resourceType, INameValueCollection headers, IComputeHash stringHMACSHA256Helper)
		{
			if (string.IsNullOrEmpty(verb))
			{
				throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, "verb");
			}
			if (resourceType == null)
			{
				throw new ArgumentNullException("resourceType");
			}
			if (stringHMACSHA256Helper == null)
			{
				throw new ArgumentNullException("stringHMACSHA256Helper");
			}
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			string verb2 = verb ?? string.Empty;
			string resourceIdOrFullName = resourceId ?? string.Empty;
			string resourceType2 = resourceType ?? string.Empty;
			string authorizationResourceIdOrFullName = GetAuthorizationResourceIdOrFullName(resourceType2, resourceIdOrFullName);
			string s = GenerateMessagePayload(verb2, authorizationResourceIdOrFullName, resourceType2, headers);
			string arg = Convert.ToBase64String(stringHMACSHA256Helper.ComputeHash(Encoding.UTF8.GetBytes(s)));
			return HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "type={0}&ver={1}&sig={2}", "master", "1.0", arg));
		}

		public static void ParseAuthorizationToken(string authorizationToken, out string typeOutput, out string versionOutput, out string tokenOutput)
		{
			typeOutput = null;
			versionOutput = null;
			tokenOutput = null;
			if (string.IsNullOrEmpty(authorizationToken))
			{
				DefaultTrace.TraceError("Auth token missing");
				throw new UnauthorizedException(RMResources.MissingAuthHeader);
			}
			if (authorizationToken.Length > 1024)
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			authorizationToken = HttpUtility.UrlDecode(authorizationToken);
			int num = authorizationToken.IndexOf('&');
			if (num == -1)
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			string text = authorizationToken.Substring(0, num);
			authorizationToken = authorizationToken.Substring(num + 1, authorizationToken.Length - num - 1);
			int num2 = authorizationToken.IndexOf('&');
			if (num2 == -1)
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			string text2 = authorizationToken.Substring(0, num2);
			authorizationToken = authorizationToken.Substring(num2 + 1, authorizationToken.Length - num2 - 1);
			string text3 = authorizationToken;
			int num3 = authorizationToken.IndexOf(',');
			if (num3 != -1)
			{
				text3 = authorizationToken.Substring(0, num3);
			}
			int num4 = text.IndexOf('=');
			if (num4 == -1 || !text.Substring(0, num4).Equals("type", StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			string text4 = text.Substring(num4 + 1);
			int num5 = text2.IndexOf('=');
			if (num5 == -1 || !text2.Substring(0, num5).Equals("ver", StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			string text5 = text2.Substring(num5 + 1);
			int num6 = text3.IndexOf('=');
			if (num6 == -1 || !text3.Substring(0, num6).Equals("sig", StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			string text6 = text3.Substring(num6 + 1);
			if (string.IsNullOrEmpty(text4) || string.IsNullOrEmpty(text5) || string.IsNullOrEmpty(text6))
			{
				throw new UnauthorizedException(RMResources.InvalidAuthHeaderFormat);
			}
			typeOutput = text4;
			versionOutput = text5;
			tokenOutput = text6;
		}

		public static bool CheckPayloadUsingKey(string inputToken, string verb, string resourceId, string resourceType, INameValueCollection headers, string key)
		{
			string str = GenerateKeyAuthorizationSignature(verb, resourceId, resourceType, headers, key);
			str = HttpUtility.UrlDecode(str);
			str = str.Substring(str.IndexOf("sig=", StringComparison.OrdinalIgnoreCase) + 4);
			return inputToken.Equals(str, StringComparison.OrdinalIgnoreCase);
		}

		public static void ValidateInputRequestTime(INameValueCollection requestHeaders, int masterTokenExpiryInSeconds, int allowedClockSkewInSeconds)
		{
			ValidateInputRequestTime(requestHeaders, (INameValueCollection headers, string field) => GetHeaderValue(headers, field), masterTokenExpiryInSeconds, allowedClockSkewInSeconds);
		}

		public static void ValidateInputRequestTime<T>(T requestHeaders, Func<T, string, string> headerGetter, int masterTokenExpiryInSeconds, int allowedClockSkewInSeconds)
		{
			if (requestHeaders == null)
			{
				DefaultTrace.TraceError("Null request headers for validating auth time");
				throw new UnauthorizedException(RMResources.MissingDateForAuthorization);
			}
			string text = headerGetter(requestHeaders, "x-ms-date");
			if (string.IsNullOrEmpty(text))
			{
				text = headerGetter(requestHeaders, "date");
			}
			ValidateInputRequestTime(text, masterTokenExpiryInSeconds, allowedClockSkewInSeconds);
		}

		private static void ValidateInputRequestTime(string dateToCompare, int masterTokenExpiryInSeconds, int allowedClockSkewInSeconds)
		{
			if (string.IsNullOrEmpty(dateToCompare))
			{
				throw new UnauthorizedException(RMResources.MissingDateForAuthorization);
			}
			if (!DateTime.TryParse(dateToCompare, CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime result))
			{
				throw new UnauthorizedException(RMResources.InvalidDateHeader);
			}
			if (result >= DateTime.MaxValue.AddSeconds(-masterTokenExpiryInSeconds))
			{
				string message = string.Format(CultureInfo.InvariantCulture, RMResources.InvalidTokenTimeRange, result.ToString("r", CultureInfo.InvariantCulture), DateTime.MaxValue.ToString("r", CultureInfo.InvariantCulture), DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
				DefaultTrace.TraceError(message);
				throw new ForbiddenException(message);
			}
			DateTime expiryDateTime = result + TimeSpan.FromSeconds(masterTokenExpiryInSeconds);
			CheckTimeRangeIsCurrent(allowedClockSkewInSeconds, result, expiryDateTime);
		}

		public static void CheckTimeRangeIsCurrent(int allowedClockSkewInSeconds, DateTime startDateTime, DateTime expiryDateTime)
		{
			if (startDateTime <= DateTime.MinValue.AddSeconds(allowedClockSkewInSeconds) || expiryDateTime >= DateTime.MaxValue.AddSeconds(-allowedClockSkewInSeconds) || startDateTime.AddSeconds(-allowedClockSkewInSeconds) > DateTime.UtcNow || expiryDateTime.AddSeconds(allowedClockSkewInSeconds) < DateTime.UtcNow)
			{
				string message = string.Format(CultureInfo.InvariantCulture, RMResources.InvalidTokenTimeRange, startDateTime.ToString("r", CultureInfo.InvariantCulture), expiryDateTime.ToString("r", CultureInfo.InvariantCulture), DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
				DefaultTrace.TraceError(message);
				throw new ForbiddenException(message);
			}
		}

		internal static void GetResourceTypeAndIdOrFullName(Uri uri, out bool isNameBased, out string resourceType, out string resourceId, string clientVersion = "")
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			resourceType = string.Empty;
			resourceId = string.Empty;
			if (uri.Segments.Length < 1)
			{
				throw new ArgumentException(RMResources.InvalidUrl);
			}
			bool isFeed = false;
			if (!PathsHelper.TryParsePathSegments(uri.PathAndQuery, out isFeed, out resourceType, out resourceId, out isNameBased, clientVersion))
			{
				resourceType = string.Empty;
				resourceId = string.Empty;
			}
		}

		public static bool IsUserRequest(string resourceType)
		{
			if (string.Compare(resourceType, "/", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(resourceType, "presplitaction", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(resourceType, "postsplitaction", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(resourceType, "controllerbatchgetoutput", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(resourceType, "controllerbatchreportcharges", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(resourceType, "getstorageaccountkey", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return false;
			}
			return true;
		}

		public static AuthorizationTokenType GetSystemOperationType(bool readOnlyRequest, string resourceType)
		{
			if (!IsUserRequest(resourceType))
			{
				if (readOnlyRequest)
				{
					return AuthorizationTokenType.SystemReadOnly;
				}
				return AuthorizationTokenType.SystemAll;
			}
			if (readOnlyRequest)
			{
				return AuthorizationTokenType.SystemReadOnly;
			}
			return AuthorizationTokenType.SystemReadWrite;
		}

		public static string GenerateMessagePayload(string verb, string resourceId, string resourceType, INameValueCollection headers, bool bUseUtcNowForMissingXDate = false)
		{
			string headerValue = GetHeaderValue(headers, "x-ms-date");
			string headerValue2 = GetHeaderValue(headers, "date");
			if (string.IsNullOrEmpty(headerValue) && string.IsNullOrWhiteSpace(headerValue2))
			{
				if (!bUseUtcNowForMissingXDate)
				{
					throw new UnauthorizedException(RMResources.InvalidDateHeader);
				}
				headers["x-ms-date"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
				headerValue = GetHeaderValue(headers, "x-ms-date");
			}
			if (!PathsHelper.IsNameBased(resourceId))
			{
				resourceId = resourceId.ToLowerInvariant();
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}\n{3}\n{4}\n", verb.ToLowerInvariant(), resourceType.ToLowerInvariant(), resourceId, headerValue.ToLowerInvariant(), headerValue.Equals("", StringComparison.OrdinalIgnoreCase) ? headerValue2.ToLowerInvariant() : "");
		}

		public static bool IsResourceToken(string token)
		{
			int num = token.IndexOf('&');
			if (num == -1)
			{
				return false;
			}
			string text = token.Substring(0, num);
			int num2 = text.IndexOf('=');
			if (num2 == -1 || !text.Substring(0, num2).Equals("type", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return text.Substring(num2 + 1).Equals("resource", StringComparison.OrdinalIgnoreCase);
		}

		internal static string GetHeaderValue(INameValueCollection headerValues, string key)
		{
			if (headerValues == null)
			{
				return string.Empty;
			}
			return headerValues[key] ?? "";
		}

		internal static string GetHeaderValue(IDictionary<string, string> headerValues, string key)
		{
			if (headerValues == null)
			{
				return string.Empty;
			}
			string value = null;
			headerValues.TryGetValue(key, out value);
			return value;
		}

		internal static string GetAuthorizationResourceIdOrFullName(string resourceType, string resourceIdOrFullName)
		{
			if (string.IsNullOrEmpty(resourceType) || string.IsNullOrEmpty(resourceIdOrFullName))
			{
				return resourceIdOrFullName;
			}
			if (PathsHelper.IsNameBased(resourceIdOrFullName))
			{
				return resourceIdOrFullName;
			}
			if (resourceType.Equals("offers", StringComparison.OrdinalIgnoreCase) || resourceType.Equals("partitions", StringComparison.OrdinalIgnoreCase) || resourceType.Equals("topology", StringComparison.OrdinalIgnoreCase) || resourceType.Equals("ridranges", StringComparison.OrdinalIgnoreCase))
			{
				return resourceIdOrFullName;
			}
			ResourceId resourceId = ResourceId.Parse(resourceIdOrFullName);
			if (resourceType.Equals("dbs", StringComparison.OrdinalIgnoreCase))
			{
				return resourceId.DatabaseId.ToString();
			}
			if (resourceType.Equals("users", StringComparison.OrdinalIgnoreCase))
			{
				return resourceId.UserId.ToString();
			}
			if (resourceType.Equals("udts", StringComparison.OrdinalIgnoreCase))
			{
				return resourceId.UserDefinedTypeId.ToString();
			}
			if (resourceType.Equals("colls", StringComparison.OrdinalIgnoreCase))
			{
				return resourceId.DocumentCollectionId.ToString();
			}
			if (resourceType.Equals("docs", StringComparison.OrdinalIgnoreCase))
			{
				return resourceId.DocumentId.ToString();
			}
			return resourceIdOrFullName;
		}

		public static Uri GenerateUriFromAddressRequestUri(Uri uri)
		{
			string text = UrlUtility.ParseQuery(uri.Query)["$resolveFor"] ?? UrlUtility.ParseQuery(uri.Query)["$generateFor"] ?? UrlUtility.ParseQuery(uri.Query)["$getChildResourcePartitions"];
			if (string.IsNullOrEmpty(text))
			{
				throw new BadRequestException(RMResources.BadUrl);
			}
			return new Uri(uri.Scheme + "://" + uri.Host + "/" + HttpUtility.UrlDecode(text).Trim(new char[1]
			{
				'/'
			}));
		}
	}
}
