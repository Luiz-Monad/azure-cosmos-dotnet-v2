using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Struct that represents either a double or 64 bit int
	/// </summary>
	[JsonConverter(typeof(Number64JsonConverter))]
	internal struct Number64 : IComparable<Number64>, IEquatable<Number64>
	{
		/// <summary>
		/// Represents an extended double number with 62-bit mantissa which is capable of representing a 64-bit integer with no precision loss
		/// </summary>
		private struct DoubleEx : IEquatable<DoubleEx>, IComparable<DoubleEx>
		{
			/// <summary>
			/// The double if the value is a double.
			/// </summary>
			public double DoubleValue
			{
				get;
			}

			/// <summary>
			/// The long if the value is a long.
			/// </summary>
			public ushort ExtraBits
			{
				get;
			}

			private DoubleEx(double doubleValue, ushort extraBits)
			{
				DoubleValue = doubleValue;
				ExtraBits = extraBits;
			}

			/// <summary>
			/// Returns if two DoubleEx are equal.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether the left is equal to the right.</returns>
			public static bool operator ==(DoubleEx left, DoubleEx right)
			{
				return left.Equals(right);
			}

			/// <summary>
			/// Returns if two DoubleEx are not equal.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether the left is not equal to the right.</returns>
			public static bool operator !=(DoubleEx left, DoubleEx right)
			{
				return !(left == right);
			}

			/// <summary>
			/// Returns if one DoubleEx is less than another DoubleEx.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether left is less than right.</returns>
			public static bool operator <(DoubleEx left, DoubleEx right)
			{
				return left.CompareTo(right) < 0;
			}

			/// <summary>
			/// Returns if one DoubleEx is greater than another DoubleEx.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether left is greater than right.</returns>
			public static bool operator >(DoubleEx left, DoubleEx right)
			{
				return left.CompareTo(right) > 0;
			}

			/// <summary>
			/// Returns if one DoubleEx is less than or equal to another DoubleEx.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether left is less than or equal to the right.</returns>
			public static bool operator <=(DoubleEx left, DoubleEx right)
			{
				return !(right < left);
			}

			/// <summary>
			/// Returns if one Number64 is greater than or equal to another Number64.
			/// </summary>
			/// <param name="left">The left hand side of the operator.</param>
			/// <param name="right">The right hand side of the operator.</param>
			/// <returns>Whether left is greater than or equal to the right.</returns>
			public static bool operator >=(DoubleEx left, DoubleEx right)
			{
				return !(left < right);
			}

			/// <summary>
			/// Implicitly converts a long to DoubleEx.
			/// </summary>
			/// <param name="value">The int to convert.</param>
			public static implicit operator DoubleEx(long value)
			{
				if (value == long.MinValue)
				{
					return new DoubleEx(value, 0);
				}
				long num = Math.Abs(value);
				int mostSignificantBitIndex = BitUtils.GetMostSignificantBitIndex((ulong)num);
				ushort extraBits;
				double doubleValue;
				if (mostSignificantBitIndex > 52 && mostSignificantBitIndex - BitUtils.GetLeastSignificantBitIndex(num) > 52)
				{
					int num2 = mostSignificantBitIndex;
					long num3 = (long)num2 + 1023L << 52;
					long num4 = (num << 62 - num2) & 0x3FFFFFFFFFFFFFFF;
					extraBits = (ushort)((num4 & 0x3FF) << 6);
					num4 >>= 10;
					long num5 = num3 | num4;
					if (value != num)
					{
						num5 |= long.MinValue;
					}
					doubleValue = BitConverter.Int64BitsToDouble(num5);
				}
				else
				{
					doubleValue = value;
					extraBits = 0;
				}
				return new DoubleEx(doubleValue, extraBits);
			}

			/// <summary>
			/// Implicitly converts a DoubleEx to long.
			/// </summary>
			/// <param name="value">The int to convert.</param>
			public static implicit operator long(DoubleEx value)
			{
				long output;
				if (value.ExtraBits != 0)
				{
					output = BitConverter.DoubleToInt64Bits(value.DoubleValue);
					bool num = BitUtils.BitTestAndReset64(output, 63, out output);
					int num2 = (int)((output >> 52) - 1023);
					output <<= 10;
					output = ((output | 0x4000000000000000) & long.MaxValue);
					output |= (long)value.ExtraBits >> 6;
					output >>= 62 - num2;
					if (num)
					{
						output = -output;
					}
				}
				else
				{
					output = (long)value.DoubleValue;
				}
				return output;
			}

			/// <summary>
			/// Implicitly converts a double to DoubleEx.
			/// </summary>
			/// <param name="value">The int to convert.</param>
			public static implicit operator DoubleEx(double value)
			{
				return new DoubleEx(value, 0);
			}

			/// <summary>
			/// Returns whether this instance equals another object.
			/// </summary>
			/// <param name="obj">The object to compare to.</param>
			/// <returns>Whether this instance equals another object.</returns>
			public override bool Equals(object obj)
			{
				if ((object)this == obj)
				{
					return true;
				}
				if (obj is DoubleEx)
				{
					return Equals((DoubleEx)obj);
				}
				return false;
			}

			/// <summary>
			/// Returns whether this DoubleEx equals another DoubleEx.
			/// </summary>
			/// <param name="other">The DoubleEx to compare to.</param>
			/// <returns>Whether this DoubleEx equals another DoubleEx.</returns>
			public bool Equals(DoubleEx other)
			{
				if (DoubleValue == other.DoubleValue)
				{
					return ExtraBits == other.ExtraBits;
				}
				return false;
			}

			/// <summary>
			/// Gets a hash code for this instance.
			/// </summary>
			/// <returns>The hash code for this instance.</returns>
			public override int GetHashCode()
			{
				return 0 ^ DoubleValue.GetHashCode() ^ ExtraBits.GetHashCode();
			}

			public int CompareTo(DoubleEx other)
			{
				int num = DoubleValue.CompareTo(other.DoubleValue);
				if (num == 0)
				{
					num = ExtraBits.CompareTo(other.ExtraBits) * Math.Sign(DoubleValue);
				}
				return num;
			}
		}

		private sealed class Number64JsonConverter : JsonConverter
		{
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				Number64 number = (Number64)value;
				writer.WriteValue(number.IsDouble ? ToDouble(number) : ((double)ToLong(number)));
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				Number64 number = (reader.TokenType != JsonToken.Float) ? ((Number64)(long)reader.Value) : ((Number64)(double)reader.Value);
				return number;
			}

			public override bool CanConvert(Type objectType)
			{
				return (object)objectType == typeof(User);
			}
		}

		/// <summary>
		/// Maximum Number64.
		/// </summary>
		public static readonly Number64 MaxValue = new Number64(double.MaxValue);

		/// <summary>
		/// Maximum Number64.
		/// </summary>
		public static readonly Number64 MinValue = new Number64(double.MinValue);

		/// <summary>
		/// The double if the value is a double.
		/// </summary>
		private readonly double? doubleValue;

		/// <summary>
		/// The long if the value is a long.
		/// </summary>
		private readonly long? longValue;

		public bool IsInteger => longValue.HasValue;

		public bool IsDouble => doubleValue.HasValue;

		public bool IsInfinity
		{
			get
			{
				if (!IsInteger)
				{
					return double.IsInfinity(doubleValue.Value);
				}
				return false;
			}
		}

		public bool IsNaN
		{
			get
			{
				if (!IsInteger)
				{
					return double.IsNaN(doubleValue.Value);
				}
				return false;
			}
		}

		private Number64(double value)
		{
			doubleValue = value;
			longValue = null;
		}

		private Number64(long value)
		{
			longValue = value;
			doubleValue = null;
		}

		public override string ToString()
		{
			return ToString(null, CultureInfo.CurrentCulture);
		}

		public string ToString(string format)
		{
			return ToString(format, CultureInfo.CurrentCulture);
		}

		public string ToString(IFormatProvider formatProvider)
		{
			return ToString(null, formatProvider);
		}

		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (IsDouble)
			{
				return ToDouble(this).ToString(format, formatProvider);
			}
			return ToLong(this).ToString(format, formatProvider);
		}

		/// <summary>
		/// Returns if one Number64 is less than another Number64.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether left is less than right.</returns>
		public static bool operator <(Number64 left, Number64 right)
		{
			return left.CompareTo(right) < 0;
		}

		/// <summary>
		/// Returns if one Number64 is greater than another Number64.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether left is greater than right.</returns>
		public static bool operator >(Number64 left, Number64 right)
		{
			return left.CompareTo(right) > 0;
		}

		/// <summary>
		/// Returns if one Number64 is less than or equal to another Number64.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether left is less than or equal to the right.</returns>
		public static bool operator <=(Number64 left, Number64 right)
		{
			return !(right < left);
		}

		/// <summary>
		/// Returns if one Number64 is greater than or equal to another Number64.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether left is greater than or equal to the right.</returns>
		public static bool operator >=(Number64 left, Number64 right)
		{
			return !(left < right);
		}

		/// <summary>
		/// Returns if two Number64 are equal.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether the left is equal to the right.</returns>
		public static bool operator ==(Number64 left, Number64 right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// Returns if two Number64 are not equal.
		/// </summary>
		/// <param name="left">The left hand side of the operator.</param>
		/// <param name="right">The right hand side of the operator.</param>
		/// <returns>Whether the left is not equal to the right.</returns>
		public static bool operator !=(Number64 left, Number64 right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Implicitly converts a long to Number64.
		/// </summary>
		/// <param name="value">The long to convert.</param>
		public static implicit operator Number64(long value)
		{
			return new Number64(value);
		}

		/// <summary>
		/// Implicitly converts a double to Number64.
		/// </summary>
		/// <param name="value">The double to convert.</param>
		public static implicit operator Number64(double value)
		{
			return new Number64(value);
		}

		public static long ToLong(Number64 number64)
		{
			if (number64.IsInteger)
			{
				return number64.longValue.Value;
			}
			return (long)number64.doubleValue.Value;
		}

		public static double ToDouble(Number64 number64)
		{
			if (number64.IsDouble)
			{
				return number64.doubleValue.Value;
			}
			return number64.longValue.Value;
		}

		/// <summary>
		/// Compares this value to an object.
		/// </summary>
		/// <param name="value">The value to compare to.</param>
		/// <returns>The comparison.</returns>
		public int CompareTo(object value)
		{
			if (value == null)
			{
				return 1;
			}
			if (value is Number64)
			{
				return CompareTo((Number64)value);
			}
			throw new ArgumentException("Value must be a Number64.");
		}

		/// <summary>
		/// Compares this Number64 to another instance of the Number64 type.
		/// </summary>
		/// <param name="other">The other instance to compare to.</param>
		/// <returns>
		/// A negative number if this instance is less than the other instance.
		/// Zero if they are the same.
		/// A positive number if this instance is greater than the other instance.
		/// </returns>
		public int CompareTo(Number64 other)
		{
			if (IsInteger && other.IsInteger)
			{
				return longValue.Value.CompareTo(other.longValue.Value);
			}
			if (IsDouble && other.IsDouble)
			{
				return doubleValue.Value.CompareTo(other.doubleValue.Value);
			}
			DoubleEx doubleEx = IsDouble ? doubleValue.Value : ((double)longValue.Value);
			DoubleEx other2 = other.IsDouble ? other.doubleValue.Value : ((double)other.longValue.Value);
			return doubleEx.CompareTo(other2);
		}

		/// <summary>
		/// Returns whether this instance equals another object.
		/// </summary>
		/// <param name="obj">The object to compare to.</param>
		/// <returns>Whether this instance equals another object.</returns>
		public override bool Equals(object obj)
		{
			if ((object)this == obj)
			{
				return true;
			}
			if (obj is Number64)
			{
				return Equals((Number64)obj);
			}
			return false;
		}

		/// <summary>
		/// Returns whether this Number64 equals another Number64.
		/// </summary>
		/// <param name="other">The Number64 to compare to.</param>
		/// <returns>Whether this Number64 equals another Number64.</returns>
		public bool Equals(Number64 other)
		{
			return CompareTo(other) == 0;
		}

		/// <summary>
		/// Gets a hash code for this instance.
		/// </summary>
		/// <returns>The hash code for this instance.</returns>
		public override int GetHashCode()
		{
			return ((!IsDouble) ? ((DoubleEx)longValue.Value) : ((DoubleEx)doubleValue.Value)).GetHashCode();
		}
	}
}
