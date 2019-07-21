using System;

namespace Microsoft.Azure.Documents
{
	[Flags]
	internal enum QueryFeatures : ulong
	{
		None = 0x0,
		Aggregate = 0x1,
		CompositeAggregate = 0x2,
		Distinct = 0x4,
		GroupBy = 0x8,
		MultipleAggregates = 0x10,
		MultipleOrderBy = 0x20,
		OffsetAndLimit = 0x40,
		OrderBy = 0x80,
		Top = 0x100
	}
}
