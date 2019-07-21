using System;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Documents
{
	[StructLayout(LayoutKind.Explicit)]
	internal struct RntbdTokenValue
	{
		[FieldOffset(0)]
		public byte valueByte;

		[FieldOffset(0)]
		public ushort valueUShort;

		[FieldOffset(0)]
		public uint valueULong;

		[FieldOffset(0)]
		public ulong valueULongLong;

		[FieldOffset(0)]
		public int valueLong;

		[FieldOffset(0)]
		public float valueFloat;

		[FieldOffset(0)]
		public double valueDouble;

		[FieldOffset(0)]
		public long valueLongLong;

		[FieldOffset(8)]
		public Guid valueGuid;

		[FieldOffset(24)]
		public byte[] valueBytes;
	}
}
