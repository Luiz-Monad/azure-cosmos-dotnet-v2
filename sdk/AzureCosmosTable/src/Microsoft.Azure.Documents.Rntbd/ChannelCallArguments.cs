using System;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ChannelCallArguments
	{
		private readonly ChannelCommonArguments commonArguments;

		public ChannelCommonArguments CommonArguments => commonArguments;

		public Dispatcher.PrepareCallResult PreparedCall
		{
			get;
			set;
		}

		public ChannelCallArguments(Guid activityId)
		{
			commonArguments = new ChannelCommonArguments(activityId, TransportErrorCode.RequestTimeout, userPayload: true);
		}
	}
}
