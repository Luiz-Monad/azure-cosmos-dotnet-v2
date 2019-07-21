using System;

namespace Microsoft.Azure.Documents.Partitioning
{
	/// <summary>
	/// A class that represents a range used by the RangePartitionResolver class in the Azure Cosmos DB service.
	/// </summary>
	/// <typeparam name="T">Any type that can be used for range comparison.</typeparam>
	/// <remarks>
	/// Support for classes used with IPartitionResolver is now obsolete. It's recommended that you use
	/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
	/// </remarks>
	[Obsolete("Support for classes used with IPartitionResolver is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
	public class Range<T> : IEquatable<Range<T>>, IComparable<Range<T>> where T : IComparable<T>, IEquatable<T>
	{
		/// <summary>
		/// Gets the low value in the range.
		/// </summary>
		/// <value>
		/// The low value in the range.
		/// </value>
		public T Low
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the high value in the range.
		/// </summary>
		/// <value>
		/// The high value in the range.
		/// </value>
		public T High
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> class using the specified low and high values.
		/// </summary>
		/// <param name="low">The low value in the range.</param>
		/// <param name="high">The high value in the range.</param>
		/// <exception cref="T:System.ArgumentException">Throws an exception if the range is invalid (low is greater than high).</exception>
		public Range(T low, T high)
		{
			if (low.CompareTo(high) > 0)
			{
				throw new ArgumentException(ClientResources.InvalidRangeError);
			}
			Low = low;
			High = high;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> class for a single value.
		/// </summary>
		/// <param name="point">A value that is used to create the range.</param>
		public Range(T point)
		{
			T val3 = Low = (High = point);
		}

		/// <summary>
		/// Checks if two ranges are equal.
		/// </summary>
		/// <param name="other">the input range to be compared with this range.</param>
		/// <returns>Returns true if the input range is equal to this range.</returns>
		public bool Equals(Range<T> other)
		{
			if (Low.Equals(other.Low) && High.Equals(other.High))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Compares two ranges.
		/// </summary>
		/// <param name="other">The other range to compare to.</param>
		/// <returns>Returns -1 if the range is smaller than the passed range, 1 if bigger and 0 if equal.</returns>
		public int CompareTo(Range<T> other)
		{
			if (Equals(other))
			{
				return 0;
			}
			if (Low.CompareTo(other.Low) < 0 || High.CompareTo(other.High) < 0)
			{
				return -1;
			}
			return 1;
		}

		/// <summary>
		/// Checks if the range contains a key.
		/// </summary>
		/// <param name="point">The key to be checked if in the range.</param>
		/// <returns>Returns true if the key is in the range.</returns>
		public bool Contains(T point)
		{
			if (point.CompareTo(Low) >= 0 && point.CompareTo(High) <= 0)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks if the range contains another range..
		/// </summary>
		/// <param name="other">The input range to be checked if it's contained in this range.</param>
		/// <returns>Returns true if the input range is contained in the range.</returns>
		public bool Contains(Range<T> other)
		{
			if (other.Low.CompareTo(Low) >= 0 && other.High.CompareTo(High) <= 0)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Creates the hashcode for the range.
		/// </summary>
		/// <returns>Returns the hashcode for the range.</returns>
		public override int GetHashCode()
		{
			return Low.GetHashCode() + High.GetHashCode();
		}

		/// <summary>
		/// Checks if the range <paramref name="other" /> intersects with this range.
		/// </summary>
		/// <param name="other">the input <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> to be compared with this range.</param>
		/// <returns>Returns true if the two ranges intersect with each other.</returns>
		public bool Intersect(Range<T> other)
		{
			T val = (Low.CompareTo(other.Low) >= 0) ? Low : other.Low;
			T other2 = (High.CompareTo(other.High) <= 0) ? High : other.High;
			if (val.CompareTo(other2) <= 0)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Converts the range to a string in the form of "low,high"
		/// </summary>
		/// <returns>Returns A string representation of the range.</returns>
		public override string ToString()
		{
			return string.Join(",", Low.ToString(), High.ToString());
		}
	}
}
