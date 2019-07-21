using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class QueryMetricsTextWriter : QueryMetricsWriter
	{
		private readonly StringBuilder stringBuilder;

		private const string ActivityIds = "Activity Ids";

		private const string RetrievedDocumentCount = "Retrieved Document Count";

		private const string RetrievedDocumentSize = "Retrieved Document Size";

		private const string OutputDocumentCount = "Output Document Count";

		private const string OutputDocumentSize = "Output Document Size";

		private const string IndexUtilization = "Index Utilization";

		private const string TotalQueryExecutionTime = "Total Query Execution Time";

		private const string QueryPreparationTimes = "Query Preparation Times";

		private const string QueryCompileTime = "Query Compilation Time";

		private const string LogicalPlanBuildTime = "Logical Plan Build Time";

		private const string PhysicalPlanBuildTime = "Physical Plan Build Time";

		private const string QueryOptimizationTime = "Query Optimization Time";

		private const string QueryEngineTimes = "Query Engine Times";

		private const string IndexLookupTime = "Index Lookup Time";

		private const string DocumentLoadTime = "Document Load Time";

		private const string DocumentWriteTime = "Document Write Time";

		private const string RuntimeExecutionTimes = "Runtime Execution Times";

		private const string TotalExecutionTime = "Query Engine Execution Time";

		private const string SystemFunctionExecuteTime = "System Function Execution Time";

		private const string UserDefinedFunctionExecutionTime = "User-defined Function Execution Time";

		private const string ClientSideQueryMetrics = "Client Side Metrics";

		private const string Retries = "Retry Count";

		private const string RequestCharge = "Request Charge";

		private const string FetchExecutionRanges = "Partition Execution Timeline";

		private const string SchedulingMetrics = "Scheduling Metrics";

		private const string StartTimeHeader = "Start Time (UTC)";

		private const string EndTimeHeader = "End Time (UTC)";

		private const string DurationHeader = "Duration (ms)";

		private const string PartitionKeyRangeIdHeader = "Partition Id";

		private const string NumberOfDocumentsHeader = "Number of Documents";

		private const string RetryCountHeader = "Retry Count";

		private const string ActivityIdHeader = "Activity Id";

		private const string PartitionIdHeader = "Partition Id";

		private const string ResponseTimeHeader = "Response Time (ms)";

		private const string RunTimeHeader = "Run Time (ms)";

		private const string WaitTimeHeader = "Wait Time (ms)";

		private const string TurnaroundTimeHeader = "Turnaround Time (ms)";

		private const string NumberOfPreemptionHeader = "Number of Preemptions";

		private const string DateTimeFormat = "HH':'mm':'ss.ffff'Z'";

		private static readonly int MaxDateTimeStringLength = DateTime.MaxValue.ToUniversalTime().ToString("HH':'mm':'ss.ffff'Z'").Length;

		private static readonly int StartTimeHeaderLength = Math.Max(MaxDateTimeStringLength, "Start Time (UTC)".Length);

		private static readonly int EndTimeHeaderLength = Math.Max(MaxDateTimeStringLength, "End Time (UTC)".Length);

		private static readonly int DurationHeaderLength = Math.Max("Duration (ms)".Length, TimeSpan.MaxValue.TotalMilliseconds.ToString("0.00").Length);

		private static readonly int PartitionKeyRangeIdHeaderLength = "Partition Id".Length;

		private static readonly int NumberOfDocumentsHeaderLength = "Number of Documents".Length;

		private static readonly int RetryCountHeaderLength = "Retry Count".Length;

		private static readonly int ActivityIdHeaderLength = Guid.Empty.ToString().Length;

		private static readonly TextTable.Column[] PartitionExecutionTimelineColumns = new TextTable.Column[7]
		{
			new TextTable.Column("Partition Id", PartitionKeyRangeIdHeaderLength),
			new TextTable.Column("Activity Id", ActivityIdHeaderLength),
			new TextTable.Column("Start Time (UTC)", StartTimeHeaderLength),
			new TextTable.Column("End Time (UTC)", EndTimeHeaderLength),
			new TextTable.Column("Duration (ms)", DurationHeaderLength),
			new TextTable.Column("Number of Documents", NumberOfDocumentsHeaderLength),
			new TextTable.Column("Retry Count", RetryCountHeaderLength)
		};

		private static readonly TextTable PartitionExecutionTimelineTable = new TextTable(PartitionExecutionTimelineColumns);

		private static readonly int MaxTimeSpanStringLength = Math.Max(TimeSpan.MaxValue.TotalMilliseconds.ToString("G17").Length, "Turnaround Time (ms)".Length);

		private static readonly int PartitionIdHeaderLength = "Partition Id".Length;

		private static readonly int ResponseTimeHeaderLength = MaxTimeSpanStringLength;

		private static readonly int RunTimeHeaderLength = MaxTimeSpanStringLength;

		private static readonly int WaitTimeHeaderLength = MaxTimeSpanStringLength;

		private static readonly int TurnaroundTimeHeaderLength = MaxTimeSpanStringLength;

		private static readonly int NumberOfPreemptionHeaderLength = "Number of Preemptions".Length;

		private static readonly TextTable.Column[] SchedulingMetricsColumns = new TextTable.Column[6]
		{
			new TextTable.Column("Partition Id", PartitionIdHeaderLength),
			new TextTable.Column("Response Time (ms)", ResponseTimeHeaderLength),
			new TextTable.Column("Run Time (ms)", RunTimeHeaderLength),
			new TextTable.Column("Wait Time (ms)", WaitTimeHeaderLength),
			new TextTable.Column("Turnaround Time (ms)", TurnaroundTimeHeaderLength),
			new TextTable.Column("Number of Preemptions", NumberOfPreemptionHeaderLength)
		};

		private static readonly TextTable SchedulingMetricsTable = new TextTable(SchedulingMetricsColumns);

		private string lastFetchPartitionId;

		private string lastActivityId;

		private DateTime lastStartTime;

		private DateTime lastEndTime;

		private long lastFetchDocumentCount;

		private long lastFetchRetryCount;

		private string lastSchedulingPartitionId;

		private TimeSpan lastResponseTime;

		private TimeSpan lastRunTime;

		private TimeSpan lastWaitTime;

		private TimeSpan lastTurnaroundTime;

		private long lastNumberOfPreemptions;

		public QueryMetricsTextWriter(StringBuilder stringBuilder)
		{
			if (stringBuilder == null)
			{
				throw new ArgumentNullException(string.Format("{0} must not be null.", "stringBuilder"));
			}
			this.stringBuilder = stringBuilder;
		}

		protected override void WriteBeforeQueryMetrics()
		{
		}

		protected override void WriteRetrievedDocumentCount(long retrievedDocumentCount)
		{
			AppendCountToStringBuilder(stringBuilder, "Retrieved Document Count", retrievedDocumentCount, 0);
		}

		protected override void WriteRetrievedDocumentSize(long retrievedDocumentSize)
		{
			AppendBytesToStringBuilder(stringBuilder, "Retrieved Document Size", retrievedDocumentSize, 0);
		}

		protected override void WriteOutputDocumentCount(long outputDocumentCount)
		{
			AppendCountToStringBuilder(stringBuilder, "Output Document Count", outputDocumentCount, 0);
		}

		protected override void WriteOutputDocumentSize(long outputDocumentSize)
		{
			AppendBytesToStringBuilder(stringBuilder, "Output Document Size", outputDocumentSize, 0);
		}

		protected override void WriteIndexHitRatio(double indexHitRatio)
		{
			AppendPercentageToStringBuilder(stringBuilder, "Index Utilization", indexHitRatio, 0);
		}

		protected override void WriteTotalQueryExecutionTime(TimeSpan totalQueryExecutionTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Total Query Execution Time", totalQueryExecutionTime, 0);
		}

		protected override void WriteBeforeQueryPreparationTimes()
		{
			AppendHeaderToStringBuilder(stringBuilder, "Query Preparation Times", 1);
		}

		protected override void WriteQueryCompilationTime(TimeSpan queryCompilationTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Query Compilation Time", queryCompilationTime, 2);
		}

		protected override void WriteLogicalPlanBuildTime(TimeSpan logicalPlanBuildTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Logical Plan Build Time", logicalPlanBuildTime, 2);
		}

		protected override void WritePhysicalPlanBuildTime(TimeSpan physicalPlanBuildTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Physical Plan Build Time", physicalPlanBuildTime, 2);
		}

		protected override void WriteQueryOptimizationTime(TimeSpan queryOptimizationTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Query Optimization Time", queryOptimizationTime, 2);
		}

		protected override void WriteAfterQueryPreparationTimes()
		{
		}

		protected override void WriteIndexLookupTime(TimeSpan indexLookupTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Index Lookup Time", indexLookupTime, 1);
		}

		protected override void WriteDocumentLoadTime(TimeSpan documentLoadTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Document Load Time", documentLoadTime, 1);
		}

		protected override void WriteVMExecutionTime(TimeSpan vmExecutionTime)
		{
		}

		protected override void WriteBeforeRuntimeExecutionTimes()
		{
			AppendHeaderToStringBuilder(stringBuilder, "Runtime Execution Times", 1);
		}

		protected override void WriteQueryEngineExecutionTime(TimeSpan queryEngineExecutionTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Query Engine Times", queryEngineExecutionTime, 2);
		}

		protected override void WriteSystemFunctionExecutionTime(TimeSpan systemFunctionExecutionTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "System Function Execution Time", systemFunctionExecutionTime, 2);
		}

		protected override void WriteUserDefinedFunctionExecutionTime(TimeSpan userDefinedFunctionExecutionTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "User-defined Function Execution Time", userDefinedFunctionExecutionTime, 2);
		}

		protected override void WriteAfterRuntimeExecutionTimes()
		{
		}

		protected override void WriteDocumentWriteTime(TimeSpan documentWriteTime)
		{
			AppendTimeSpanToStringBuilder(stringBuilder, "Document Write Time", documentWriteTime, 1);
		}

		protected override void WriteBeforeClientSideMetrics()
		{
			AppendHeaderToStringBuilder(stringBuilder, "Client Side Metrics", 0);
		}

		protected override void WriteRetries(long retries)
		{
			AppendCountToStringBuilder(stringBuilder, "Retry Count", retries, 1);
		}

		protected override void WriteRequestCharge(double requestCharge)
		{
			AppendRUToStringBuilder(stringBuilder, "Request Charge", requestCharge, 1);
		}

		protected override void WriteBeforePartitionExecutionTimeline()
		{
			AppendNewlineToStringBuilder(stringBuilder);
			AppendHeaderToStringBuilder(stringBuilder, "Partition Execution Timeline", 1);
			AppendHeaderToStringBuilder(stringBuilder, PartitionExecutionTimelineTable.TopLine, 1);
			AppendHeaderToStringBuilder(stringBuilder, PartitionExecutionTimelineTable.Header, 1);
			AppendHeaderToStringBuilder(stringBuilder, PartitionExecutionTimelineTable.MiddleLine, 1);
		}

		protected override void WriteBeforeFetchExecutionRange()
		{
		}

		protected override void WriteFetchPartitionKeyRangeId(string partitionId)
		{
			lastFetchPartitionId = partitionId;
		}

		protected override void WriteActivityId(string activityId)
		{
			lastActivityId = activityId;
		}

		protected override void WriteStartTime(DateTime startTime)
		{
			lastStartTime = startTime;
		}

		protected override void WriteEndTime(DateTime endTime)
		{
			lastEndTime = endTime;
		}

		protected override void WriteFetchDocumentCount(long numberOfDocuments)
		{
			lastFetchDocumentCount = numberOfDocuments;
		}

		protected override void WriteFetchRetryCount(long retryCount)
		{
			lastFetchRetryCount = retryCount;
		}

		protected override void WriteAfterFetchExecutionRange()
		{
			AppendHeaderToStringBuilder(stringBuilder, PartitionExecutionTimelineTable.GetRow(lastFetchPartitionId, lastActivityId, lastStartTime.ToUniversalTime().ToString("HH':'mm':'ss.ffff'Z'"), lastEndTime.ToUniversalTime().ToString("HH':'mm':'ss.ffff'Z'"), (lastEndTime - lastStartTime).TotalMilliseconds.ToString("0.00"), lastFetchDocumentCount, lastFetchRetryCount), 1);
		}

		protected override void WriteAfterPartitionExecutionTimeline()
		{
			AppendHeaderToStringBuilder(stringBuilder, PartitionExecutionTimelineTable.BottomLine, 1);
		}

		protected override void WriteBeforeSchedulingMetrics()
		{
			AppendNewlineToStringBuilder(stringBuilder);
			AppendHeaderToStringBuilder(stringBuilder, "Scheduling Metrics", 1);
			AppendHeaderToStringBuilder(stringBuilder, SchedulingMetricsTable.TopLine, 1);
			AppendHeaderToStringBuilder(stringBuilder, SchedulingMetricsTable.Header, 1);
			AppendHeaderToStringBuilder(stringBuilder, SchedulingMetricsTable.MiddleLine, 1);
		}

		protected override void WriteBeforePartitionSchedulingTimeSpan()
		{
		}

		protected override void WritePartitionSchedulingTimeSpanId(string partitionId)
		{
			lastSchedulingPartitionId = partitionId;
		}

		protected override void WriteResponseTime(TimeSpan responseTime)
		{
			lastResponseTime = responseTime;
		}

		protected override void WriteRunTime(TimeSpan runTime)
		{
			lastRunTime = runTime;
		}

		protected override void WriteWaitTime(TimeSpan waitTime)
		{
			lastWaitTime = waitTime;
		}

		protected override void WriteTurnaroundTime(TimeSpan turnaroundTime)
		{
			lastTurnaroundTime = turnaroundTime;
		}

		protected override void WriteNumberOfPreemptions(long numPreemptions)
		{
			lastNumberOfPreemptions = numPreemptions;
		}

		protected override void WriteAfterPartitionSchedulingTimeSpan()
		{
			AppendHeaderToStringBuilder(stringBuilder, SchedulingMetricsTable.GetRow(lastSchedulingPartitionId, lastResponseTime.TotalMilliseconds.ToString("0.00"), lastRunTime.TotalMilliseconds.ToString("0.00"), lastWaitTime.TotalMilliseconds.ToString("0.00"), lastTurnaroundTime.TotalMilliseconds.ToString("0.00"), lastNumberOfPreemptions), 1);
		}

		protected override void WriteAfterSchedulingMetrics()
		{
			AppendHeaderToStringBuilder(stringBuilder, SchedulingMetricsTable.BottomLine, 1);
		}

		protected override void WriteAfterClientSideMetrics()
		{
		}

		protected override void WriteAfterQueryMetrics()
		{
		}

		private static void AppendToStringBuilder(StringBuilder stringBuilder, string property, string value, string units, int indentLevel)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0,-40} : {1,15} {2,-12}{3}", string.Concat(Enumerable.Repeat("  ", indentLevel)) + property, value, units, Environment.NewLine);
		}

		private static void AppendBytesToStringBuilder(StringBuilder stringBuilder, string property, long bytes, int indentLevel)
		{
			AppendToStringBuilder(stringBuilder, property, string.Format(CultureInfo.InvariantCulture, "{0:n0}", bytes), "bytes", indentLevel);
		}

		private static void AppendCountToStringBuilder(StringBuilder stringBuilder, string property, long count, int indentLevel)
		{
			AppendToStringBuilder(stringBuilder, property, string.Format(CultureInfo.InvariantCulture, "{0:n0}", count), "", indentLevel);
		}

		private static void AppendPercentageToStringBuilder(StringBuilder stringBuilder, string property, double percentage, int indentLevel)
		{
			AppendToStringBuilder(stringBuilder, property, string.Format(CultureInfo.InvariantCulture, "{0:n2}", percentage * 100.0), "%", indentLevel);
		}

		private static void AppendTimeSpanToStringBuilder(StringBuilder stringBuilder, string property, TimeSpan timeSpan, int indentLevel)
		{
			AppendToStringBuilder(stringBuilder, property, string.Format(CultureInfo.InvariantCulture, "{0:n2}", timeSpan.TotalMilliseconds), "milliseconds", indentLevel);
		}

		private static void AppendHeaderToStringBuilder(StringBuilder stringBuilder, string headerTitle, int indentLevel)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", string.Concat(Enumerable.Repeat("  ", indentLevel)) + headerTitle, Environment.NewLine);
		}

		private static void AppendRUToStringBuilder(StringBuilder stringBuilder, string property, double requestCharge, int indentLevel)
		{
			AppendToStringBuilder(stringBuilder, property, string.Format(CultureInfo.InvariantCulture, "{0:n2}", requestCharge), "RUs", indentLevel);
		}

		private static void AppendNewlineToStringBuilder(StringBuilder stringBuilder)
		{
			AppendHeaderToStringBuilder(stringBuilder, string.Empty, 0);
		}
	}
}
