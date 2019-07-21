using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class StoreReader
	{
		private sealed class ReadReplicaResult
		{
			public bool RetryWithForceRefresh
			{
				get;
				private set;
			}

			public IList<StoreResult> Responses
			{
				get;
				private set;
			}

			public ReadReplicaResult(bool retryWithForceRefresh, IList<StoreResult> responses)
			{
				RetryWithForceRefresh = retryWithForceRefresh;
				Responses = responses;
			}
		}

		private readonly TransportClient transportClient;

		private readonly AddressSelector addressSelector;

		private readonly ISessionContainer sessionContainer;

		private readonly bool canUseLocalLSNBasedHeaders;

		[ThreadStatic]
		private static Random random;

		internal string LastReadAddress
		{
			get;
			set;
		}

		public StoreReader(TransportClient transportClient, AddressSelector addressSelector, ISessionContainer sessionContainer)
		{
			this.transportClient = transportClient;
			this.addressSelector = addressSelector;
			this.sessionContainer = sessionContainer;
			canUseLocalLSNBasedHeaders = VersionUtility.IsLaterThan(HttpConstants.Versions.CurrentVersion, HttpConstants.Versions.v2018_06_18);
		}

		/// <summary>
		/// Makes requests to multiple replicas at once and returns responses
		/// </summary>
		/// <param name="entity"> DocumentServiceRequest</param>
		/// <param name="includePrimary">flag to indicate whether to indicate primary replica in the reads</param>
		/// <param name="replicaCountToRead"> number of replicas to read from </param>
		/// <param name="requiresValidLsn"> flag to indicate whether a valid lsn is required to consider a response as valid </param>
		/// <param name="useSessionToken"> flag to indicate whether to use session token </param>
		/// <param name="readMode"> Read mode </param>
		/// <param name="checkMinLSN"> set minimum required session lsn </param>
		/// <param name="forceReadAll"> reads from all available replicas to gather result from readsToRead number of replicas </param>
		/// <returns> ReadReplicaResult which indicates the LSN and whether Quorum was Met / Not Met etc </returns>
		public async Task<IList<StoreResult>> ReadMultipleReplicaAsync(DocumentServiceRequest entity, bool includePrimary, int replicaCountToRead, bool requiresValidLsn, bool useSessionToken, ReadMode readMode, bool checkMinLSN = false, bool forceReadAll = false)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			string originalSessionToken = entity.Headers["x-ms-session-token"];
			try
			{
				ReadReplicaResult readReplicaResult = await ReadMultipleReplicasInternalAsync(entity, includePrimary, replicaCountToRead, requiresValidLsn, useSessionToken, readMode, checkMinLSN, forceReadAll);
				if (entity.RequestContext.PerformLocalRefreshOnGoneException && readReplicaResult.RetryWithForceRefresh && !entity.RequestContext.ForceRefreshAddressCache)
				{
					entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
					entity.RequestContext.ForceRefreshAddressCache = true;
					readReplicaResult = await ReadMultipleReplicasInternalAsync(entity, includePrimary, replicaCountToRead, requiresValidLsn, useSessionToken, readMode, checkMinLSN: false, forceReadAll);
				}
				return readReplicaResult.Responses;
			}
			finally
			{
				SessionTokenHelper.SetOriginalSessionToken(entity, originalSessionToken);
			}
		}

		public async Task<StoreResult> ReadPrimaryAsync(DocumentServiceRequest entity, bool requiresValidLsn, bool useSessionToken)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			string originalSessionToken = entity.Headers["x-ms-session-token"];
			try
			{
				ReadReplicaResult readReplicaResult = await ReadPrimaryInternalAsync(entity, requiresValidLsn, useSessionToken);
				if (entity.RequestContext.PerformLocalRefreshOnGoneException && readReplicaResult.RetryWithForceRefresh && !entity.RequestContext.ForceRefreshAddressCache)
				{
					entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
					entity.RequestContext.ForceRefreshAddressCache = true;
					readReplicaResult = await ReadPrimaryInternalAsync(entity, requiresValidLsn, useSessionToken);
				}
				if (readReplicaResult.Responses.Count == 0)
				{
					throw new GoneException(RMResources.Gone);
				}
				return readReplicaResult.Responses[0];
			}
			finally
			{
				SessionTokenHelper.SetOriginalSessionToken(entity, originalSessionToken);
			}
		}

		/// <summary>
		/// Makes requests to multiple replicas at once and returns responses
		/// </summary>
		/// <param name="entity"> DocumentServiceRequest</param>
		/// <param name="includePrimary">flag to indicate whether to indicate primary replica in the reads</param>
		/// <param name="replicaCountToRead"> number of replicas to read from </param>
		/// <param name="requiresValidLsn"> flag to indicate whether a valid lsn is required to consider a response as valid </param>
		/// <param name="useSessionToken"> flag to indicate whether to use session token </param>
		/// <param name="readMode"> Read mode </param>
		/// <param name="checkMinLSN"> set minimum required session lsn </param>
		/// <param name="forceReadAll"> will read from all available replicas to put together result from readsToRead number of replicas </param>
		/// <returns> ReadReplicaResult which indicates the LSN and whether Quorum was Met / Not Met etc </returns>
		private async Task<ReadReplicaResult> ReadMultipleReplicasInternalAsync(DocumentServiceRequest entity, bool includePrimary, int replicaCountToRead, bool requiresValidLsn, bool useSessionToken, ReadMode readMode, bool checkMinLSN = false, bool forceReadAll = false)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			List<StoreResult> responseResult = new List<StoreResult>();
			string requestedCollectionRid = null;
			if (entity.ForceNameCacheRefresh)
			{
				requestedCollectionRid = entity.RequestContext.ResolvedCollectionRid;
			}
			List<Uri> resolveApiResults = (await addressSelector.ResolveAllUriAsync(entity, includePrimary, entity.RequestContext.ForceRefreshAddressCache)).ToList();
			if (!string.IsNullOrEmpty(requestedCollectionRid) && !string.IsNullOrEmpty(entity.RequestContext.ResolvedCollectionRid) && !requestedCollectionRid.Equals(entity.RequestContext.ResolvedCollectionRid))
			{
				sessionContainer.ClearTokenByResourceId(requestedCollectionRid);
			}
			int resolvedAddressCount = resolveApiResults.Count;
			ISessionToken requestSessionToken = null;
			if (useSessionToken)
			{
				SessionTokenHelper.SetPartitionLocalSessionToken(entity, sessionContainer);
				if (checkMinLSN)
				{
					requestSessionToken = entity.RequestContext.SessionToken;
				}
			}
			else
			{
				entity.Headers.Remove("x-ms-session-token");
			}
			if (resolveApiResults.Count < replicaCountToRead)
			{
				if (!entity.RequestContext.ForceRefreshAddressCache)
				{
					return new ReadReplicaResult(retryWithForceRefresh: true, responseResult);
				}
				return new ReadReplicaResult(retryWithForceRefresh: false, responseResult);
			}
			int num = replicaCountToRead;
			string text = entity.Headers["x-ms-version"];
			bool enforceSessionCheck = !string.IsNullOrEmpty(text) && VersionUtility.IsLaterThan(text, HttpConstants.Versions.v2016_05_30);
			bool hasGoneException = false;
			Exception exceptionToThrow = null;
			while (num > 0 && resolveApiResults.Count > 0)
			{
				entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
				Dictionary<Task<StoreResponse>, Uri> readStoreTasks = new Dictionary<Task<StoreResponse>, Uri>();
				int num2 = GenerateNextRandom(resolveApiResults.Count);
				while (resolveApiResults.Count > 0)
				{
					num2 %= resolveApiResults.Count;
					readStoreTasks.Add(ReadFromStoreAsync(resolveApiResults[num2], entity), resolveApiResults[num2]);
					resolveApiResults.RemoveAt(num2);
					if (!forceReadAll && readStoreTasks.Count == num)
					{
						break;
					}
				}
				if (readStoreTasks.Count < num)
				{
					int num3 = num - readStoreTasks.Count;
				}
				try
				{
					await Task.WhenAll(readStoreTasks.Keys);
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceInformation("Exception {0} is thrown while doing readMany", ex);
					exceptionToThrow = ex;
				}
				foreach (Task<StoreResponse> key in readStoreTasks.Keys)
				{
					StoreResponse obj = (key.Exception == null) ? key.Result : null;
					Exception ex2 = (key.Exception != null) ? key.Exception.InnerException : null;
					Uri uri = readStoreTasks[key];
					StoreResult storeResult = StoreResult.CreateStoreResult(obj, ex2, requiresValidLsn, canUseLocalLSNBasedHeaders && readMode != ReadMode.Strong, uri);
					entity.RequestContext.RequestChargeTracker.AddCharge(storeResult.RequestCharge);
					if (obj != null)
					{
						entity.RequestContext.ClientRequestStatistics.ContactedReplicas.Add(uri);
					}
					if (ex2 != null && ex2.InnerException is TransportException)
					{
						entity.RequestContext.ClientRequestStatistics.FailedReplicas.Add(uri);
					}
					entity.RequestContext.ClientRequestStatistics.RecordResponse(entity, storeResult);
					if (storeResult.IsValid && (requestSessionToken == null || (storeResult.SessionToken != null && requestSessionToken.IsValid(storeResult.SessionToken)) || (!enforceSessionCheck && !storeResult.IsNotFoundException)))
					{
						responseResult.Add(storeResult);
					}
					hasGoneException |= (storeResult.IsGoneException && !storeResult.IsInvalidPartitionException);
				}
				if (responseResult.Count >= replicaCountToRead)
				{
					if (hasGoneException && !entity.RequestContext.PerformedBackgroundAddressRefresh)
					{
						StartBackgroundAddressRefresh(entity);
						entity.RequestContext.PerformedBackgroundAddressRefresh = true;
					}
					return new ReadReplicaResult(retryWithForceRefresh: false, responseResult);
				}
				num = replicaCountToRead - responseResult.Count;
			}
			if (responseResult.Count < replicaCountToRead)
			{
				DefaultTrace.TraceInformation("Could not get quorum number of responses. ValidResponsesReceived: {0} ResponsesExpected: {1}, ResolvedAddressCount: {2}, ResponsesString: {3}", responseResult.Count, replicaCountToRead, resolvedAddressCount, string.Join(";", responseResult));
				if (hasGoneException)
				{
					if (!entity.RequestContext.PerformLocalRefreshOnGoneException)
					{
						throw new GoneException(exceptionToThrow);
					}
					if (!entity.RequestContext.ForceRefreshAddressCache)
					{
						return new ReadReplicaResult(retryWithForceRefresh: true, responseResult);
					}
				}
			}
			return new ReadReplicaResult(retryWithForceRefresh: false, responseResult);
		}

		private async Task<ReadReplicaResult> ReadPrimaryInternalAsync(DocumentServiceRequest entity, bool requiresValidLsn, bool useSessionToken)
		{
			entity.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			Uri primaryUri = await addressSelector.ResolvePrimaryUriAsync(entity, entity.RequestContext.ForceRefreshAddressCache);
			if (useSessionToken)
			{
				SessionTokenHelper.SetPartitionLocalSessionToken(entity, sessionContainer);
			}
			else
			{
				entity.Headers.Remove("x-ms-session-token");
			}
			Exception storeTaskException = null;
			StoreResponse storeResponse = null;
			try
			{
				storeResponse = await ReadFromStoreAsync(primaryUri, entity);
			}
			catch (Exception ex)
			{
				Exception ex2 = storeTaskException = ex;
				DefaultTrace.TraceInformation("Exception {0} is thrown while doing Read Primary", ex2);
			}
			StoreResult storeResult = StoreResult.CreateStoreResult(storeResponse, storeTaskException, requiresValidLsn, canUseLocalLSNBasedHeaders, primaryUri);
			entity.RequestContext.ClientRequestStatistics.RecordResponse(entity, storeResult);
			entity.RequestContext.RequestChargeTracker.AddCharge(storeResult.RequestCharge);
			if (storeResult.IsGoneException && !storeResult.IsInvalidPartitionException)
			{
				return new ReadReplicaResult(retryWithForceRefresh: true, new List<StoreResult>());
			}
			return new ReadReplicaResult(retryWithForceRefresh: false, new StoreResult[1]
			{
				storeResult
			});
		}

		private async Task<StoreResponse> ReadFromStoreAsync(Uri physicalAddress, DocumentServiceRequest request)
		{
			request.RequestContext.TimeoutHelper.ThrowGoneIfElapsed();
			QueryRequestPerformanceActivity activity = null;
			string text2 = request.Headers["If-None-Match"];
			LastReadAddress = physicalAddress.ToString();
			if (request.OperationType == OperationType.ReadFeed || request.OperationType == OperationType.Query)
			{
				string text = request.Headers["x-ms-continuation"];
				string text3 = request.Headers["x-ms-max-item-count"];
				if (text != null && Enumerable.Contains(text, ';'))
				{
					string[] array = text.Split(new char[1]
					{
						';'
					});
					if (array.Length < 3)
					{
						throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, "x-ms-continuation"));
					}
					text = array[0];
				}
				request.Continuation = text;
				activity = CustomTypeExtensions.StartActivity(request);
			}
			switch (request.OperationType)
			{
			case OperationType.Read:
			case OperationType.Head:
				return await transportClient.InvokeResourceOperationAsync(physicalAddress, request);
			case OperationType.ExecuteJavaScript:
			case OperationType.ReadFeed:
			case OperationType.SqlQuery:
			case OperationType.Query:
			case OperationType.HeadFeed:
				return await CompleteActivity(transportClient.InvokeResourceOperationAsync(physicalAddress, request), activity);
			default:
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unexpected operation type {0}", request.OperationType));
			}
		}

		private static async Task<StoreResponse> CompleteActivity(Task<StoreResponse> task, QueryRequestPerformanceActivity activity)
		{
			if (activity == null)
			{
				return await task;
			}
			StoreResponse result;
			try
			{
				result = await task;
			}
			catch
			{
				activity.ActivityComplete(markComplete: false);
				throw;
			}
			activity.ActivityComplete(markComplete: true);
			return result;
		}

		private void StartBackgroundAddressRefresh(DocumentServiceRequest request)
		{
			try
			{
				addressSelector.ResolveAllUriAsync(request, includePrimary: true, forceRefresh: true).ContinueWith(delegate(Task<IReadOnlyList<Uri>> task)
				{
					if (task.IsFaulted)
					{
						DefaultTrace.TraceWarning("Background refresh of the addresses failed with {0}", task.Exception.ToString());
					}
				});
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceWarning("Background refresh of the addresses failed with {0}", ex.ToString());
			}
		}

		private static int GenerateNextRandom(int maxValue)
		{
			if (random == null)
			{
				random = CustomTypeExtensions.GetRandomNumber();
			}
			return random.Next(maxValue);
		}
	}
}
