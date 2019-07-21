using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	[SuppressMessage("", "AvoidMultiLineComments", Justification = "Multi line business logic")]
	internal sealed class ConsistencyWriter
	{
		private const int maxNumberOfWriteBarrierReadRetries = 30;

		private const int delayBetweenWriteBarrierCallsInMs = 30;

		private const int maxShortBarrierRetriesForMultiRegion = 4;

		private const int shortbarrierRetryIntervalInMsForMultiRegion = 10;

		private readonly StoreReader storeReader;

		private readonly TransportClient transportClient;

		private readonly AddressSelector addressSelector;

		private readonly ISessionContainer sessionContainer;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly IAuthorizationTokenProvider authorizationTokenProvider;

		private readonly bool useMultipleWriteLocations;

		internal string LastWriteAddress
		{
			get;
			private set;
		}

		public ConsistencyWriter(AddressSelector addressSelector, ISessionContainer sessionContainer, TransportClient transportClient, IServiceConfigurationReader serviceConfigReader, IAuthorizationTokenProvider authorizationTokenProvider, bool useMultipleWriteLocations)
		{
			this.transportClient = transportClient;
			this.addressSelector = addressSelector;
			this.sessionContainer = sessionContainer;
			this.serviceConfigReader = serviceConfigReader;
			this.authorizationTokenProvider = authorizationTokenProvider;
			this.useMultipleWriteLocations = useMultipleWriteLocations;
			storeReader = new StoreReader(transportClient, addressSelector, null);
		}

		public async Task<StoreResponse> WriteAsync(DocumentServiceRequest entity, TimeoutHelper timeout, bool forceRefresh, CancellationToken cancellationToken = default(CancellationToken))
		{
			timeout.ThrowTimeoutIfElapsed();
			string sessionToken = entity.Headers["x-ms-session-token"];
			try
			{
				return await BackoffRetryUtility<StoreResponse>.ExecuteAsync(() => WritePrivateAsync(entity, timeout, forceRefresh), new SessionTokenMismatchRetryPolicy(), cancellationToken);
			}
			finally
			{
				SessionTokenHelper.SetOriginalSessionToken(entity, sessionToken);
			}
		}

		private async Task<StoreResponse> WritePrivateAsync(DocumentServiceRequest request, TimeoutHelper timeout, bool forceRefresh)
		{
			timeout.ThrowTimeoutIfElapsed();
			request.RequestContext.TimeoutHelper = timeout;
			if (request.RequestContext.RequestChargeTracker == null)
			{
				request.RequestContext.RequestChargeTracker = new RequestChargeTracker();
			}
			if (request.RequestContext.ClientRequestStatistics == null)
			{
				request.RequestContext.ClientRequestStatistics = new ClientSideRequestStatistics();
			}
			request.RequestContext.ForceRefreshAddressCache = forceRefresh;
			if (request.RequestContext.GlobalStrongWriteResponse == null)
			{
				PerProtocolPartitionAddressInformation perProtocolPartitionAddressInformation = await addressSelector.ResolveAddressesAsync(request, forceRefresh);
				request.RequestContext.ClientRequestStatistics.ContactedReplicas = perProtocolPartitionAddressInformation.ReplicaUris.ToList();
				Uri primaryUri = perProtocolPartitionAddressInformation.GetPrimaryUri(request);
				LastWriteAddress = primaryUri.ToString();
				if (useMultipleWriteLocations && RequestHelper.GetConsistencyLevelToUse(serviceConfigReader, request) == ConsistencyLevel.Session)
				{
					SessionTokenHelper.SetPartitionLocalSessionToken(request, sessionContainer);
				}
				else
				{
					SessionTokenHelper.ValidateAndRemoveSessionToken(request);
				}
				StoreResponse storeResponse;
				try
				{
					storeResponse = await transportClient.InvokeResourceOperationAsync(primaryUri, request);
					request.RequestContext.ClientRequestStatistics.RecordResponse(request, StoreResult.CreateStoreResult(storeResponse, null, requiresValidLsn: false, useLocalLSNBasedHeaders: false, primaryUri));
				}
				catch (DocumentClientException ex)
				{
					request.RequestContext.ClientRequestStatistics.RecordResponse(request, StoreResult.CreateStoreResult(null, ex, requiresValidLsn: false, useLocalLSNBasedHeaders: false, primaryUri));
					int result;
					if (!string.IsNullOrWhiteSpace(ex.Headers["x-ms-write-request-trigger-refresh"]) && int.TryParse(ex.Headers.GetValues("x-ms-write-request-trigger-refresh")[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result == 1)
					{
						StartBackgroundAddressRefresh(request);
					}
					throw;
				}
				if (!ReplicatedResourceClient.IsGlobalStrongEnabled() || !IsGlobalStrongRequest(request, storeResponse))
				{
					return storeResponse;
				}
				long lsn = -1L;
				long globalCommittedLsn = -1L;
				GetLsnAndGlobalCommittedLsn(storeResponse, out lsn, out globalCommittedLsn);
				if (lsn == -1 || globalCommittedLsn == -1)
				{
					DefaultTrace.TraceWarning("ConsistencyWriter: LSN {0} or GlobalCommittedLsn {1} is not set for global strong request", lsn, globalCommittedLsn);
					throw new GoneException(RMResources.Gone);
				}
				request.RequestContext.GlobalStrongWriteResponse = storeResponse;
				request.RequestContext.GlobalCommittedSelectedLSN = lsn;
				request.RequestContext.ForceRefreshAddressCache = false;
				DefaultTrace.TraceInformation("ConsistencyWriter: globalCommittedLsn {0}, lsn {1}", globalCommittedLsn, lsn);
				if (globalCommittedLsn < lsn)
				{
					using (DocumentServiceRequest barrierRequest2 = await BarrierRequestHelper.CreateAsync(request, authorizationTokenProvider, null, request.RequestContext.GlobalCommittedSelectedLSN))
					{
						if (!(await WaitForWriteBarrierAsync(barrierRequest2, request.RequestContext.GlobalCommittedSelectedLSN)))
						{
							DefaultTrace.TraceError("ConsistencyWriter: Write barrier has not been met for global strong request. SelectedGlobalCommittedLsn: {0}", request.RequestContext.GlobalCommittedSelectedLSN);
							throw new GoneException(RMResources.GlobalStrongWriteBarrierNotMet);
						}
					}
				}
			}
			else
			{
				using (DocumentServiceRequest barrierRequest2 = await BarrierRequestHelper.CreateAsync(request, authorizationTokenProvider, null, request.RequestContext.GlobalCommittedSelectedLSN))
				{
					if (!(await WaitForWriteBarrierAsync(barrierRequest2, request.RequestContext.GlobalCommittedSelectedLSN)))
					{
						DefaultTrace.TraceWarning("ConsistencyWriter: Write barrier has not been met for global strong request. SelectedGlobalCommittedLsn: {0}", request.RequestContext.GlobalCommittedSelectedLSN);
						throw new GoneException(RMResources.GlobalStrongWriteBarrierNotMet);
					}
				}
			}
			return request.RequestContext.GlobalStrongWriteResponse;
		}

		private bool IsGlobalStrongRequest(DocumentServiceRequest request, StoreResponse response)
		{
			if (serviceConfigReader.DefaultConsistencyLevel == ConsistencyLevel.Strong)
			{
				int num = -1;
				string value = null;
				if (response.TryGetHeaderValue("x-ms-number-of-read-regions", out value))
				{
					num = int.Parse(value, CultureInfo.InvariantCulture);
				}
				if (num > 0 && serviceConfigReader.DefaultConsistencyLevel == ConsistencyLevel.Strong)
				{
					return true;
				}
			}
			return false;
		}

		private void GetLsnAndGlobalCommittedLsn(StoreResponse response, out long lsn, out long globalCommittedLsn)
		{
			lsn = -1L;
			globalCommittedLsn = -1L;
			string value = null;
			if (response.TryGetHeaderValue("lsn", out value))
			{
				lsn = long.Parse(value, CultureInfo.InvariantCulture);
			}
			if (response.TryGetHeaderValue("x-ms-global-Committed-lsn", out value))
			{
				globalCommittedLsn = long.Parse(value, CultureInfo.InvariantCulture);
			}
		}

		private async Task<bool> WaitForWriteBarrierAsync(DocumentServiceRequest barrierRequest, long selectedGlobalCommittedLsn)
		{
			int writeBarrierRetryCount = 30;
			long maxGlobalCommittedLsnReceived = 0L;
			while (writeBarrierRetryCount-- > 0)
			{
				barrierRequest.RequestContext.TimeoutHelper.ThrowTimeoutIfElapsed();
				IList<StoreResult> list = await storeReader.ReadMultipleReplicaAsync(barrierRequest, includePrimary: true, 1, requiresValidLsn: false, useSessionToken: false, ReadMode.Strong);
				if (list != null && list.Any((StoreResult response) => response.GlobalCommittedLSN >= selectedGlobalCommittedLsn))
				{
					return true;
				}
				long num2 = list?.Select((StoreResult s) => s.GlobalCommittedLSN).DefaultIfEmpty(0L).Max() ?? 0;
				maxGlobalCommittedLsnReceived = ((maxGlobalCommittedLsnReceived > num2) ? maxGlobalCommittedLsnReceived : num2);
				barrierRequest.RequestContext.ForceRefreshAddressCache = false;
				if (writeBarrierRetryCount == 0)
				{
					DefaultTrace.TraceInformation("ConsistencyWriter: WaitForWriteBarrierAsync - Last barrier multi-region strong. Responses: {0}", string.Join("; ", list));
				}
				else if (30 - writeBarrierRetryCount > 4)
				{
					await Task.Delay(30);
				}
				else
				{
					await Task.Delay(10);
				}
			}
			DefaultTrace.TraceInformation("ConsistencyWriter: Highest global committed lsn received for write barrier call is {0}", maxGlobalCommittedLsnReceived);
			return false;
		}

		private void StartBackgroundAddressRefresh(DocumentServiceRequest request)
		{
			try
			{
				addressSelector.ResolvePrimaryUriAsync(request, forceAddressRefresh: true).ContinueWith(delegate(Task<Uri> task)
				{
					if (task.IsFaulted)
					{
						DefaultTrace.TraceWarning("Background refresh of the primary address failed with {0}", task.Exception.ToString());
					}
				});
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceWarning("Background refresh of the primary address failed with {0}", ex.ToString());
			}
		}
	}
}
