using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlArrayCreateScalarExpression : SqlScalarExpression
	{
		private static readonly SqlArrayCreateScalarExpression Empty = new SqlArrayCreateScalarExpression(new List<SqlScalarExpression>());

		public IReadOnlyList<SqlScalarExpression> Items
		{
			get;
		}

		private SqlArrayCreateScalarExpression(IReadOnlyList<SqlScalarExpression> items)
			: base(SqlObjectKind.ArrayCreateScalarExpression)
		{
			if (items == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "items"));
			}
			foreach (SqlScalarExpression item in items)
			{
				if (item == null)
				{
					throw new ArgumentException(string.Format("{0} must not have null items.", "item"));
				}
			}
			Items = new List<SqlScalarExpression>(items);
		}

		public static SqlArrayCreateScalarExpression Create()
		{
			return Empty;
		}

		public static SqlArrayCreateScalarExpression Create(params SqlScalarExpression[] items)
		{
			return new SqlArrayCreateScalarExpression(items);
		}

		public static SqlArrayCreateScalarExpression Create(IReadOnlyList<SqlScalarExpression> items)
		{
			return new SqlArrayCreateScalarExpression(items);
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
