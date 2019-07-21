namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlPathExpression : SqlObject
	{
		public SqlPathExpression ParentPath
		{
			get;
		}

		protected SqlPathExpression(SqlObjectKind kind, SqlPathExpression parentPath)
			: base(kind)
		{
			ParentPath = parentPath;
		}

		public abstract void Accept(SqlPathExpressionVisitor visitor);

		public abstract TResult Accept<TResult>(SqlPathExpressionVisitor<TResult> visitor);
	}
}
