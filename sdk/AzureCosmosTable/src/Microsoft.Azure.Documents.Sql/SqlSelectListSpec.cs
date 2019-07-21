using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlSelectListSpec : SqlSelectSpec
	{
		public IReadOnlyList<SqlSelectItem> Items
		{
			get;
		}

		private SqlSelectListSpec(IReadOnlyList<SqlSelectItem> items)
			: base(SqlObjectKind.SelectListSpec)
		{
			if (items == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "items"));
			}
			foreach (SqlSelectItem item in items)
			{
				if (item == null)
				{
					throw new ArgumentException(string.Format("{0} must not contain null items.", "items"));
				}
			}
			Items = new List<SqlSelectItem>(items);
		}

		public static SqlSelectListSpec Create(params SqlSelectItem[] items)
		{
			return new SqlSelectListSpec(items);
		}

		public static SqlSelectListSpec Create(IReadOnlyList<SqlSelectItem> items)
		{
			return new SqlSelectListSpec(items);
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

		public override void Accept(SqlSelectSpecVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlSelectSpecVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}

		public override TResult Accept<T, TResult>(SqlSelectSpecVisitor<T, TResult> visitor, T input)
		{
			return visitor.Visit(this, input);
		}
	}
}
