using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Query.Aggregation
{
	/// <summary>
	/// Concrete implementation of IAggregator that can take the global count from the local counts from multiple partitions and continuations.
	/// Let count_i,j be the count from the ith continuation in the jth partition, 
	/// then the count for the entire query is SUM(count_i,j for all i and j)
	/// </summary>
	internal sealed class CountAggregator : IAggregator
	{
		/// <summary>
		/// The global count.
		/// </summary>
		private long globalCount;

		/// <summary>
		/// Adds a count to the running count.
		/// </summary>
		/// <param name="localCount">The count to add.</param>
		public void Aggregate(object localCount)
		{
			globalCount += Convert.ToInt64(localCount, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Gets the global count.
		/// </summary>
		/// <returns>The global count.</returns>
		public object GetResult()
		{
			return globalCount;
		}
	}
}
