using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSubqueryScalarExpression : SqlScalarExpression
	{
		public SqlQuery Query
		{
			get;
		}

		private SqlSubqueryScalarExpression(SqlQuery query)
			: base(SqlObjectKind.SubqueryScalarExpression)
		{
			if (query == null)
			{
				throw new ArgumentNullException("query");
			}
			Query = query;
		}

		public static SqlSubqueryScalarExpression Create(SqlQuery query)
		{
			return new SqlSubqueryScalarExpression(query);
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
