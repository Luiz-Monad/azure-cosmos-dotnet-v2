using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.Azure.Documents.Client
{
	internal sealed class SessionContainer : ISessionContainer
	{
		private sealed class SessionContainerState
		{
			public readonly string hostName;

			public readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

			public readonly ConcurrentDictionary<string, ulong> collectionNameByResourceId = new ConcurrentDictionary<string, ulong>();

			public readonly ConcurrentDictionary<ulong, string> collectionResourceIdByName = new ConcurrentDictionary<ulong, string>();

			public readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, ISessionToken>> sessionTokensRIDBased = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, ISessionToken>>();

			public SessionContainerState(string hostName)
			{
				this.hostName = hostName;
			}

			~SessionContainerState()
			{
				if (rwlock != null)
				{
					rwlock.Dispose();
				}
			}
		}

		private sealed class SessionContainerSnapshot
		{
			private readonly Dictionary<string, ulong> collectionNameByResourceId;

			private readonly Dictionary<ulong, string> collectionResourceIdByName;

			private readonly Dictionary<ulong, Dictionary<string, ISessionToken>> sessionTokensRIDBased;

			public SessionContainerSnapshot(ConcurrentDictionary<string, ulong> collectionNameByResourceId, ConcurrentDictionary<ulong, string> collectionResourceIdByName, ConcurrentDictionary<ulong, ConcurrentDictionary<string, ISessionToken>> sessionTokensRIDBased)
			{
				this.collectionNameByResourceId = new Dictionary<string, ulong>(collectionNameByResourceId);
				this.collectionResourceIdByName = new Dictionary<ulong, string>(collectionResourceIdByName);
				this.sessionTokensRIDBased = new Dictionary<ulong, Dictionary<string, ISessionToken>>();
				foreach (KeyValuePair<ulong, ConcurrentDictionary<string, ISessionToken>> item in sessionTokensRIDBased)
				{
					this.sessionTokensRIDBased.Add(item.Key, new Dictionary<string, ISessionToken>(item.Value));
				}
			}

			public override int GetHashCode()
			{
				return 1;
			}

			public override bool Equals(object obj)
			{
				if (obj == null || (object)GetType() != obj.GetType())
				{
					return false;
				}
				SessionContainerSnapshot sessionContainerSnapshot = (SessionContainerSnapshot)obj;
				if (!AreDictionariesEqual(collectionNameByResourceId, sessionContainerSnapshot.collectionNameByResourceId, (ulong x, ulong y) => x == y))
				{
					return false;
				}
				if (!AreDictionariesEqual(collectionResourceIdByName, sessionContainerSnapshot.collectionResourceIdByName, (string x, string y) => x == y))
				{
					return false;
				}
				if (!AreDictionariesEqual(sessionTokensRIDBased, sessionContainerSnapshot.sessionTokensRIDBased, (Dictionary<string, ISessionToken> x, Dictionary<string, ISessionToken> y) => AreDictionariesEqual(x, y, (ISessionToken a, ISessionToken b) => a.Equals(b))))
				{
					return false;
				}
				return true;
			}

			private static bool AreDictionariesEqual<T, U>(Dictionary<T, U> left, Dictionary<T, U> right, Func<U, U, bool> areEqual)
			{
				if (left.Count != right.Count)
				{
					return false;
				}
				foreach (T key in left.Keys)
				{
					if (!right.ContainsKey(key))
					{
						return false;
					}
					if (!areEqual(left[key], right[key]))
					{
						return false;
					}
				}
				return true;
			}
		}

		private volatile SessionContainerState state;

		public string HostName => state.hostName;

		public SessionContainer(string hostName)
		{
			state = new SessionContainerState(hostName);
		}

		public void ReplaceCurrrentStateWithStateOf(SessionContainer comrade)
		{
			state = comrade.state;
		}

		public string GetSessionToken(string collectionLink)
		{
			return GetSessionToken(state, collectionLink);
		}

		public string ResolveGlobalSessionToken(DocumentServiceRequest request)
		{
			return ResolveGlobalSessionToken(state, request);
		}

		public ISessionToken ResolvePartitionLocalSessionToken(DocumentServiceRequest request, string partitionKeyRangeId)
		{
			return ResolvePartitionLocalSessionToken(state, request, partitionKeyRangeId);
		}

		public void ClearTokenByCollectionFullname(string collectionFullname)
		{
			ClearTokenByCollectionFullname(state, collectionFullname);
		}

		public void ClearTokenByResourceId(string resourceId)
		{
			ClearTokenByResourceId(state, resourceId);
		}

		public void SetSessionToken(string collectionRid, string collectionFullname, INameValueCollection responseHeaders)
		{
			SetSessionToken(state, collectionRid, collectionFullname, responseHeaders);
		}

		public void SetSessionToken(DocumentServiceRequest request, INameValueCollection responseHeaders)
		{
			SetSessionToken(state, request, responseHeaders);
		}

		public object MakeSnapshot()
		{
			return MakeSnapshot(state);
		}

		private static string GetSessionToken(SessionContainerState self, string collectionLink)
		{
			bool isFeed;
			string resourcePath;
			string resourceIdOrFullName;
			bool isNameBased;
			bool num = PathsHelper.TryParsePathSegments(collectionLink, out isFeed, out resourcePath, out resourceIdOrFullName, out isNameBased);
			ConcurrentDictionary<string, ISessionToken> value = null;
			if (num)
			{
				ulong? num2 = null;
				if (isNameBased)
				{
					string collectionPath = PathsHelper.GetCollectionPath(resourceIdOrFullName);
					if (self.collectionNameByResourceId.TryGetValue(collectionPath, out ulong value2))
					{
						num2 = value2;
					}
				}
				else
				{
					ResourceId resourceId = ResourceId.Parse(resourceIdOrFullName);
					if (resourceId.DocumentCollection != 0)
					{
						num2 = resourceId.UniqueDocumentCollectionId;
					}
				}
				if (num2.HasValue)
				{
					self.sessionTokensRIDBased.TryGetValue(num2.Value, out value);
				}
			}
			if (value == null)
			{
				return string.Empty;
			}
			return GetSessionTokenString(value);
		}

		private static string ResolveGlobalSessionToken(SessionContainerState self, DocumentServiceRequest request)
		{
			ConcurrentDictionary<string, ISessionToken> partitionKeyRangeIdToTokenMap = GetPartitionKeyRangeIdToTokenMap(self, request);
			if (partitionKeyRangeIdToTokenMap != null)
			{
				return GetSessionTokenString(partitionKeyRangeIdToTokenMap);
			}
			return string.Empty;
		}

		private static ISessionToken ResolvePartitionLocalSessionToken(SessionContainerState self, DocumentServiceRequest request, string partitionKeyRangeId)
		{
			return SessionTokenHelper.ResolvePartitionLocalSessionToken(request, partitionKeyRangeId, GetPartitionKeyRangeIdToTokenMap(self, request));
		}

		private static void ClearTokenByCollectionFullname(SessionContainerState self, string collectionFullname)
		{
			if (!string.IsNullOrEmpty(collectionFullname))
			{
				string collectionPath = PathsHelper.GetCollectionPath(collectionFullname);
				self.rwlock.EnterWriteLock();
				try
				{
					if (self.collectionNameByResourceId.ContainsKey(collectionPath))
					{
						ulong key = self.collectionNameByResourceId[collectionPath];
						self.sessionTokensRIDBased.TryRemove(key, out ConcurrentDictionary<string, ISessionToken> _);
						self.collectionResourceIdByName.TryRemove(key, out string _);
						self.collectionNameByResourceId.TryRemove(collectionPath, out ulong _);
					}
				}
				finally
				{
					self.rwlock.ExitWriteLock();
				}
			}
		}

		private static void ClearTokenByResourceId(SessionContainerState self, string resourceId)
		{
			if (!string.IsNullOrEmpty(resourceId))
			{
				ResourceId resourceId2 = ResourceId.Parse(resourceId);
				if (resourceId2.DocumentCollection != 0)
				{
					ulong uniqueDocumentCollectionId = resourceId2.UniqueDocumentCollectionId;
					self.rwlock.EnterWriteLock();
					try
					{
						if (self.collectionResourceIdByName.ContainsKey(uniqueDocumentCollectionId))
						{
							string key = self.collectionResourceIdByName[uniqueDocumentCollectionId];
							self.sessionTokensRIDBased.TryRemove(uniqueDocumentCollectionId, out ConcurrentDictionary<string, ISessionToken> _);
							self.collectionResourceIdByName.TryRemove(uniqueDocumentCollectionId, out string _);
							self.collectionNameByResourceId.TryRemove(key, out ulong _);
						}
					}
					finally
					{
						self.rwlock.ExitWriteLock();
					}
				}
			}
		}

		private static void SetSessionToken(SessionContainerState self, string collectionRid, string collectionFullname, INameValueCollection responseHeaders)
		{
			ResourceId resourceId = ResourceId.Parse(collectionRid);
			string collectionPath = PathsHelper.GetCollectionPath(collectionFullname);
			string text = responseHeaders["x-ms-session-token"];
			if (!string.IsNullOrEmpty(text))
			{
				SetSessionToken(self, resourceId, collectionPath, text);
			}
		}

		private static void SetSessionToken(SessionContainerState self, DocumentServiceRequest request, INameValueCollection responseHeaders)
		{
			string text = responseHeaders["x-ms-session-token"];
			if (!string.IsNullOrEmpty(text) && ShouldUpdateSessionToken(request, responseHeaders, out ResourceId resourceId, out string collectionName))
			{
				SetSessionToken(self, resourceId, collectionName, text);
			}
		}

		private static SessionContainerSnapshot MakeSnapshot(SessionContainerState self)
		{
			self.rwlock.EnterReadLock();
			try
			{
				return new SessionContainerSnapshot(self.collectionNameByResourceId, self.collectionResourceIdByName, self.sessionTokensRIDBased);
			}
			finally
			{
				self.rwlock.ExitReadLock();
			}
		}

		private static ConcurrentDictionary<string, ISessionToken> GetPartitionKeyRangeIdToTokenMap(SessionContainerState self, DocumentServiceRequest request)
		{
			ulong? num = null;
			if (request.IsNameBased)
			{
				string collectionPath = PathsHelper.GetCollectionPath(request.ResourceAddress);
				if (self.collectionNameByResourceId.TryGetValue(collectionPath, out ulong value))
				{
					num = value;
				}
			}
			else if (!string.IsNullOrEmpty(request.ResourceId))
			{
				ResourceId resourceId = ResourceId.Parse(request.ResourceId);
				if (resourceId.DocumentCollection != 0)
				{
					num = resourceId.UniqueDocumentCollectionId;
				}
			}
			ConcurrentDictionary<string, ISessionToken> value2 = null;
			if (num.HasValue)
			{
				self.sessionTokensRIDBased.TryGetValue(num.Value, out value2);
			}
			return value2;
		}

		private static void SetSessionToken(SessionContainerState self, ResourceId resourceId, string collectionName, string encodedToken)
		{
			string partitionKeyRangeId;
			ISessionToken sessionToken;
			if (VersionUtility.IsLaterThan(HttpConstants.Versions.CurrentVersion, HttpConstants.Versions.v2015_12_16))
			{
				string[] array = encodedToken.Split(new char[1]
				{
					':'
				});
				partitionKeyRangeId = array[0];
				sessionToken = SessionTokenHelper.Parse(array[1], HttpConstants.Versions.CurrentVersion);
			}
			else
			{
				partitionKeyRangeId = "0";
				sessionToken = SessionTokenHelper.Parse(encodedToken, HttpConstants.Versions.CurrentVersion);
			}
			DefaultTrace.TraceVerbose("Update Session token {0} {1} {2}", resourceId.UniqueDocumentCollectionId, collectionName, sessionToken);
			bool flag = false;
			self.rwlock.EnterReadLock();
			try
			{
				ulong value;
				flag = (self.collectionNameByResourceId.TryGetValue(collectionName, out value) && self.collectionResourceIdByName.TryGetValue(resourceId.UniqueDocumentCollectionId, out string value2) && value == resourceId.UniqueDocumentCollectionId && value2 == collectionName);
				if (flag)
				{
					AddSessionToken(self, resourceId.UniqueDocumentCollectionId, partitionKeyRangeId, sessionToken);
				}
			}
			finally
			{
				self.rwlock.ExitReadLock();
			}
			if (!flag)
			{
				self.rwlock.EnterWriteLock();
				try
				{
					if (self.collectionNameByResourceId.TryGetValue(collectionName, out ulong value3))
					{
						self.sessionTokensRIDBased.TryRemove(value3, out ConcurrentDictionary<string, ISessionToken> _);
						self.collectionResourceIdByName.TryRemove(value3, out string _);
					}
					self.collectionNameByResourceId[collectionName] = resourceId.UniqueDocumentCollectionId;
					self.collectionResourceIdByName[resourceId.UniqueDocumentCollectionId] = collectionName;
					AddSessionToken(self, resourceId.UniqueDocumentCollectionId, partitionKeyRangeId, sessionToken);
				}
				finally
				{
					self.rwlock.ExitWriteLock();
				}
			}
		}

		private static void AddSessionToken(SessionContainerState self, ulong rid, string partitionKeyRangeId, ISessionToken token)
		{
			self.sessionTokensRIDBased.AddOrUpdate(rid, (ulong ridKey) => new ConcurrentDictionary<string, ISessionToken>
			{
				[partitionKeyRangeId] = token
			}, delegate(ulong ridKey, ConcurrentDictionary<string, ISessionToken> tokens)
			{
				tokens.AddOrUpdate(partitionKeyRangeId, token, (string existingPartitionKeyRangeId, ISessionToken existingToken) => existingToken.Merge(token));
				return tokens;
			});
		}

		private static string GetSessionTokenString(ConcurrentDictionary<string, ISessionToken> partitionKeyRangeIdToTokenMap)
		{
			if (VersionUtility.IsLaterThan(HttpConstants.Versions.CurrentVersion, HttpConstants.Versions.v2015_12_16))
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<string, ISessionToken> item in partitionKeyRangeIdToTokenMap)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(",");
					}
					stringBuilder.Append(item.Key);
					stringBuilder.Append(":");
					stringBuilder.Append(item.Value.ConvertToString());
				}
				return stringBuilder.ToString();
			}
			if (partitionKeyRangeIdToTokenMap.TryGetValue("0", out ISessionToken value))
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}", value);
			}
			return string.Empty;
		}

		private static bool AreDictionariesEqual(Dictionary<string, ISessionToken> first, Dictionary<string, ISessionToken> second)
		{
			if (first.Count != second.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, ISessionToken> item in first)
			{
				if (second.TryGetValue(item.Key, out ISessionToken value) && !value.Equals(item.Value))
				{
					return false;
				}
			}
			return true;
		}

		private static bool ShouldUpdateSessionToken(DocumentServiceRequest request, INameValueCollection responseHeaders, out ResourceId resourceId, out string collectionName)
		{
			resourceId = null;
			string text = responseHeaders["x-ms-alt-content-path"];
			if (string.IsNullOrEmpty(text))
			{
				text = request.ResourceAddress;
			}
			collectionName = PathsHelper.GetCollectionPath(text);
			string text2;
			if (request.IsNameBased)
			{
				text2 = responseHeaders["x-ms-content-path"];
				if (string.IsNullOrEmpty(text2))
				{
					text2 = request.ResourceId;
				}
			}
			else
			{
				text2 = request.ResourceId;
			}
			if (!string.IsNullOrEmpty(text2))
			{
				resourceId = ResourceId.Parse(text2);
				if (resourceId.DocumentCollection != 0 && collectionName != null && !ReplicatedResourceClient.IsReadingFromMaster(request.ResourceType, request.OperationType))
				{
					return true;
				}
			}
			return false;
		}
	}
}
