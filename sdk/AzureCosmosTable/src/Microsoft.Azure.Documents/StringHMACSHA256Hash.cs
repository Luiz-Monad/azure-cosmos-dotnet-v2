using System;
using System.Security;
using System.Security.Cryptography;

namespace Microsoft.Azure.Documents
{
	internal sealed class StringHMACSHA256Hash : IComputeHash, IDisposable
	{
		private readonly string base64EncodedKey;

		private readonly byte[] keyBytes;

		private SecureString secureString;

		public SecureString Key
		{
			get
			{
				if (secureString != null)
				{
					return secureString;
				}
				secureString = SecureStringUtility.ConvertToSecureString(base64EncodedKey);
				return secureString;
			}
		}

		public StringHMACSHA256Hash(string base64EncodedKey)
		{
			this.base64EncodedKey = base64EncodedKey;
			keyBytes = Convert.FromBase64String(base64EncodedKey);
		}

		public byte[] ComputeHash(byte[] bytesToHash)
		{
			using (HMACSHA256 hMACSHA = new HMACSHA256(keyBytes))
			{
				return hMACSHA.ComputeHash(bytesToHash);
			}
		}

		public void Dispose()
		{
			if (secureString != null)
			{
				secureString.Dispose();
				secureString = null;
			}
		}
	}
}
