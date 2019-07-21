using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	internal sealed class DocumentServiceRequestContext
	{
		public TimeoutHelper TimeoutHelper
		{
			get;
			set;
		}

		public RequestChargeTracker RequestChargeTracker
		{
			get;
			set;
		}

		public bool ForceRefreshAddressCache
		{
			get;
			set;
		}

		public StoreResult QuorumSelectedStoreResponse
		{
			get;
			set;
		}

		/// <summary>
		/// Cache the string representation of last returned store responses when exercising QuorumReader logic
		/// At the time of introducing this, this is purely for logging purposes and
		/// has not effect on correctness.
		/// </summary>
		public List<string> StoreResponses
		{
			get;
			set;
		}

		public ConsistencyLevel? OriginalRequestConsistencyLevel
		{
			get;
			set;
		}

		public long QuorumSelectedLSN
		{
			get;
			set;
		}

		public long GlobalCommittedSelectedLSN
		{
			get;
			set;
		}

		/// <summary>
		/// Cache the write response in context during global strong
		/// where we want to lock on a single initial write response and perform barrier calls until globalCommittedLsn is caught up
		/// </summary>
		public StoreResponse GlobalStrongWriteResponse
		{
			get;
			set;
		}

		/// <summary>
		/// Unique Identity that represents the target partition where the request should reach.
		/// In gateway it is same as ServiceIdentity. 
		/// In client it is a string that represents the partition and service index
		/// </summary>
		public ServiceIdentity TargetIdentity
		{
			get;
			set;
		}

		/// <summary>
		/// If the StoreReader should perform the local refresh for GoneException instead of 
		/// throwing is back to retry policy. This is done to avoid losing the state (response + LSN)
		/// while executing quorum read logic
		/// </summary>
		public bool PerformLocalRefreshOnGoneException
		{
			get;
			set;
		}

		/// <summary>
		/// Effective partition key value to be used for routing.
		/// For server resources either this, or PartitionKeyRangeId header must be specified.
		/// </summary>
		public PartitionKeyInternal EffectivePartitionKey
		{
			get;
			set;
		}

		/// <summary>
		/// Is used to figure out which part of global session token is relevant
		/// for the partition to which request is sent.
		/// It is set automatically by address cache.
		/// Is set as part of address resolution.
		/// </summary>
		public PartitionKeyRange ResolvedPartitionKeyRange
		{
			get;
			set;
		}

		/// <summary>
		/// Session token used for this request.
		/// </summary>
		public ISessionToken SessionToken
		{
			get;
			set;
		}

		/// <summary>
		/// If the background refresh has been performed for this request to eliminate the 
		/// extra replica that is not participating in quorum but causes Gone
		/// </summary>
		public bool PerformedBackgroundAddressRefresh
		{
			get;
			set;
		}

		public ClientSideRequestStatistics ClientRequestStatistics
		{
			get;
			set;
		}

		public string ResolvedCollectionRid
		{
			get;
			set;
		}

		/// <summary>
		/// Region which is going to serve the DocumentServiceRequest.
		/// Populated during address resolution for the request.
		/// </summary>
		public string RegionName
		{
			get;
			set;
		}

		public bool? UsePreferredLocations
		{
			get;
			private set;
		}

		public int? LocationIndexToRoute
		{
			get;
			private set;
		}

		public Uri LocationEndpointToRoute
		{
			get;
			private set;
		}

		public bool EnsureCollectionExistsCheck
		{
			get;
			set;
		}

		/// <summary>
		/// Sets routing directive for <see cref="T:Microsoft.Azure.Documents.Routing.GlobalEndpointManager" /> to resolve
		/// the request to endpoint based on location index
		/// </summary>
		/// <param name="locationIndex">Index of the location to which the request should be routed</param>
		/// <param name="usePreferredLocations">Use preferred locations to route request</param>
		public void RouteToLocation(int locationIndex, bool usePreferredLocations)
		{
			LocationIndexToRoute = locationIndex;
			UsePreferredLocations = usePreferredLocations;
			LocationEndpointToRoute = null;
		}

		/// <summary>
		/// Sets location-based routing directive for <see cref="T:Microsoft.Azure.Documents.Routing.GlobalEndpointManager" /> to resolve
		/// the request to given <paramref name="locationEndpoint" />
		/// </summary>
		/// <param name="locationEndpoint">Location endpoint to which the request should be routed</param>
		public void RouteToLocation(Uri locationEndpoint)
		{
			LocationEndpointToRoute = locationEndpoint;
			LocationIndexToRoute = null;
			UsePreferredLocations = null;
		}

		/// <summary>
		/// Clears location-based routing directive
		/// </summary>
		public void ClearRouteToLocation()
		{
			LocationIndexToRoute = null;
			LocationEndpointToRoute = null;
			UsePreferredLocations = null;
		}

		public DocumentServiceRequestContext Clone()
		{
			return new DocumentServiceRequestContext
			{
				TimeoutHelper = TimeoutHelper,
				RequestChargeTracker = RequestChargeTracker,
				ForceRefreshAddressCache = ForceRefreshAddressCache,
				TargetIdentity = TargetIdentity,
				PerformLocalRefreshOnGoneException = PerformLocalRefreshOnGoneException,
				SessionToken = SessionToken,
				ResolvedPartitionKeyRange = ResolvedPartitionKeyRange,
				PerformedBackgroundAddressRefresh = PerformedBackgroundAddressRefresh,
				ResolvedCollectionRid = ResolvedCollectionRid,
				EffectivePartitionKey = EffectivePartitionKey,
				ClientRequestStatistics = ClientRequestStatistics,
				OriginalRequestConsistencyLevel = OriginalRequestConsistencyLevel,
				UsePreferredLocations = UsePreferredLocations,
				LocationIndexToRoute = LocationIndexToRoute,
				LocationEndpointToRoute = LocationEndpointToRoute,
				EnsureCollectionExistsCheck = EnsureCollectionExistsCheck
			};
		}
	}
}
