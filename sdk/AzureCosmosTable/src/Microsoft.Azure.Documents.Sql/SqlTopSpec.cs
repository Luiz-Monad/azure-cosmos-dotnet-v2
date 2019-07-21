using System.Linq;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlTopSpec : SqlObject
	{
		private const int PremadeTopIndex = 256;

		private static readonly SqlTopSpec[] PremadeTopSpecs = (from top in Enumerable.Range(0, 256)
		select new SqlTopSpec(top)).ToArray();

		public long Count
		{
			get;
		}

		private SqlTopSpec(long count)
			: base(SqlObjectKind.TopSpec)
		{
			Count = count;
		}

		public static SqlTopSpec Create(long value)
		{
			if (value < 256 && value >= 0)
			{
				return PremadeTopSpecs[value];
			}
			return new SqlTopSpec(value);
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
