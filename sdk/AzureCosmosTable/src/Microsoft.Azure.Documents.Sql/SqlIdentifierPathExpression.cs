using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlIdentifierPathExpression : SqlPathExpression
	{
		public SqlIdentifier Value
		{
			get;
		}

		private SqlIdentifierPathExpression(SqlPathExpression parentPath, SqlIdentifier value)
			: base(SqlObjectKind.IdentifierPathExpression, parentPath)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			Value = value;
		}

		public static SqlIdentifierPathExpression Create(SqlPathExpression parentPath, SqlIdentifier value)
		{
			return new SqlIdentifierPathExpression(parentPath, value);
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

		public override void Accept(SqlPathExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlPathExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}
	}
}
