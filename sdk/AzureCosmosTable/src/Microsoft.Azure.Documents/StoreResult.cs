using Microsoft.Azure.Documents.Routing;
using System;
using System.Globalization;
using System.Net;
using System.Runtime.ExceptionServices;

namespace Microsoft.Azure.Documents
{
	internal sealed class StoreResult
	{
		private readonly StoreResponse storeResponse;

		private readonly DocumentClientException exception;

		private static bool UseSessionTokenHeader = VersionUtility.IsLaterThan(HttpConstants.Versions.CurrentVersion, HttpConstants.Versions.v2018_06_18);

		public long LSN
		{
			get;
			private set;
		}

		public string PartitionKeyRangeId
		{
			get;
			private set;
		}

		public long QuorumAckedLSN
		{
			get;
			private set;
		}

		public long GlobalCommittedLSN
		{
			get;
			private set;
		}

		public long NumberOfReadRegions
		{
			get;
			private set;
		}

		public long ItemLSN
		{
			get;
			private set;
		}

		public ISessionToken SessionToken
		{
			get;
			private set;
		}

		public bool UsingLocalLSN
		{
			get;
			private set;
		}

		public double RequestCharge
		{
			get;
			private set;
		}

		public int CurrentReplicaSetSize
		{
			get;
			private set;
		}

		public int CurrentWriteQuorum
		{
			get;
			private set;
		}

		public bool IsValid
		{
			get;
			private set;
		}

		public bool IsGoneException
		{
			get;
			private set;
		}

		public bool IsNotFoundException
		{
			get;
			private set;
		}

		public bool IsInvalidPartitionException
		{
			get;
			private set;
		}

		public Uri StorePhysicalAddress
		{
			get;
			private set;
		}

		public bool IsClientCpuOverloaded => (exception?.InnerException as TransportException)?.IsClientCpuOverloaded ?? false;

