using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlOrderByItem : SqlObject
	{
		public SqlScalarExpression Expression
		{
			get;
		}

		public bool IsDescending
		{
			get;
		}

		private SqlOrderByItem(SqlScalarExpression expression, bool isDescending)
			: base(SqlObjectKind.OrderByItem)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			Expression = expression;
			IsDescending = isDescending;
		}

		public static SqlOrderByItem Create(SqlScalarExpression expression, bool isDescending)
		{
			return new SqlOrderByItem(expression, isDescending);
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
