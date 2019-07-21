namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlLiteral : SqlObject
	{
		protected SqlLiteral(SqlObjectKind kind)
			: base(kind)
		{
		}

		public abstract void Accept(SqlLiteralVisitor visitor);

		public abstract TResult Accept<TResult>(SqlLiteralVisitor<TResult> visitor);
	}
}
