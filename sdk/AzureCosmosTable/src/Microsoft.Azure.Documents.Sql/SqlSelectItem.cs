using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSelectItem : SqlObject
	{
		public SqlScalarExpression Expression
		{
			get;
		}

		public SqlIdentifier Alias
		{
			get;
		}

		private SqlSelectItem(SqlScalarExpression expression, SqlIdentifier alias)
			: base(SqlObjectKind.SelectItem)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			Expression = expression;
			Alias = alias;
		}

		public static SqlSelectItem Create(SqlScalarExpression expression, SqlIdentifier alias = null)
		{
			return new SqlSelectItem(expression, alias);
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
