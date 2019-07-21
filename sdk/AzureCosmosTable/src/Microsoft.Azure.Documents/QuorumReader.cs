using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// QuorumReader wraps the client side quorum logic on top of the StoreReader
	/// </summary>
	internal sealed class QuorumReader
	{
		private enum ReadQuorumResultKind
		{
			QuorumMet,
			QuorumSelected,
			QuorumNotSelected
		}

		private abstract class ReadResult
		{
			private readonly StoreResult response;

			private readonly RequestChargeTracker requestChargeTracker;

			protected ReadResult(RequestChargeTracker requestChargeTracker, StoreResult response)
			{
				this.requestChargeTracker = requestChargeTracker;
				this.response = response;
			}

			public StoreResponse GetResponse()
			{
				if (!IsValidResult())
				{
					DefaultTrace.TraceCritical("GetResponse called for invalid result");
					throw new InternalServerErrorException(RMResources.InternalServerError);
				}
				return response.ToResponse(requestChargeTracker);
			}

			protected abstract bool IsValidResult();
		}

		private sealed class ReadQuorumResult : ReadResult
		{
			public ReadQuorumResultKind QuorumResult
			{
				get;
				private set;
			}

			/// <summary>
			/// Response selected to lock on the LSN. This is the response with the highest
			/// LSN
			/// </summary>
			public StoreResult SelectedResponse
			{
				get;
				private set;
			}

			/// <summary>
			/// All store responses from Quorum Read.
			/// </summary>
			public List<string> StoreResponses
			{
				get;
				private set;
			}

			public long SelectedLsn
			{
				get;
				private set;
			}

			public long GlobalCommittedSelectedLsn
			{
				get;
				private set;
			}

			public ReadQuorumResult(RequestChargeTracker requestChargeTracker, ReadQuorumResultKind QuorumResult, long selectedLsn, long globalCommittedSelectedLsn, StoreResult selectedResponse, List<string> storeResponses)
				: base(requestChargeTracker, selectedResponse)
			{
				this.QuorumResult = QuorumResult;
				SelectedLsn = selectedLsn;
				GlobalCommittedSelectedLsn = globalCommittedSelectedLsn;
				SelectedResponse = selectedResponse;
				StoreResponses = storeResponses;
			}

			protected override bool IsValidResult()
			{
				if (QuorumResult != 0)
				{
					return QuorumResult == ReadQuorumResultKind.QuorumSelected;
				}
				return true;
			}
		}

		private sealed class ReadPrimaryResult : ReadResult
		{
			public bool ShouldRetryOnSecondary
			{
				get;
				private set;
			}

			public bool IsSuccessful
			{
				get;
				private set;
			}

			public ReadPrimaryResult(RequestChargeTracker requestChargeTracker, bool isSuccessful, bool shouldRetryOnSecondary, StoreResult response)
				: base(requestChargeTracker, response)
			{
				IsSuccessful = isSuccessful;
				ShouldRetryOnSecondary = shouldRetryOnSecondary;
			}

			protected override bool IsValidResult()
			{
				return IsSuccessful;
			}
		}

		private enum PrimaryReadOutcome
		{
			QuorumNotMet,
			QuorumInconclusive,
			QuorumMet
		}

		private const int maxNumberOfReadBarrierReadRetries = 6;

		private const int maxNumberOfPrimaryReadRetries = 6;

		private const int maxNumberOfReadQuorumRetries = 6;

		private const int delayBetweenReadBarrierCallsInMs = 5;

		private const int maxBarrierRetriesForMultiRegion = 30;

		private const int barrierRetryIntervalInMsForMultiRegion = 30;

		private const int maxShortBarrierRetriesForMultiRegion = 4;

		private const int shortbarrierRetryIntervalInMsForMultiRegion = 10;

		private readonly StoreReader storeReader;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly IAuthorizationTokenProvider authorizationTokenProvider;

		public QuorumReader(TransportClient transportClient, AddressSelector addressSelector, StoreReader storeReader, IServiceConfigurationReader serviceConfigReader, IAuthorizationTokenProvider authorizationTokenProvider)
		{
			this.storeReader = storeReader;
			this.serviceConfigReader = serviceConfigReader;
			this.authorizationTokenProvider = authorizationTokenProvider;
		}

		public async Task<StoreResponse> ReadStrongAsync(DocumentServiceRequest entity, int readQuorumValue, ReadMode readMode)
		{
			int readQuorumRetry = 6;
			bool hasPerformedReadFromPrimary = false;
			bool shouldRetryOnSecondary;
			do
			{
				entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
				shouldRetryOnSecondary = false;
				ReadQuorumResult secondaryQuorumReadResult = await ReadQuorumAsync(entity, readQuorumValue, includePrimary: false, readMode);
				switch (secondaryQuorumReadResult.QuorumResult)
				{
				case ReadQuorumResultKind.QuorumMet:
					return secondaryQuorumReadResult.GetResponse();
				case ReadQuorumResultKind.QuorumSelected:
					if (await WaitForReadBarrierAsync(await BarrierRequestHelper.CreateAsync(entity, authorizationTokenProvider, secondaryQuorumReadResult.SelectedLsn, secondaryQuorumReadResult.GlobalCommittedSelectedLsn), allowPrimary: true, readQuorumValue, secondaryQuorumReadResult.SelectedLsn, secondaryQuorumReadResult.GlobalCommittedSelectedLsn, readMode))
					{
						return secondaryQuorumReadResult.GetResponse();
					}
					DefaultTrace.TraceWarning("QuorumSelected: Could not converge on the LSN {0} GlobalCommittedLSN {3} after primary read barrier with read quorum {1} for strong read, Responses: {2}", secondaryQuorumReadResult.SelectedLsn, readQuorumValue, string.Join(";", secondaryQuorumReadResult.StoreResponses), secondaryQuorumReadResult.GlobalCommittedSelectedLsn);
					entity.RequestContext.QuorumSelectedStoreResponse = secondaryQuorumReadResult.SelectedResponse;
					entity.RequestContext.StoreResponses = secondaryQuorumReadResult.StoreResponses;
					entity.RequestContext.QuorumSelectedLSN = secondaryQuorumReadResult.SelectedLsn;
					entity.RequestContext.GlobalCommittedSelectedLSN = secondaryQuorumReadResult.GlobalCommittedSelectedLsn;
					break;
				case ReadQuorumResultKind.QuorumNotSelected:
				{
					if (hasPerformedReadFromPrimary)
					{
						DefaultTrace.TraceWarning("QuorumNotSelected: Primary read already attempted. Quorum could not be selected after retrying on secondaries.");
						throw new GoneException(RMResources.ReadQuorumNotMet);
					}
					DefaultTrace.TraceWarning("QuorumNotSelected: Quorum could not be selected with read quorum of {0}", readQuorumValue);
					ReadPrimaryResult readPrimaryResult = await ReadPrimaryAsync(entity, readQuorumValue, useSessionToken: false);
					if (readPrimaryResult.IsSuccessful && readPrimaryResult.ShouldRetryOnSecondary)
					{
						DefaultTrace.TraceCritical("PrimaryResult has both Successful and ShouldRetryOnSecondary flags set");
						break;
					}
					if (readPrimaryResult.IsSuccessful)
					{
						DefaultTrace.TraceInformation("QuorumNotSelected: ReadPrimary successful");
						return readPrimaryResult.GetResponse();
					}
					if (readPrimaryResult.ShouldRetryOnSecondary)
					{
						shouldRetryOnSecondary = true;
						DefaultTrace.TraceWarning("QuorumNotSelected: ReadPrimary did not succeed. Will retry on secondary.");
						hasPerformedReadFromPrimary = true;
						break;
					}
					DefaultTrace.TraceWarning("QuorumNotSelected: Could not get successful response from ReadPrimary");
					throw new GoneException(RMResources.ReadQuorumNotMet);
				}
				default:
					DefaultTrace.TraceCritical("Unknown ReadQuorum result {0}", secondaryQuorumReadResult.QuorumResult.ToString());
					throw new InternalServerErrorException(RMResources.InternalServerError);
				}
			}
			while (--readQuorumRetry > 0 && shouldRetryOnSecondary);
			DefaultTrace.TraceWarning("Could not complete read quorum with read quorum value of {0}", readQuorumValue);
			throw new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ReadQuorumNotMet, readQuorumValue));
		}

		public async Task<StoreResponse> ReadBoundedStalenessAsync(DocumentServiceRequest entity, int readQuorumValue)
		{
			int readQuorumRetry = 6;
			bool hasPerformedReadFromPrimary = false;
			bool shouldRetryOnSecondary;
			do
			{
				entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
				shouldRetryOnSecondary = false;
				ReadQuorumResult readQuorumResult = await ReadQuorumAsync(entity, readQuorumValue, includePrimary: false, ReadMode.BoundedStaleness);
				switch (readQuorumResult.QuorumResult)
				{
				case ReadQuorumResultKind.QuorumMet:
					return readQuorumResult.GetResponse();
				case ReadQuorumResultKind.QuorumSelected:
					DefaultTrace.TraceWarning("QuorumSelected: Could not converge on LSN {0} after barrier with QuorumValue {1} Will not perform barrier call on Primary for BoundedStaleness, Responses: {2}", readQuorumResult.SelectedLsn, readQuorumValue, string.Join(";", readQuorumResult.StoreResponses));
					entity.RequestContext.QuorumSelectedStoreResponse = readQuorumResult.SelectedResponse;
					entity.RequestContext.StoreResponses = readQuorumResult.StoreResponses;
					entity.RequestContext.QuorumSelectedLSN = readQuorumResult.SelectedLsn;
					break;
				case ReadQuorumResultKind.QuorumNotSelected:
				{
					if (hasPerformedReadFromPrimary)
					{
						DefaultTrace.TraceWarning("QuorumNotSelected: Primary read already attempted. Quorum could not be selected after retrying on secondaries.");
						throw new GoneException(RMResources.ReadQuorumNotMet);
					}
					DefaultTrace.TraceWarning("QuorumNotSelected: Quorum could not be selected with read quorum of {0}", readQuorumValue);
					ReadPrimaryResult readPrimaryResult = await ReadPrimaryAsync(entity, readQuorumValue, useSessionToken: false);
					if (readPrimaryResult.IsSuccessful && readPrimaryResult.ShouldRetryOnSecondary)
					{
						DefaultTrace.TraceCritical("QuorumNotSelected: PrimaryResult has both Successful and ShouldRetryOnSecondary flags set");
						break;
					}
					if (readPrimaryResult.IsSuccessful)
					{
						DefaultTrace.TraceInformation("QuorumNotSelected: ReadPrimary successful");
						return readPrimaryResult.GetResponse();
					}
					if (readPrimaryResult.ShouldRetryOnSecondary)
					{
						shouldRetryOnSecondary = true;
						DefaultTrace.TraceWarning("QuorumNotSelected: ReadPrimary did not succeed. Will retry on secondary.");
						hasPerformedReadFromPrimary = true;
						break;
					}
					DefaultTrace.TraceWarning("QuorumNotSelected: Could not get successful response from ReadPrimary");
					throw new GoneException(RMResources.ReadQuorumNotMet);
				}
				default:
					DefaultTrace.TraceCritical("Unknown ReadQuorum result {0}", readQuorumResult.QuorumResult.ToString());
					throw new InternalServerErrorException(RMResources.InternalServerError);
				}
			}
			while (--readQuorumRetry > 0 && shouldRetryOnSecondary);
			DefaultTrace.TraceError("Could not complete read quorum with read quorum value of {0}, RetryCount: {1}", readQuorumValue, 6);
			throw new GoneException(string.Format(CultureInfo.CurrentUICulture, RMResources.ReadQuorumNotMet, readQuorumValue));
		}

		private async Task<ReadQuorumResult> ReadQuorumAsync(DocumentServiceRequest entity, int readQuorum, bool includePrimary, ReadMode readMode)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			long readLsn = -1L;
			long globalCommittedLSN = -1L;
			StoreResult storeResult = null;
			List<string> storeResponses;
			if (entity.RequestContext.QuorumSelectedStoreResponse == null)
			{
				IList<StoreResult> list = await storeReader.ReadMultipleReplicaAsync(entity, includePrimary, readQuorum, requiresValidLsn: true, useSessionToken: false, readMode);
				storeResponses = (from response in list
				select response.ToString()).ToList();
				if (list.Count((StoreResult response) => response.IsValid) < readQuorum)
				{
					return new ReadQuorumResult(entity.RequestContext.RequestChargeTracker, ReadQuorumResultKind.QuorumNotSelected, -1L, -1L, null, storeResponses);
				}
				int num;
				if (ReplicatedResourceClient.IsGlobalStrongEnabled() && serviceConfigReader.DefaultConsistencyLevel == ConsistencyLevel.Strong)
				{
					if (entity.RequestContext.OriginalRequestConsistencyLevel.HasValue)
					{
						ConsistencyLevel? originalRequestConsistencyLevel = entity.RequestContext.OriginalRequestConsistencyLevel;
						num = ((originalRequestConsistencyLevel.GetValueOrDefault() == ConsistencyLevel.Strong && originalRequestConsistencyLevel.HasValue) ? 1 : 0);
					}
					else
					{
						num = 1;
					}
				}
				else
				{
					num = 0;
				}
				bool isGlobalStrongRead = (byte)num != 0;
				if (IsQuorumMet(list, readQuorum, isPrimaryIncluded: false, isGlobalStrongRead, out readLsn, out globalCommittedLSN, out storeResult))
				{
					return new ReadQuorumResult(entity.RequestContext.RequestChargeTracker, ReadQuorumResultKind.QuorumMet, readLsn, globalCommittedLSN, storeResult, storeResponses);
				}
				entity.RequestContext.ForceRefreshAddressCache = false;
			}
			else
			{
				readLsn = entity.RequestContext.QuorumSelectedLSN;
				globalCommittedLSN = entity.RequestContext.GlobalCommittedSelectedLSN;
				storeResult = entity.RequestContext.QuorumSelectedStoreResponse;
				storeResponses = entity.RequestContext.StoreResponses;
			}
			if (!(await WaitForReadBarrierAsync(await BarrierRequestHelper.CreateAsync(entity, authorizationTokenProvider, readLsn, globalCommittedLSN), allowPrimary: false, readQuorum, readLsn, globalCommittedLSN, readMode)))
			{
				return new ReadQuorumResult(entity.RequestContext.RequestChargeTracker, ReadQuorumResultKind.QuorumSelected, readLsn, globalCommittedLSN, storeResult, storeResponses);
			}
			return new ReadQuorumResult(entity.RequestContext.RequestChargeTracker, ReadQuorumResultKind.QuorumMet, readLsn, globalCommittedLSN, storeResult, storeResponses);
		}

		/// <summary>
		/// Read and get response from Primary
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="readQuorum"></param>
		/// <param name="useSessionToken"></param>
		/// <returns></returns>
		private async Task<ReadPrimaryResult> ReadPrimaryAsync(DocumentServiceRequest entity, int readQuorum, bool useSessionToken)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			entity.RequestContext.ForceRefreshAddressCache = false;
			StoreResult storeResult = await storeReader.ReadPrimaryAsync(entity, requiresValidLsn: true, useSessionToken);
			if (!storeResult.IsValid)
			{
				ExceptionDispatchInfo.Capture(storeResult.GetException()).Throw();
			}
			if (storeResult.CurrentReplicaSetSize <= 0 || storeResult.LSN < 0 || storeResult.QuorumAckedLSN < 0)
			{
				string message = string.Format(CultureInfo.CurrentCulture, "Invalid value received from response header. CurrentReplicaSetSize {0}, StoreLSN {1}, QuorumAckedLSN {2}", storeResult.CurrentReplicaSetSize, storeResult.LSN, storeResult.QuorumAckedLSN);
				if (storeResult.CurrentReplicaSetSize <= 0)
				{
					DefaultTrace.TraceError(message);
				}
				else
				{
					DefaultTrace.TraceCritical(message);
				}
				throw new GoneException(RMResources.ReadQuorumNotMet);
			}
			if (storeResult.CurrentReplicaSetSize > readQuorum)
			{
				DefaultTrace.TraceWarning("Unexpected response. Replica Set size is {0} which is greater than min value {1}", storeResult.CurrentReplicaSetSize, readQuorum);
				return new ReadPrimaryResult(entity.RequestContext.RequestChargeTracker, isSuccessful: false, shouldRetryOnSecondary: true, null);
			}
			if (storeResult.LSN != storeResult.QuorumAckedLSN)
			{
				DefaultTrace.TraceWarning("Store LSN {0} and quorum acked LSN {1} don't match", storeResult.LSN, storeResult.QuorumAckedLSN);
				long higherLsn = (storeResult.LSN > storeResult.QuorumAckedLSN) ? storeResult.LSN : storeResult.QuorumAckedLSN;
				switch (await WaitForPrimaryLsnAsync(await BarrierRequestHelper.CreateAsync(entity, authorizationTokenProvider, higherLsn, null), higherLsn, readQuorum))
				{
				case PrimaryReadOutcome.QuorumNotMet:
					return new ReadPrimaryResult(entity.RequestContext.RequestChargeTracker, isSuccessful: false, shouldRetryOnSecondary: false, null);
				case PrimaryReadOutcome.QuorumInconclusive:
					return new ReadPrimaryResult(entity.RequestContext.RequestChargeTracker, isSuccessful: false, shouldRetryOnSecondary: true, null);
				default:
					return new ReadPrimaryResult(entity.RequestContext.RequestChargeTracker, isSuccessful: true, shouldRetryOnSecondary: false, storeResult);
				}
			}
			return new ReadPrimaryResult(entity.RequestContext.RequestChargeTracker, isSuccessful: true, shouldRetryOnSecondary: false, storeResult);
		}

		private async Task<PrimaryReadOutcome> WaitForPrimaryLsnAsync(DocumentServiceRequest barrierRequest, long lsnToWaitFor, int readQuorum)
		{
			int primaryRetries = 6;
			do
			{
				barrierRequest.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
				barrierRequest.RequestContext.ForceRefreshAddressCache = false;
				StoreResult storeResult = await storeReader.ReadPrimaryAsync(barrierRequest, requiresValidLsn: true, useSessionToken: false);
				if (!storeResult.IsValid)
				{
					ExceptionDispatchInfo.Capture(storeResult.GetException()).Throw();
				}
				if (storeResult.CurrentReplicaSetSize > readQuorum)
				{
					DefaultTrace.TraceWarning("Unexpected response. Replica Set size is {0} which is greater than min value {1}", storeResult.CurrentReplicaSetSize, readQuorum);
					return PrimaryReadOutcome.QuorumInconclusive;
				}
				if (storeResult.LSN < lsnToWaitFor || storeResult.QuorumAckedLSN < lsnToWaitFor)
				{
					DefaultTrace.TraceWarning("Store LSN {0} or quorum acked LSN {1} are lower than expected LSN {2}", storeResult.LSN, storeResult.QuorumAckedLSN, lsnToWaitFor);
					await Task.Delay(5);
					continue;
				}
				return PrimaryReadOutcome.QuorumMet;
			}
			while (--primaryRetries > 0);
			return PrimaryReadOutcome.QuorumNotMet;
		}

		private async Task<bool> WaitForReadBarrierAsync(DocumentServiceRequest barrierRequest, bool allowPrimary, int readQuorum, long readBarrierLsn, long targetGlobalCommittedLSN, ReadMode readMode)
		{
			int readBarrierRetryCount = 6;
			int readBarrierRetryCountMultiRegion = 30;
			long maxGlobalCommittedLsn = 0L;
			while (readBarrierRetryCount-- > 0)
			{
				barrierRequest.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
				IList<StoreResult> list = await storeReader.ReadMultipleReplicaAsync(barrierRequest, allowPrimary, readQuorum, requiresValidLsn: true, useSessionToken: false, readMode, checkMinLSN: false, forceReadAll: true);
				long num2 = (list.Count > 0) ? list.Max((StoreResult response) => response.GlobalCommittedLSN) : 0;
				if (list.Count((StoreResult response) => response.LSN >= readBarrierLsn) >= readQuorum && (targetGlobalCommittedLSN <= 0 || num2 >= targetGlobalCommittedLSN))
				{
					return true;
				}
				maxGlobalCommittedLsn = ((maxGlobalCommittedLsn > num2) ? maxGlobalCommittedLsn : num2);
				barrierRequest.RequestContext.ForceRefreshAddressCache = false;
				if (readBarrierRetryCount == 0)
				{
					DefaultTrace.TraceInformation("QuorumReader: WaitForReadBarrierAsync - Last barrier for single-region requests. Responses: {0}", string.Join("; ", list));
				}
				else
				{
					await Task.Delay(5);
				}
			}
			if (targetGlobalCommittedLSN > 0)
			{
				while (readBarrierRetryCountMultiRegion-- > 0)
				{
					barrierRequest.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
					IList<StoreResult> list2 = await storeReader.ReadMultipleReplicaAsync(barrierRequest, allowPrimary, readQuorum, requiresValidLsn: true, useSessionToken: false, readMode, checkMinLSN: false, forceReadAll: true);
					long num4 = (list2.Count > 0) ? list2.Max((StoreResult response) => response.GlobalCommittedLSN) : 0;
					if (list2.Count((StoreResult response) => response.LSN >= readBarrierLsn) >= readQuorum && num4 >= targetGlobalCommittedLSN)
					{
						return true;
					}
					maxGlobalCommittedLsn = ((maxGlobalCommittedLsn > num4) ? maxGlobalCommittedLsn : num4);
					if (readBarrierRetryCountMultiRegion == 0)
					{
						DefaultTrace.TraceInformation("QuorumReader: WaitForReadBarrierAsync - Last barrier for mult-region strong requests. Responses: {0}", string.Join("; ", list2));
					}
					else if (30 - readBarrierRetryCountMultiRegion > 4)
					{
						await Task.Delay(30);
					}
					else
					{
						await Task.Delay(10);
					}
				}
			}
			DefaultTrace.TraceInformation("QuorumReader: WaitForReadBarrierAsync - TargetGlobalCommittedLsn: {0}, MaxGlobalCommittedLsn: {1}.", targetGlobalCommittedLSN, maxGlobalCommittedLsn);
			return false;
		}

		private bool IsQuorumMet(IList<StoreResult> readResponses, int readQuorum, bool isPrimaryIncluded, bool isGlobalStrongRead, out long readLsn, out long globalCommittedLSN, out StoreResult selectedResponse)
		{
			long maxLsn = 0L;
			long num = long.MaxValue;
			int num2 = 0;
			IEnumerable<StoreResult> enumerable = from response in readResponses
			where response.IsValid
			select response;
			int num3 = enumerable.Count();
			if (num3 == 0)
			{
				readLsn = 0L;
				globalCommittedLSN = -1L;
				selectedResponse = null;
				return false;
			}
			long num4 = enumerable.Max((StoreResult res) => res.NumberOfReadRegions);
			bool flag = isGlobalStrongRead && num4 > 0;
			foreach (StoreResult item in enumerable)
			{
				if (item.LSN == maxLsn)
				{
					num2++;
				}
				else if (item.LSN > maxLsn)
				{
					num2 = 1;
					maxLsn = item.LSN;
				}
				if (item.LSN < num)
				{
					num = item.LSN;
				}
			}
			selectedResponse = enumerable.First((StoreResult s) => s.LSN == maxLsn);
			readLsn = ((selectedResponse.ItemLSN == -1) ? maxLsn : Math.Min(selectedResponse.ItemLSN, maxLsn));
			globalCommittedLSN = (flag ? readLsn : (-1));
			long num5 = enumerable.Max((StoreResult res) => res.GlobalCommittedLSN);
			DefaultTrace.TraceInformation("QuorumReader: MaxLSN {0} ReplicaCountMaxLSN {1} bCheckGlobalStrong {2} MaxGlobalCommittedLSN {3} NumberOfReadRegions {4} SelectedResponseItemLSN {5}", maxLsn, num2, flag, num5, num4, selectedResponse.ItemLSN);
			bool flag2 = false;
			if (readLsn > 0 && num2 >= readQuorum && (!flag || num5 >= maxLsn))
			{
				flag2 = true;
			}
			if (!flag2 && num3 >= readQuorum && selectedResponse.ItemLSN != -1 && num != long.MaxValue && selectedResponse.ItemLSN <= num && (!flag || selectedResponse.ItemLSN <= num5))
			{
				flag2 = true;
			}
			return flag2;
		}
	}
}
