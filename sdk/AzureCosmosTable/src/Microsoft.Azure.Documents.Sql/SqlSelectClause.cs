using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSelectClause : SqlObject
	{
		public static readonly SqlSelectClause SelectStar = new SqlSelectClause(SqlSelectStarSpec.Singleton);

		public SqlSelectSpec SelectSpec
		{
			get;
		}

		public SqlTopSpec TopSpec
		{
			get;
		}

		public bool HasDistinct
		{
			get;
		}

		private SqlSelectClause(SqlSelectSpec selectSpec, SqlTopSpec topSpec = null, bool hasDistinct = false)
			: base(SqlObjectKind.SelectClause)
		{
			if (selectSpec == null)
			{
				throw new ArgumentNullException("selectSpec");
			}
			SelectSpec = selectSpec;
			TopSpec = topSpec;
			HasDistinct = hasDistinct;
		}

		public static SqlSelectClause Create(SqlSelectSpec selectSpec, SqlTopSpec topSpec = null, bool hasDistinct = false)
		{
			return new SqlSelectClause(selectSpec, topSpec, hasDistinct);
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
