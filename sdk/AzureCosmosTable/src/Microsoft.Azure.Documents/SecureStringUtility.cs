using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Utility for converting string to SecureString.
	/// </summary>
	internal static class SecureStringUtility
	{
		/// <summary>
		/// Converts a unsecure string into a SecureString.
		/// </summary>
		/// <param name="unsecureStr">the string to convert.</param>
		/// <returns>the resulting SecureString</returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		public static SecureString ConvertToSecureString(string unsecureStr)
		{
			if (string.IsNullOrEmpty(unsecureStr))
			{
				throw new ArgumentNullException("unsecureStr");
			}
			SecureString secureString = new SecureString();
			char[] array = unsecureStr.ToCharArray();
			foreach (char c in array)
			{
				secureString.AppendChar(c);
			}
			return secureString;
		}
	}
}
