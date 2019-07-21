using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlAliasedCollectionExpression : SqlCollectionExpression
	{
		public SqlCollection Collection
		{
			get;
		}

		public SqlIdentifier Alias
		{
			get;
		}

		private SqlAliasedCollectionExpression(SqlCollection collection, SqlIdentifier alias)
			: base(SqlObjectKind.AliasedCollectionExpression)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			Collection = collection;
			Alias = alias;
		}

		public static SqlAliasedCollectionExpression Create(SqlCollection collection, SqlIdentifier alias)
		{
			return new SqlAliasedCollectionExpression(collection, alias);
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
