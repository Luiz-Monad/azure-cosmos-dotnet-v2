using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlOrderbyClause : SqlObject
	{
		public IReadOnlyList<SqlOrderByItem> OrderbyItems
		{
			get;
		}

		private SqlOrderbyClause(IReadOnlyList<SqlOrderByItem> orderbyItems)
			: base(SqlObjectKind.OrderByClause)
		{
			if (orderbyItems == null)
			{
				throw new ArgumentNullException("orderbyItems");
			}
			foreach (SqlOrderByItem orderbyItem in orderbyItems)
			{
				if (orderbyItem == null)
				{
					throw new ArgumentException(string.Format("{0} must have have null items.", "sqlOrderbyItem"));
				}
			}
			OrderbyItems = orderbyItems;
		}

		public static SqlOrderbyClause Create(params SqlOrderByItem[] orderbyItems)
		{
			return new SqlOrderbyClause(orderbyItems);
		}

		public static SqlOrderbyClause Create(IReadOnlyList<SqlOrderByItem> orderbyItems)
		{
			return new SqlOrderbyClause(orderbyItems);
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
	}
}
