namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// Types of partition key component
	/// </summary>
	/// <remarks>
	/// Some of the values might not be used, but this class to be consistent with the backend enum
	/// </remarks>
	internal enum PartitionKeyComponentType
	{
		Undefined = 0,
		Null = 1,
		False = 2,
		True = 3,
		MinNumber = 4,
		Number = 5,
		MaxNumber = 6,
		MinString = 7,
		String = 8,
		MaxString = 9,
		Int64 = 10,
		Int32 = 11,
		Int16 = 12,
		Int8 = 13,
		Uint64 = 14,
		Uint32 = 0xF,
		Uint16 = 0x10,
		Uint8 = 17,
		Binary = 18,
		Guid = 19,
		Float = 20,
		Infinity = 0xFF
	}
}
