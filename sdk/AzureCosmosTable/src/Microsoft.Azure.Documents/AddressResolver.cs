using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Abstracts out the logic to resolve physical replica addresses for the given <see cref="T:Microsoft.Azure.Documents.DocumentServiceRequest" />.
	///
	/// AddressCache internally maintains CollectionCache, CollectionRoutingMapCache and BackendAddressCache.
	/// Logic in this class mainly joins these 3 caches and deals with potential staleness of the caches.
	///
	/// More details are available here:
	/// https://microsoft.sharepoint.com/teams/DocumentDB/Design%20Documents/Manageability/Elastic%20Collections%20Routing.docx?d=w3356bd9ad32746b280c0bcb8f9905986
	/// </summary>
	internal sealed class AddressResolver : IAddressResolver
	{
		private class ResolutionResult
		{
			public PartitionKeyRange TargetPartitionKeyRange
			{
				get;
				private set;
			}

			public PartitionAddressInformation Addresses
			{
				get;
				private set;
			}

			public ServiceIdentity TargetServiceIdentity
			{
				get;
				private set;
			}

			public ResolutionResult(PartitionAddressInformation addresses, ServiceIdentity serviceIdentity)
			{
				if (addresses == null)
				{
					throw new ArgumentNullException("addresses");
				}
				if (serviceIdentity == null)
				{
					throw new ArgumentNullException("serviceIdentity");
				}
				Addresses = addresses;
				TargetServiceIdentity = serviceIdentity;
			}

			public ResolutionResult(PartitionKeyRange targetPartitionKeyRange, PartitionAddressInformation addresses, ServiceIdentity serviceIdentity)
			{
				if (targetPartitionKeyRange == null)
				{
					throw new ArgumentNullException("targetPartitionKeyRange");
				}
				if (addresses == null)
				{
					throw new ArgumentNullException("addresses");
				}
				TargetPartitionKeyRange = targetPartitionKeyRange;
				Addresses = addresses;
				TargetServiceIdentity = serviceIdentity;
			}
		}

		private CollectionCache collectionCache;

		private ICollectionRoutingMapCache collectionRoutingMapCache;

		private IAddressCache addressCache;

		private readonly IMasterServiceIdentityProvider masterServiceIdentityProvider;

		private readonly IRequestSigner requestSigner;

		private readonly string location;

		private readonly PartitionKeyRangeIdentity masterPartitionKeyRangeIdentity = new PartitionKeyRangeIdentity("M");

		public AddressResolver(IMasterServiceIdentityProvider masterServiceIdentityProvider, IRequestSigner requestSigner, string location)
		{
			this.masterServiceIdentityProvider = masterServiceIdentityProvider;
			this.requestSigner = requestSigner;
			this.location = location;
		}

		public void InitializeCaches(CollectionCache collectionCache, ICollectionRoutingMapCache collectionRoutingMapCache, IAddressCache addressCache)
		{
			this.collectionCache = collectionCache;
			this.addressCache = addressCache;
			this.collectionRoutingMapCache = collectionRoutingMapCache;
		}

		public async Task<PartitionAddressInformation> ResolveAsync(DocumentServiceRequest request, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ResolutionResult result = await ResolveAddressesAndIdentityAsync(request, forceRefreshPartitionAddresses, cancellationToken);
			ThrowIfTargetChanged(request, result.TargetPartitionKeyRange);
			request.RequestContext.TargetIdentity = result.TargetServiceIdentity;
			request.RequestContext.ResolvedPartitionKeyRange = result.TargetPartitionKeyRange;
			request.RequestContext.RegionName = location;
			await requestSigner.SignRequestAsync(request, cancellationToken);
			return result.Addresses;
		}

		private static bool IsSameCollection(PartitionKeyRange initiallyResolved, PartitionKeyRange newlyResolved)
		{
			if (initiallyResolved == null)
			{
				throw new ArgumentException("parent");
			}
			if (newlyResolved == null)
			{
				return false;
			}
			if (initiallyResolved.Id == "M" && newlyResolved.Id == "M")
			{
				return true;
			}
			if (initiallyResolved.Id == "M" || newlyResolved.Id == "M")
			{
				DefaultTrace.TraceCritical("Request was resolved to master partition and then to server partition.");
				return false;
			}
			if (ResourceId.Parse(initiallyResolved.ResourceId).DocumentCollection != ResourceId.Parse(newlyResolved.ResourceId).DocumentCollection)
			{
				return false;
			}
			if (initiallyResolved.Id != newlyResolved.Id && (newlyResolved.Parents == null || !newlyResolved.Parents.Contains(initiallyResolved.Id)))
			{
				DefaultTrace.TraceCritical("Request is targeted at a partition key range which is not child of previously targeted range.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Validates if the target partition to which the request is being sent has changed during retry.
		/// If that happens, the request is no more valid and need to be retried.
		/// Also has the side-effect that if the target identity is not set, we set it on the request
		/// </summary>
		/// <param name="request">Request in progress</param>
		/// <param name="targetRange">Target partition key range determined by address resolver</param>
		private void ThrowIfTargetChanged(DocumentServiceRequest request, PartitionKeyRange targetRange)
		{
			if (request.RequestContext.ResolvedPartitionKeyRange != null && !IsSameCollection(request.RequestContext.ResolvedPartitionKeyRange, targetRange))
			{
				if (!request.IsNameBased)
				{
					DefaultTrace.TraceCritical(string.Format(CultureInfo.CurrentCulture, "Target should not change for non name based requests. Previous target {0}, Current {1}", request.RequestContext.ResolvedPartitionKeyRange, targetRange));
				}
				request.RequestContext.TargetIdentity = null;
				request.RequestContext.ResolvedPartitionKeyRange = null;
				throw new InvalidPartitionException(RMResources.InvalidTarget)
				{
					ResourceAddress = request.ResourceAddress
				};
			}
		}

		/// <summary>
		/// Resolves the endpoint of the partition for the given request
		/// </summary>
		/// <param name="request">Request for which the partition endpoint resolution is to be performed</param>
		/// <param name="forceRefreshPartitionAddresses">Force refresh the partition's endpoint</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		private async Task<ResolutionResult> ResolveAddressesAndIdentityAsync(DocumentServiceRequest request, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (request.ServiceIdentity != null)
			{
				ServiceIdentity identity = request.ServiceIdentity;
				PartitionAddressInformation partitionAddressInformation = await addressCache.TryGetAddresses(request, null, identity, forceRefreshPartitionAddresses, cancellationToken);
				if (partitionAddressInformation == null)
				{
					DefaultTrace.TraceInformation("Could not get addresses for explicitly specified ServiceIdentity {0}", identity);
					throw new NotFoundException
					{
						ResourceAddress = request.ResourceAddress
					};
				}
				return new ResolutionResult(partitionAddressInformation, identity);
			}
			if (ReplicatedResourceClient.IsReadingFromMaster(request.ResourceType, request.OperationType) && request.PartitionKeyRangeIdentity == null)
			{
				DefaultTrace.TraceInformation("Resolving Master service address, forceMasterRefresh: {0}, currentMaster: {1}", request.ForceMasterRefresh, masterServiceIdentityProvider?.MasterServiceIdentity);
				if (request.ForceMasterRefresh && masterServiceIdentityProvider != null)
				{
					ServiceIdentity masterServiceIdentity = masterServiceIdentityProvider.MasterServiceIdentity;
					await masterServiceIdentityProvider.RefreshAsync(masterServiceIdentity, cancellationToken);
				}
				ServiceIdentity identity = masterServiceIdentityProvider?.MasterServiceIdentity;
				PartitionKeyRangeIdentity partitionKeyRangeIdentity = masterPartitionKeyRangeIdentity;
				PartitionAddressInformation partitionAddressInformation2 = await addressCache.TryGetAddresses(request, partitionKeyRangeIdentity, identity, forceRefreshPartitionAddresses, cancellationToken);
				if (partitionAddressInformation2 == null && masterServiceIdentityProvider != null)
				{
					DefaultTrace.TraceWarning("Could not get addresses for master partition {0} on first attempt, will refresh masterServiceIdentity and retry", identity);
					await masterServiceIdentityProvider.RefreshAsync(masterServiceIdentityProvider.MasterServiceIdentity, cancellationToken);
					identity = masterServiceIdentityProvider.MasterServiceIdentity;
					partitionAddressInformation2 = await addressCache.TryGetAddresses(request, partitionKeyRangeIdentity, identity, forceRefreshPartitionAddresses, cancellationToken);
				}
				if (partitionAddressInformation2 == null)
				{
					DefaultTrace.TraceCritical("Could not get addresses for master partition {0}", identity);
					throw new NotFoundException
					{
						ResourceAddress = request.ResourceAddress
					};
				}
				return new ResolutionResult(new PartitionKeyRange
				{
					Id = "M"
				}, partitionAddressInformation2, identity);
			}
			bool collectionCacheIsUptoDate = !request.IsNameBased || (request.PartitionKeyRangeIdentity != null && request.PartitionKeyRangeIdentity.CollectionRid != null);
			bool collectionRoutingMapCacheIsUptoDate = false;
			DocumentCollection collection = await collectionCache.ResolveCollectionAsync(request, cancellationToken);
			CollectionRoutingMap routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, null, request, cancellationToken);
			if (routingMap != null && request.ForceCollectionRoutingMapRefresh)
			{
				DefaultTrace.TraceInformation("AddressResolver.ResolveAddressesAndIdentityAsync ForceCollectionRoutingMapRefresh collection.ResourceId = {0}", collection.ResourceId);
				routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, routingMap, request, cancellationToken);
			}
			if (request.ForcePartitionKeyRangeRefresh)
			{
				collectionRoutingMapCacheIsUptoDate = true;
				request.ForcePartitionKeyRangeRefresh = false;
				if (routingMap != null)
				{
					routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, routingMap, request, cancellationToken);
				}
			}
			if (routingMap == null && !collectionCacheIsUptoDate)
			{
				request.ForceNameCacheRefresh = true;
				collectionCacheIsUptoDate = true;
				collectionRoutingMapCacheIsUptoDate = false;
				collection = await collectionCache.ResolveCollectionAsync(request, cancellationToken);
				routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, null, request, cancellationToken);
			}
			EnsureRoutingMapPresent(request, routingMap, collection);
			ResolutionResult resolutionResult = await TryResolveServerPartitionAsync(request, collection, routingMap, collectionCacheIsUptoDate, collectionRoutingMapCacheIsUptoDate, forceRefreshPartitionAddresses, cancellationToken);
			if (resolutionResult == null)
			{
				if (!collectionCacheIsUptoDate)
				{
					request.ForceNameCacheRefresh = true;
					collection = await collectionCache.ResolveCollectionAsync(request, cancellationToken);
					if (collection.ResourceId != routingMap.CollectionUniqueId)
					{
						collectionRoutingMapCacheIsUptoDate = false;
						routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, null, request, cancellationToken);
					}
				}
				if (!collectionRoutingMapCacheIsUptoDate)
				{
					routingMap = await collectionRoutingMapCache.TryLookupAsync(collection.ResourceId, routingMap, request, cancellationToken);
				}
				EnsureRoutingMapPresent(request, routingMap, collection);
				resolutionResult = await TryResolveServerPartitionAsync(request, collection, routingMap, collectionCacheIsUptodate: true, collectionRoutingMapCacheIsUptodate: true, forceRefreshPartitionAddresses, cancellationToken);
			}
			if (resolutionResult == null)
			{
				DefaultTrace.TraceInformation("Couldn't route partitionkeyrange-oblivious request after retry/cache refresh. Collection doesn't exist.");
				throw new NotFoundException
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			if (request.IsNameBased)
			{
				request.Headers["x-ms-documentdb-collection-rid"] = collection.ResourceId;
			}
			return resolutionResult;
		}

		private static void EnsureRoutingMapPresent(DocumentServiceRequest request, CollectionRoutingMap routingMap, DocumentCollection collection)
		{
			if (routingMap == null && request.IsNameBased && request.PartitionKeyRangeIdentity != null && request.PartitionKeyRangeIdentity.CollectionRid != null)
			{
				DefaultTrace.TraceInformation("Routing map for request with partitionkeyrageid {0} was not found", request.PartitionKeyRangeIdentity.ToHeader());
				throw new InvalidPartitionException
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			if (routingMap == null)
			{
				DefaultTrace.TraceInformation("Routing map was not found although collection cache is upto date for collection {0}", collection.ResourceId);
				throw new NotFoundException
				{
					ResourceAddress = request.ResourceAddress
				};
			}
		}

		private async Task<ResolutionResult> TryResolveServerPartitionAsync(DocumentServiceRequest request, DocumentCollection collection, CollectionRoutingMap routingMap, bool collectionCacheIsUptodate, bool collectionRoutingMapCacheIsUptodate, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken)
		{
			if (request.PartitionKeyRangeIdentity != null)
			{
				return await TryResolveServerPartitionByPartitionKeyRangeIdAsync(request, collection, routingMap, collectionCacheIsUptodate, collectionRoutingMapCacheIsUptodate, forceRefreshPartitionAddresses, cancellationToken);
			}
			if (!request.ResourceType.IsPartitioned() && (request.ResourceType != ResourceType.StoredProcedure || request.OperationType != OperationType.ExecuteJavaScript) && (request.ResourceType != ResourceType.Collection || request.OperationType != OperationType.Head))
			{
				DefaultTrace.TraceCritical("Shouldn't come here for non partitioned resources. resourceType : {0}, operationtype:{1}, resourceaddress:{2}", request.ResourceType, request.OperationType, request.ResourceAddress);
				throw new InternalServerErrorException(RMResources.InternalServerError)
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			string text = request.Headers["x-ms-documentdb-partitionkey"];
			object value = null;
			PartitionKeyRange range;
			if (text != null)
			{
				range = TryResolveServerPartitionByPartitionKey(request, text, collectionCacheIsUptodate, collection, routingMap);
			}
			else if (request.Properties != null && request.Properties.TryGetValue("x-ms-effective-partition-key-string", out value))
			{
				if (!collection.HasPartitionKey || collection.PartitionKey.IsSystemKey.GetValueOrDefault(false))
				{
					throw new ArgumentOutOfRangeException("collection");
				}
				string text2 = value as string;
				if (string.IsNullOrEmpty(text2))
				{
					throw new ArgumentOutOfRangeException("effectivePartitionKeyString");
				}
				range = routingMap.GetRangeByEffectivePartitionKey(text2);
			}
			else
			{
				range = TryResolveSinglePartitionCollection(request, routingMap, collectionCacheIsUptodate);
			}
			if (range == null)
			{
				return null;
			}
			ServiceIdentity serviceIdentity = routingMap.TryGetInfoByPartitionKeyRangeId(range.Id);
			PartitionAddressInformation partitionAddressInformation = await addressCache.TryGetAddresses(request, new PartitionKeyRangeIdentity(collection.ResourceId, range.Id), serviceIdentity, forceRefreshPartitionAddresses, cancellationToken);
			if (partitionAddressInformation == null)
			{
				DefaultTrace.TraceVerbose("Could not resolve addresses for identity {0}/{1}. Potentially collection cache or routing map cache is outdated. Return null - upper logic will refresh and retry. ", new PartitionKeyRangeIdentity(collection.ResourceId, range.Id), serviceIdentity);
				return null;
			}
			return new ResolutionResult(range, partitionAddressInformation, serviceIdentity);
		}

		private PartitionKeyRange TryResolveSinglePartitionCollection(DocumentServiceRequest request, CollectionRoutingMap routingMap, bool collectionCacheIsUptoDate)
		{
			if (routingMap.OrderedPartitionKeyRanges.Count == 1)
			{
				return routingMap.OrderedPartitionKeyRanges.Single();
			}
			if (collectionCacheIsUptoDate)
			{
				throw new BadRequestException(RMResources.MissingPartitionKeyValue)
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			return null;
		}

		private ResolutionResult HandleRangeAddressResolutionFailure(DocumentServiceRequest request, bool collectionCacheIsUpToDate, bool routingMapCacheIsUpToDate, CollectionRoutingMap routingMap)
		{
			if ((collectionCacheIsUpToDate && routingMapCacheIsUpToDate) || (collectionCacheIsUpToDate && routingMap.IsGone(request.PartitionKeyRangeIdentity.PartitionKeyRangeId)))
			{
				throw new PartitionKeyRangeGoneException(string.Format(CultureInfo.InvariantCulture, RMResources.PartitionKeyRangeNotFound, request.PartitionKeyRangeIdentity.PartitionKeyRangeId, request.PartitionKeyRangeIdentity.CollectionRid))
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			return null;
		}

		private async Task<ResolutionResult> TryResolveServerPartitionByPartitionKeyRangeIdAsync(DocumentServiceRequest request, DocumentCollection collection, CollectionRoutingMap routingMap, bool collectionCacheIsUpToDate, bool routingMapCacheIsUpToDate, bool forceRefreshPartitionAddresses, CancellationToken cancellationToken)
		{
			PartitionKeyRange partitionKeyRange = routingMap.TryGetRangeByPartitionKeyRangeId(request.PartitionKeyRangeIdentity.PartitionKeyRangeId);
			if (partitionKeyRange == null)
			{
				DefaultTrace.TraceInformation("Cannot resolve range '{0}'", request.PartitionKeyRangeIdentity.ToHeader());
				return HandleRangeAddressResolutionFailure(request, collectionCacheIsUpToDate, routingMapCacheIsUpToDate, routingMap);
			}
			ServiceIdentity identity = routingMap.TryGetInfoByPartitionKeyRangeId(request.PartitionKeyRangeIdentity.PartitionKeyRangeId);
			PartitionAddressInformation partitionAddressInformation = await addressCache.TryGetAddresses(request, new PartitionKeyRangeIdentity(collection.ResourceId, request.PartitionKeyRangeIdentity.PartitionKeyRangeId), identity, forceRefreshPartitionAddresses, cancellationToken);
			if (partitionAddressInformation == null)
			{
				DefaultTrace.TraceInformation("Cannot resolve addresses for range '{0}'", request.PartitionKeyRangeIdentity.ToHeader());
				return HandleRangeAddressResolutionFailure(request, collectionCacheIsUpToDate, routingMapCacheIsUpToDate, routingMap);
			}
			return new ResolutionResult(partitionKeyRange, partitionAddressInformation, identity);
		}

		private PartitionKeyRange TryResolveServerPartitionByPartitionKey(DocumentServiceRequest request, string partitionKeyString, bool collectionCacheUptoDate, DocumentCollection collection, CollectionRoutingMap routingMap)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			if (partitionKeyString == null)
			{
				throw new ArgumentNullException("partitionKeyString");
			}
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (routingMap == null)
			{
				throw new ArgumentNullException("routingMap");
			}
			PartitionKeyInternal partitionKeyInternal;
			try
			{
				partitionKeyInternal = PartitionKeyInternal.FromJsonString(partitionKeyString);
			}
			catch (JsonException innerException)
			{
				throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.InvalidPartitionKey, partitionKeyString), innerException)
				{
					ResourceAddress = request.ResourceAddress
				};
			}
			if (partitionKeyInternal == null)
			{
				throw new InternalServerErrorException(string.Format(CultureInfo.InvariantCulture, "partition key is null '{0}'", partitionKeyString));
			}
			if (partitionKeyInternal.Equals(PartitionKeyInternal.Empty) || partitionKeyInternal.Components.Count == collection.PartitionKey.Paths.Count)
			{
				string effectivePartitionKeyString = partitionKeyInternal.GetEffectivePartitionKeyString(collection.PartitionKey);
				return routingMap.GetRangeByEffectivePartitionKey(effectivePartitionKeyString);
			}
			if (collectionCacheUptoDate)
			{
				BadRequestException ex = new BadRequestException(RMResources.PartitionKeyMismatch);
				ex.ResourceAddress = request.ResourceAddress;
				ex.Headers["x-ms-substatus"] = 1001u.ToString(CultureInfo.InvariantCulture);
				throw ex;
			}
			DefaultTrace.TraceInformation("Cannot compute effective partition key. Definition has '{0}' paths, values supplied has '{1}' paths. Will refresh cache and retry.", collection.PartitionKey.Paths.Count, partitionKeyInternal.Components.Count);
			return null;
		}
	}
}
