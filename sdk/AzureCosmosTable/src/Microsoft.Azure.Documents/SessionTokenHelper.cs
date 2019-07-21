using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	internal static class SessionTokenHelper
	{
		public static void SetOriginalSessionToken(DocumentServiceRequest request, string originalSessionToken)
		{
			if (request == null)
			{
				throw new ArgumentException("request");
			}
			if (originalSessionToken == null)
			{
				request.Headers.Remove("x-ms-session-token");
			}
			else
			{
				request.Headers["x-ms-session-token"] = originalSessionToken;
			}
		}

		public static void ValidateAndRemoveSessionToken(DocumentServiceRequest request)
		{
			string text = request.Headers["x-ms-session-token"];
			if (!string.IsNullOrEmpty(text))
			{
				GetLocalSessionToken(request, text, string.Empty);
				request.Headers.Remove("x-ms-session-token");
			}
		}

		public static void SetPartitionLocalSessionToken(DocumentServiceRequest entity, ISessionContainer sessionContainer)
		{
			if (entity == null)
			{
				throw new ArgumentException("entity");
			}
			string text = entity.Headers["x-ms-session-token"];
			string id = entity.RequestContext.ResolvedPartitionKeyRange.Id;
			if (string.IsNullOrEmpty(id))
			{
				throw new InternalServerErrorException(RMResources.PartitionKeyRangeIdAbsentInContext);
			}
			if (!string.IsNullOrEmpty(text))
			{
				ISessionToken localSessionToken = GetLocalSessionToken(entity, text, id);
				entity.RequestContext.SessionToken = localSessionToken;
			}
			else
			{
				ISessionToken sessionToken = sessionContainer.ResolvePartitionLocalSessionToken(entity, id);
				entity.RequestContext.SessionToken = sessionToken;
			}
			if (entity.RequestContext.SessionToken == null)
			{
				entity.Headers.Remove("x-ms-session-token");
				return;
			}
			string text2 = entity.Headers["x-ms-version"];
			text2 = (string.IsNullOrEmpty(text2) ? HttpConstants.Versions.CurrentVersion : text2);
			if (VersionUtility.IsLaterThan(text2, HttpConstants.Versions.v2015_12_16))
			{
				entity.Headers["x-ms-session-token"] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", id, entity.RequestContext.SessionToken.ConvertToString());
			}
			else
			{
				entity.Headers["x-ms-session-token"] = entity.RequestContext.SessionToken.ConvertToString();
			}
		}

		internal static ISessionToken GetLocalSessionToken(DocumentServiceRequest request, string globalSessionToken, string partitionKeyRangeId)
		{
			string text = request.Headers["x-ms-version"];
			text = (string.IsNullOrEmpty(text) ? HttpConstants.Versions.CurrentVersion : text);
			if (!VersionUtility.IsLaterThan(text, HttpConstants.Versions.v2015_12_16))
			{
				if (!SimpleSessionToken.TryCreate(globalSessionToken, out ISessionToken parsedSessionToken))
				{
					throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidSessionToken, globalSessionToken));
				}
				return parsedSessionToken;
			}
			string[] array = globalSessionToken.Split(new char[1]
			{
				','
			}, StringSplitOptions.RemoveEmptyEntries);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal)
			{
				partitionKeyRangeId
			};
			ISessionToken sessionToken = null;
			if (request.RequestContext.ResolvedPartitionKeyRange != null && request.RequestContext.ResolvedPartitionKeyRange.Parents != null)
			{
				hashSet.UnionWith(request.RequestContext.ResolvedPartitionKeyRange.Parents);
			}
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				string[] array3 = text2.Split(new char[1]
				{
					':'
				}, StringSplitOptions.RemoveEmptyEntries);
				if (array3.Length != 2)
				{
					throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidSessionToken, text2));
				}
				ISessionToken sessionToken2 = Parse(array3[1]);
				if (hashSet.Contains(array3[0]))
				{
					sessionToken = ((sessionToken != null) ? sessionToken.Merge(sessionToken2) : sessionToken2);
				}
			}
			return sessionToken;
		}

		internal static ISessionToken ResolvePartitionLocalSessionToken(DocumentServiceRequest request, string partitionKeyRangeId, ConcurrentDictionary<string, ISessionToken> partitionKeyRangeIdToTokenMap)
		{
			if (partitionKeyRangeIdToTokenMap != null)
			{
				if (partitionKeyRangeIdToTokenMap.TryGetValue(partitionKeyRangeId, out ISessionToken value))
				{
					return value;
				}
				if (request.RequestContext.ResolvedPartitionKeyRange.Parents != null)
				{
					for (int num = request.RequestContext.ResolvedPartitionKeyRange.Parents.Count - 1; num >= 0; num--)
					{
						if (partitionKeyRangeIdToTokenMap.TryGetValue(request.RequestContext.ResolvedPartitionKeyRange.Parents[num], out value))
						{
							return value;
						}
					}
				}
			}
			return null;
		}

		internal static ISessionToken Parse(string sessionToken)
		{
			ISessionToken parsedSessionToken = null;
			if (TryParse(sessionToken, out parsedSessionToken))
			{
				return parsedSessionToken;
			}
			throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidSessionToken, sessionToken));
		}

		internal static bool TryParse(string sessionToken, out ISessionToken parsedSessionToken)
		{
			parsedSessionToken = null;
			if (!string.IsNullOrEmpty(sessionToken))
			{
				string[] source = sessionToken.Split(new char[1]
				{
					':'
				});
				if (!SimpleSessionToken.TryCreate(source.Last(), out parsedSessionToken))
				{
					return VectorSessionToken.TryCreate(source.Last(), out parsedSessionToken);
				}
				return true;
			}
			return false;
		}

		internal static ISessionToken Parse(string sessionToken, string version)
		{
			if (!string.IsNullOrEmpty(sessionToken))
			{
				string[] source = sessionToken.Split(new char[1]
				{
					':'
				});
				ISessionToken parsedSessionToken;
				if (VersionUtility.IsLaterThan(version, HttpConstants.Versions.v2018_06_18))
				{
					if (VectorSessionToken.TryCreate(source.Last(), out parsedSessionToken))
					{
						return parsedSessionToken;
					}
				}
				else if (SimpleSessionToken.TryCreate(source.Last(), out parsedSessionToken))
				{
					return parsedSessionToken;
				}
			}
			DefaultTrace.TraceCritical("Unable to parse session token {0} for version {1}", sessionToken, version);
			throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidSessionToken, sessionToken));
		}
	}
}
