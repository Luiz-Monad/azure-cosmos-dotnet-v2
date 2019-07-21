using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal class SqlQuery : SqlObject
	{
		public SqlSelectClause SelectClause
		{
			get;
		}

		public SqlFromClause FromClause
		{
			get;
		}

		public SqlWhereClause WhereClause
		{
			get;
		}

		public SqlGroupByClause GroupByClause
		{
			get;
		}

		public SqlOrderbyClause OrderbyClause
		{
			get;
		}

		public SqlOffsetLimitClause OffsetLimitClause
		{
			get;
		}

		protected SqlQuery(SqlSelectClause selectClause, SqlFromClause fromClause, SqlWhereClause whereClause, SqlGroupByClause groupByClause, SqlOrderbyClause orderbyClause, SqlOffsetLimitClause offsetLimitClause)
			: base(SqlObjectKind.Query)
		{
			if (selectClause == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "selectClause"));
			}
			SelectClause = selectClause;
			FromClause = fromClause;
			WhereClause = whereClause;
			GroupByClause = groupByClause;
			OrderbyClause = orderbyClause;
			OffsetLimitClause = offsetLimitClause;
		}

		public static SqlQuery Create(SqlSelectClause selectClause, SqlFromClause fromClause, SqlWhereClause whereClause, SqlGroupByClause groupByClause, SqlOrderbyClause orderByClause, SqlOffsetLimitClause offsetLimitClause)
		{
			return new SqlQuery(selectClause, fromClause, whereClause, groupByClause, orderByClause, offsetLimitClause);
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
