using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSelectValueSpec : SqlSelectSpec
	{
		public SqlScalarExpression Expression
		{
			get;
		}

		private SqlSelectValueSpec(SqlScalarExpression expression)
			: base(SqlObjectKind.SelectValueSpec)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			Expression = expression;
		}

		public static SqlSelectValueSpec Create(SqlScalarExpression expression)
		{
			return new SqlSelectValueSpec(expression);
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

		public override void Accept(SqlSelectSpecVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlSelectSpecVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlSelectSpecVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
