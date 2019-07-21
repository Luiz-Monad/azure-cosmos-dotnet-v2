namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlJoinCollectionExpression : SqlCollectionExpression
	{
		public SqlCollectionExpression LeftExpression
		{
			get;
		}

		public SqlCollectionExpression RightExpression
		{
			get;
		}

		private SqlJoinCollectionExpression(SqlCollectionExpression leftExpression, SqlCollectionExpression rightExpression)
			: base(SqlObjectKind.JoinCollectionExpression)
		{
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
		}

		public static SqlJoinCollectionExpression Create(SqlCollectionExpression leftExpression, SqlCollectionExpression rightExpression)
		{
			return new SqlJoinCollectionExpression(leftExpression, rightExpression);
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

		public override void Accept(SqlCollectionExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlCollectionExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlCollectionExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
