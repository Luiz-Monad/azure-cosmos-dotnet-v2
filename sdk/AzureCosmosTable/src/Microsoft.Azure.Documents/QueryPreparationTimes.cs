using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Query preparation metrics in the Azure Cosmos database service.
	/// </summary>
	public sealed class QueryPreparationTimes
	{
		internal static readonly QueryPreparationTimes Zero = new QueryPreparationTimes(default(TimeSpan), default(TimeSpan), default(TimeSpan), default(TimeSpan));

		private readonly TimeSpan queryCompilationTime;

		private readonly TimeSpan logicalPlanBuildTime;

		private readonly TimeSpan physicalPlanBuildTime;

		private readonly TimeSpan queryOptimizationTime;

		/// <summary>
		/// Gets the query compile time in the Azure Cosmos database service. 
		/// </summary>
		internal TimeSpan QueryCompilationTime => queryCompilationTime;

		/// <summary>
		/// Gets the query compile time in the Azure Cosmos database service. 
		/// </summary>
		public TimeSpan CompileTime => queryCompilationTime;

		/// <summary>
		/// Gets the query logical plan build time in the Azure Cosmos database service. 
		/// </summary>
		public TimeSpan LogicalPlanBuildTime => logicalPlanBuildTime;

		/// <summary>
		/// Gets the query physical plan build time in the Azure Cosmos database service. 
		/// </summary>
		public TimeSpan PhysicalPlanBuildTime => physicalPlanBuildTime;

		/// <summary>
		/// Gets the query optimization time in the Azure Cosmos database service. 
		/// </summary>
		public TimeSpan QueryOptimizationTime => queryOptimizationTime;

		/// <summary>
		/// Initializes a new instance of the QueryPreparationTimes class.
		/// </summary>
		/// <param name="queryCompilationTime">Query compile and optimization time</param>
		/// <param name="logicalPlanBuildTime">Query logical plan build time</param>
		/// <param name="physicalPlanBuildTime">Query physical plan build time</param>
		/// <param name="queryOptimizationTime">Query optimization time</param>
		[JsonConstructor]
		internal QueryPreparationTimes(TimeSpan queryCompilationTime, TimeSpan logicalPlanBuildTime, TimeSpan physicalPlanBuildTime, TimeSpan queryOptimizationTime)
		{
			this.queryCompilationTime = queryCompilationTime;
			this.logicalPlanBuildTime = logicalPlanBuildTime;
			this.physicalPlanBuildTime = physicalPlanBuildTime;
			this.queryOptimizationTime = queryOptimizationTime;
		}

		/// <summary>
		/// Creates a new QueryPreparationTimes from the backend delimited string.
		/// </summary>
		/// <param name="delimitedString">The backend delimited string to deserialize from.</param>
		/// <returns>A new QueryPreparationTimes from the backend delimited string.</returns>
		internal static QueryPreparationTimes CreateFromDelimitedString(string delimitedString)
		{
			Dictionary<string, double> metrics = QueryMetricsUtils.ParseDelimitedString(delimitedString);
			return new QueryPreparationTimes(QueryMetricsUtils.TimeSpanFromMetrics(metrics, "queryCompileTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(metrics, "queryLogicalPlanBuildTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(metrics, "queryPhysicalPlanBuildTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(metrics, "queryOptimizationTimeInMs"));
		}

		/// <summary>
		/// Creates a new QueryPreparationTimes that is the sum of all elements in an IEnumerable.
		/// </summary>
		/// <param name="queryPreparationTimesList">The IEnumerable to aggregate.</param>
		/// <returns>A new QueryPreparationTimes that is the sum of all elements in an IEnumerable.</returns>
		internal static QueryPreparationTimes CreateFromIEnumerable(IEnumerable<QueryPreparationTimes> queryPreparationTimesList)
		{
			if (queryPreparationTimesList == null)
			{
				throw new ArgumentNullException("queryPreparationTimesList");
			}
			TimeSpan t = default(TimeSpan);
			TimeSpan t2 = default(TimeSpan);
			TimeSpan t3 = default(TimeSpan);
			TimeSpan t4 = default(TimeSpan);
			foreach (QueryPreparationTimes queryPreparationTimes in queryPreparationTimesList)
			{
				if (queryPreparationTimes == null)
				{
					throw new ArgumentException("queryPreparationTimesList can not have a null element");
				}
				t += queryPreparationTimes.queryCompilationTime;
				t2 += queryPreparationTimes.logicalPlanBuildTime;
				t3 += queryPreparationTimes.physicalPlanBuildTime;
				t4 += queryPreparationTimes.queryOptimizationTime;
			}
			return new QueryPreparationTimes(t, t2, t3, t4);
		}
	}
}
