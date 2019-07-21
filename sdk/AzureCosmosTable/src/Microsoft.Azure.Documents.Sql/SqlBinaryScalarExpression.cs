using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlBinaryScalarExpression : SqlScalarExpression
	{
		public SqlBinaryScalarOperatorKind OperatorKind
		{
			get;
		}

		public SqlScalarExpression LeftExpression
		{
			get;
		}

		public SqlScalarExpression RightExpression
		{
			get;
		}

		private SqlBinaryScalarExpression(SqlBinaryScalarOperatorKind operatorKind, SqlScalarExpression leftExpression, SqlScalarExpression rightExpression)
			: base(SqlObjectKind.BinaryScalarExpression)
		{
			if (leftExpression == null || rightExpression == null)
			{
				throw new ArgumentNullException();
			}
			OperatorKind = operatorKind;
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
		}

		public static SqlBinaryScalarExpression Create(SqlBinaryScalarOperatorKind operatorKind, SqlScalarExpression leftExpression, SqlScalarExpression rightExpression)
		{
			return new SqlBinaryScalarExpression(operatorKind, leftExpression, rightExpression);
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
