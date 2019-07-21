using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlFromClause : SqlObject
	{
		public SqlCollectionExpression Expression
		{
			get;
		}

		private SqlFromClause(SqlCollectionExpression expression)
			: base(SqlObjectKind.FromClause)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			Expression = expression;
		}

		public static SqlFromClause Create(SqlCollectionExpression expression)
		{
			return new SqlFromClause(expression);
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
