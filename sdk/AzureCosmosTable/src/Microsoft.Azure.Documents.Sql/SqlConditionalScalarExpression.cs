using System;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlConditionalScalarExpression : SqlScalarExpression
	{
		public SqlScalarExpression ConditionExpression
		{
			get;
		}

		public SqlScalarExpression FirstExpression
		{
			get;
		}

		public SqlScalarExpression SecondExpression
		{
			get;
		}

		private SqlConditionalScalarExpression(SqlScalarExpression condition, SqlScalarExpression first, SqlScalarExpression second)
			: base(SqlObjectKind.ConditionalScalarExpression)
		{
			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}
			if (first == null)
			{
				throw new ArgumentNullException("first");
			}
			if (second == null)
			{
				throw new ArgumentNullException("second");
			}
			ConditionExpression = condition;
			FirstExpression = first;
			SecondExpression = second;
		}

		public static SqlConditionalScalarExpression Create(SqlScalarExpression condition, SqlScalarExpression first, SqlScalarExpression second)
		{
			return new SqlConditionalScalarExpression(condition, first, second);
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
