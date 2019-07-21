using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class LoadBalancingChannel : IChannel, IDisposable
	{
		private readonly Uri serverUri;

		private readonly LoadBalancingPartition singlePartition;

		private readonly LoadBalancingPartition[] partitions;

		private bool disposed;

		public bool Healthy
		{
			get
			{
				ThrowIfDisposed();
				return true;
			}
		}

		public LoadBalancingChannel(Uri serverUri, ChannelProperties channelProperties)
		{
			this.serverUri = serverUri;
			if (channelProperties.PartitionCount < 1 || channelProperties.PartitionCount > 8)
			{
				throw new ArgumentOutOfRangeException("PartitionCount", channelProperties.PartitionCount, "The partition count must be between 1 and 8");
			}
			if (channelProperties.PartitionCount > 1)
			{
				ChannelProperties channelProperties2 = new ChannelProperties(channelProperties.UserAgent, channelProperties.CertificateHostNameOverride, channelProperties.RequestTimerPool, channelProperties.RequestTimeout, channelProperties.OpenTimeout, channelProperties.PortReusePolicy, channelProperties.UserPortPool, MathUtils.CeilingMultiple(channelProperties.MaxChannels, channelProperties.PartitionCount) / channelProperties.PartitionCount, 1, channelProperties.MaxRequestsPerChannel, channelProperties.ReceiveHangDetectionTime, channelProperties.SendHangDetectionTime, channelProperties.IdleTimeout, channelProperties.IdleTimerPool);
				partitions = new LoadBalancingPartition[channelProperties.PartitionCount];
				for (int i = 0; i < partitions.Length; i++)
				{
					partitions[i] = new LoadBalancingPartition(serverUri, channelProperties2);
				}
			}
			else
			{
				singlePartition = new LoadBalancingPartition(serverUri, channelProperties);
			}
		}

		public Task<StoreResponse> RequestAsync(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId)
		{
			ThrowIfDisposed();
			if (singlePartition != null)
			{
				return singlePartition.RequestAsync(request, physicalAddress, resourceOperation, activityId);
			}
			int hashCode = activityId.GetHashCode();
			return partitions[(hashCode & 2415919103u) % partitions.Length].RequestAsync(request, physicalAddress, resourceOperation, activityId);
		}

		public void Close()
		{
			((IDisposable)this).Dispose();
		}

		void IDisposable.Dispose()
		{
			ThrowIfDisposed();
			disposed = true;
			if (singlePartition != null)
			{
				singlePartition.Dispose();
			}
			if (partitions != null)
			{
				for (int i = 0; i < partitions.Length; i++)
				{
					partitions[i].Dispose();
				}
			}
		}

		private void ThrowIfDisposed()
		{
			if (disposed)
			{
				throw new ObjectDisposedException(string.Format("{0}:{1}", "LoadBalancingChannel", serverUri));
			}
		}
	}
}
