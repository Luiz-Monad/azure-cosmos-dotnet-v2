using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Common
{
	/// <summary>
	/// Cache to provide resource id lookup based on resource name
	/// </summary>
	internal abstract class CollectionCache
	{
		/// <summary>
		/// Master Service returns collection definition based on API Version and may not be always same for all API Versions.
		/// Here the InternalCache stores collection information related to a particular API Version
		/// </summary>
		protected class InternalCache
		{
			internal readonly AsyncCache<string, DocumentCollection> collectionInfoByName;

			internal readonly AsyncCache<string, DocumentCollection> collectionInfoById;

			internal readonly ConcurrentDictionary<string, DateTime> collectionInfoByNameLastRefreshTime;

			internal readonly ConcurrentDictionary<string, DateTime> collectionInfoByIdLastRefreshTime;

			internal InternalCache()
			{
				collectionInfoByName = new AsyncCache<string, DocumentCollection>(new CollectionRidComparer());
				collectionInfoById = new AsyncCache<string, DocumentCollection>(new CollectionRidComparer());
				collectionInfoByNameLastRefreshTime = new ConcurrentDictionary<string, DateTime>();
				collectionInfoByIdLastRefreshTime = new ConcurrentDictionary<string, DateTime>();
			}
		}

		private sealed class CollectionRidComparer : IEqualityComparer<DocumentCollection>
		{
			public bool Equals(DocumentCollection left, DocumentCollection right)
			{
				if (left == null && right == null)
				{
					return true;
				}
				if ((left == null) ^ (right == null))
				{
					return false;
				}
				return StringComparer.Ordinal.Compare(left.ResourceId, right.ResourceId) == 0;
			}

			public int GetHashCode(DocumentCollection collection)
			{
				return collection.ResourceId.GetHashCode();
			}
		}

		/// <summary>
		/// cacheByApiList caches the collection information by API Version. In general it is expected that only a single version is populated
		/// for a collection, but this handles the situation if customer is using multiple API versions from different applications
		/// </summary>
		protected readonly InternalCache[] cacheByApiList;

		protected CollectionCache()
		{
			cacheByApiList = new InternalCache[2];
			cacheByApiList[0] = new InternalCache();
			cacheByApiList[1] = new InternalCache();
		}

		/// <summary>
		/// Resolve the DocumentCollection object from the cache. If the collection was read before "refreshAfter" Timespan, force a cache refresh by reading from the backend.
		/// </summary>
		/// <param name="request">Request to resolve.</param>
		/// <param name="refreshAfter"> Time duration to refresh</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.DocumentCollection" />.</returns>
		public virtual Task<DocumentCollection> ResolveCollectionAsync(DocumentServiceRequest request, TimeSpan refreshAfter, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			InternalCache cache = GetCache(request.Headers["x-ms-version"]);
			DateTime utcNow = DateTime.UtcNow;
			DateTime value = DateTime.MinValue;
			if (request.IsNameBased)
			{
				string collectionPath = PathsHelper.GetCollectionPath(request.ResourceAddress);
				if (cache.collectionInfoByNameLastRefreshTime.TryGetValue(collectionPath, out value) && utcNow - value > refreshAfter)
				{
					cache.collectionInfoByName.TryRemoveIfCompleted(collectionPath);
				}
			}
			else
			{
				string key = ResourceId.Parse(request.ResourceId).DocumentCollectionId.ToString();
				if (cache.collectionInfoByIdLastRefreshTime.TryGetValue(key, out value) && utcNow - value > refreshAfter)
				{
					cache.collectionInfoById.TryRemoveIfCompleted(request.ResourceId);
				}
			}
			return ResolveCollectionAsync(request, cancellationToken);
		}

		/// <summary>
		/// Resolves a request to a collection in a sticky manner.
		/// Unless request.ForceNameCacheRefresh is equal to true, it will return the same collection.
		/// </summary>
		/// <param name="request">Request to resolve.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.DocumentCollection" />.</returns>
		public virtual async Task<DocumentCollection> ResolveCollectionAsync(DocumentServiceRequest request, CancellationToken cancellationToken)
		{
			if (request.IsNameBased)
			{
				if (request.ForceNameCacheRefresh)
				{
					await RefreshAsync(request, cancellationToken);
					request.ForceNameCacheRefresh = false;
				}
				DocumentCollection documentCollection = await ResolveByPartitionKeyRangeIdentityAsync(request.Headers["x-ms-version"], request.PartitionKeyRangeIdentity, cancellationToken);
				if (documentCollection != null)
				{
					return documentCollection;
				}
				if (request.RequestContext.ResolvedCollectionRid == null)
				{
					documentCollection = await ResolveByNameAsync(request.Headers["x-ms-version"], request.ResourceAddress, cancellationToken);
					if (documentCollection != null)
					{
						DefaultTrace.TraceVerbose("Mapped resourceName {0} to resourceId {1}. '{2}'", request.ResourceAddress, documentCollection.ResourceId, Trace.CorrelationManager.ActivityId);
						request.ResourceId = documentCollection.ResourceId;
						request.RequestContext.ResolvedCollectionRid = documentCollection.ResourceId;
					}
					else
					{
						DefaultTrace.TraceVerbose("Collection with resourceName {0} not found. '{1}'", request.ResourceAddress, Trace.CorrelationManager.ActivityId);
					}
					return documentCollection;
				}
				return await ResolveByRidAsync(request.Headers["x-ms-version"], request.RequestContext.ResolvedCollectionRid, cancellationToken);
			}
			return (await ResolveByPartitionKeyRangeIdentityAsync(request.Headers["x-ms-version"], request.PartitionKeyRangeIdentity, cancellationToken)) ?? (await ResolveByRidAsync(request.Headers["x-ms-version"], request.ResourceAddress, cancellationToken));
		}

		/// <summary>
		/// This method is only used in client SDK in retry policy as it doesn't have request handy.
		/// </summary>
		public void Refresh(string resourceAddress, string apiVersion = null)
		{
			InternalCache cache = GetCache(apiVersion);
			if (PathsHelper.IsNameBased(resourceAddress))
			{
				string collectionPath = PathsHelper.GetCollectionPath(resourceAddress);
				cache.collectionInfoByName.TryRemoveIfCompleted(collectionPath);
			}
		}

		protected abstract Task<DocumentCollection> GetByRidAsync(string apiVersion, string collectionRid, CancellationToken cancellationToken);

		protected abstract Task<DocumentCollection> GetByNameAsync(string apiVersion, string resourceAddress, CancellationToken cancellationToken);

		private async Task<DocumentCollection> ResolveByPartitionKeyRangeIdentityAsync(string apiVersion, PartitionKeyRangeIdentity partitionKeyRangeIdentity, CancellationToken cancellationToken)
		{
			if (partitionKeyRangeIdentity != null && partitionKeyRangeIdentity.CollectionRid != null)
			{
				try
				{
					return await ResolveByRidAsync(apiVersion, partitionKeyRangeIdentity.CollectionRid, cancellationToken);
				}
				catch (NotFoundException)
				{
					throw new InvalidPartitionException(RMResources.InvalidDocumentCollection);
				}
			}
			return null;
		}

		private Task<DocumentCollection> ResolveByRidAsync(string apiVersion, string resourceId, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ResourceId resourceId2 = ResourceId.Parse(resourceId);
			string collectionResourceId = resourceId2.DocumentCollectionId.ToString();
			InternalCache cache = GetCache(apiVersion);
			return cache.collectionInfoById.GetAsync(collectionResourceId, null, async delegate
			{
				DateTime currentTime = DateTime.UtcNow;
				DocumentCollection result = await GetByRidAsync(apiVersion, collectionResourceId, cancellationToken);
				cache.collectionInfoByIdLastRefreshTime.AddOrUpdate(collectionResourceId, currentTime, (string currentKey, DateTime currentValue) => currentTime);
				return result;
			}, cancellationToken);
		}

		private async Task<DocumentCollection> ResolveByNameAsync(string apiVersion, string resourceAddress, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string resourceFullName = PathsHelper.GetCollectionPath(resourceAddress);
			InternalCache cache = GetCache(apiVersion);
			return await cache.collectionInfoByName.GetAsync(resourceFullName, null, async delegate
			{
				DateTime currentTime = DateTime.UtcNow;
				DocumentCollection documentCollection = await GetByNameAsync(apiVersion, resourceFullName, cancellationToken);
				cache.collectionInfoById.Set(documentCollection.ResourceId, documentCollection);
				cache.collectionInfoByNameLastRefreshTime.AddOrUpdate(resourceFullName, currentTime, (string currentKey, DateTime currentValue) => currentTime);
				cache.collectionInfoByIdLastRefreshTime.AddOrUpdate(documentCollection.ResourceId, currentTime, (string currentKey, DateTime currentValue) => currentTime);
				return documentCollection;
			}, cancellationToken);
		}

		private async Task RefreshAsync(DocumentServiceRequest request, CancellationToken cancellationToken)
		{
			InternalCache cache = GetCache(request.Headers["x-ms-version"]);
			string resourceFullName = PathsHelper.GetCollectionPath(request.ResourceAddress);
			if (request.RequestContext.ResolvedCollectionRid != null)
			{
				await cache.collectionInfoByName.GetAsync(resourceFullName, new DocumentCollection
				{
					ResourceId = request.RequestContext.ResolvedCollectionRid
				}, async delegate
				{
					DateTime currentTime = DateTime.UtcNow;
					DocumentCollection documentCollection = await GetByNameAsync(request.Headers["x-ms-version"], resourceFullName, cancellationToken);
					cache.collectionInfoById.Set(documentCollection.ResourceId, documentCollection);
					cache.collectionInfoByNameLastRefreshTime.AddOrUpdate(resourceFullName, currentTime, (string currentKey, DateTime currentValue) => currentTime);
					cache.collectionInfoByIdLastRefreshTime.AddOrUpdate(documentCollection.ResourceId, currentTime, (string currentKey, DateTime currentValue) => currentTime);
					return documentCollection;
				}, cancellationToken);
			}
			else
			{
				Refresh(request.ResourceAddress, request.Headers["x-ms-version"]);
			}
			request.RequestContext.ResolvedCollectionRid = null;
		}

		/// <summary>
		/// The function selects the right cache based on apiVersion. 
		/// </summary>
		protected InternalCache GetCache(string apiVersion)
		{
			if (!string.IsNullOrEmpty(apiVersion) && VersionUtility.IsLaterThan(apiVersion, HttpConstants.Versions.v2018_12_31))
			{
				return cacheByApiList[1];
			}
			return cacheByApiList[0];
		}
	}
}
