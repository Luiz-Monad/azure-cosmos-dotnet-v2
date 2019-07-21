using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelDictionary : IDisposable
	{
		private readonly ChannelProperties channelProperties;

		private bool disposed;

		private ConcurrentDictionary<ServerKey, IChannel> channels = new ConcurrentDictionary<ServerKey, IChannel>();

		public ChannelDictionary(ChannelProperties channelProperties)
		{
			this.channelProperties = channelProperties;
		}

		public IChannel GetChannel(Uri requestUri)
		{
			ThrowIfDisposed();
			ServerKey key = new ServerKey(requestUri);
			IChannel value = null;
			if (channels.TryGetValue(key, out value))
			{
				return value;
			}
			value = new LoadBalancingChannel(new Uri(requestUri.GetLeftPart(UriPartial.Authority)), channelProperties);
			if (channels.TryAdd(key, value))
			{
				return value;
			}
			channels.TryGetValue(key, out value);
			return value;
		}

		public void Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			foreach (IChannel value in channels.Values)
			{
				value.Close();
			}
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException("ChannelDictionary");
			}
		}
	}
}
