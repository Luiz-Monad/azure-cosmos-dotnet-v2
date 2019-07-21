using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Azure.Documents
{
	internal static class Base64Helper
	{
		/// <summary>
		/// Interprets <paramref name="secureString" /> as a Base64 string, and decodes it into a native byte array,
		/// which it returns.
		/// Avoids loading either the original Base64 or decoded binary into managed heap.
		/// </summary>
		/// <param name="secureString">Base64 string to decode</param>
		/// <param name="secureStringLength">Length of the Base64 string to decode</param>
		/// <param name="bytes">
		///   An IntPtr allocated with Marshal.AllocCoTaskMem, which, when the user is done, 
		///   MUST be zeroed out and then freed with Marshal.FreeCoTaskMem by the caller.
		/// </param>
		/// <param name="bytesLength">Number of bytes in the decoded binary currentCharacter</param>
		public static void SecureStringToNativeBytes(SecureString secureString, int secureStringLength, out IntPtr bytes, out uint bytesLength)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = Marshal.AllocCoTaskMem(secureStringLength);
				uint actualLength = 0u;
				ParseStringToIntPtr(secureString, intPtr, secureStringLength, out actualLength);
				bytes = intPtr;
				bytesLength = actualLength;
			}
			catch
			{
				if (intPtr != IntPtr.Zero)
				{
					for (int i = 0; i < secureStringLength; i++)
					{
						Marshal.WriteByte(intPtr, i, 0);
					}
					Marshal.FreeCoTaskMem(intPtr);
				}
				intPtr = IntPtr.Zero;
				bytes = IntPtr.Zero;
				bytesLength = 0u;
				throw;
			}
		}

		private static void ParseStringToIntPtr(SecureString secureString, IntPtr bytes, int allocationSize, out uint actualLength)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = CustomTypeExtensions.SecureStringToCoTaskMemAnsi(secureString);
				int num = 0;
				int num2 = 0;
				byte b = 0;
				while (num < allocationSize && (b = Marshal.ReadByte(intPtr, num)) != 0)
				{
					uint num3 = 0u;
					int num4 = 0;
					for (int i = 0; i < 4; i++)
					{
						if (num >= allocationSize)
						{
							break;
						}
						b = Marshal.ReadByte(intPtr, num);
						int num5 = 0;
						if (b >= 65 && b <= 90)
						{
							num5 = b - 65;
						}
						else if (b >= 97 && b <= 122)
						{
							num5 = b - 97 + 26;
						}
						else if (b >= 48 && b <= 57)
						{
							num5 = b - 48 + 52;
						}
						else
						{
							switch (b)
							{
							case 43:
								num5 = 62;
								break;
							case 47:
								num5 = 63;
								break;
							default:
								num5 = -1;
								break;
							}
						}
						num++;
						if (num5 == -1)
						{
							i--;
						}
						else
						{
							num3 <<= 6;
							num3 |= (byte)num5;
							num4 += 6;
						}
					}
					if (num2 + num4 / 8 > allocationSize)
					{
						throw new ArgumentException("allocationSize");
					}
					num3 <<= 24 - num4;
					for (int j = 0; j < num4 / 8; j++)
					{
						Marshal.WriteByte(bytes, num2, (byte)((num3 & 0xFF0000) >> 16));
						num2++;
						num3 <<= 8;
					}
				}
				actualLength = (uint)num2;
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.ZeroFreeCoTaskMemAnsi(intPtr);
					intPtr = IntPtr.Zero;
				}
			}
		}
	}
}
