using System;

namespace Microsoft.Azure.Documents.Routing
{
	internal static class MurmurHash3
	{
		public static uint Hash32(byte[] bytes, long length, uint seed = 0u)
		{
			uint num = 3432918353u;
			uint num2 = 461845907u;
			uint num3 = seed;
			for (int i = 0; i < length - 3; i += 4)
			{
				uint num4 = BitConverter.ToUInt32(bytes, i);
				num4 *= num;
				num4 = RotateLeft32(num4, 15);
				num4 *= num2;
				num3 ^= num4;
				num3 = RotateLeft32(num3, 13);
				num3 = (uint)((int)(num3 * 5) + -430675100);
			}
			uint num5 = 0u;
			long num6 = length & 3;
			long num7 = num6 - 1;
			if ((ulong)num7 <= 2uL)
			{
				switch (num7)
				{
				case 2L:
					num5 = (uint)((int)num5 ^ (bytes[length - 1] << 16));
					num5 = (uint)((int)num5 ^ (bytes[length - 2] << 8));
					num5 ^= bytes[length - 3];
					break;
				case 1L:
					num5 = (uint)((int)num5 ^ (bytes[length - 1] << 8));
					num5 ^= bytes[length - 2];
					break;
				case 0L:
					num5 ^= bytes[length - 1];
					break;
				}
			}
			num5 *= num;
			num5 = RotateLeft32(num5, 15);
			num5 *= num2;
			num3 ^= num5;
			num3 = (uint)((int)num3 ^ (int)length);
			num3 ^= num3 >> 16;
			num3 = (uint)((int)num3 * -2048144789);
			num3 ^= num3 >> 13;
			num3 = (uint)((int)num3 * -1028477387);
			return num3 ^ (num3 >> 16);
		}

		public static ulong Hash64(byte[] bytes, int length, ulong seed = 0uL)
		{
			int num4 = length / 8;
			ulong num = seed;
			int i;
			for (i = 0; i < length - 7; i += 8)
			{
				ulong num2 = BitConverter.ToUInt64(bytes, i);
				num2 = (ulong)((long)num2 * -8663945395140668459L);
				num2 = RotateLeft64(num2, 31);
				num2 *= 5545529020109919103L;
				num ^= num2;
				num = RotateLeft64(num, 27);
				num = num * 5 + 1390208809;
			}
			ulong num3 = 0uL;
			switch (length & 7)
			{
			case 7:
				num3 ^= (ulong)bytes[i + 6] << 48;
				break;
			case 6:
				num3 ^= (ulong)bytes[i + 5] << 40;
				break;
			case 5:
				num3 ^= (ulong)bytes[i + 4] << 32;
				break;
			case 4:
				num3 ^= (ulong)bytes[i + 3] << 24;
				break;
			case 3:
				num3 ^= (ulong)bytes[i + 2] << 16;
				break;
			case 2:
				num3 ^= (ulong)bytes[i + 1] << 8;
				break;
			case 1:
				num3 ^= bytes[i];
				break;
			}
			num3 = (ulong)((long)num3 * -8663945395140668459L);
			num3 = RotateLeft64(num3, 31);
			num3 *= 5545529020109919103L;
			num ^= num3;
			num = (ulong)((long)num ^ (long)length);
			num ^= num >> 33;
			num = (ulong)((long)num * -49064778989728563L);
			num ^= num >> 33;
			num = (ulong)((long)num * -4265267296055464877L);
			return num ^ (num >> 33);
		}

