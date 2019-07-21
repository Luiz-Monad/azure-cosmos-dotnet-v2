using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlLiteralArrayCollection : SqlCollection
	{
		private static readonly SqlLiteralArrayCollection Empty = new SqlLiteralArrayCollection(new List<SqlScalarExpression>());

		public IReadOnlyList<SqlScalarExpression> Items
		{
			get;
		}

		private SqlLiteralArrayCollection(IReadOnlyList<SqlScalarExpression> items)
			: base(SqlObjectKind.LiteralArrayCollection)
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

		public static SqlLiteralArrayCollection Create(params SqlScalarExpression[] items)
		{
			return new SqlLiteralArrayCollection(items);
		}

		public static SqlLiteralArrayCollection Create(IReadOnlyList<SqlScalarExpression> items)
		{
			return Create(items);
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

		public override void Accept(SqlCollectionVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlCollectionVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlCollectionVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
