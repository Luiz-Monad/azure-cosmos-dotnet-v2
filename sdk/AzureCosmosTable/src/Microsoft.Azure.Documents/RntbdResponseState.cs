using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal class RntbdResponseState
	{
		private static readonly string[] StateNames = Enum.GetNames(typeof(RntbdResponseStateEnum));

		private RntbdResponseStateEnum state;

		private int headerAndMetadataRead;

		private int bodyRead;

		private DateTimeOffset lastReadTime;

		public RntbdResponseState()
		{
			state = RntbdResponseStateEnum.NotStarted;
			headerAndMetadataRead = 0;
			bodyRead = 0;
			lastReadTime = DateTimeOffset.MinValue;
		}

		public void SetState(RntbdResponseStateEnum newState)
		{
			if (newState < state || newState > RntbdResponseStateEnum.Done)
			{
				throw new InternalServerErrorException();
			}
			state = newState;
		}

		public void AddHeaderMetadataRead(int amountRead)
		{
			headerAndMetadataRead += amountRead;
			lastReadTime = DateTimeOffset.Now;
		}

		public void AddBodyRead(int amountRead)
		{
			bodyRead += amountRead;
			lastReadTime = DateTimeOffset.Now;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "State: {0}. Meta bytes read: {1}. Body bytes read: {2}. Last read completion: {3}", StateNames[(int)state], headerAndMetadataRead, bodyRead, lastReadTime);
		}
	}
}
