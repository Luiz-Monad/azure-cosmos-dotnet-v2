using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Models vector clock bases session token. Session token has the following format:
	/// {Version}#{GlobalLSN}#{RegionId1}={LocalLsn1}#{RegionId2}={LocalLsn2}....#{RegionIdN}={LocalLsnN}
	/// 'Version' captures the configuration number of the partition which returned this session token.
	/// 'Version' is incremented everytime topology of the partition is updated (say due to Add/Remove/Failover).
	///
	/// The choice of separators '#' and '=' is important. Separators ';' and ',' are used to delimit
	/// per-partitionKeyRange session token
	/// session
	///
	/// We make assumption that instances of this class are immutable (read only after they are constructed), so if you want to change
	/// this behaviour please review all of its uses and make sure that mutability doesn't break anything.
	/// </summary>
	internal sealed class VectorSessionToken : ISessionToken, IEquatable<ISessionToken>
	{
		private const char SegmentSeparator = '#';

		private const char RegionProgressSeparator = '=';

		private readonly string sessionToken;

		private readonly long version;

		private readonly long globalLsn;

		private readonly IReadOnlyDictionary<uint, long> localLsnByRegion;

		public long LSN => globalLsn;

		private VectorSessionToken(long version, long globalLsn, IReadOnlyDictionary<uint, long> localLsnByRegion, string sessionToken = null)
		{
			this.version = version;
			this.globalLsn = globalLsn;
			this.localLsnByRegion = localLsnByRegion;
			this.sessionToken = sessionToken;
			if (this.sessionToken == null)
			{
				string text = string.Join('#'.ToString(), from kvp in localLsnByRegion
				select string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", kvp.Key, '=', kvp.Value));
				if (string.IsNullOrEmpty(text))
				{
					this.sessionToken = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", this.version, '#', this.globalLsn);
				}
				else
				{
					this.sessionToken = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}{4}", this.version, '#', this.globalLsn, '#', text);
				}
			}
		}

		public VectorSessionToken(VectorSessionToken other, long globalLSN)
			: this(other.version, globalLSN, other.localLsnByRegion.ToDictionary((KeyValuePair<uint, long> kvp) => kvp.Key, (KeyValuePair<uint, long> kvp) => kvp.Value))
		{
		}

		public static bool TryCreate(string sessionToken, out ISessionToken parsedSessionToken)
		{
			parsedSessionToken = null;
			long num = -1L;
			long num2 = -1L;
			if (TryParseSessionToken(sessionToken, out num, out num2, out IReadOnlyDictionary<uint, long> readOnlyDictionary))
			{
				parsedSessionToken = new VectorSessionToken(num, num2, readOnlyDictionary, sessionToken);
				return true;
			}
			return false;
		}

		public bool Equals(ISessionToken obj)
		{
			VectorSessionToken vectorSessionToken = obj as VectorSessionToken;
			if (vectorSessionToken == null)
			{
				return false;
			}
			if (version == vectorSessionToken.version && globalLsn == vectorSessionToken.globalLsn)
			{
				return AreRegionProgressEqual(vectorSessionToken.localLsnByRegion);
			}
			return false;
		}

		public bool IsValid(ISessionToken otherSessionToken)
		{
			VectorSessionToken vectorSessionToken = otherSessionToken as VectorSessionToken;
			if (vectorSessionToken == null)
			{
				throw new ArgumentNullException("otherSessionToken");
			}
			if (vectorSessionToken.version < version || vectorSessionToken.globalLsn < globalLsn)
			{
				return false;
			}
			if (vectorSessionToken.version == version && vectorSessionToken.localLsnByRegion.Count != localLsnByRegion.Count)
			{
				throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, sessionToken, vectorSessionToken.sessionToken));
			}
			foreach (KeyValuePair<uint, long> item in vectorSessionToken.localLsnByRegion)
			{
				uint key = item.Key;
				long value = item.Value;
				long value2 = -1L;
				if (!localLsnByRegion.TryGetValue(key, out value2))
				{
					if (version == vectorSessionToken.version)
					{
						throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, sessionToken, vectorSessionToken.sessionToken));
					}
				}
				else if (value < value2)
				{
					return false;
				}
			}
			return true;
		}

		public ISessionToken Merge(ISessionToken obj)
		{
			VectorSessionToken vectorSessionToken = obj as VectorSessionToken;
			if (vectorSessionToken == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (version == vectorSessionToken.version && localLsnByRegion.Count != vectorSessionToken.localLsnByRegion.Count)
			{
				throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, sessionToken, vectorSessionToken.sessionToken));
			}
			VectorSessionToken vectorSessionToken2;
			VectorSessionToken vectorSessionToken3;
			if (version < vectorSessionToken.version)
			{
				vectorSessionToken2 = this;
				vectorSessionToken3 = vectorSessionToken;
			}
			else
			{
				vectorSessionToken2 = vectorSessionToken;
				vectorSessionToken3 = this;
			}
			Dictionary<uint, long> dictionary = new Dictionary<uint, long>();
			foreach (KeyValuePair<uint, long> item in vectorSessionToken3.localLsnByRegion)
			{
				uint key = item.Key;
				long value = item.Value;
				long value2 = -1L;
				if (vectorSessionToken2.localLsnByRegion.TryGetValue(key, out value2))
				{
					dictionary[key] = Math.Max(value, value2);
				}
				else
				{
					if (version == vectorSessionToken.version)
					{
						throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidRegionsInSessionToken, sessionToken, vectorSessionToken.sessionToken));
					}
					dictionary[key] = value;
				}
			}
			return new VectorSessionToken(Math.Max(version, vectorSessionToken.version), Math.Max(globalLsn, vectorSessionToken.globalLsn), dictionary);
		}

		string ISessionToken.ConvertToString()
		{
			return sessionToken;
		}

		private bool AreRegionProgressEqual(IReadOnlyDictionary<uint, long> other)
		{
			if (localLsnByRegion.Count != other.Count)
			{
				return false;
			}
			foreach (KeyValuePair<uint, long> item in localLsnByRegion)
			{
				uint key = item.Key;
				long value = item.Value;
				long value2 = -1L;
				if (other.TryGetValue(key, out value2) && value != value2)
				{
					return false;
				}
			}
			return true;
		}

		private static bool TryParseSessionToken(string sessionToken, out long version, out long globalLsn, out IReadOnlyDictionary<uint, long> localLsnByRegion)
		{
			version = 0L;
			localLsnByRegion = null;
			globalLsn = -1L;
			if (string.IsNullOrEmpty(sessionToken))
			{
				DefaultTrace.TraceCritical("Session token is empty");
				return false;
			}
			string[] array = sessionToken.Split(new char[1]
			{
				'#'
			});
			if (array.Length < 2)
			{
				return false;
			}
			if (!long.TryParse(array[0], NumberStyles.Number, CultureInfo.InvariantCulture, out version) || !long.TryParse(array[1], NumberStyles.Number, CultureInfo.InvariantCulture, out globalLsn))
			{
				DefaultTrace.TraceCritical("Unexpected session token version number '{0}' OR global lsn '{1}'.", array[0], array[1]);
				return false;
			}
			Dictionary<uint, long> dictionary = new Dictionary<uint, long>();
			foreach (string item in array.Skip(2))
			{
				string[] array2 = item.Split(new char[1]
				{
					'='
				});
				if (array2.Length != 2)
				{
					DefaultTrace.TraceCritical("Unexpected region progress segment length '{0}' in session token.", array2.Length);
					return false;
				}
				uint result = 0u;
				long result2 = -1L;
				if (!uint.TryParse(array2[0], NumberStyles.Number, CultureInfo.InvariantCulture, out result) || !long.TryParse(array2[1], NumberStyles.Number, CultureInfo.InvariantCulture, out result2))
				{
					DefaultTrace.TraceCritical("Unexpected region progress '{0}' for region '{1}' in session token.", array2[0], array2[1]);
					return false;
				}
				dictionary[result] = result2;
			}
			localLsnByRegion = dictionary;
			return true;
		}
	}
}
