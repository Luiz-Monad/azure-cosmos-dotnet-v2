namespace Microsoft.Azure.Documents.Query.Aggregation
{
	/// <summary>
	/// Interface for all aggregators that are used to aggregate across continuation and partition boundaries.
	/// </summary>
	internal interface IAggregator
	{
		/// <summary>
		/// Adds an item to the aggregation.
		/// </summary>
		/// <param name="item">The item to add to the aggregation.</param>
		void Aggregate(object item);

		/// <summary>
		/// Gets the result of the aggregation.
		/// </summary>
		/// <returns>The result of the aggregation.</returns>
		object GetResult();
	}
}
