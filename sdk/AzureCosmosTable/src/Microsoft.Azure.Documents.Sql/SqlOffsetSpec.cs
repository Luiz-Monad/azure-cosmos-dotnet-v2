using System.Linq;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlOffsetSpec : SqlObject
	{
		private const int PremadeOffsetIndex = 256;

		private static readonly SqlOffsetSpec[] PremadeOffsetSpecs = (from offset in Enumerable.Range(0, 256)
		select new SqlOffsetSpec(offset)).ToArray();

		public long Offset
		{
			get;
		}

		private SqlOffsetSpec(long offset)
			: base(SqlObjectKind.OffsetSpec)
		{
			Offset = offset;
		}

		public static SqlOffsetSpec Create(long value)
		{
			if (value < 256 && value >= 0)
			{
				return PremadeOffsetSpecs[value];
			}
			return new SqlOffsetSpec(value);
		}

		public override void Accept(SqlObjectVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
