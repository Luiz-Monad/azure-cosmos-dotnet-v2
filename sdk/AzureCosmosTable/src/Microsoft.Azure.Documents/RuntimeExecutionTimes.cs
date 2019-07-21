using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Query runtime execution times in the Azure Cosmos DB service.
	/// </summary>
	public sealed class RuntimeExecutionTimes
	{
		internal static readonly RuntimeExecutionTimes Zero = new RuntimeExecutionTimes(default(TimeSpan), default(TimeSpan), default(TimeSpan));

		private readonly TimeSpan queryEngineExecutionTime;

		private readonly TimeSpan systemFunctionExecutionTime;

		private readonly TimeSpan userDefinedFunctionExecutionTime;

		/// <summary>
		/// Gets the total query runtime execution time in the Azure Cosmos DB service.
		/// </summary>
		internal TimeSpan QueryEngineExecutionTime => queryEngineExecutionTime;

		/// <summary>
		/// Gets the query system function execution time in the Azure Cosmos DB service.
		/// </summary>
		public TimeSpan SystemFunctionExecutionTime => systemFunctionExecutionTime;

		/// <summary>
		/// Gets the query user defined function execution time in the Azure Cosmos DB service.
		/// </summary>
		public TimeSpan UserDefinedFunctionExecutionTime => userDefinedFunctionExecutionTime;

		/// <summary>
		/// Gets the total query runtime execution time in the Azure Cosmos database service.
		/// </summary>
		public TimeSpan TotalTime => queryEngineExecutionTime;

		/// <summary>
		/// Initializes a new instance of the RuntimeExecutionTimes class.
		/// </summary>
		/// <param name="queryEngineExecutionTime">Query end - to - end execution time</param>
		/// <param name="systemFunctionExecutionTime">Total time spent executing system functions</param>
		/// <param name="userDefinedFunctionExecutionTime">Total time spent executing user - defined functions</param>
		[JsonConstructor]
		internal RuntimeExecutionTimes(TimeSpan queryEngineExecutionTime, TimeSpan systemFunctionExecutionTime, TimeSpan userDefinedFunctionExecutionTime)
		{
			this.queryEngineExecutionTime = queryEngineExecutionTime;
			this.systemFunctionExecutionTime = systemFunctionExecutionTime;
			this.userDefinedFunctionExecutionTime = userDefinedFunctionExecutionTime;
		}

		/// <summary>
		/// Creates a new RuntimeExecutionTimes from the backend delimited string.
		/// </summary>
		/// <param name="delimitedString">The backend delimited string to deserialize from.</param>
		/// <returns>A new RuntimeExecutionTimes from the backend delimited string.</returns>
		internal static RuntimeExecutionTimes CreateFromDelimitedString(string delimitedString)
		{
			Dictionary<string, double> metrics = QueryMetricsUtils.ParseDelimitedString(delimitedString);
			TimeSpan t = QueryMetricsUtils.TimeSpanFromMetrics(metrics, "VMExecutionTimeInMs");
			TimeSpan t2 = QueryMetricsUtils.TimeSpanFromMetrics(metrics, "indexLookupTimeInMs");
			TimeSpan t3 = QueryMetricsUtils.TimeSpanFromMetrics(metrics, "documentLoadTimeInMs");
			TimeSpan t4 = QueryMetricsUtils.TimeSpanFromMetrics(metrics, "writeOutputTimeInMs");
			return new RuntimeExecutionTimes(t - t2 - t3 - t4, QueryMetricsUtils.TimeSpanFromMetrics(metrics, "systemFunctionExecuteTimeInMs"), QueryMetricsUtils.TimeSpanFromMetrics(metrics, "userFunctionExecuteTimeInMs"));
		}

		/// <summary>
		/// Creates a new RuntimeExecutionTimes that is the sum of all elements in an IEnumerable.
		/// </summary>
		/// <param name="runtimeExecutionTimesList">The IEnumerable to aggregate.</param>
		/// <returns>A new RuntimeExecutionTimes that is the sum of all elements in an IEnumerable.</returns>
		internal static RuntimeExecutionTimes CreateFromIEnumerable(IEnumerable<RuntimeExecutionTimes> runtimeExecutionTimesList)
		{
			if (runtimeExecutionTimesList == null)
			{
				throw new ArgumentNullException("runtimeExecutionTimesList");
			}
			TimeSpan t = default(TimeSpan);
			TimeSpan t2 = default(TimeSpan);
			TimeSpan t3 = default(TimeSpan);
			foreach (RuntimeExecutionTimes runtimeExecutionTimes in runtimeExecutionTimesList)
			{
				t += runtimeExecutionTimes.queryEngineExecutionTime;
				t2 += runtimeExecutionTimes.systemFunctionExecutionTime;
				t3 += runtimeExecutionTimes.userDefinedFunctionExecutionTime;
			}
			return new RuntimeExecutionTimes(t, t2, t3);
		}
	}
}
