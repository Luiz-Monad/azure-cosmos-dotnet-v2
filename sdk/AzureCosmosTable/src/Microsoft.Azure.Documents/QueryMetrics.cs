using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Query metrics in the Azure Cosmos database service.
	/// This metric represents a moving average for a set of queries whose metrics have been aggregated together.
	/// </summary>
	public sealed class QueryMetrics
	{
		/// <summary>
		/// QueryMetrics that with all members having default (but not null) members.
		/// </summary>
		internal static readonly QueryMetrics Zero = new QueryMetrics(0L, 0L, 0L, 0L, 0L, default(TimeSpan), QueryPreparationTimes.Zero, default(TimeSpan), default(TimeSpan), default(TimeSpan), RuntimeExecutionTimes.Zero, default(TimeSpan), ClientSideMetrics.Zero);

		private readonly long retrievedDocumentCount;

		private readonly long retrievedDocumentSize;

		private readonly long outputDocumentCount;

		private readonly long outputDocumentSize;

		private readonly long indexHitDocumentCount;

		private readonly TimeSpan totalQueryExecutionTime;

		private readonly QueryPreparationTimes queryPreparationTimes;

		private readonly TimeSpan indexLookupTime;

		private readonly TimeSpan documentLoadTime;

		private readonly TimeSpan vmExecutionTime;

		private readonly RuntimeExecutionTimes runtimeExecutionTimes;

		private readonly TimeSpan documentWriteTime;

		private readonly ClientSideMetrics clientSideMetrics;

		private readonly QueryEngineTimes queryEngineTimes;

		/// <summary>
		/// Gets the total query time in the Azure Cosmos database service.
		/// </summary>
		public TimeSpan TotalTime => totalQueryExecutionTime;

		/// <summary>
		/// Gets the number of documents retrieved during query in the Azure Cosmos database service.
		/// </summary>
		public long RetrievedDocumentCount => retrievedDocumentCount;

		/// <summary>
		/// Gets the size of documents retrieved in bytes during query in the Azure Cosmos DB service.
		/// </summary>
		public long RetrievedDocumentSize => retrievedDocumentSize;

		/// <summary>
		/// Gets the number of documents returned by query in the Azure Cosmos DB service.
		/// </summary>
		public long OutputDocumentCount => outputDocumentCount;

		/// <summary>
		/// Gets the size of documents outputted in bytes during query in the Azure Cosmos database service.
		/// </summary>
		internal long OutputDocumentSize => outputDocumentSize;

		/// <summary>
		/// Gets the total query time in the Azure Cosmos database service.
		/// </summary>
		internal TimeSpan TotalQueryExecutionTime => totalQueryExecutionTime;

		/// <summary>
		/// Gets the query QueryPreparationTimes in the Azure Cosmos database service.
		/// </summary>
		public QueryPreparationTimes QueryPreparationTimes => queryPreparationTimes;

		/// <summary>
		/// Gets the <see cref="T:Microsoft.Azure.Documents.QueryEngineTimes" /> instance in the Azure Cosmos database service.
		/// </summary>
		public QueryEngineTimes QueryEngineTimes => queryEngineTimes;

		/// <summary>
		/// Gets number of reties in the Azure Cosmos database service.
		/// </summary>
		public long Retries => clientSideMetrics.Retries;

		/// <summary>
		/// Gets the query index lookup time in the Azure Cosmos database service.
		/// </summary>
		internal TimeSpan IndexLookupTime => indexLookupTime;

		/// <summary>
		/// Gets the document loading time during query in the Azure Cosmos database service.
		/// </summary>
		internal TimeSpan DocumentLoadTime => documentLoadTime;

		/// <summary>
		/// Gets the query runtime execution times during query in the Azure Cosmos database service.
		/// </summary>
		internal RuntimeExecutionTimes RuntimeExecutionTimes => runtimeExecutionTimes;

		/// <summary>
		/// Gets the output writing/serializing time during query in the Azure Cosmos database service.
		/// </summary>
		internal TimeSpan DocumentWriteTime => documentWriteTime;

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.QueryMetrics.ClientSideMetrics" /> instance in the Azure Cosmos database service.
		/// </summary>
		[JsonProperty(PropertyName = "ClientSideMetrics")]
		internal ClientSideMetrics ClientSideMetrics
		{
			get
			{
				return clientSideMetrics;
			}
		}

		/// <summary>
		/// Gets the index hit ratio by query in the Azure Cosmos database service.
		/// </summary>
		public double IndexHitRatio
		{
			get
			{
				if (retrievedDocumentCount != 0L)
				{
					return (double)indexHitDocumentCount / (double)retrievedDocumentCount;
				}
				return 1.0;
			}
		}

		/// <summary>
		/// Gets the Index Hit Document Count.
		/// </summary>
		internal long IndexHitDocumentCount => indexHitDocumentCount;

		/// <summary>
		/// Gets the VMExecution Time.
		/// </summary>
		internal TimeSpan VMExecutionTime => vmExecutionTime;

		/// <summary>
		/// Gets the Index Utilization.
		/// </summary>
		private double IndexUtilization => IndexHitRatio * 100.0;

		/// <summary>
		/// Initializes a new instance of the QueryMetrics class.
		/// </summary>
		/// <param name="retrievedDocumentCount">Retrieved Document Count</param>
		/// <param name="retrievedDocumentSize">Retrieved Document Size</param>
		/// <param name="outputDocumentCount">Output Document Count</param>
		/// <param name="outputDocumentSize">Output Document Size</param>
		/// <param name="indexHitDocumentCount">Index Hit DocumentCount</param>
		/// <param name="totalQueryExecutionTime">Total Query Execution Time</param>
		/// <param name="queryPreparationTimes">Query Preparation Times</param>
		/// <param name="indexLookupTime">Time spent in physical index layer.</param>
		/// <param name="documentLoadTime">Time spent in loading documents.</param>
		/// <param name="vmExecutionTime">Time spent in VM execution.</param>
		/// <param name="runtimeExecutionTimes">Runtime Execution Times</param>
		/// <param name="documentWriteTime">Time spent writing output document</param>
		/// <param name="clientSideMetrics">Client Side Metrics</param>
		[JsonConstructor]
		internal QueryMetrics(long retrievedDocumentCount, long retrievedDocumentSize, long outputDocumentCount, long outputDocumentSize, long indexHitDocumentCount, TimeSpan totalQueryExecutionTime, QueryPreparationTimes queryPreparationTimes, TimeSpan indexLookupTime, TimeSpan documentLoadTime, TimeSpan vmExecutionTime, RuntimeExecutionTimes runtimeExecutionTimes, TimeSpan documentWriteTime, ClientSideMetrics clientSideMetrics)
		{
			if (queryPreparationTimes == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "queryPreparationTimes"));
			}
			if (runtimeExecutionTimes == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "runtimeExecutionTimes"));
			}
			if (clientSideMetrics == null)
			{
				throw new ArgumentNullException(string.Format("{0} can not be null.", "clientSideMetrics"));
			}
			this.retrievedDocumentCount = retrievedDocumentCount;
			this.retrievedDocumentSize = retrievedDocumentSize;
			this.outputDocumentCount = outputDocumentCount;
			this.outputDocumentSize = outputDocumentSize;
			this.indexHitDocumentCount = indexHitDocumentCount;
			this.totalQueryExecutionTime = totalQueryExecutionTime;
			this.queryPreparationTimes = queryPreparationTimes;
			this.indexLookupTime = indexLookupTime;
			this.documentLoadTime = documentLoadTime;
			this.vmExecutionTime = vmExecutionTime;
			this.runtimeExecutionTimes = runtimeExecutionTimes;
			this.documentWriteTime = documentWriteTime;
			this.clientSideMetrics = clientSideMetrics;
			queryEngineTimes = new QueryEngineTimes(indexLookupTime, documentLoadTime, vmExecutionTime, documentWriteTime, runtimeExecutionTimes);
		}

		/// <summary>
		/// Add two specified <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instances
		/// </summary>
		/// <param name="queryMetrics1">The first <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance</param>
		/// <param name="queryMetrics2">The second <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance</param>
		/// <returns>A new <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance that is the sum of two <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instances</returns>
		public static QueryMetrics operator +(QueryMetrics queryMetrics1, QueryMetrics queryMetrics2)
		{
			return queryMetrics1.Add(queryMetrics2);
		}

		/// <summary>
		/// Gets the stringified <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance in the Azure Cosmos database service.
		/// </summary>
		/// <returns>The stringified <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance in the Azure Cosmos database service.</returns>
		public override string ToString()
		{
			return ToTextString();
		}

		private string ToTextString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			new QueryMetricsTextWriter(stringBuilder).WriteQueryMetrics(this);
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Gets the delimited stringified <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance in the Azure Cosmos database service as if from a backend response.
		/// </summary>
		/// <returns>The delimited stringified <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> instance in the Azure Cosmos database service as if from a backend response.</returns>
		internal string ToDelimitedString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			new QueryMetricsDelimitedStringWriter(stringBuilder).WriteQueryMetrics(this);
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Creates a new QueryMetrics that is the sum of all elements in an IEnumerable.
		/// </summary>
		/// <param name="queryMetricsList">The IEnumerable to aggregate.</param>
		/// <returns>A new QueryMetrics that is the sum of all elements in an IEnumerable.</returns>
		internal static QueryMetrics CreateFromIEnumerable(IEnumerable<QueryMetrics> queryMetricsList)
		{
			if (queryMetricsList == null)
			{
				throw new ArgumentNullException("queryMetricsList");
			}
			long num = 0L;
			long num2 = 0L;
			long num3 = 0L;
			long num4 = 0L;
			long num5 = 0L;
			TimeSpan t = default(TimeSpan);
			List<QueryPreparationTimes> list = new List<QueryPreparationTimes>();
			TimeSpan t2 = default(TimeSpan);
			TimeSpan t3 = default(TimeSpan);
			TimeSpan t4 = default(TimeSpan);
			List<RuntimeExecutionTimes> list2 = new List<RuntimeExecutionTimes>();
			TimeSpan t5 = default(TimeSpan);
			List<ClientSideMetrics> list3 = new List<ClientSideMetrics>();
			foreach (QueryMetrics queryMetrics in queryMetricsList)
			{
				if (queryMetrics == null)
				{
					throw new ArgumentNullException("queryMetricsList can not have null elements");
				}
				num += queryMetrics.retrievedDocumentCount;
				num2 += queryMetrics.retrievedDocumentSize;
				num3 += queryMetrics.outputDocumentCount;
				num4 += queryMetrics.outputDocumentSize;
				num5 += queryMetrics.indexHitDocumentCount;
				t += queryMetrics.totalQueryExecutionTime;
				list.Add(queryMetrics.queryPreparationTimes);
				t2 += queryMetrics.indexLookupTime;
				t3 += queryMetrics.documentLoadTime;
				t4 += queryMetrics.vmExecutionTime;
				list2.Add(queryMetrics.runtimeExecutionTimes);
				t5 += queryMetrics.documentWriteTime;
				list3.Add(queryMetrics.clientSideMetrics);
			}
			return new QueryMetrics(num, num2, num3, num4, num5, t, QueryPreparationTimes.CreateFromIEnumerable(list), t2, t3, t4, RuntimeExecutionTimes.CreateFromIEnumerable(list2), t5, ClientSideMetrics.CreateFromIEnumerable(list3));
		}

		/// <summary>
		/// Creates a new QueryMetrics from the backend delimited string.
		/// </summary>
		/// <param name="delimitedString">The backend delimited string to deserialize from.</param>
		/// <returns>A new QueryMetrics from the backend delimited string.</returns>
		internal static QueryMetrics CreateFromDelimitedString(string delimitedString)
		{
			return CreateFromDelimitedStringAndClientSideMetrics(delimitedString, new ClientSideMetrics(0L, 0.0, new List<FetchExecutionRange>(), new List<Tuple<string, SchedulingTimeSpan>>()));
		}

		/// <summary>
		/// Creates a new QueryMetrics from the backend delimited string and ClientSideMetrics.
		/// </summary>
		/// <param name="delimitedString">The backend delimited string to deserialize from.</param>
		/// <param name="clientSideMetrics">The additional client side metrics.</param>
		/// <returns>A new QueryMetrics.</returns>
		internal static QueryMetrics CreateFromDelimitedStringAndClientSideMetrics(string delimitedString, ClientSideMetrics clientSideMetrics)
		{
			Dictionary<string, double> dictionary = QueryMetricsUtils.ParseDelimitedString(delimitedString);
			dictionary.TryGetValue("indexUtilizationRatio", out double value);
			dictionary.TryGetValue("retrievedDocumentCount", out double value2);
			long num = (long)(value * value2);
			dictionary.TryGetValue("outputDocumentCount", out double value3);
			dictionary.TryGetValue("outputDocumentSize", out double value4);
			dictionary.TryGetValue("retrievedDocumentSize", out double value5);
			TimeSpan timeSpan = QueryMetricsUtils.TimeSpanFromMetrics(dictionary, "totalExecutionTimeInMs");
			return new QueryMetrics((long)value2, (long)value5, (long)value3, (long)value4, num, timeSpan, QueryPreparationTimes.CreateFromDelimitedString(delimitedString), QueryMetricsUtils.TimeSpanFromMetrics(dictionary, "indexLookupTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(dictionary, "documentLoadTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(dictionary, "VMExecutionTimeInMs"), RuntimeExecutionTimes.CreateFromDelimitedString(delimitedString), QueryMetricsUtils.TimeSpanFromMetrics(dictionary, "writeOutputTimeInMs"), clientSideMetrics);
		}

		internal static QueryMetrics CreateWithSchedulingMetrics(QueryMetrics queryMetrics, List<Tuple<string, SchedulingTimeSpan>> partitionSchedulingTimeSpans)
		{
			return new QueryMetrics(queryMetrics.RetrievedDocumentCount, queryMetrics.RetrievedDocumentSize, queryMetrics.OutputDocumentCount, queryMetrics.OutputDocumentSize, queryMetrics.IndexHitDocumentCount, queryMetrics.TotalQueryExecutionTime, queryMetrics.QueryPreparationTimes, queryMetrics.IndexLookupTime, queryMetrics.DocumentLoadTime, queryMetrics.VMExecutionTime, queryMetrics.RuntimeExecutionTimes, queryMetrics.DocumentWriteTime, new ClientSideMetrics(queryMetrics.ClientSideMetrics.Retries, queryMetrics.ClientSideMetrics.RequestCharge, queryMetrics.ClientSideMetrics.FetchExecutionRanges, partitionSchedulingTimeSpans));
		}

		/// <summary>
		/// Adds all QueryMetrics in a list along with the current instance.
		/// </summary>
		/// <param name="queryMetricsList">The list to sum up.</param>
		/// <returns>A new QueryMetrics instance that is the sum of the current instance and the list.</returns>
		internal QueryMetrics Add(params QueryMetrics[] queryMetricsList)
		{
			List<QueryMetrics> list = new List<QueryMetrics>(queryMetricsList.Length + 1);
			list.Add(this);
			list.AddRange(queryMetricsList);
			return CreateFromIEnumerable(list);
		}
	}
}
