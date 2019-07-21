namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlCollectionExpression : SqlObject
	{
		protected SqlCollectionExpression(SqlObjectKind kind)
			: base(kind)
		{
		}

		public abstract void Accept(SqlCollectionExpressionVisitor visitor);

		public abstract TResult Accept<TResult>(SqlCollectionExpressionVisitor<TResult> visitor);

		public abstract TResult Accept<T, TResult>(SqlCollectionExpressionVisitor<T, TResult> visitor, T input);
	}
}
