using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlMemberIndexerScalarExpression : SqlScalarExpression
	{
		public SqlScalarExpression MemberExpression
		{
			get;
		}

		public SqlScalarExpression IndexExpression
		{
			get;
		}

		private SqlMemberIndexerScalarExpression(SqlScalarExpression memberExpression, SqlScalarExpression indexExpression)
			: base(SqlObjectKind.MemberIndexerScalarExpression)
		{
			if (memberExpression == null)
			{
				throw new ArgumentNullException("memberExpression");
			}
			if (indexExpression == null)
			{
				throw new ArgumentNullException("indexExpression");
			}
			MemberExpression = memberExpression;
			IndexExpression = indexExpression;
		}

		public static SqlMemberIndexerScalarExpression Create(SqlScalarExpression memberExpression, SqlScalarExpression indexExpression)
		{
			return new SqlMemberIndexerScalarExpression(memberExpression, indexExpression);
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

		public override void Accept(SqlScalarExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlScalarExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlScalarExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
