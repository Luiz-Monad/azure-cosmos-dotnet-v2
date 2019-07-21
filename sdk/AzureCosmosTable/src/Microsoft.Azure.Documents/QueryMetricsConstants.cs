namespace Microsoft.Azure.Documents
{
	internal static class QueryMetricsConstants
	{
		public const string RetrievedDocumentCount = "retrievedDocumentCount";

		public const string RetrievedDocumentSize = "retrievedDocumentSize";

		public const string OutputDocumentCount = "outputDocumentCount";

		public const string OutputDocumentSize = "outputDocumentSize";

		public const string IndexHitRatio = "indexUtilizationRatio";

		public const string IndexHitDocumentCount = "indexHitDocumentCount";

		public const string TotalQueryExecutionTimeInMs = "totalExecutionTimeInMs";

		public const string QueryCompileTimeInMs = "queryCompileTimeInMs";

		public const string LogicalPlanBuildTimeInMs = "queryLogicalPlanBuildTimeInMs";

		public const string PhysicalPlanBuildTimeInMs = "queryPhysicalPlanBuildTimeInMs";

		public const string QueryOptimizationTimeInMs = "queryOptimizationTimeInMs";

		public const string IndexLookupTimeInMs = "indexLookupTimeInMs";

		public const string DocumentLoadTimeInMs = "documentLoadTimeInMs";

		public const string VMExecutionTimeInMs = "VMExecutionTimeInMs";

		public const string DocumentWriteTimeInMs = "writeOutputTimeInMs";

		public const string QueryEngineTimes = "queryEngineTimes";

		public const string SystemFunctionExecuteTimeInMs = "systemFunctionExecuteTimeInMs";

		public const string UserDefinedFunctionExecutionTimeInMs = "userFunctionExecuteTimeInMs";
	}
}
