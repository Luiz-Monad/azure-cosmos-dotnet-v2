using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents.Routing
{
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class Range<T> where T : IComparable<T>
	{
		public class MinComparer : IComparer<Range<T>>
		{
			public static readonly MinComparer Instance = new MinComparer(Range<T>.TComparer);

			private readonly IComparer<T> boundsComparer;

			private MinComparer(IComparer<T> boundsComparer)
			{
				this.boundsComparer = boundsComparer;
			}

			public int Compare(Range<T> left, Range<T> right)
			{
				int num = boundsComparer.Compare(left.Min, right.Min);
				if (num != 0 || left.IsMinInclusive == right.IsMinInclusive)
				{
					return num;
				}
				if (left.IsMinInclusive)
				{
					return -1;
				}
				return 1;
			}
		}

		public class MaxComparer : IComparer<Range<T>>
		{
			public static readonly MaxComparer Instance = new MaxComparer(Range<T>.TComparer);

			private readonly IComparer<T> boundsComparer;

			private MaxComparer(IComparer<T> boundsComparer)
			{
				this.boundsComparer = boundsComparer;
			}

			public int Compare(Range<T> left, Range<T> right)
			{
				int num = boundsComparer.Compare(left.Max, right.Max);
				if (num != 0 || left.IsMaxInclusive == right.IsMaxInclusive)
				{
					return num;
				}
				if (left.IsMaxInclusive)
				{
					return 1;
				}
				return -1;
			}
		}

		public static readonly IComparer<T> TComparer;

		[JsonProperty("min")]
		public T Min
		{
			get;
			private set;
		}

		[JsonProperty("max")]
		public T Max
		{
			get;
			private set;
		}

		[JsonProperty("isMinInclusive")]
		public bool IsMinInclusive
		{
			get;
			private set;
		}

		[JsonProperty("isMaxInclusive")]
		public bool IsMaxInclusive
		{
			get;
			private set;
		}

		public bool IsSingleValue
		{
			get
			{
				if (IsMinInclusive && IsMaxInclusive)
				{
					return TComparer.Compare(Min, Max) == 0;
				}
				return false;
			}
		}

		public bool IsEmpty
		{
			get
			{
				if (TComparer.Compare(Min, Max) == 0)
				{
					if (IsMinInclusive)
					{
						return !IsMaxInclusive;
					}
					return true;
				}
				return false;
			}
		}

		[JsonConstructor]
		public Range(T min, T max, bool isMinInclusive, bool isMaxInclusive)
		{
			if (min == null)
			{
				throw new ArgumentNullException("min");
			}
			if (max == null)
			{
				throw new ArgumentNullException("max");
			}
			Min = min;
			Max = max;
			IsMinInclusive = isMinInclusive;
			IsMaxInclusive = isMaxInclusive;
		}

		public static Range<T> GetPointRange(T value)
		{
			return new Range<T>(value, value, isMinInclusive: true, isMaxInclusive: true);
		}

		public static Range<T> GetEmptyRange(T value)
		{
			return new Range<T>(value, value, isMinInclusive: true, isMaxInclusive: false);
		}

		public bool Contains(T value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			int num = TComparer.Compare(Min, value);
			int num2 = TComparer.Compare(Max, value);
			if ((IsMinInclusive && num <= 0) || (!IsMinInclusive && num < 0))
			{
				if (!IsMaxInclusive || num2 < 0)
				{
					if (!IsMaxInclusive)
					{
						return num2 > 0;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public static bool CheckOverlapping(Range<T> range1, Range<T> range2)
		{
			if (range1 == null || range2 == null || range1.IsEmpty || range2.IsEmpty)
			{
				return false;
			}
			int num = TComparer.Compare(range1.Min, range2.Max);
			int num2 = TComparer.Compare(range2.Min, range1.Max);
			if (num <= 0 && num2 <= 0)
			{
				if ((num == 0 && (!range1.IsMinInclusive || !range2.IsMaxInclusive)) || (num2 == 0 && (!range2.IsMinInclusive || !range1.IsMaxInclusive)))
				{
					return false;
				}
				return true;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Range<T>);
		}

		public override int GetHashCode()
		{
			return (((((((0 * 397) ^ Min.GetHashCode()) * 397) ^ Max.GetHashCode()) * 397) ^ Convert.ToInt32(IsMinInclusive)) * 397) ^ Convert.ToInt32(IsMaxInclusive);
		}

		public bool Equals(Range<T> other)
		{
			if (other == null)
			{
				return false;
			}
			if (TComparer.Compare(Min, other.Min) == 0 && TComparer.Compare(Max, other.Max) == 0 && IsMinInclusive == other.IsMinInclusive)
			{
				return IsMaxInclusive == other.IsMaxInclusive;
			}
			return false;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}{1},{2}{3}", IsMinInclusive ? "[" : "(", Min, Max, IsMaxInclusive ? "]" : ")");
		}

		static Range()
		{
			object tComparer;
			if ((object)typeof(T) != typeof(string))
			{
				IComparer<T> @default = Comparer<T>.Default;
				tComparer = @default;
			}
			else
			{
				tComparer = (IComparer<T>)StringComparer.Ordinal;
			}
			TComparer = (IComparer<T>)tComparer;
		}
	}
}
