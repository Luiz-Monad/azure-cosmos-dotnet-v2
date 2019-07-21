using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class CpuLoadHistory
	{
		private readonly ReadOnlyCollection<CpuLoad> cpuLoad;

		private readonly TimeSpan monitoringInterval;

		private readonly Lazy<bool> cpuOverload;

		public bool IsCpuOverloaded => cpuOverload.Value;

		internal DateTime LastTimestamp => cpuLoad[cpuLoad.Count - 1].Timestamp;

		public CpuLoadHistory(ReadOnlyCollection<CpuLoad> cpuLoad, TimeSpan monitoringInterval)
		{
			if (cpuLoad == null)
			{
				throw new ArgumentNullException("cpuLoad");
			}
			this.cpuLoad = cpuLoad;
			if (monitoringInterval <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("monitoringInterval", monitoringInterval, string.Format("{0} must be strictly positive", "monitoringInterval"));
			}
			this.monitoringInterval = monitoringInterval;
			cpuOverload = new Lazy<bool>(GetCpuOverload, LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public override string ToString()
		{
			ReadOnlyCollection<CpuLoad> readOnlyCollection = cpuLoad;
			if (readOnlyCollection != null && readOnlyCollection.Count == 0)
			{
				return "empty";
			}
			return string.Join(", ", cpuLoad);
		}

		private bool GetCpuOverload()
		{
			for (int i = 0; i < this.cpuLoad.Count; i++)
			{
				if ((double)this.cpuLoad[i].Value > 0.9)
				{
					return true;
				}
			}
			for (int j = 0; j < this.cpuLoad.Count - 1; j++)
			{
				CpuLoad cpuLoad = this.cpuLoad[j + 1];
				if (cpuLoad.Timestamp.Subtract(this.cpuLoad[j].Timestamp).TotalMilliseconds > 1.5 * monitoringInterval.TotalMilliseconds)
				{
					return true;
				}
			}
			return false;
		}
	}
}
