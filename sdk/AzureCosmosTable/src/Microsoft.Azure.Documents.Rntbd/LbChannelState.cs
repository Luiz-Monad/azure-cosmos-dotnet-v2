using System;
using System.Threading;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class LbChannelState : IDisposable
	{
		private readonly int maxRequestsPending;

		private readonly IChannel channel;

		private int requestsPending;

		private bool cachedHealthy = true;

		public bool DeepHealthy
		{
			get
			{
				if (!ShallowHealthy)
				{
					return false;
				}
				bool healthy = channel.Healthy;
				if (!healthy)
				{
					cachedHealthy = false;
					Interlocked.MemoryBarrier();
				}
				return healthy;
			}
		}

		public bool ShallowHealthy
		{
			get
			{
				Interlocked.MemoryBarrier();
				return cachedHealthy;
			}
		}

		public IChannel Channel => channel;

		public LbChannelState(IChannel channel, int maxRequestsPending)
		{
			this.channel = channel;
			this.maxRequestsPending = maxRequestsPending;
		}

		public bool Enter()
		{
			if (Interlocked.Increment(ref requestsPending) > maxRequestsPending)
			{
				Interlocked.Decrement(ref requestsPending);
				return false;
			}
			return true;
		}

		public bool Exit()
		{
			return Interlocked.Decrement(ref requestsPending) == 0;
		}

		public void Dispose()
		{
			channel.Close();
		}
	}
}
