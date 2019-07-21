using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Manufactures SHA256 HMACs of byte payloads using a key. The key is a Base64-encoded SecureString.
	/// In keeping with the goals of SecureString, neither the original Base64 characters nor the decoded 
	/// bytes ever enters the managed heap, and they are kept decrypted in native memory for as short a 
	/// time as possible: just the duration of a single ComputeHash call.
	/// </summary>
	internal sealed class SecureStringHMACSHA256Helper : IComputeHash, IDisposable
	{
		private static class NativeMethods
		{
			public const string BCRYPT_SHA256_ALGORITHM = "SHA256";

			public const uint BCRYPT_ALG_HANDLE_HMAC_FLAG = 8u;

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptOpenAlgorithmProvider(out IntPtr algorithmHandle, string algorithmId, IntPtr implementation, uint flags);

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptCloseAlgorithmProvider(IntPtr algorithmHandle, uint flags);

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptCreateHash(IntPtr algorithmHandle, out IntPtr hashHandle, IntPtr workingSpace, uint workingSpaceSize, IntPtr keyBytes, uint keyBytesLength, uint flags);

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptDestroyHash(IntPtr hashHandle);

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptHashData(IntPtr hashHandle, IntPtr bytes, uint byteLength, uint flags);

			[DllImport("Bcrypt.dll", CharSet = CharSet.Unicode)]
			public static extern int BCryptFinishHash(IntPtr hashHandle, IntPtr byteOutputLocation, uint byteOutputLocationSize, uint flags);
		}

		private const uint SHA256HashOutputSizeInBytes = 32u;

		private IntPtr algorithmHandle;

		private readonly SecureString key;

		private readonly int keyLength;

		public SecureString Key => key;

		public SecureStringHMACSHA256Helper(SecureString base64EncodedKey)
		{
			key = base64EncodedKey;
			keyLength = base64EncodedKey.Length;
			algorithmHandle = IntPtr.Zero;
			int num = NativeMethods.BCryptOpenAlgorithmProvider(out algorithmHandle, "SHA256", IntPtr.Zero, 8u);
			if (num != 0)
			{
				throw new Win32Exception(num, "BCryptOpenAlgorithmProvider");
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				IntPtr algorithmHandle2 = algorithmHandle;
				int num = NativeMethods.BCryptCloseAlgorithmProvider(algorithmHandle, 0u);
				if (num != 0)
				{
					DefaultTrace.TraceError("Failed to close algorithm provider: {0}", num);
				}
				algorithmHandle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Decode the SecureString containing the Base64-encoded key into native memory, compute the
		/// SHA256 HMAC of the payload, and destroy the native memory containing the decoded key.
		/// </summary>
		/// <param name="bytesToHash">payload that is hashed</param>
		/// <returns></returns>
		public byte[] ComputeHash(byte[] bytesToHash)
		{
			IntPtr hashHandle = IntPtr.Zero;
			try
			{
				InitializeBCryptHash(key, keyLength, out hashHandle);
				AddData(hashHandle, bytesToHash);
				return FinishHash(hashHandle);
			}
			finally
			{
				if (hashHandle != IntPtr.Zero)
				{
					NativeMethods.BCryptDestroyHash(hashHandle);
					hashHandle = IntPtr.Zero;
				}
			}
		}

		private void AddData(IntPtr hashHandle, byte[] data)
		{
			GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				int num = NativeMethods.BCryptHashData(hashHandle, gCHandle.AddrOfPinnedObject(), (uint)data.Length, 0u);
				if (num != 0)
				{
					throw new Win32Exception(num, "BCryptHashData");
				}
			}
			finally
			{
				gCHandle.Free();
			}
		}

		private byte[] FinishHash(IntPtr hashHandle)
		{
			byte[] array = new byte[32];
			GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			try
			{
				int num = NativeMethods.BCryptFinishHash(hashHandle, gCHandle.AddrOfPinnedObject(), (uint)array.Length, 0u);
				if (num != 0)
				{
					throw new Win32Exception(num, "BCryptFinishData");
				}
				return array;
			}
			finally
			{
				gCHandle.Free();
			}
		}

		private void InitializeBCryptHash(SecureString base64EncodedPassword, int base64EncodedPasswordLength, out IntPtr hashHandle)
		{
			IntPtr bytes = IntPtr.Zero;
			uint bytesLength = 0u;
			try
			{
				Base64Helper.SecureStringToNativeBytes(base64EncodedPassword, base64EncodedPasswordLength, out bytes, out bytesLength);
				int num = NativeMethods.BCryptCreateHash(algorithmHandle, out hashHandle, IntPtr.Zero, 0u, bytes, bytesLength, 0u);
				if (num != 0)
				{
					throw new Win32Exception(num, "BCryptCreateHash");
				}
			}
			finally
			{
				if (bytes != IntPtr.Zero)
				{
					for (int i = 0; i < (int)bytesLength; i++)
					{
						Marshal.WriteByte(bytes, i, 0);
					}
					Marshal.FreeCoTaskMem(bytes);
					bytes = IntPtr.Zero;
					bytesLength = 0u;
				}
			}
		}
	}
}
