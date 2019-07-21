using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Stores client side QueryMetrics.
	/// </summary>
	internal sealed class ClientSideMetrics
	{
		public static readonly ClientSideMetrics Zero = new ClientSideMetrics(0L, 0.0, new List<FetchExecutionRange>(), new List<Tuple<string, SchedulingTimeSpan>>());

		private readonly long retries;

		private readonly double requestCharge;

		private readonly IEnumerable<FetchExecutionRange> fetchExecutionRanges;

		private readonly IEnumerable<Tuple<string, SchedulingTimeSpan>> partitionSchedulingTimeSpans;

		/// <summary>
		/// Gets number of retries in the Azure Cosmos database service (see IRetryPolicy.cs).
		/// </summary>
		public long Retries => retries;

		/// <summary>
		/// Gets the request charge for this continuation of the query.
		/// </summary>
		public double RequestCharge => requestCharge;

		/// <summary>
		/// Gets the Fetch Execution Ranges for this continuation of the query.
		/// </summary>
		public IEnumerable<FetchExecutionRange> FetchExecutionRanges => fetchExecutionRanges;

		/// <summary>
		/// Gets the Partition Scheduling TimeSpans for this query.
		/// </summary>
		public IEnumerable<Tuple<string, SchedulingTimeSpan>> PartitionSchedulingTimeSpans => partitionSchedulingTimeSpans;

		/// <summary>
		/// Initializes a new instance of the ClientSideMetrics class.
		/// </summary>
		/// <param name="retries">The number of retries required to execute the query.</param>
		/// <param name="requestCharge">The request charge incurred from executing the query.</param>
		/// <param name="fetchExecutionRanges">The fetch execution ranges from executing the query.</param>
		/// <param name="partitionSchedulingTimeSpans">The partition scheduling timespans from the query.</param>
		[JsonConstructor]
		public ClientSideMetrics(long retries, double requestCharge, IEnumerable<FetchExecutionRange> fetchExecutionRanges, IEnumerable<Tuple<string, SchedulingTimeSpan>> partitionSchedulingTimeSpans)
		{
			if (fetchExecutionRanges == null)
			{
				throw new ArgumentNullException("fetchExecutionRanges");
			}
			if (partitionSchedulingTimeSpans == null)
			{
				throw new ArgumentNullException("partitionSchedulingTimeSpans");
			}
			this.retries = retries;
			this.requestCharge = requestCharge;
			this.fetchExecutionRanges = fetchExecutionRanges;
			this.partitionSchedulingTimeSpans = partitionSchedulingTimeSpans;
		}

		/// <summary>
		/// Creates a new ClientSideMetrics that is the sum of all elements in an IEnumerable.
		/// </summary>
		/// <param name="clientSideMetricsList">The IEnumerable to aggregate.</param>
		/// <returns>A new ClientSideMetrics that is the sum of all elements in an IEnumerable.</returns>
		public static ClientSideMetrics CreateFromIEnumerable(IEnumerable<ClientSideMetrics> clientSideMetricsList)
		{
			long num = 0L;
			double num2 = 0.0;
			IEnumerable<FetchExecutionRange> first = new List<FetchExecutionRange>();
			IEnumerable<Tuple<string, SchedulingTimeSpan>> first2 = new List<Tuple<string, SchedulingTimeSpan>>();
			if (clientSideMetricsList == null)
			{
				throw new ArgumentNullException("clientSideQueryMetricsList");
			}
			foreach (ClientSideMetrics clientSideMetrics in clientSideMetricsList)
			{
				num += clientSideMetrics.retries;
				num2 += clientSideMetrics.requestCharge;
				first = first.Concat(clientSideMetrics.fetchExecutionRanges);
				first2 = first2.Concat(clientSideMetrics.partitionSchedulingTimeSpans);
			}
			return new ClientSideMetrics(num, num2, first, first2);
		}
	}
}
