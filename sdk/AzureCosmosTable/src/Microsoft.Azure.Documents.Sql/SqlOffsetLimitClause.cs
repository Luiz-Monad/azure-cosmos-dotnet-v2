using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlOffsetLimitClause : SqlObject
	{
		public SqlOffsetSpec OffsetSpec
		{
			get;
		}

		public SqlLimitSpec LimitSpec
		{
			get;
		}

		private SqlOffsetLimitClause(SqlOffsetSpec offsetSpec, SqlLimitSpec limitSpec)
			: base(SqlObjectKind.OffsetLimitClause)
		{
			if (offsetSpec == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "offsetSpec"));
			}
			if (limitSpec == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "limitSpec"));
			}
			OffsetSpec = offsetSpec;
			LimitSpec = limitSpec;
		}

		public static SqlOffsetLimitClause Create(SqlOffsetSpec offsetSpec, SqlLimitSpec limitSpec)
		{
			return new SqlOffsetLimitClause(offsetSpec, limitSpec);
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
