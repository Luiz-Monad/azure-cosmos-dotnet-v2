using System;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class QueryMetricsDelimitedStringWriter : QueryMetricsWriter
	{
		private readonly StringBuilder stringBuilder;

		private const string RetrievedDocumentCount = "retrievedDocumentCount";

		private const string RetrievedDocumentSize = "retrievedDocumentSize";

		private const string OutputDocumentCount = "outputDocumentCount";

		private const string OutputDocumentSize = "outputDocumentSize";

		private const string IndexHitRatio = "indexUtilizationRatio";

		private const string IndexHitDocumentCount = "indexHitDocumentCount";

		private const string TotalQueryExecutionTimeInMs = "totalExecutionTimeInMs";

		private const string QueryCompileTimeInMs = "queryCompileTimeInMs";

		private const string LogicalPlanBuildTimeInMs = "queryLogicalPlanBuildTimeInMs";

		private const string PhysicalPlanBuildTimeInMs = "queryPhysicalPlanBuildTimeInMs";

		private const string QueryOptimizationTimeInMs = "queryOptimizationTimeInMs";

		private const string IndexLookupTimeInMs = "indexLookupTimeInMs";

		private const string DocumentLoadTimeInMs = "documentLoadTimeInMs";

		private const string VMExecutionTimeInMs = "VMExecutionTimeInMs";

		private const string DocumentWriteTimeInMs = "writeOutputTimeInMs";

		private const string QueryEngineTimes = "queryEngineTimes";

		private const string SystemFunctionExecuteTimeInMs = "systemFunctionExecuteTimeInMs";

		private const string UserDefinedFunctionExecutionTimeInMs = "userFunctionExecuteTimeInMs";

		private const string KeyValueDelimiter = "=";

		private const string KeyValuePairDelimiter = ";";

		public QueryMetricsDelimitedStringWriter(StringBuilder stringBuilder)
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
			AppendKeyValuePair("retrievedDocumentCount", retrievedDocumentCount);
		}

		protected override void WriteRetrievedDocumentSize(long retrievedDocumentSize)
		{
			AppendKeyValuePair("retrievedDocumentSize", retrievedDocumentSize);
		}

		protected override void WriteOutputDocumentCount(long outputDocumentCount)
		{
			AppendKeyValuePair("outputDocumentCount", outputDocumentCount);
		}

		protected override void WriteOutputDocumentSize(long outputDocumentSize)
		{
			AppendKeyValuePair("outputDocumentSize", outputDocumentSize);
		}

		protected override void WriteIndexHitRatio(double indexHitRatio)
		{
			AppendKeyValuePair("indexUtilizationRatio", indexHitRatio);
		}

		protected override void WriteTotalQueryExecutionTime(TimeSpan totalQueryExecutionTime)
		{
			AppendTimeSpan("totalExecutionTimeInMs", totalQueryExecutionTime);
		}

		protected override void WriteBeforeQueryPreparationTimes()
		{
		}

		protected override void WriteQueryCompilationTime(TimeSpan queryCompilationTime)
		{
			AppendTimeSpan("queryCompileTimeInMs", queryCompilationTime);
		}

		protected override void WriteLogicalPlanBuildTime(TimeSpan logicalPlanBuildTime)
		{
			AppendTimeSpan("queryLogicalPlanBuildTimeInMs", logicalPlanBuildTime);
		}

		protected override void WritePhysicalPlanBuildTime(TimeSpan physicalPlanBuildTime)
		{
			AppendTimeSpan("queryPhysicalPlanBuildTimeInMs", physicalPlanBuildTime);
		}

		protected override void WriteQueryOptimizationTime(TimeSpan queryOptimizationTime)
		{
			AppendTimeSpan("queryOptimizationTimeInMs", queryOptimizationTime);
		}

		protected override void WriteAfterQueryPreparationTimes()
		{
		}

		protected override void WriteIndexLookupTime(TimeSpan indexLookupTime)
		{
			AppendTimeSpan("indexLookupTimeInMs", indexLookupTime);
		}

		protected override void WriteDocumentLoadTime(TimeSpan documentLoadTime)
		{
			AppendTimeSpan("documentLoadTimeInMs", documentLoadTime);
		}

		protected override void WriteVMExecutionTime(TimeSpan vmExecutionTime)
		{
			AppendTimeSpan("VMExecutionTimeInMs", vmExecutionTime);
		}

		protected override void WriteBeforeRuntimeExecutionTimes()
		{
		}

		protected override void WriteQueryEngineExecutionTime(TimeSpan queryEngineExecutionTime)
		{
			AppendTimeSpan("queryEngineTimes", queryEngineExecutionTime);
		}

		protected override void WriteSystemFunctionExecutionTime(TimeSpan systemFunctionExecutionTime)
		{
			AppendTimeSpan("systemFunctionExecuteTimeInMs", systemFunctionExecutionTime);
		}

		protected override void WriteUserDefinedFunctionExecutionTime(TimeSpan userDefinedFunctionExecutionTime)
		{
			AppendTimeSpan("userFunctionExecuteTimeInMs", userDefinedFunctionExecutionTime);
		}

		protected override void WriteAfterRuntimeExecutionTimes()
		{
		}

		protected override void WriteDocumentWriteTime(TimeSpan documentWriteTime)
		{
			AppendTimeSpan("writeOutputTimeInMs", documentWriteTime);
		}

		protected override void WriteBeforeClientSideMetrics()
		{
		}

		protected override void WriteRetries(long retries)
		{
		}

		protected override void WriteRequestCharge(double requestCharge)
		{
		}

		protected override void WriteBeforePartitionExecutionTimeline()
		{
		}

		protected override void WriteBeforeFetchExecutionRange()
		{
		}

		protected override void WriteFetchPartitionKeyRangeId(string partitionId)
		{
		}

		protected override void WriteActivityId(string activityId)
		{
		}

		protected override void WriteStartTime(DateTime startTime)
		{
		}

		protected override void WriteEndTime(DateTime endTime)
		{
		}

		protected override void WriteFetchDocumentCount(long numberOfDocuments)
		{
		}

		protected override void WriteFetchRetryCount(long retryCount)
		{
		}

		protected override void WriteAfterFetchExecutionRange()
		{
		}

		protected override void WriteAfterPartitionExecutionTimeline()
		{
		}

		protected override void WriteBeforeSchedulingMetrics()
		{
		}

		protected override void WriteBeforePartitionSchedulingTimeSpan()
		{
		}

		protected override void WritePartitionSchedulingTimeSpanId(string partitionId)
		{
		}

		protected override void WriteResponseTime(TimeSpan responseTime)
		{
		}

		protected override void WriteRunTime(TimeSpan runTime)
		{
		}

		protected override void WriteWaitTime(TimeSpan waitTime)
		{
		}

		protected override void WriteTurnaroundTime(TimeSpan turnaroundTime)
		{
		}

		protected override void WriteNumberOfPreemptions(long numPreemptions)
		{
		}

		protected override void WriteAfterPartitionSchedulingTimeSpan()
		{
		}

		protected override void WriteAfterSchedulingMetrics()
		{
		}

		protected override void WriteAfterClientSideMetrics()
		{
		}

		protected override void WriteAfterQueryMetrics()
		{
			stringBuilder.Length--;
		}

		private void AppendKeyValuePair<T>(string name, T value)
		{
			stringBuilder.Append(name);
			stringBuilder.Append("=");
			stringBuilder.Append(value);
			stringBuilder.Append(";");
		}

		private void AppendTimeSpan(string name, TimeSpan dateTime)
		{
			AppendKeyValuePair(name, dateTime.TotalMilliseconds.ToString("0.00"));
		}
	}
}
