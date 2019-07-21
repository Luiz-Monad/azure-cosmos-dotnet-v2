using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlUnaryScalarExpression : SqlScalarExpression
	{
		public SqlUnaryScalarOperatorKind OperatorKind
		{
			get;
		}

		public SqlScalarExpression Expression
		{
			get;
		}

		private SqlUnaryScalarExpression(SqlUnaryScalarOperatorKind operatorKind, SqlScalarExpression expression)
			: base(SqlObjectKind.UnaryScalarExpression)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			OperatorKind = operatorKind;
			Expression = expression;
		}

		public static SqlUnaryScalarExpression Create(SqlUnaryScalarOperatorKind operatorKind, SqlScalarExpression expression)
		{
			return new SqlUnaryScalarExpression(operatorKind, expression);
		}

		public override void Accept(SqlObjectVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override void Accept(SqlScalarExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlScalarExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}

		public override TResult Accept<T, TResult>(SqlScalarExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
