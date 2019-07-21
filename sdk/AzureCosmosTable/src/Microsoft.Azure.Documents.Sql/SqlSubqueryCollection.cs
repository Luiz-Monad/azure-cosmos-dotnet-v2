using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSubqueryCollection : SqlCollection
	{
		public SqlQuery Query
		{
			get;
		}

		private SqlSubqueryCollection(SqlQuery query)
			: base(SqlObjectKind.SubqueryCollection)
		{
			if (query == null)
			{
				throw new ArgumentNullException("query");
			}
			Query = query;
		}

		public static SqlSubqueryCollection Create(SqlQuery query)
		{
			return new SqlSubqueryCollection(query);
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

		public override void Accept(SqlCollectionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlCollectionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlCollectionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
