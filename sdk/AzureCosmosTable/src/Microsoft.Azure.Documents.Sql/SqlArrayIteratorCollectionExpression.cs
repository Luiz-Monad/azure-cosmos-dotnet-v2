using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlArrayIteratorCollectionExpression : SqlCollectionExpression
	{
		public SqlIdentifier Alias
		{
			get;
		}

		public SqlCollection Collection
		{
			get;
		}

		private SqlArrayIteratorCollectionExpression(SqlIdentifier alias, SqlCollection collection)
			: base(SqlObjectKind.ArrayIteratorCollectionExpression)
		{
			if (alias == null)
			{
				throw new ArgumentNullException("alias");
			}
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			Alias = alias;
			Collection = collection;
		}

		public static SqlArrayIteratorCollectionExpression Create(SqlIdentifier alias, SqlCollection collection)
		{
			return new SqlArrayIteratorCollectionExpression(alias, collection);
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
