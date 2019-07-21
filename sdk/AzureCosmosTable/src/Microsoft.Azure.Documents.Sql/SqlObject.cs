namespace Microsoft.Azure.Documents.Sql
{
	internal abstract class SqlObject
	{
		public SqlObjectKind Kind
		{
			get;
		}

		protected SqlObject(SqlObjectKind kind)
		{
			Kind = kind;
		}

		public abstract void Accept(SqlObjectVisitor visitor);

		public abstract TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor);

		public abstract TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input);

		public override string ToString()
		{
			return Serialize(prettyPrint: false);
		}

		public override int GetHashCode()
		{
			return Accept(SqlObjectHasher.Singleton);
		}

		public string PrettyPrint()
		{
			return Serialize(prettyPrint: true);
		}

		public SqlObject GetObfuscatedObject()
		{
			SqlObjectObfuscator visitor = new SqlObjectObfuscator();
			return Accept(visitor);
		}

		private string Serialize(bool prettyPrint)
		{
			SqlObjectTextSerializer sqlObjectTextSerializer = new SqlObjectTextSerializer(prettyPrint);
			Accept(sqlObjectTextSerializer);
			return sqlObjectTextSerializer.ToString();
		}
	}
}
