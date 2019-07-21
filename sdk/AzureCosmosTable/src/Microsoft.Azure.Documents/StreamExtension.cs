using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal static class StreamExtension
	{
		public static async Task CopyToAsync(this Stream srcStream, Stream destinationStream, long maxSizeToCopy = long.MaxValue)
		{
			if (srcStream == null)
			{
				throw new ArgumentNullException("srcStream");
			}
			if (destinationStream == null)
			{
				throw new ArgumentNullException("destinationStream");
			}
			byte[] buffer = new byte[1024];
			long numberOfBytesRead = 0L;
			while (true)
			{
				int num = await srcStream.ReadAsync(buffer, 0, 1024);
				if (num <= 0)
				{
					return;
				}
				numberOfBytesRead += num;
				if (numberOfBytesRead > maxSizeToCopy)
				{
					break;
				}
				await destinationStream.WriteAsync(buffer, 0, num);
			}
			throw new RequestEntityTooLargeException(RMResources.RequestTooLarge);
		}
	}
}
