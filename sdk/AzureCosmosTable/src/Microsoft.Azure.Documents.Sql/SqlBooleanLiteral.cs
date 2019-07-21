namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlBooleanLiteral : SqlLiteral
	{
		public static readonly SqlBooleanLiteral True = new SqlBooleanLiteral(value: true);

		public static readonly SqlBooleanLiteral False = new SqlBooleanLiteral(value: false);

		public bool Value
		{
			get;
		}

		private SqlBooleanLiteral(bool value)
			: base(SqlObjectKind.BooleanLiteral)
		{
			Value = value;
		}

		public static SqlBooleanLiteral Create(bool value)
		{
			if (!value)
			{
				return False;
			}
			return True;
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
