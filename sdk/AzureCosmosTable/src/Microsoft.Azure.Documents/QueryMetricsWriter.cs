using System;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	internal abstract class QueryMetricsWriter
	{
		public void WriteQueryMetrics(QueryMetrics queryMetrics)
		{
			WriteBeforeQueryMetrics();
			WriteRetrievedDocumentCount(queryMetrics.RetrievedDocumentCount);
			WriteRetrievedDocumentSize(queryMetrics.RetrievedDocumentSize);
			WriteOutputDocumentCount(queryMetrics.OutputDocumentCount);
			WriteOutputDocumentSize(queryMetrics.OutputDocumentSize);
			WriteIndexHitRatio(queryMetrics.IndexHitRatio);
			WriteTotalQueryExecutionTime(queryMetrics.TotalQueryExecutionTime);
			WriteQueryPreparationTimes(queryMetrics.QueryPreparationTimes);
			WriteIndexLookupTime(queryMetrics.IndexLookupTime);
			WriteDocumentLoadTime(queryMetrics.DocumentLoadTime);
			WriteVMExecutionTime(queryMetrics.VMExecutionTime);
			WriteRuntimesExecutionTimes(queryMetrics.RuntimeExecutionTimes);
			WriteDocumentWriteTime(queryMetrics.DocumentWriteTime);
			WriteClientSideMetrics(queryMetrics.ClientSideMetrics);
			WriteAfterQueryMetrics();
		}

		protected abstract void WriteBeforeQueryMetrics();

		protected abstract void WriteRetrievedDocumentCount(long retrievedDocumentCount);

		protected abstract void WriteRetrievedDocumentSize(long retrievedDocumentSize);

		protected abstract void WriteOutputDocumentCount(long outputDocumentCount);

		protected abstract void WriteOutputDocumentSize(long outputDocumentSize);

		protected abstract void WriteIndexHitRatio(double indexHitRatio);

		protected abstract void WriteTotalQueryExecutionTime(TimeSpan totalQueryExecutionTime);

		private void WriteQueryPreparationTimes(QueryPreparationTimes queryPreparationTimes)
		{
			WriteBeforeQueryPreparationTimes();
			WriteQueryCompilationTime(queryPreparationTimes.QueryCompilationTime);
			WriteLogicalPlanBuildTime(queryPreparationTimes.LogicalPlanBuildTime);
			WritePhysicalPlanBuildTime(queryPreparationTimes.PhysicalPlanBuildTime);
			WriteQueryOptimizationTime(queryPreparationTimes.QueryOptimizationTime);
			WriteAfterQueryPreparationTimes();
		}

		protected abstract void WriteBeforeQueryPreparationTimes();

		protected abstract void WriteQueryCompilationTime(TimeSpan queryCompilationTime);

		protected abstract void WriteLogicalPlanBuildTime(TimeSpan logicalPlanBuildTime);

		protected abstract void WritePhysicalPlanBuildTime(TimeSpan physicalPlanBuildTime);

		protected abstract void WriteQueryOptimizationTime(TimeSpan queryOptimizationTime);

		protected abstract void WriteAfterQueryPreparationTimes();

		protected abstract void WriteIndexLookupTime(TimeSpan indexLookupTime);

		protected abstract void WriteDocumentLoadTime(TimeSpan documentLoadTime);

		protected abstract void WriteVMExecutionTime(TimeSpan vMExecutionTime);

		private void WriteRuntimesExecutionTimes(RuntimeExecutionTimes runtimeExecutionTimes)
		{
			WriteBeforeRuntimeExecutionTimes();
			WriteQueryEngineExecutionTime(runtimeExecutionTimes.QueryEngineExecutionTime);
			WriteSystemFunctionExecutionTime(runtimeExecutionTimes.SystemFunctionExecutionTime);
			WriteUserDefinedFunctionExecutionTime(runtimeExecutionTimes.UserDefinedFunctionExecutionTime);
			WriteAfterRuntimeExecutionTimes();
		}

		protected abstract void WriteBeforeRuntimeExecutionTimes();

		protected abstract void WriteQueryEngineExecutionTime(TimeSpan queryEngineExecutionTime);

		protected abstract void WriteSystemFunctionExecutionTime(TimeSpan systemFunctionExecutionTime);

		protected abstract void WriteUserDefinedFunctionExecutionTime(TimeSpan userDefinedFunctionExecutionTime);

		protected abstract void WriteAfterRuntimeExecutionTimes();

		protected abstract void WriteDocumentWriteTime(TimeSpan documentWriteTime);

		private void WriteClientSideMetrics(ClientSideMetrics clientSideMetrics)
		{
			WriteBeforeClientSideMetrics();
			WriteRetries(clientSideMetrics.Retries);
			WriteRequestCharge(clientSideMetrics.RequestCharge);
			WritePartitionExecutionTimeline(clientSideMetrics);
			WriteSchedulingMetrics(clientSideMetrics);
			WriteAfterClientSideMetrics();
		}

		protected abstract void WriteBeforeClientSideMetrics();

		protected abstract void WriteRetries(long retries);

		protected abstract void WriteRequestCharge(double requestCharge);

		private void WritePartitionExecutionTimeline(ClientSideMetrics clientSideMetrics)
		{
			WriteBeforePartitionExecutionTimeline();
			foreach (FetchExecutionRange item in from fetchExecutionRange in clientSideMetrics.FetchExecutionRanges
			orderby fetchExecutionRange.StartTime
			select fetchExecutionRange)
			{
				WriteFetchExecutionRange(item);
			}
			WriteAfterPartitionExecutionTimeline();
		}

		protected abstract void WriteBeforePartitionExecutionTimeline();

		private void WriteFetchExecutionRange(FetchExecutionRange fetchExecutionRange)
		{
			WriteBeforeFetchExecutionRange();
			WriteFetchPartitionKeyRangeId(fetchExecutionRange.PartitionId);
			WriteActivityId(fetchExecutionRange.ActivityId);
			WriteStartTime(fetchExecutionRange.StartTime);
			WriteEndTime(fetchExecutionRange.EndTime);
			WriteFetchDocumentCount(fetchExecutionRange.NumberOfDocuments);
			WriteFetchRetryCount(fetchExecutionRange.RetryCount);
			WriteAfterFetchExecutionRange();
		}

		protected abstract void WriteBeforeFetchExecutionRange();

		protected abstract void WriteFetchPartitionKeyRangeId(string partitionId);

		protected abstract void WriteActivityId(string activityId);

		protected abstract void WriteStartTime(DateTime startTime);

		protected abstract void WriteEndTime(DateTime endTime);

		protected abstract void WriteFetchDocumentCount(long numberOfDocuments);

		protected abstract void WriteFetchRetryCount(long retryCount);

		protected abstract void WriteAfterFetchExecutionRange();

		protected abstract void WriteAfterPartitionExecutionTimeline();

		private void WriteSchedulingMetrics(ClientSideMetrics clientSideMetrics)
		{
			WriteBeforeSchedulingMetrics();
			foreach (Tuple<string, SchedulingTimeSpan> item3 in from x in clientSideMetrics.PartitionSchedulingTimeSpans
			orderby x.Item2.ResponseTime
			select x)
			{
				string item = item3.Item1;
				SchedulingTimeSpan item2 = item3.Item2;
				WritePartitionSchedulingTimeSpan(item, item2);
			}
			WriteAfterSchedulingMetrics();
		}

		protected abstract void WriteBeforeSchedulingMetrics();

		private void WritePartitionSchedulingTimeSpan(string partitionId, SchedulingTimeSpan schedulingTimeSpan)
		{
			WriteBeforePartitionSchedulingTimeSpan();
			WritePartitionSchedulingTimeSpanId(partitionId);
			WriteResponseTime(schedulingTimeSpan.ResponseTime);
			WriteRunTime(schedulingTimeSpan.RunTime);
			WriteWaitTime(schedulingTimeSpan.WaitTime);
			WriteTurnaroundTime(schedulingTimeSpan.TurnaroundTime);
			WriteNumberOfPreemptions(schedulingTimeSpan.NumPreemptions);
			WriteAfterPartitionSchedulingTimeSpan();
		}

		protected abstract void WriteBeforePartitionSchedulingTimeSpan();

		protected abstract void WritePartitionSchedulingTimeSpanId(string partitionId);

		protected abstract void WriteResponseTime(TimeSpan responseTime);

		protected abstract void WriteRunTime(TimeSpan runTime);

		protected abstract void WriteWaitTime(TimeSpan waitTime);

		protected abstract void WriteTurnaroundTime(TimeSpan turnaroundTime);

		protected abstract void WriteNumberOfPreemptions(long numPreemptions);

		protected abstract void WriteAfterPartitionSchedulingTimeSpan();

		protected abstract void WriteAfterSchedulingMetrics();

		protected abstract void WriteAfterClientSideMetrics();

		protected abstract void WriteAfterQueryMetrics();
	}
}
