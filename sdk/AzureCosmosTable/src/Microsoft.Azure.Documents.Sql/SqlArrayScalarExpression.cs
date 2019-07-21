using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlArrayScalarExpression : SqlScalarExpression
	{
		public SqlQuery SqlQuery
		{
			get;
		}

		private SqlArrayScalarExpression(SqlQuery sqlQuery)
			: base(SqlObjectKind.ArrayScalarExpression)
		{
			if (sqlQuery == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null", "sqlQuery"));
			}
			SqlQuery = sqlQuery;
		}

		public static SqlArrayScalarExpression Create(SqlQuery sqlQuery)
		{
			return new SqlArrayScalarExpression(sqlQuery);
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

		public override void Accept(SqlScalarExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlScalarExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlScalarExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
