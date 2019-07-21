using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlLiteralScalarExpression : SqlScalarExpression
	{
		public static readonly SqlLiteralScalarExpression SqlNullLiteralScalarExpression = new SqlLiteralScalarExpression(SqlNullLiteral.Create());

		public static readonly SqlLiteralScalarExpression SqlTrueLiteralScalarExpression = new SqlLiteralScalarExpression(SqlBooleanLiteral.True);

		public static readonly SqlLiteralScalarExpression SqlFalseLiteralScalarExpression = new SqlLiteralScalarExpression(SqlBooleanLiteral.False);

		public static readonly SqlLiteralScalarExpression SqlUndefinedLiteralScalarExpression = new SqlLiteralScalarExpression(SqlUndefinedLiteral.Singleton);

		public SqlLiteral Literal
		{
			get;
		}

		private SqlLiteralScalarExpression(SqlLiteral literal)
			: base(SqlObjectKind.LiteralScalarExpression)
		{
			if (literal == null)
			{
				throw new ArgumentNullException("literal");
			}
			Literal = literal;
		}

		public static SqlLiteralScalarExpression Create(SqlLiteral sqlLiteral)
		{
			if (sqlLiteral == SqlBooleanLiteral.True)
			{
				return SqlTrueLiteralScalarExpression;
			}
			if (sqlLiteral == SqlBooleanLiteral.False)
			{
				return SqlFalseLiteralScalarExpression;
			}
			if (sqlLiteral == SqlNullLiteral.Singleton)
			{
				return SqlNullLiteralScalarExpression;
			}
			if (sqlLiteral == SqlUndefinedLiteral.Singleton)
			{
				return SqlUndefinedLiteralScalarExpression;
			}
			return new SqlLiteralScalarExpression(sqlLiteral);
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
