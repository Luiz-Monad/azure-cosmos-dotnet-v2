using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlGroupByClause : SqlObject
	{
		public IReadOnlyList<SqlScalarExpression> Expressions
		{
			get;
		}

		private SqlGroupByClause(IReadOnlyList<SqlScalarExpression> expressions)
			: base(SqlObjectKind.GroupByClause)
		{
			if (expressions == null)
			{
				throw new ArgumentNullException(string.Format("{0}", "expressions"));
			}
			foreach (SqlScalarExpression expression in expressions)
			{
				if (expression == null)
				{
					throw new ArgumentException(string.Format("{0} must not have null items.", "expressions"));
				}
			}
			Expressions = expressions;
		}

		public static SqlGroupByClause Create(params SqlScalarExpression[] expressions)
		{
			return new SqlGroupByClause(expressions);
		}

		public static SqlGroupByClause Create(IReadOnlyList<SqlScalarExpression> expressions)
		{
			return new SqlGroupByClause(expressions);
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
