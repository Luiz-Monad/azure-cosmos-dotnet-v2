using System.Linq;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlLimitSpec : SqlObject
	{
		private const int PremadeLimitIndex = 256;

		private static readonly SqlLimitSpec[] PremadeLimitSpecs = (from limit in Enumerable.Range(0, 256)
		select new SqlLimitSpec(limit)).ToArray();

		public long Limit
		{
			get;
		}

		private SqlLimitSpec(long limit)
			: base(SqlObjectKind.LimitSpec)
		{
			Limit = limit;
		}

		public static SqlLimitSpec Create(long value)
		{
			if (value < 256 && value >= 0)
			{
				return PremadeLimitSpecs[value];
			}
			return new SqlLimitSpec(value);
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
