namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlBetweenScalarExpression : SqlScalarExpression
	{
		public SqlScalarExpression Expression
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

		public bool IsNot
		{
			get;
		}

		private SqlBetweenScalarExpression(SqlScalarExpression expression, SqlScalarExpression leftExpression, SqlScalarExpression rightExpression, bool isNot = false)
			: base(SqlObjectKind.BetweenScalarExpression)
		{
			Expression = expression;
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
			IsNot = isNot;
		}

		public static SqlBetweenScalarExpression Create(SqlScalarExpression expression, SqlScalarExpression leftExpression, SqlScalarExpression rightExpression, bool isNot = false)
		{
			return new SqlBetweenScalarExpression(expression, leftExpression, rightExpression, isNot);
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
