using System;
using System.Security;

namespace Microsoft.Azure.Documents
{
	internal interface IComputeHash : IDisposable
	{
		SecureString Key
		{
			get;
		}

		byte[] ComputeHash(byte[] bytesToHash);
	}
}
