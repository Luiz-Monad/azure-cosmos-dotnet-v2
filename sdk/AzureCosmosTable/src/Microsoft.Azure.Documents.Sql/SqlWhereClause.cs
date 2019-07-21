using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlWhereClause : SqlObject
	{
		public SqlScalarExpression FilterExpression
		{
			get;
		}

		private SqlWhereClause(SqlScalarExpression filterExpression)
			: base(SqlObjectKind.WhereClause)
		{
			if (filterExpression == null)
			{
				throw new ArgumentNullException("filterExpression");
			}
			FilterExpression = filterExpression;
		}

		public static SqlWhereClause Create(SqlScalarExpression filterExpression)
		{
			return new SqlWhereClause(filterExpression);
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
