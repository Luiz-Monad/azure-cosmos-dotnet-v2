using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	internal class PartitionKeyRangeCache : IRoutingMapProvider, ICollectionRoutingMapCache
	{
		private const string PageSizeString = "-1";

		private readonly AsyncCache<string, CollectionRoutingMap> routingMapCache;

		private readonly IAuthorizationTokenProvider authorizationTokenProvider;

		private readonly IStoreModel storeModel;

		private readonly CollectionCache collectionCache;

		public PartitionKeyRangeCache(IAuthorizationTokenProvider authorizationTokenProvider, IStoreModel storeModel, CollectionCache collectionCache)
		{
			routingMapCache = new AsyncCache<string, CollectionRoutingMap>(EqualityComparer<CollectionRoutingMap>.Default, StringComparer.Ordinal);
			this.authorizationTokenProvider = authorizationTokenProvider;
			this.storeModel = storeModel;
			this.collectionCache = collectionCache;
		}

		public async Task<IReadOnlyList<PartitionKeyRange>> TryGetOverlappingRangesAsync(string collectionRid, Range<string> range, bool forceRefresh = false)
		{
			CollectionRoutingMap collectionRoutingMap = await TryLookupAsync(collectionRid, null, null, CancellationToken.None);
			if (forceRefresh && collectionRoutingMap != null)
			{
				collectionRoutingMap = await TryLookupAsync(collectionRid, collectionRoutingMap, null, CancellationToken.None);
			}
			if (collectionRoutingMap == null)
			{
				DefaultTrace.TraceInformation($"Routing Map Null for collection: {collectionRid} for range: {range.ToString()}, forceRefresh:{forceRefresh}");
				return null;
			}
			return collectionRoutingMap.GetOverlappingRanges(range);
		}

		public async Task<PartitionKeyRange> TryGetPartitionKeyRangeByIdAsync(string collectionResourceId, string partitionKeyRangeId, bool forceRefresh = false)
		{
			CollectionRoutingMap collectionRoutingMap = await TryLookupAsync(collectionResourceId, null, null, CancellationToken.None);
			if (forceRefresh && collectionRoutingMap != null)
			{
				collectionRoutingMap = await TryLookupAsync(collectionResourceId, collectionRoutingMap, null, CancellationToken.None);
			}
			if (collectionRoutingMap == null)
			{
				DefaultTrace.TraceInformation($"Routing Map Null for collection: {collectionResourceId}, PartitionKeyRangeId: {partitionKeyRangeId}, forceRefresh:{forceRefresh}");
				return null;
			}
			return collectionRoutingMap.TryGetRangeByPartitionKeyRangeId(partitionKeyRangeId);
		}

		public async Task<CollectionRoutingMap> TryLookupAsync(string collectionRid, CollectionRoutingMap previousValue, DocumentServiceRequest request, CancellationToken cancellationToken)
		{
			try
			{
				return await routingMapCache.GetAsync(collectionRid, previousValue, () => GetRoutingMapForCollectionAsync(collectionRid, previousValue, cancellationToken), CancellationToken.None);
			}
			catch (DocumentClientException ex)
			{
				if (previousValue != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					foreach (PartitionKeyRange orderedPartitionKeyRange in previousValue.OrderedPartitionKeyRanges)
					{
						stringBuilder.Append(orderedPartitionKeyRange.ToRange().ToString());
						stringBuilder.Append(", ");
					}
					DefaultTrace.TraceInformation($"DocumentClientException in TryLookupAsync Collection: {collectionRid}, previousValue: {stringBuilder.ToString()} Exception: {ex.ToString()}");
				}
				if (ex.StatusCode == HttpStatusCode.NotFound)
				{
					return null;
				}
				throw;
			}
		}

		public async Task<PartitionKeyRange> TryGetRangeByPartitionKeyRangeId(string collectionRid, string partitionKeyRangeId)
		{
			try
			{
				return (await routingMapCache.GetAsync(collectionRid, null, () => GetRoutingMapForCollectionAsync(collectionRid, null, CancellationToken.None), CancellationToken.None)).TryGetRangeByPartitionKeyRangeId(partitionKeyRangeId);
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode == HttpStatusCode.NotFound)
				{
					return null;
				}
				throw;
			}
		}

		private async Task<CollectionRoutingMap> GetRoutingMapForCollectionAsync(string collectionRid, CollectionRoutingMap previousRoutingMap, CancellationToken cancellationToken)
		{
			List<PartitionKeyRange> ranges = new List<PartitionKeyRange>();
			string text = previousRoutingMap?.ChangeFeedNextIfNoneMatch;
			HttpStatusCode httpStatusCode = HttpStatusCode.OK;
			do
			{
				INameValueCollection headers = new StringKeyValueCollection();
				headers.Set("x-ms-max-item-count", "-1");
				headers.Set("A-IM", "Incremental Feed");
				if (text != null)
				{
					headers.Set("If-None-Match", text);
				}
				RetryOptions retryOptions = new RetryOptions();
				using (DocumentServiceResponse documentServiceResponse = await BackoffRetryUtility<DocumentServiceResponse>.ExecuteAsync(() => ExecutePartitionKeyRangeReadChangeFeed(collectionRid, headers), new ResourceThrottleRetryPolicy(retryOptions.MaxRetryAttemptsOnThrottledRequests, retryOptions.MaxRetryWaitTimeInSeconds), cancellationToken))
				{
					httpStatusCode = documentServiceResponse.StatusCode;
					text = documentServiceResponse.Headers["etag"];
					FeedResource<PartitionKeyRange> resource = documentServiceResponse.GetResource<FeedResource<PartitionKeyRange>>();
					if (resource != null)
					{
						ranges.AddRange(resource);
					}
				}
			}
			while (httpStatusCode != HttpStatusCode.NotModified);
			IEnumerable<Tuple<PartitionKeyRange, ServiceIdentity>> enumerable = from range in ranges
			select Tuple.Create<PartitionKeyRange, ServiceIdentity>(range, null);
			CollectionRoutingMap collectionRoutingMap;
			if (previousRoutingMap == null)
			{
				HashSet<string> goneRanges = new HashSet<string>(ranges.SelectMany(delegate(PartitionKeyRange range)
				{
					IEnumerable<string> parents = range.Parents;
					return parents ?? Enumerable.Empty<string>();
				}));
				collectionRoutingMap = CollectionRoutingMap.TryCreateCompleteRoutingMap(from tuple in enumerable
				where !goneRanges.Contains(tuple.Item1.Id)
				select tuple, string.Empty, text);
			}
			else
			{
				collectionRoutingMap = previousRoutingMap.TryCombine(enumerable, text);
			}
			if (collectionRoutingMap == null)
			{
				throw new NotFoundException(string.Format("{0}: GetRoutingMapForCollectionAsync(collectionRid: {1}), Range information either doesn't exist or is not complete.", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), collectionRid));
			}
			return collectionRoutingMap;
		}

		private async Task<DocumentServiceResponse> ExecutePartitionKeyRangeReadChangeFeed(string collectionRid, INameValueCollection headers)
		{
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.ReadFeed, collectionRid, ResourceType.PartitionKeyRange, AuthorizationTokenType.PrimaryMasterKey, headers))
			{
				string text = null;
				try
				{
					text = authorizationTokenProvider.GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "GET", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
				}
				catch (UnauthorizedException)
				{
				}
				if (text == null)
				{
					DocumentCollection documentCollection = await collectionCache.ResolveCollectionAsync(request, CancellationToken.None);
					text = authorizationTokenProvider.GetUserAuthorizationToken(documentCollection.AltLink, PathsHelper.GetResourcePath(request.ResourceType), "GET", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
				}
				request.Headers["authorization"] = text;
				using (new ActivityScope(Guid.NewGuid()))
				{
					return await storeModel.ProcessMessageAsync(request);
				}
			}
		}
	}
}
