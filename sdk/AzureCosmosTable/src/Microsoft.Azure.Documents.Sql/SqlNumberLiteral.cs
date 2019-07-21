using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlNumberLiteral : SqlLiteral
	{
		private const int Capacity = 256;

		private static readonly Dictionary<long, SqlNumberLiteral> FrequentLongs = Enumerable.Range(-256, 256).ToDictionary((Func<int, long>)((int x) => x), (Func<int, SqlNumberLiteral>)((int x) => new SqlNumberLiteral(x)));

		private static readonly Dictionary<double, SqlNumberLiteral> FrequentDoubles = Enumerable.Range(-256, 256).ToDictionary((Func<int, double>)((int x) => x), (Func<int, SqlNumberLiteral>)((int x) => new SqlNumberLiteral((double)x)));

		public Number64 Value
		{
			get;
		}

		private SqlNumberLiteral(Number64 value)
			: base(SqlObjectKind.NumberLiteral)
		{
			Value = value;
		}

		public static SqlNumberLiteral Create(decimal number)
		{
			if (number >= new decimal(long.MinValue) && number <= new decimal(long.MaxValue) && number % decimal.One == decimal.Zero)
			{
				return Create(Convert.ToInt64(number));
			}
			return Create(Convert.ToDouble(number));
		}

		public static SqlNumberLiteral Create(double number)
		{
			if (!FrequentDoubles.TryGetValue(number, out SqlNumberLiteral value))
			{
				return new SqlNumberLiteral(number);
			}
			return value;
		}

		public static SqlNumberLiteral Create(long number)
		{
			if (!FrequentLongs.TryGetValue(number, out SqlNumberLiteral value))
			{
				return new SqlNumberLiteral(number);
			}
			return value;
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

		public override void Accept(SqlLiteralVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override TResult Accept<TResult>(SqlLiteralVisitor<TResult> visitor)
		{
			return visitor.Visit(this);
		}
	}
}