		public static StoreResult CreateStoreResult(StoreResponse storeResponse, Exception responseException, bool requiresValidLsn, bool useLocalLSNBasedHeaders, Uri storePhysicalAddress = null)
		{
			if (storeResponse == null && responseException == null)
			{
				throw new ArgumentException("storeResponse or responseException must be populated.");
			}
			if (responseException == null)
			{
				string value = null;
				long quorumAckedLsn = -1L;
				int currentReplicaSetSize = -1;
				int currentWriteQuorum = -1;
				long globalCommittedLSN = -1L;
				int numberOfReadRegions = -1;
				long itemLSN = -1L;
				if (storeResponse.TryGetHeaderValue(useLocalLSNBasedHeaders ? "x-ms-cosmos-quorum-acked-llsn" : "x-ms-quorum-acked-lsn", out value))
				{
					quorumAckedLsn = long.Parse(value, CultureInfo.InvariantCulture);
				}
				if (storeResponse.TryGetHeaderValue("x-ms-current-replica-set-size", out value))
				{
					currentReplicaSetSize = int.Parse(value, CultureInfo.InvariantCulture);
				}
				if (storeResponse.TryGetHeaderValue("x-ms-current-write-quorum", out value))
				{
					currentWriteQuorum = int.Parse(value, CultureInfo.InvariantCulture);
				}
				double requestCharge = 0.0;
				if (storeResponse.TryGetHeaderValue("x-ms-request-charge", out value))
				{
					requestCharge = double.Parse(value, CultureInfo.InvariantCulture);
				}
				if (storeResponse.TryGetHeaderValue("x-ms-number-of-read-regions", out value))
				{
					numberOfReadRegions = int.Parse(value, CultureInfo.InvariantCulture);
				}
				if (storeResponse.TryGetHeaderValue("x-ms-global-Committed-lsn", out value))
				{
					globalCommittedLSN = long.Parse(value, CultureInfo.InvariantCulture);
				}
				if (storeResponse.TryGetHeaderValue(useLocalLSNBasedHeaders ? "x-ms-cosmos-item-llsn" : "x-ms-item-lsn", out value))
				{
					itemLSN = long.Parse(value, CultureInfo.InvariantCulture);
				}
				long lsn = -1L;
				if (useLocalLSNBasedHeaders)
				{
					if (storeResponse.TryGetHeaderValue("x-ms-cosmos-llsn", out value))
					{
						lsn = long.Parse(value, CultureInfo.InvariantCulture);
					}
				}
				else
				{
					lsn = storeResponse.LSN;
				}
				ISessionToken sessionToken = null;
				if (UseSessionTokenHeader)
				{
					if (storeResponse.TryGetHeaderValue("x-ms-session-token", out value))
					{
						sessionToken = SessionTokenHelper.Parse(value);
					}
				}
				else
				{
					sessionToken = new SimpleSessionToken(storeResponse.LSN);
				}
				return new StoreResult(storeResponse, null, storeResponse.PartitionKeyRangeId, lsn, quorumAckedLsn, requestCharge, currentReplicaSetSize, currentWriteQuorum, isValid: true, storePhysicalAddress, globalCommittedLSN, numberOfReadRegions, itemLSN, sessionToken, useLocalLSNBasedHeaders);
			}
			DocumentClientException ex = responseException as DocumentClientException;
			if (ex != null)
			{
				VerifyCanContinueOnException(ex);
				long quorumAckedLsn2 = -1L;
				int currentReplicaSetSize2 = -1;
				int currentWriteQuorum2 = -1;
				long globalCommittedLSN2 = -1L;
				int numberOfReadRegions2 = -1;
				string text = ex.Headers[useLocalLSNBasedHeaders ? "x-ms-cosmos-quorum-acked-llsn" : "x-ms-quorum-acked-lsn"];
				if (!string.IsNullOrEmpty(text))
				{
					quorumAckedLsn2 = long.Parse(text, CultureInfo.InvariantCulture);
				}
				text = ex.Headers["x-ms-current-replica-set-size"];
				if (!string.IsNullOrEmpty(text))
				{
					currentReplicaSetSize2 = int.Parse(text, CultureInfo.InvariantCulture);
				}
				text = ex.Headers["x-ms-current-write-quorum"];
				if (!string.IsNullOrEmpty(text))
				{
					currentReplicaSetSize2 = int.Parse(text, CultureInfo.InvariantCulture);
				}
				double requestCharge2 = 0.0;
				text = ex.Headers["x-ms-request-charge"];
				if (!string.IsNullOrEmpty(text))
				{
					requestCharge2 = double.Parse(text, CultureInfo.InvariantCulture);
				}
				text = ex.Headers["x-ms-number-of-read-regions"];
				if (!string.IsNullOrEmpty(text))
				{
					numberOfReadRegions2 = int.Parse(text, CultureInfo.InvariantCulture);
				}
				text = ex.Headers["x-ms-global-Committed-lsn"];
				if (!string.IsNullOrEmpty(text))
				{
					globalCommittedLSN2 = long.Parse(text, CultureInfo.InvariantCulture);
				}
				long num = -1L;
				if (useLocalLSNBasedHeaders)
				{
					text = ex.Headers["x-ms-cosmos-llsn"];
					if (!string.IsNullOrEmpty(text))
					{
						num = long.Parse(text, CultureInfo.InvariantCulture);
					}
				}
				else
				{
					num = ex.LSN;
				}
				ISessionToken sessionToken2 = null;
				if (UseSessionTokenHeader)
				{
					text = ex.Headers["x-ms-session-token"];
					if (!string.IsNullOrEmpty(text))
					{
						sessionToken2 = SessionTokenHelper.Parse(text);
					}
				}
				else
				{
					sessionToken2 = new SimpleSessionToken(ex.LSN);
				}
				return new StoreResult(null, ex, ex.PartitionKeyRangeId, num, quorumAckedLsn2, requestCharge2, currentReplicaSetSize2, currentWriteQuorum2, !requiresValidLsn || ((ex.StatusCode != HttpStatusCode.Gone || ex.GetSubStatus() == SubStatusCodes.NameCacheIsStale) && num >= 0), (storePhysicalAddress == null) ? ex.RequestUri : storePhysicalAddress, globalCommittedLSN2, numberOfReadRegions2, -1L, sessionToken2, useLocalLSNBasedHeaders);
			}
			DefaultTrace.TraceCritical("Unexpected exception {0} received while reading from store.", responseException);
			return new StoreResult(null, new InternalServerErrorException(RMResources.InternalServerError, responseException), null, -1L, -1L, 0.0, 0, 0, isValid: false, storePhysicalAddress, -1L, 0, -1L, null, useLocalLSNBasedHeaders);
		}

