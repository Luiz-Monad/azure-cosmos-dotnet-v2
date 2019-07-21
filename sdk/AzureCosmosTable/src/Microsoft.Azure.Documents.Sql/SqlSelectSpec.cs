namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlSelectSpec : SqlObject
	{
		protected SqlSelectSpec(SqlObjectKind kind)
			: base(kind)
		{
		}

		public abstract void Accept(SqlSelectSpecVisitor visitor);

		public abstract TResult Accept<TResult>(SqlSelectSpecVisitor<TResult> visitor);

		public abstract TResult Accept<T, TResult>(SqlSelectSpecVisitor<T, TResult> visitor, T input);
	}
}
