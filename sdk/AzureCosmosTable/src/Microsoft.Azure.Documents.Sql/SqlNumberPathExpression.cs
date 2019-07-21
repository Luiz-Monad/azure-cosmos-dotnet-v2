namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlNumberPathExpression : SqlPathExpression
	{
		public SqlNumberLiteral Value
		{
			get;
		}

		private SqlNumberPathExpression(SqlPathExpression parentPath, SqlNumberLiteral value)
			: base(SqlObjectKind.NumberPathExpression, parentPath)
		{
			Value = value;
		}

		public static SqlNumberPathExpression Create(SqlPathExpression parentPath, SqlNumberLiteral value)
		{
			return new SqlNumberPathExpression(parentPath, value);
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

		public override void Accept(SqlPathExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlPathExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}
	}
}
