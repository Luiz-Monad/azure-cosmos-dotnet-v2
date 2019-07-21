using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlObjectCreateScalarExpression : SqlScalarExpression
	{
		public IEnumerable<SqlObjectProperty> Properties
		{
			get;
		}

		private SqlObjectCreateScalarExpression(IEnumerable<SqlObjectProperty> properties)
			: base(SqlObjectKind.ObjectCreateScalarExpression)
		{
			if (properties == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "properties"));
			}
			foreach (SqlObjectProperty property in properties)
			{
				if (property == null)
				{
					throw new ArgumentException(string.Format("{0} must not have null items.", "properties"));
				}
			}
			Properties = new List<SqlObjectProperty>(properties);
		}

		public static SqlObjectCreateScalarExpression Create(params SqlObjectProperty[] properties)
		{
			return new SqlObjectCreateScalarExpression(properties);
		}

		public static SqlObjectCreateScalarExpression Create(IEnumerable<SqlObjectProperty> properties)
		{
			return new SqlObjectCreateScalarExpression(properties);
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
