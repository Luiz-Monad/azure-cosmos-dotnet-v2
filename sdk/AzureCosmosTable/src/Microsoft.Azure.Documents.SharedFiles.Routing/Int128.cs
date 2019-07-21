using System;
using System.Numerics;

namespace Microsoft.Azure.Documents.SharedFiles.Routing
{
	internal struct Int128
	{
		private readonly BigInteger value;

		private static readonly BigInteger MaxBigIntValue = new BigInteger(new byte[16]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			128
		});

		public static readonly Int128 MaxValue = new Int128(new BigInteger(new byte[16]
		{
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			127
		}));

		public byte[] Bytes
		{
			get
			{
				byte[] array = value.ToByteArray();
				if (array.Length < 16)
				{
					byte[] array2 = new byte[16];
					Buffer.BlockCopy(array, 0, array2, 0, array.Length);
					return array2;
				}
				return array;
			}
		}

		private Int128(BigInteger value)
		{
			this.value = value % MaxBigIntValue;
		}

		public static implicit operator Int128(int n)
		{
			return new Int128(new BigInteger(n));
		}

		public Int128(byte[] data)
		{
			if (data.Length != 16)
			{
				throw new ArgumentException("data");
			}
			value = new BigInteger(data);
			if (value > MaxValue.value)
			{
				throw new ArgumentException();
			}
		}

		public static Int128 operator *(Int128 left, Int128 right)
		{
			return new Int128(left.value * right.value);
		}

		public static Int128 operator +(Int128 left, Int128 right)
		{
			return new Int128(left.value + right.value);
		}

		public static Int128 operator -(Int128 left, Int128 right)
		{
			return new Int128(left.value - right.value);
		}

		public static Int128 operator /(Int128 left, Int128 right)
		{
			return new Int128(left.value / right.value);
		}

		public static bool operator >(Int128 left, Int128 right)
		{
			return left.value > right.value;
		}

		public static bool operator <(Int128 left, Int128 right)
		{
			return left.value < right.value;
		}
	}
}
