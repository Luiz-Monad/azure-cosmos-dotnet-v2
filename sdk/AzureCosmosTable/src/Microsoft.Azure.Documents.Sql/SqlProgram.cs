using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlProgram : SqlObject
	{
		public SqlQuery Query
		{
			get;
		}

		private SqlProgram(SqlQuery query)
			: base(SqlObjectKind.Program)
		{
			if (query == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "query"));
			}
			Query = query;
		}

		public static SqlProgram Create(SqlQuery query)
		{
			return new SqlProgram(query);
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
