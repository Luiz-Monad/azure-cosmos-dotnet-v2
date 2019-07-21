using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// ConsistencyReader has a dependency on both StoreReader and QuorumReader. For Bounded Staleness and Strong Consistency, it uses the Quorum Reader
	/// to converge on a read from read quorum number of replicas. 
	/// For Session and Eventual Consistency, it directly uses the store reader.
	/// </summary>
	[SuppressMessage("", "AvoidMultiLineComments", Justification = "Multi line business logic")]
	internal sealed class ConsistencyReader
	{
		private const int maxNumberOfSecondaryReadRetries = 3;

		private readonly AddressSelector addressSelector;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly IAuthorizationTokenProvider authorizationTokenProvider;

		private readonly StoreReader storeReader;

		private readonly QuorumReader quorumReader;

		public string LastReadAddress
		{
			get
			{
				return storeReader.LastReadAddress;
			}
			set
			{
				storeReader.LastReadAddress = value;
			}
		}

		public ConsistencyReader(AddressSelector addressSelector, ISessionContainer sessionContainer, TransportClient transportClient, IServiceConfigurationReader serviceConfigReader, IAuthorizationTokenProvider authorizationTokenProvider)
		{
			this.addressSelector = addressSelector;
			this.serviceConfigReader = serviceConfigReader;
			this.authorizationTokenProvider = authorizationTokenProvider;
			storeReader = new StoreReader(transportClient, addressSelector, sessionContainer);
			quorumReader = new QuorumReader(transportClient, addressSelector, storeReader, serviceConfigReader, authorizationTokenProvider);
		}

		public Task<StoreResponse> ReadAsync(DocumentServiceRequest entity, TimeoutHelper timeout, bool isInRetry, bool forceRefresh)
		{
			if (!isInRetry)
			{
				timeout.ThrowTimeoutIfElapsed();
			}
			else
			{
				timeout.ThrowGoneIfElapsed();
			}
			entity.RequestContext.TimeoutHelper = timeout;
			if (entity.RequestContext.RequestChargeTracker == null)
			{
				entity.RequestContext.RequestChargeTracker = new RequestChargeTracker();
			}
			if (entity.RequestContext.ClientRequestStatistics == null)
			{
				entity.RequestContext.ClientRequestStatistics = new ClientSideRequestStatistics();
			}
			entity.RequestContext.ForceRefreshAddressCache = forceRefresh;
			ConsistencyLevel targetConsistencyLevel;
			bool useSessionToken;
			ReadMode readMode = DeduceReadMode(entity, out targetConsistencyLevel, out useSessionToken);
			int maxReplicaSetSize = GetMaxReplicaSetSize(entity);
			int readQuorumValue = maxReplicaSetSize - maxReplicaSetSize / 2;
			switch (readMode)
			{
			case ReadMode.Primary:
				return ReadPrimaryAsync(entity, useSessionToken);
			case ReadMode.Strong:
				entity.RequestContext.PerformLocalRefreshOnGoneException = true;
				return quorumReader.ReadStrongAsync(entity, readQuorumValue, readMode);
			case ReadMode.BoundedStaleness:
				entity.RequestContext.PerformLocalRefreshOnGoneException = true;
				return quorumReader.ReadStrongAsync(entity, readQuorumValue, readMode);
			case ReadMode.Any:
				if (targetConsistencyLevel == ConsistencyLevel.Session)
				{
					return ReadSessionAsync(entity, readMode);
				}
				return ReadAnyAsync(entity, readMode);
			default:
				throw new InvalidOperationException();
			}
		}

		private async Task<StoreResponse> ReadPrimaryAsync(DocumentServiceRequest entity, bool useSessionToken)
		{
			return (await storeReader.ReadPrimaryAsync(entity, requiresValidLsn: false, useSessionToken)).ToResponse();
		}

		private async Task<StoreResponse> ReadAnyAsync(DocumentServiceRequest entity, ReadMode readMode)
		{
			IList<StoreResult> obj = await storeReader.ReadMultipleReplicaAsync(entity, includePrimary: true, 1, requiresValidLsn: false, useSessionToken: false, readMode);
			if (obj.Count == 0)
			{
				throw new GoneException(RMResources.Gone);
			}
			return obj[0].ToResponse();
		}

		private async Task<StoreResponse> ReadSessionAsync(DocumentServiceRequest entity, ReadMode readMode)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			IList<StoreResult> list = await storeReader.ReadMultipleReplicaAsync(entity, includePrimary: true, 1, requiresValidLsn: true, useSessionToken: true, readMode, checkMinLSN: true);
			if (list.Count > 0)
			{
				try
				{
					StoreResponse storeResponse = list[0].ToResponse(entity.RequestContext.RequestChargeTracker);
					if (storeResponse.Status == 404 && entity.IsValidStatusCodeForExceptionlessRetry(storeResponse.Status) && entity.RequestContext.SessionToken != null && list[0].SessionToken != null && !entity.RequestContext.SessionToken.IsValid(list[0].SessionToken))
					{
						DefaultTrace.TraceInformation("Convert to session read exception, request {0} Session Lsn {1}, responseLSN {2}", entity.ResourceAddress, entity.RequestContext.SessionToken.ConvertToString(), list[0].LSN);
						StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
						((INameValueCollection)nameValueCollection).Set("x-ms-substatus", 1002.ToString());
						throw new NotFoundException(RMResources.ReadSessionNotAvailable, nameValueCollection);
					}
					return storeResponse;
				}
				catch (NotFoundException ex)
				{
					if (entity.RequestContext.SessionToken != null && list[0].SessionToken != null && !entity.RequestContext.SessionToken.IsValid(list[0].SessionToken))
					{
						DefaultTrace.TraceInformation("Convert to session read exception, request {0} Session Lsn {1}, responseLSN {2}", entity.ResourceAddress, entity.RequestContext.SessionToken.ConvertToString(), list[0].LSN);
						ex.Headers.Set("x-ms-substatus", 1002.ToString());
					}
					throw ex;
				}
			}
			INameValueCollection nameValueCollection2 = new StringKeyValueCollection();
			nameValueCollection2.Set("x-ms-substatus", 1002.ToString());
			ISessionToken sessionToken = entity.RequestContext.SessionToken;
			DefaultTrace.TraceInformation("Fail the session read {0}, request session token {1}", entity.ResourceAddress, (sessionToken == null) ? "<empty>" : sessionToken.ConvertToString());
			throw new NotFoundException(RMResources.ReadSessionNotAvailable, nameValueCollection2);
		}

		private ReadMode DeduceReadMode(DocumentServiceRequest request, out ConsistencyLevel targetConsistencyLevel, out bool useSessionToken)
		{
			targetConsistencyLevel = RequestHelper.GetConsistencyLevelToUse(serviceConfigReader, request);
			useSessionToken = (targetConsistencyLevel == ConsistencyLevel.Session);
			if (request.DefaultReplicaIndex.HasValue)
			{
				useSessionToken = false;
				return ReadMode.Primary;
			}
			switch (targetConsistencyLevel)
			{
			case ConsistencyLevel.Eventual:
				return ReadMode.Any;
			case ConsistencyLevel.ConsistentPrefix:
				return ReadMode.Any;
			case ConsistencyLevel.Session:
				return ReadMode.Any;
			case ConsistencyLevel.BoundedStaleness:
				return ReadMode.BoundedStaleness;
			case ConsistencyLevel.Strong:
				return ReadMode.Strong;
			default:
				throw new InvalidOperationException();
			}
		}

		public int GetMaxReplicaSetSize(DocumentServiceRequest entity)
		{
			if (ReplicatedResourceClient.IsReadingFromMaster(entity.ResourceType, entity.OperationType))
			{
				return serviceConfigReader.SystemReplicationPolicy.MaxReplicaSetSize;
			}
			return serviceConfigReader.UserReplicationPolicy.MaxReplicaSetSize;
		}

		public int GetMinReplicaSetSize(DocumentServiceRequest entity)
		{
			if (ReplicatedResourceClient.IsReadingFromMaster(entity.ResourceType, entity.OperationType))
			{
				return serviceConfigReader.SystemReplicationPolicy.MinReplicaSetSize;
			}
			return serviceConfigReader.UserReplicationPolicy.MinReplicaSetSize;
		}
	}
}
