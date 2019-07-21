using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Implements session token with Global LSN
	///
	/// We make assumption that instances of this interface are immutable (read only after they are constructed), so if you want to change
	/// this behaviour please review all of its uses and make sure that mutability doesn't break anything.
	/// </summary>
	internal sealed class SimpleSessionToken : ISessionToken, IEquatable<ISessionToken>
	{
		private readonly long globalLsn;

		public long LSN => globalLsn;

		public SimpleSessionToken(long globalLsn)
		{
			this.globalLsn = globalLsn;
		}

		public static bool TryCreate(string globalLsn, out ISessionToken parsedSessionToken)
		{
			parsedSessionToken = null;
			long result = -1L;
			if (long.TryParse(globalLsn, out result))
			{
				parsedSessionToken = new SimpleSessionToken(result);
				return true;
			}
			return false;
		}

		public bool Equals(ISessionToken obj)
		{
			SimpleSessionToken simpleSessionToken = obj as SimpleSessionToken;
			if (simpleSessionToken == null)
			{
				return false;
			}
			return globalLsn.Equals(simpleSessionToken.globalLsn);
		}

		public ISessionToken Merge(ISessionToken obj)
		{
			SimpleSessionToken simpleSessionToken = obj as SimpleSessionToken;
			if (simpleSessionToken == null)
			{
				throw new ArgumentNullException("obj");
			}
			return new SimpleSessionToken(Math.Max(globalLsn, simpleSessionToken.globalLsn));
		}

		public bool IsValid(ISessionToken otherSessionToken)
		{
			SimpleSessionToken obj = otherSessionToken as SimpleSessionToken;
			if (obj == null)
			{
				throw new ArgumentNullException("otherSessionToken");
			}
			return obj.globalLsn >= globalLsn;
		}

		string ISessionToken.ConvertToString()
		{
			return globalLsn.ToString(CultureInfo.InvariantCulture);
		}
	}
}
