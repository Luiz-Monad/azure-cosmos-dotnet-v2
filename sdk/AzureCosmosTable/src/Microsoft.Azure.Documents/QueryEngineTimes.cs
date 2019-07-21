using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Query engine time in the Azure Cosmos database service.
	/// (dummy class that will be deprecated).
	/// </summary>
	public sealed class QueryEngineTimes
	{
		private readonly TimeSpan indexLookupTime;

		private readonly TimeSpan documentLoadTime;

		private readonly TimeSpan vmExecutionTime;

		private readonly TimeSpan writeOutputTime;

		private readonly RuntimeExecutionTimes runtimeExecutionTimes;

		/// <summary>
		/// Gets the query index lookup time in the Azure Cosmos database service.
		/// </summary>
		public TimeSpan IndexLookupTime => indexLookupTime;

		/// <summary>
		/// Gets the document loading time during query in the Azure Cosmos database service.
		/// </summary>
		public TimeSpan DocumentLoadTime => documentLoadTime;

		/// <summary>
		/// Gets the output writing/serializing time during query in the Azure Cosmos database service.
		/// </summary>
		public TimeSpan WriteOutputTime => writeOutputTime;

		/// <summary>
		/// Gets the query runtime execution times during query in the Azure Cosmos database service.
		/// </summary>
		public RuntimeExecutionTimes RuntimeExecutionTimes => runtimeExecutionTimes;

		internal TimeSpan VMExecutionTime => vmExecutionTime;

		internal QueryEngineTimes(TimeSpan indexLookupTime, TimeSpan documentLoadTime, TimeSpan vmExecutionTime, TimeSpan writeOutputTime, RuntimeExecutionTimes runtimeExecutionTimes)
		{
			this.indexLookupTime = indexLookupTime;
			this.documentLoadTime = documentLoadTime;
			this.vmExecutionTime = vmExecutionTime;
			this.writeOutputTime = writeOutputTime;
			this.runtimeExecutionTimes = runtimeExecutionTimes;
		}
	}
}