		public StoreResult(StoreResponse storeResponse, DocumentClientException exception, string partitionKeyRangeId, long lsn, long quorumAckedLsn, double requestCharge, int currentReplicaSetSize, int currentWriteQuorum, bool isValid, Uri storePhysicalAddress, long globalCommittedLSN, int numberOfReadRegions, long itemLSN, ISessionToken sessionToken, bool usingLocalLSN)
		{
			this.storeResponse = storeResponse;
			this.exception = exception;
			PartitionKeyRangeId = partitionKeyRangeId;
			LSN = lsn;
			QuorumAckedLSN = quorumAckedLsn;
			RequestCharge = requestCharge;
			CurrentReplicaSetSize = currentReplicaSetSize;
			CurrentWriteQuorum = currentWriteQuorum;
			IsValid = isValid;
			IsGoneException = (this.exception != null && this.exception.StatusCode == HttpStatusCode.Gone);
			IsNotFoundException = ((this.exception != null && this.exception.StatusCode == HttpStatusCode.NotFound) || (storeResponse != null && storeResponse.Status == 404));
			IsInvalidPartitionException = (this.exception != null && this.exception.StatusCode == HttpStatusCode.Gone && this.exception.GetSubStatus() == SubStatusCodes.NameCacheIsStale);
			StorePhysicalAddress = storePhysicalAddress;
			GlobalCommittedLSN = globalCommittedLSN;
			NumberOfReadRegions = numberOfReadRegions;
			ItemLSN = itemLSN;
			SessionToken = sessionToken;
			UsingLocalLSN = usingLocalLSN;
		}

		public DocumentClientException GetException()
		{
			if (exception == null)
			{
				DefaultTrace.TraceCritical("Exception should be available but found none");
				throw new InternalServerErrorException(RMResources.InternalServerError);
			}
			return exception;
		}

		public StoreResponse ToResponse(RequestChargeTracker requestChargeTracker = null)
		{
			if (!IsValid)
			{
				if (exception == null)
				{
					DefaultTrace.TraceCritical("Exception not set for invalid response");
					throw new InternalServerErrorException(RMResources.InternalServerError);
				}
				throw exception;
			}
			if (requestChargeTracker != null)
			{
				SetRequestCharge(storeResponse, exception, requestChargeTracker.TotalRequestCharge);
			}
			if (exception != null)
			{
				throw exception;
			}
			return storeResponse;
		}

		public override string ToString()
		{
			int num = (storeResponse != null) ? storeResponse.Status : ((int)((exception != null && exception.StatusCode.HasValue) ? exception.StatusCode.Value : ((HttpStatusCode)0)));
			int num2 = (int)((storeResponse != null) ? storeResponse.SubStatusCode : ((exception != null) ? exception.GetSubStatus() : SubStatusCodes.Unknown));
			return string.Format(CultureInfo.InvariantCulture, "StorePhysicalAddress: {0}, LSN: {1}, GlobalCommittedLsn: {2}, PartitionKeyRangeId: {3}, IsValid: {4}, StatusCode: {5}, SubStatusCode: {6}, RequestCharge: {7}, ItemLSN: {8}, SessionToken: {9}, UsingLocalLSN: {10}, TransportException: {11}", StorePhysicalAddress, LSN, GlobalCommittedLSN, PartitionKeyRangeId, IsValid, num, num2, RequestCharge, ItemLSN, SessionToken?.ConvertToString(), UsingLocalLSN, (exception?.InnerException is TransportException) ? exception.InnerException.Message : "null");
		}

		private static void SetRequestCharge(StoreResponse response, DocumentClientException documentClientException, double totalRequestCharge)
		{
			if (documentClientException != null)
			{
				documentClientException.Headers["x-ms-request-charge"] = totalRequestCharge.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				if (response.ResponseHeaderNames == null)
				{
					return;
				}
				int num = 0;
				while (true)
				{
					if (num < response.ResponseHeaderNames.Length)
					{
						if (string.Equals(response.ResponseHeaderNames[num], "x-ms-request-charge", StringComparison.OrdinalIgnoreCase))
						{
							break;
						}
						num++;
						continue;
					}
					return;
				}
				response.ResponseHeaderValues[num] = totalRequestCharge.ToString(CultureInfo.InvariantCulture);
			}
		}

		private static void VerifyCanContinueOnException(DocumentClientException ex)
		{
			if (ex is PartitionKeyRangeGoneException || ex is PartitionKeyRangeIsSplittingException || ex is PartitionIsMigratingException)
			{
				ExceptionDispatchInfo.Capture(ex).Throw();
			}
			int result;
			if (!string.IsNullOrWhiteSpace(ex.Headers["x-ms-request-validation-failure"]) && int.TryParse(ex.Headers.GetValues("x-ms-request-validation-failure")[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result == 1)
			{
				ExceptionDispatchInfo.Capture(ex).Throw();
			}
		}
	}
}
