using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlObjectProperty : SqlObject
	{
		public SqlPropertyName Name
		{
			get;
		}

		public SqlScalarExpression Expression
		{
			get;
		}

		private SqlObjectProperty(SqlPropertyName name, SqlScalarExpression expression)
			: base(SqlObjectKind.ObjectProperty)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			Name = name;
			Expression = expression;
		}

		public static SqlObjectProperty Create(SqlPropertyName name, SqlScalarExpression expression)
		{
			return new SqlObjectProperty(name, expression);
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
