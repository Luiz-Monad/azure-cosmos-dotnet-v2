using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents.Client
{
	internal static class FeedResponseBinder
	{
		public static FeedResponse<T> Convert<T>(FeedResponse<dynamic> dynamicFeed)
		{
			if (typeof(T) == typeof(object))
			{
				return (FeedResponse<T>)(object)dynamicFeed;
			}
			IList<T> list = new List<T>();
			foreach (object item2 in dynamicFeed)
			{
				T item = (T)(dynamic)item2;
				list.Add(item);
			}
			return new FeedResponse<T>(list, dynamicFeed.Count, dynamicFeed.Headers, dynamicFeed.UseETagAsContinuation, dynamicFeed.QueryMetrics, dynamicFeed.RequestStatistics, null, dynamicFeed.ResponseLengthBytes);
		}

		public static IQueryable<T> AsQueryable<T>(FeedResponse<dynamic> dynamicFeed)
		{
			return Convert<T>(dynamicFeed).AsQueryable();
		}
	}
}
