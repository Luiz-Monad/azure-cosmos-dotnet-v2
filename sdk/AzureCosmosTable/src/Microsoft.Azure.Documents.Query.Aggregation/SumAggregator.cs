using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Query.Aggregation
{
	/// <summary>
	/// Concrete implementation of IAggregator that can take the global sum from the local sum of multiple partitions and continuations.
	/// Let sum_i,j be the sum from the ith continuation in the jth partition, 
	/// then the sum for the entire query is SUM(sum_i,j for all i and j).
	/// </summary>
	internal sealed class SumAggregator : IAggregator
	{
		/// <summary>
		/// The global sum.
		/// </summary>
		private double globalSum;

		/// <summary>
		/// Adds a local sum to the global sum.
		/// </summary>
		/// <param name="localSum">The local sum.</param>
		public void Aggregate(object localSum)
		{
			if (Undefined.Value.Equals(localSum))
			{
				globalSum = double.NaN;
			}
			else
			{
				globalSum += Convert.ToDouble(localSum, CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// Gets the current sum.
		/// </summary>
		/// <returns>The current sum.</returns>
		public object GetResult()
		{
			if (double.IsNaN(globalSum))
			{
				return Undefined.Value;
			}
			return globalSum;
		}
	}
}
