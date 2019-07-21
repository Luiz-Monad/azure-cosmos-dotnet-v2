using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class LoadBalancingPartition : IDisposable
	{
		private sealed class SequenceGenerator
		{
			private int current;

			public uint Next()
			{
				return (uint)(2147483648u + Interlocked.Increment(ref current));
			}
		}

		private readonly Uri serverUri;

		private readonly ChannelProperties channelProperties;

		private readonly int maxCapacity;

		private int requestsPending;

		private readonly SequenceGenerator sequenceGenerator = new SequenceGenerator();

		private readonly ReaderWriterLockSlim capacityLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		private int capacity;

		private readonly List<LbChannelState> openChannels = new List<LbChannelState>();

		public LoadBalancingPartition(Uri serverUri, ChannelProperties channelProperties)
		{
			this.serverUri = serverUri;
			this.channelProperties = channelProperties;
			maxCapacity = checked(channelProperties.MaxChannels * channelProperties.MaxRequestsPerChannel);
		}

		public async Task<StoreResponse> RequestAsync(DocumentServiceRequest request, Uri physicalAddress, ResourceOperation resourceOperation, Guid activityId)
		{
			int num = Interlocked.Increment(ref requestsPending);
			try
			{
				if (num > maxCapacity)
				{
					throw new RequestRateTooLargeException($"All connections to {serverUri} are fully utilized. Increase the maximum number of connections or the maximum number of requests per connection", SubStatusCodes.ClientTcpChannelFull);
				}
				while (true)
				{
					LbChannelState channelState = null;
					bool flag = false;
					uint num2 = sequenceGenerator.Next();
					capacityLock.EnterReadLock();
					try
					{
						if (num <= capacity)
						{
							int index = (int)((long)num2 % (long)openChannels.Count);
							LbChannelState lbChannelState = openChannels[index];
							if (lbChannelState.Enter())
							{
								channelState = lbChannelState;
							}
						}
						else
						{
							flag = true;
						}
					}
					finally
					{
						capacityLock.ExitReadLock();
					}
					if (channelState != null)
					{
						try
						{
							if (channelState.DeepHealthy)
							{
								return await channelState.Channel.RequestAsync(request, physicalAddress, resourceOperation, activityId);
							}
							capacityLock.EnterWriteLock();
							try
							{
								if (openChannels.Remove(channelState))
								{
									capacity -= channelProperties.MaxRequestsPerChannel;
								}
							}
							finally
							{
								capacityLock.ExitWriteLock();
							}
						}
						finally
						{
							if (channelState.Exit() && !channelState.ShallowHealthy)
							{
								channelState.Dispose();
								DefaultTrace.TraceInformation("Closed unhealthy channel {0}", channelState.Channel);
							}
						}
					}
					else if (flag)
					{
						int num3 = MathUtils.CeilingMultiple(num, channelProperties.MaxRequestsPerChannel) / channelProperties.MaxRequestsPerChannel;
						int num4 = 0;
						capacityLock.EnterWriteLock();
						try
						{
							if (openChannels.Count < num3)
							{
								num4 = num3 - openChannels.Count;
							}
							while (openChannels.Count < num3)
							{
								Channel channel = new Channel(activityId, serverUri, channelProperties);
								channel.Initialize();
								openChannels.Add(new LbChannelState(channel, channelProperties.MaxRequestsPerChannel));
								capacity += channelProperties.MaxRequestsPerChannel;
							}
						}
						finally
						{
							capacityLock.ExitWriteLock();
						}
						if (num4 > 0)
						{
							DefaultTrace.TraceInformation("Opened {0} channels to server {1}", num4, serverUri);
						}
					}
				}
			}
			finally
			{
				Interlocked.Decrement(ref requestsPending);
			}
		}

		public void Dispose()
		{
			capacityLock.EnterWriteLock();
			try
			{
				foreach (LbChannelState openChannel in openChannels)
				{
					openChannel.Dispose();
				}
			}
			finally
			{
				capacityLock.ExitWriteLock();
			}
			capacityLock.Dispose();
		}
	}
}
