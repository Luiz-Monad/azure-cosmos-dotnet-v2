using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Accumlator that acts as a builder of FetchExecutionRanges
	/// </summary>
	internal sealed class FetchExecutionRangeAccumulator
	{
		private readonly DateTime constructionTime;

		private readonly Stopwatch stopwatch;

		private List<FetchExecutionRange> fetchExecutionRanges;

		private DateTime startTime;

		private DateTime endTime;

		private bool isFetching;

		/// <summary>
		/// Initializes a new instance of the FetchExecutionRangeStopwatch class.
		/// </summary>
		public FetchExecutionRangeAccumulator()
		{
			constructionTime = DateTime.UtcNow;
			stopwatch = Stopwatch.StartNew();
			fetchExecutionRanges = new List<FetchExecutionRange>();
		}

		/// <summary>
		/// Gets the FetchExecutionRanges and resets the accumulator.
		/// </summary>
		/// <returns>the SchedulingMetricsResult.</returns>
		public IEnumerable<FetchExecutionRange> GetExecutionRanges()
		{
			List<FetchExecutionRange> result = fetchExecutionRanges;
			fetchExecutionRanges = new List<FetchExecutionRange>();
			return result;
		}

		/// <summary>
		/// Updates the most recent start time internally.
		/// </summary>
		public void BeginFetchRange()
		{
			if (!isFetching)
			{
				startTime = constructionTime.Add(stopwatch.Elapsed);
				isFetching = true;
			}
		}

		/// <summary>
		/// Updates the most recent end time internally and constructs a new FetchExecutionRange
		/// </summary>
		/// <param name="partitionIdentifier">The identifier for the partition.</param>
		/// <param name="activityId">The activity of the fetch.</param>
		/// <param name="numberOfDocuments">The number of documents that were fetched for this range.</param>
		/// <param name="retryCount">The number of times we retried for this fetch execution range.</param>
		public void EndFetchRange(string partitionIdentifier, string activityId, long numberOfDocuments, long retryCount)
		{
			if (isFetching)
			{
				endTime = constructionTime.Add(stopwatch.Elapsed);
				FetchExecutionRange item = new FetchExecutionRange(partitionIdentifier, activityId, startTime, endTime, numberOfDocuments, retryCount);
				fetchExecutionRanges.Add(item);
				isFetching = false;
			}
		}
	}
}
