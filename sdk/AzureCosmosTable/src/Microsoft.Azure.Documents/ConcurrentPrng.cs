using System;

namespace Microsoft.Azure.Documents
{
	internal sealed class ConcurrentPrng
	{
		private readonly object mutex = new object();

		private readonly Random rng = new Random();

		public int Next(int maxValue)
		{
			lock (mutex)
			{
				return rng.Next(maxValue);
			}
		}
	}
}
