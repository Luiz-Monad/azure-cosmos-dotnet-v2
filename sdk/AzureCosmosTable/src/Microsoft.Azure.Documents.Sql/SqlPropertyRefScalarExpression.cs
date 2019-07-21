using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlPropertyRefScalarExpression : SqlScalarExpression
	{
		public SqlIdentifier PropertyIdentifier
		{
			get;
		}

		public SqlScalarExpression MemberExpression
		{
			get;
		}

		private SqlPropertyRefScalarExpression(SqlScalarExpression memberExpression, SqlIdentifier propertyIdentifier)
			: base(SqlObjectKind.PropertyRefScalarExpression)
		{
			if (propertyIdentifier == null)
			{
				throw new ArgumentNullException("propertyIdentifier");
			}
			MemberExpression = memberExpression;
			PropertyIdentifier = propertyIdentifier;
		}

		public static SqlPropertyRefScalarExpression Create(SqlScalarExpression memberExpression, SqlIdentifier propertyIdentifier)
		{
			return new SqlPropertyRefScalarExpression(memberExpression, propertyIdentifier);
		}

		public override void Accept(SqlObjectVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlObjectVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override void Accept(SqlScalarExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlScalarExpressionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlObjectVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}

		public override TResult Accept<T, TResult>(SqlScalarExpressionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
