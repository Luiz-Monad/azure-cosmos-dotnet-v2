using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal sealed class Metrics
	{
		private readonly Stopwatch stopwatch;

		public int Count
		{
			get;
			private set;
		}

		public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

		public double AverageElapsedMilliseconds => (double)ElapsedMilliseconds / (double)Count;

		public Metrics()
		{
			stopwatch = new Stopwatch();
		}

		public void Start()
		{
			stopwatch.Start();
		}

		public void Stop()
		{
			stopwatch.Stop();
			int num = ++Count;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "Total time (ms): {0}, Count: {1}, Average Time (ms): {2}", ElapsedMilliseconds, Count, AverageElapsedMilliseconds);
		}
	}
}
