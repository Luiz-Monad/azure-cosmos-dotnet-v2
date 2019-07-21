namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlCollection : SqlObject
	{
		protected SqlCollection(SqlObjectKind kind)
			: base(kind)
		{
		}

		public abstract void Accept(SqlCollectionVisitor visitor);

		public abstract TResult Accept<TResult>(SqlCollectionVisitor<TResult> visitor);

		public abstract TResult Accept<T, TResult>(SqlCollectionVisitor<T, TResult> visitor, T input);
	}
}
