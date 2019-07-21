namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlNullLiteral : SqlLiteral
	{
		public static readonly SqlNullLiteral Singleton = new SqlNullLiteral();

		private SqlNullLiteral()
			: base(SqlObjectKind.NullLiteral)
		{
		}

		public static SqlNullLiteral Create()
		{
			return Singleton;
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

		public override void Accept(SqlLiteralVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlLiteralVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}
	}
}
