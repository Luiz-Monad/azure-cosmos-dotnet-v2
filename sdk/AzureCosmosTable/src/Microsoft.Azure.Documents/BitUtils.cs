namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// List of utility for doing bit (hacks).
	/// </summary>
	internal static class BitUtils
	{
		/// <summary>
		/// https://stackoverflow.com/questions/11376288/fast-computing-of-log2-for-64-bit-integers
		/// </summary>
		private static readonly int[] tab64 = new int[64]
		{
			63,
			0,
			58,
			1,
			59,
			47,
			53,
			2,
			60,
			39,
			48,
			27,
			54,
			33,
			42,
			3,
			61,
			51,
			37,
			40,
			49,
			18,
			28,
			20,
			55,
			30,
			34,
			11,
			43,
			14,
			22,
			4,
			62,
			57,
			46,
			52,
			38,
			26,
			32,
			41,
			50,
			36,
			17,
			19,
			29,
			10,
			13,
			21,
			56,
			45,
			25,
			31,
			35,
			16,
			9,
			12,
			44,
			24,
			15,
			8,
			23,
			7,
			6,
			5
		};

		public static long GetMostSignificantBit(long x)
		{
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			x |= x >> 32;
			return x & ~(x >> 1);
		}

		public static int FloorLog2(ulong value)
		{
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value |= value >> 32;
			return tab64[(value - (value >> 1)) * 571347909858961602L >> 58];
		}

		public static bool IsPowerOf2(ulong x)
		{
			return (x & (x - 1)) == 0;
		}

		public static int GetMostSignificantBitIndex(ulong x)
		{
			return FloorLog2(x);
		}

		public static long GetLeastSignificantBit(long x)
		{
			return (int)(x & -x);
		}

		public static int GetLeastSignificantBitIndex(long x)
		{
			return FloorLog2((ulong)GetLeastSignificantBit(x));
		}

		public static bool BitTestAndReset64(long input, int index, out long output)
		{
			bool result = (input & (1L << index)) != 0;
			output = (input &= ~(1L << index));
			return result;
		}
	}
}