		public static UInt128 Hash128(byte[] bytes, int length, UInt128 seed)
		{
			ulong num = seed.GetHigh();
			ulong num2 = seed.GetLow();
			int i;
			for (i = 0; i < length - 15; i += 16)
			{
				ulong num3 = BitConverter.ToUInt64(bytes, i);
				ulong num4 = BitConverter.ToUInt64(bytes, i + 8);
				num3 = (ulong)((long)num3 * -8663945395140668459L);
				num3 = RotateLeft64(num3, 31);
				num3 *= 5545529020109919103L;
				num ^= num3;
				num = RotateLeft64(num, 27);
				num += num2;
				num = num * 5 + 1390208809;
				num4 *= 5545529020109919103L;
				num4 = RotateLeft64(num4, 33);
				num4 = (ulong)((long)num4 * -8663945395140668459L);
				num2 ^= num4;
				num2 = RotateLeft64(num2, 31);
				num2 += num;
				num2 = num2 * 5 + 944331445;
			}
			ulong num5 = 0uL;
			ulong num6 = 0uL;
			int num7 = length & 0xF;
			if (num7 >= 15)
			{
				num6 ^= (ulong)bytes[i + 14] << 48;
			}
			if (num7 >= 14)
			{
				num6 ^= (ulong)bytes[i + 13] << 40;
			}
			if (num7 >= 13)
			{
				num6 ^= (ulong)bytes[i + 12] << 32;
			}
			if (num7 >= 12)
			{
				num6 ^= (ulong)bytes[i + 11] << 24;
			}
			if (num7 >= 11)
			{
				num6 ^= (ulong)bytes[i + 10] << 16;
			}
			if (num7 >= 10)
			{
				num6 ^= (ulong)bytes[i + 9] << 8;
			}
			if (num7 >= 9)
			{
				num6 ^= bytes[i + 8];
			}
			num6 *= 5545529020109919103L;
			num6 = RotateLeft64(num6, 33);
			num6 = (ulong)((long)num6 * -8663945395140668459L);
			num2 ^= num6;
			if (num7 >= 8)
			{
				num5 ^= (ulong)bytes[i + 7] << 56;
			}
			if (num7 >= 7)
			{
				num5 ^= (ulong)bytes[i + 6] << 48;
			}
			if (num7 >= 6)
			{
				num5 ^= (ulong)bytes[i + 5] << 40;
			}
			if (num7 >= 5)
			{
				num5 ^= (ulong)bytes[i + 4] << 32;
			}
			if (num7 >= 4)
			{
				num5 ^= (ulong)bytes[i + 3] << 24;
			}
			if (num7 >= 3)
			{
				num5 ^= (ulong)bytes[i + 2] << 16;
			}
			if (num7 >= 2)
			{
				num5 ^= (ulong)bytes[i + 1] << 8;
			}
			if (num7 >= 1)
			{
				num5 ^= bytes[i];
			}
			num5 = (ulong)((long)num5 * -8663945395140668459L);
			num5 = RotateLeft64(num5, 31);
			num5 *= 5545529020109919103L;
			num ^= num5;
			num = (ulong)((long)num ^ (long)length);
			num2 = (ulong)((long)num2 ^ (long)length);
			num += num2;
			num2 += num;
			num ^= num >> 33;
			num = (ulong)((long)num * -49064778989728563L);
			num ^= num >> 33;
			num = (ulong)((long)num * -4265267296055464877L);
			num ^= num >> 33;
			num2 ^= num2 >> 33;
			num2 = (ulong)((long)num2 * -49064778989728563L);
			num2 ^= num2 >> 33;
			num2 = (ulong)((long)num2 * -4265267296055464877L);
			num2 ^= num2 >> 33;
			num += num2;
			num2 += num;
			if (!BitConverter.IsLittleEndian)
			{
				num = Reverse(num);
				num2 = Reverse(num2);
			}
			return UInt128.Create(num, num2);
		}

		public static ulong Reverse(ulong value)
		{
			ulong num = value & 0xFF;
			ulong num2 = (value >> 8) & 0xFF;
			ulong num3 = (value >> 16) & 0xFF;
			ulong num4 = (value >> 24) & 0xFF;
			ulong num5 = (value >> 32) & 0xFF;
			ulong num6 = (value >> 40) & 0xFF;
			ulong num7 = (value >> 48) & 0xFF;
			ulong num8 = (value >> 56) & 0xFF;
			return (num << 56) | (num2 << 48) | (num3 << 40) | (num4 << 32) | (num5 << 24) | (num6 << 16) | (num7 << 8) | num8;
		}

		private static uint RotateLeft32(uint n, int numBits)
		{
			return (n << numBits) | (n >> 32 - numBits);
		}

		private static ulong RotateLeft64(ulong n, int numBits)
		{
			return (n << numBits) | (n >> 64 - numBits);
		}
	}
}
