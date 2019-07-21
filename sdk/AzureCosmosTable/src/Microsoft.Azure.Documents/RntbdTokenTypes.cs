namespace Microsoft.Azure.Documents
{
	internal enum RntbdTokenTypes : byte
	{
		Byte = 0,
		UShort = 1,
		ULong = 2,
		Long = 3,
		ULongLong = 4,
		LongLong = 5,
		Guid = 6,
		SmallString = 7,
		String = 8,
		ULongString = 9,
		SmallBytes = 10,
		Bytes = 11,
		ULongBytes = 12,
		Float = 13,
		Double = 14,
		Invalid = byte.MaxValue
	}
}
