using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal static class TransportSerialization
	{
		internal class RntbdHeader
		{
			public StatusCodes Status
			{
				get;
				private set;
			}

			public Guid ActivityId
			{
				get;
				private set;
			}

			public RntbdHeader(StatusCodes status, Guid activityId)
			{
				Status = status;
				ActivityId = activityId;
			}
		}

		internal static readonly char[] UrlTrim = new char[1]
		{
			'/'
		};

		internal static byte[] BuildRequest(DocumentServiceRequest request, string replicaPath, ResourceOperation resourceOperation, Guid activityId, out int headerSize, out int bodySize)
		{
			RntbdConstants.RntbdOperationType rntbdOperationType = GetRntbdOperationType(resourceOperation.operationType);
			RntbdConstants.RntbdResourceType rntbdResourceType = GetRntbdResourceType(resourceOperation.resourceType);
			RntbdConstants.Request request2 = new RntbdConstants.Request();
			request2.replicaPath.value.valueBytes = Encoding.UTF8.GetBytes(replicaPath);
			request2.replicaPath.isPresent = true;
			AddResourceIdOrPathHeaders(request, request2);
			AddDateHeader(request, request2);
			AddContinuation(request, request2);
			AddMatchHeader(request, rntbdOperationType, request2);
			AddIfModifiedSinceHeader(request, request2);
			AddA_IMHeader(request, request2);
			AddIndexingDirectiveHeader(request, request2);
			AddMigrateCollectionDirectiveHeader(request, request2);
			AddConsistencyLevelHeader(request, request2);
			AddIsFanout(request, request2);
			AddEntityId(request, request2);
			AddAllowScanOnQuery(request, request2);
			AddEmitVerboseTracesInQuery(request, request2);
			AddCanCharge(request, request2);
			AddCanThrottle(request, request2);
			AddProfileRequest(request, request2);
			AddEnableLowPrecisionOrderBy(request, request2);
			AddPageSize(request, request2);
			AddSupportSpatialLegacyCoordinates(request, request2);
			AddUsePolygonsSmallerThanAHemisphere(request, request2);
			AddEnableLogging(request, request2);
			AddPopulateQuotaInfo(request, request2);
			AddPopulateResourceCount(request, request2);
			AddDisableRUPerMinuteUsage(request, request2);
			AddPopulateQueryMetrics(request, request2);
			AddQueryForceScan(request, request2);
			AddResponseContinuationTokenLimitInKb(request, request2);
			AddPopulatePartitionStatistics(request, request2);
			AddRemoteStorageType(request, request2);
			AddCollectionRemoteStorageSecurityIdentifier(request, request2);
			AddPopulateCollectionThroughputInfo(request, request2);
			AddShareThroughput(request, request2);
			AddIsReadOnlyScript(request, request2);
			AddIsAutoScaleRequest(request, request2);
			AddCanOfferReplaceComplete(request, request2);
			AddExcludeSystemProperties(request, request2);
			AddEnumerationDirection(request, request2);
			AddFanoutOperationStateHeader(request, request2);
			AddStartAndEndKeys(request, request2);
			AddContentSerializationFormat(request, request2);
			AddIsUserRequest(request, request2);
			AddPreserveFullContent(request, request2);
			FillTokenFromHeader(request, "authorization", request2.authorizationToken);
			FillTokenFromHeader(request, "x-ms-session-token", request2.sessionToken);
			FillTokenFromHeader(request, "x-ms-documentdb-pre-trigger-include", request2.preTriggerInclude);
			FillTokenFromHeader(request, "x-ms-documentdb-pre-trigger-exclude", request2.preTriggerExclude);
			FillTokenFromHeader(request, "x-ms-documentdb-post-trigger-include", request2.postTriggerInclude);
			FillTokenFromHeader(request, "x-ms-documentdb-post-trigger-exclude", request2.postTriggerExclude);
			FillTokenFromHeader(request, "x-ms-documentdb-partitionkey", request2.partitionKey);
			FillTokenFromHeader(request, "x-ms-documentdb-partitionkeyrangeid", request2.partitionKeyRangeId);
			FillTokenFromHeader(request, "x-ms-documentdb-expiry-seconds", request2.resourceTokenExpiry);
			FillTokenFromHeader(request, "x-ms-documentdb-filterby-schema-rid", request2.filterBySchemaRid);
			FillTokenFromHeader(request, "x-ms-cosmos-batch-continue-on-error", request2.shouldBatchContinueOnError);
			FillTokenFromHeader(request, "x-ms-cosmos-batch-ordered", request2.isBatchOrdered);
			FillTokenFromHeader(request, "x-ms-cosmos-batch-atomic", request2.isBatchAtomic);
			FillTokenFromHeader(request, "collection-partition-index", request2.collectionPartitionIndex);
			FillTokenFromHeader(request, "collection-service-index", request2.collectionServiceIndex);
			FillTokenFromHeader(request, "x-ms-resource-schema-name", request2.resourceSchemaName);
			FillTokenFromHeader(request, "x-ms-bind-replica", request2.bindReplicaDirective);
			FillTokenFromHeader(request, "x-ms-primary-master-key", request2.primaryMasterKey);
			FillTokenFromHeader(request, "x-ms-secondary-master-key", request2.secondaryMasterKey);
			FillTokenFromHeader(request, "x-ms-primary-readonly-key", request2.primaryReadonlyKey);
			FillTokenFromHeader(request, "x-ms-secondary-readonly-key", request2.secondaryReadonlyKey);
			FillTokenFromHeader(request, "x-ms-documentdb-partitioncount", request2.partitionCount);
			FillTokenFromHeader(request, "x-ms-documentdb-collection-rid", request2.collectionRid);
			FillTokenFromHeader(request, "x-ms-gateway-signature", request2.gatewaySignature);
			FillTokenFromHeader(request, "x-ms-remaining-time-in-ms-on-client", request2.remainingTimeInMsOnClientRequest);
			FillTokenFromHeader(request, "x-ms-client-retry-attempt-count", request2.clientRetryAttemptCount);
			FillTokenFromHeader(request, "x-ms-target-lsn", request2.targetLsn);
			FillTokenFromHeader(request, "x-ms-target-global-committed-lsn", request2.targetGlobalCommittedLsn);
			FillTokenFromHeader(request, "x-ms-transport-request-id", request2.transportRequestID);
			FillTokenFromHeader(request, "x-ms-restore-metadata-filter", request2.restoreMetadataFilter);
			FillTokenFromHeader(request, "x-ms-restore-params", request2.restoreParams);
			FillTokenFromHeader(request, "x-ms-partition-resource-filter", request2.partitionResourceFilter);
			FillTokenFromHeader(request, "x-ms-enable-dynamic-rid-range-allocation", request2.enableDynamicRidRangeAllocation);
			AddBinaryIdIfPresent(request, request2);
			FillTokenFromHeader(request, "x-ms-time-to-live-in-seconds", request2.timeToLiveInSeconds);
			AddEffectivePartitionKeyIfPresent(request, request2);
			FillTokenFromHeader(request, "x-ms-binary-passthrough-request", request2.binaryPassthroughRequest);
			FillTokenFromHeader(request, "x-ms-cosmos-allow-tentative-writes", request2.allowTentativeWrites);
			FillTokenFromHeader(request, "x-ms-cosmos-include-tentative-writes", request2.includeTentativeWrites);
			AddMergeStaticIdIfPresent(request, request2);
			FillTokenFromHeader(request, "x-ms-version", request2.clientVersion);
			byte[] array = activityId.ToByteArray();
			int num = 8 + array.Length;
			int num2 = 0;
			int num3 = 0;
			CloneableStream cloneableStream = null;
			if (request.CloneableBody != null)
			{
				cloneableStream = request.CloneableBody.Clone();
				num3 = (int)cloneableStream.Length;
			}
			byte[] array2 = null;
			using (cloneableStream)
			{
				if (num3 > 0)
				{
					num2 += 4;
					num2 += num3;
					request2.payloadPresent.value.valueByte = 1;
					request2.payloadPresent.isPresent = true;
				}
				else
				{
					request2.payloadPresent.value.valueByte = 0;
					request2.payloadPresent.isPresent = true;
				}
				num += request2.CalculateLength();
				num2 += num;
				array2 = new byte[num2];
				using (MemoryStream memoryStream = new MemoryStream(array2, writable: true))
				{
					using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
					{
						binaryWriter.Write((uint)num);
						binaryWriter.Write((ushort)rntbdResourceType);
						binaryWriter.Write((ushort)rntbdOperationType);
						binaryWriter.Write(array);
						int num4 = 8 + array.Length;
						int tokensLength = 0;
						request2.SerializeToBinaryWriter(binaryWriter, out tokensLength);
						num4 += tokensLength;
						if (num4 != num)
						{
							DefaultTrace.TraceCritical("Bug in RNTBD token serialization. Calculated header size: {0}. Actual header size: {1}", num, num4);
							throw new InternalServerErrorException();
						}
						if (num3 > 0)
						{
							binaryWriter.Write((uint)num3);
							binaryWriter.Flush();
							cloneableStream.WriteTo(memoryStream);
						}
						binaryWriter.Flush();
					}
				}
			}
			headerSize = num;
			bodySize = 4 + num3;
			if (headerSize > 131072)
			{
				DefaultTrace.TraceWarning("The request header is large. Header size: {0}. Warning threshold: {1}. RID: {2}. Resource type: {3}. Operation: {4}. Address: {5}", headerSize, 131072, request.ResourceAddress, request.ResourceType, resourceOperation, replicaPath);
			}
			if (bodySize > 2097152)
			{
				DefaultTrace.TraceWarning("The request body is large. Body size: {0}. Warning threshold: {1}. RID: {2}. Resource type: {3}. Operation: {4}. Address: {5}", bodySize, 2097152, request.ResourceAddress, request.ResourceType, resourceOperation, replicaPath);
			}
			return array2;
		}

		internal static byte[] BuildContextRequest(Guid activityId, UserAgentContainer userAgent)
		{
			byte[] array = activityId.ToByteArray();
			RntbdConstants.ConnectionContextRequest connectionContextRequest = new RntbdConstants.ConnectionContextRequest();
			connectionContextRequest.protocolVersion.value.valueULong = 1u;
			connectionContextRequest.protocolVersion.isPresent = true;
			connectionContextRequest.clientVersion.value.valueBytes = HttpConstants.Versions.CurrentVersionUTF8;
			connectionContextRequest.clientVersion.isPresent = true;
			connectionContextRequest.userAgent.value.valueBytes = userAgent.UserAgentUTF8;
			connectionContextRequest.userAgent.isPresent = true;
			int num = 8 + array.Length;
			num += connectionContextRequest.CalculateLength();
			byte[] array2 = new byte[num];
			using (MemoryStream output = new MemoryStream(array2, writable: true))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(output))
				{
					binaryWriter.Write(num);
					binaryWriter.Write((ushort)0);
					binaryWriter.Write((ushort)0);
					binaryWriter.Write(array);
					int tokensLength = 0;
					connectionContextRequest.SerializeToBinaryWriter(binaryWriter, out tokensLength);
					binaryWriter.Flush();
					return array2;
				}
			}
		}

		internal static StoreResponse MakeStoreResponse(StatusCodes status, Guid activityId, RntbdConstants.Response response, Stream body, string serverVersion)
		{
			List<string> list = new List<string>(response.tokens.Length);
			List<string> list2 = new List<string>(response.tokens.Length);
			AddResponseStringHeaderIfPresent(response.lastStateChangeDateTime, "x-ms-last-state-change-utc", list, list2);
			AddResponseStringHeaderIfPresent(response.continuationToken, "x-ms-continuation", list, list2);
			AddResponseStringHeaderIfPresent(response.eTag, "etag", list, list2);
			AddResponseULongHeaderIfPresent(response.retryAfterMilliseconds, "x-ms-retry-after-ms", list, list2);
			AddResponseStringHeaderIfPresent(response.storageMaxResoureQuota, "x-ms-resource-quota", list, list2);
			AddResponseStringHeaderIfPresent(response.storageResourceQuotaUsage, "x-ms-resource-usage", list, list2);
			AddResponseULongHeaderIfPresent(response.collectionPartitionIndex, "collection-partition-index", list, list2);
			AddResponseULongHeaderIfPresent(response.collectionServiceIndex, "collection-service-index", list, list2);
			AddResponseLongLongHeaderIfPresent(response.LSN, "lsn", list, list2);
			AddResponseULongHeaderIfPresent(response.itemCount, "x-ms-item-count", list, list2);
			AddResponseStringHeaderIfPresent(response.schemaVersion, "x-ms-schemaversion", list, list2);
			AddResponseStringHeaderIfPresent(response.ownerFullName, "x-ms-alt-content-path", list, list2);
			AddResponseStringHeaderIfPresent(response.ownerId, "x-ms-content-path", list, list2);
			AddResponseStringHeaderIfPresent(response.databaseAccountId, "x-ms-database-account-id", list, list2);
			AddResponseLongLongHeaderIfPresent(response.quorumAckedLSN, "x-ms-quorum-acked-lsn", list, list2);
			AddResponseByteHeaderIfPresent(response.requestValidationFailure, "x-ms-request-validation-failure", list, list2);
			AddResponseULongHeaderIfPresent(response.subStatus, "x-ms-substatus", list, list2);
			AddResponseULongHeaderIfPresent(response.collectionUpdateProgress, "x-ms-documentdb-collection-index-transformation-progress", list, list2);
			AddResponseULongHeaderIfPresent(response.currentWriteQuorum, "x-ms-current-write-quorum", list, list2);
			AddResponseULongHeaderIfPresent(response.currentReplicaSetSize, "x-ms-current-replica-set-size", list, list2);
			AddResponseULongHeaderIfPresent(response.collectionLazyIndexProgress, "x-ms-documentdb-collection-lazy-indexing-progress", list, list2);
			AddResponseStringHeaderIfPresent(response.partitionKeyRangeId, "x-ms-documentdb-partitionkeyrangeid", list, list2);
			AddResponseStringHeaderIfPresent(response.logResults, "x-ms-documentdb-script-log-results", list, list2);
			AddResponseULongHeaderIfPresent(response.xpRole, "x-ms-xp-role", list, list2);
			AddResponseByteHeaderIfPresent(response.isRUPerMinuteUsed, "x-ms-documentdb-is-ru-per-minute-used", list, list2);
			AddResponseStringHeaderIfPresent(response.queryMetrics, "x-ms-documentdb-query-metrics", list, list2);
			AddResponseLongLongHeaderIfPresent(response.globalCommittedLSN, "x-ms-global-Committed-lsn", list, list2);
			AddResponseULongHeaderIfPresent(response.numberOfReadRegions, "x-ms-number-of-read-regions", list, list2);
			AddResponseBoolHeaderIfPresent(response.offerReplacePending, "x-ms-offer-replace-pending", list, list2);
			AddResponseLongLongHeaderIfPresent(response.itemLSN, "x-ms-item-lsn", list, list2);
			AddResponseStringHeaderIfPresent(response.restoreState, "x-ms-restore-state", list, list2);
			AddResponseStringHeaderIfPresent(response.collectionSecurityIdentifier, "x-ms-collection-security-identifier", list, list2);
			AddResponseULongHeaderIfPresent(response.transportRequestID, "x-ms-transport-request-id", list, list2);
			AddResponseBoolHeaderIfPresent(response.shareThroughput, "x-ms-share-throughput", list, list2);
			AddResponseBoolHeaderIfPresent(response.disableRntbdChannel, "x-ms-disable-rntbd-channel", list, list2);
			AddResponseStringHeaderIfPresent(response.serverDateTimeUtc, "x-ms-date", list, list2);
			AddResponseLongLongHeaderIfPresent(response.localLSN, "x-ms-cosmos-llsn", list, list2);
			AddResponseLongLongHeaderIfPresent(response.quorumAckedLocalLSN, "x-ms-cosmos-quorum-acked-llsn", list, list2);
			AddResponseLongLongHeaderIfPresent(response.itemLocalLSN, "x-ms-cosmos-item-llsn", list, list2);
			AddResponseBoolHeaderIfPresent(response.hasTentativeWrites, "x-ms-cosmosdb-has-tentative-writes", list, list2);
			AddResponseStringHeaderIfPresent(response.sessionToken, "x-ms-session-token", list, list2);
			AddResponseLongLongHeaderIfPresent(response.replicatorLSNToGLSNDelta, "x-ms-cosmos-replicator-glsn-delta", list, list2);
			AddResponseLongLongHeaderIfPresent(response.replicatorLSNToLLSNDelta, "x-ms-cosmos-replicator-llsn-delta", list, list2);
			AddResponseLongLongHeaderIfPresent(response.vectorClockLocalProgress, "x-ms-cosmos-vectorclock-local-progress", list, list2);
			AddResponseULongHeaderIfPresent(response.minimumRUsForOffer, "x-ms-cosmos-min-throughput", list, list2);
			if (response.requestCharge.isPresent)
			{
				list.Add("x-ms-request-charge");
				list2.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.##}", response.requestCharge.value.valueDouble));
			}
			if (response.indexingDirective.isPresent)
			{
				string text = null;
				switch (response.indexingDirective.value.valueByte)
				{
				case 0:
					text = IndexingDirectiveStrings.Default;
					break;
				case 2:
					text = IndexingDirectiveStrings.Exclude;
					break;
				case 1:
					text = IndexingDirectiveStrings.Include;
					break;
				default:
					throw new Exception();
				}
				list.Add("x-ms-indexing-directive");
				list2.Add(text);
			}
			list.Add("x-ms-serviceversion");
			list2.Add(serverVersion);
			list.Add("x-ms-activity-id");
			list2.Add(activityId.ToString());
			return new StoreResponse
			{
				ResponseBody = body,
				Status = (int)status,
				ResponseHeaderValues = list2.ToArray(),
				ResponseHeaderNames = list.ToArray()
			};
		}

		internal static RntbdHeader DecodeRntbdHeader(byte[] header)
		{
			uint status = BitConverter.ToUInt32(header, 4);
			byte[] array = new byte[16];
			Buffer.BlockCopy(header, 8, array, 0, 16);
			return new RntbdHeader((StatusCodes)status, new Guid(array));
		}

		private static void AddResponseByteHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(token.value.valueByte.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void AddResponseBoolHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add((token.value.valueByte != 0).ToString().ToLowerInvariant());
			}
		}

		private static void AddResponseStringHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(Encoding.UTF8.GetString(token.value.valueBytes));
			}
		}

		private static void AddResponseULongHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(token.value.valueULong.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void AddResponseDoubleHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(token.value.valueDouble.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void AddResponseFloatHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(token.value.valueFloat.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void AddResponseLongLongHeaderIfPresent(RntbdToken token, string header, List<string> headerNames, List<string> headerValues)
		{
			if (token.isPresent)
			{
				headerNames.Add(header);
				headerValues.Add(token.value.valueLongLong.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static RntbdConstants.RntbdOperationType GetRntbdOperationType(OperationType operationType)
		{
			switch (operationType)
			{
			case OperationType.Create:
				return RntbdConstants.RntbdOperationType.Create;
			case OperationType.Delete:
				return RntbdConstants.RntbdOperationType.Delete;
			case OperationType.ExecuteJavaScript:
				return RntbdConstants.RntbdOperationType.ExecuteJavaScript;
			case OperationType.Query:
				return RntbdConstants.RntbdOperationType.Query;
			case OperationType.Read:
				return RntbdConstants.RntbdOperationType.Read;
			case OperationType.ReadFeed:
				return RntbdConstants.RntbdOperationType.ReadFeed;
			case OperationType.Replace:
				return RntbdConstants.RntbdOperationType.Replace;
			case OperationType.SqlQuery:
				return RntbdConstants.RntbdOperationType.SQLQuery;
			case OperationType.Patch:
				return RntbdConstants.RntbdOperationType.Patch;
			case OperationType.Head:
				return RntbdConstants.RntbdOperationType.Head;
			case OperationType.HeadFeed:
				return RntbdConstants.RntbdOperationType.HeadFeed;
			case OperationType.Upsert:
				return RntbdConstants.RntbdOperationType.Upsert;
			case OperationType.BatchApply:
				return RntbdConstants.RntbdOperationType.BatchApply;
			case OperationType.Batch:
				return RntbdConstants.RntbdOperationType.Batch;
			case OperationType.Crash:
				return RntbdConstants.RntbdOperationType.Crash;
			case OperationType.Pause:
				return RntbdConstants.RntbdOperationType.Pause;
			case OperationType.Recreate:
				return RntbdConstants.RntbdOperationType.Recreate;
			case OperationType.Recycle:
				return RntbdConstants.RntbdOperationType.Recycle;
			case OperationType.Resume:
				return RntbdConstants.RntbdOperationType.Resume;
			case OperationType.Stop:
				return RntbdConstants.RntbdOperationType.Stop;
			case OperationType.ForceConfigRefresh:
				return RntbdConstants.RntbdOperationType.ForceConfigRefresh;
			case OperationType.Throttle:
				return RntbdConstants.RntbdOperationType.Throttle;
			case OperationType.PreCreateValidation:
				return RntbdConstants.RntbdOperationType.PreCreateValidation;
			case OperationType.GetSplitPoint:
				return RntbdConstants.RntbdOperationType.GetSplitPoint;
			case OperationType.AbortSplit:
				return RntbdConstants.RntbdOperationType.AbortSplit;
			case OperationType.CompleteSplit:
				return RntbdConstants.RntbdOperationType.CompleteSplit;
			case OperationType.OfferUpdateOperation:
				return RntbdConstants.RntbdOperationType.OfferUpdateOperation;
			case OperationType.OfferPreGrowValidation:
				return RntbdConstants.RntbdOperationType.OfferPreGrowValidation;
			case OperationType.BatchReportThroughputUtilization:
				return RntbdConstants.RntbdOperationType.BatchReportThroughputUtilization;
			case OperationType.AbortPartitionMigration:
				return RntbdConstants.RntbdOperationType.AbortPartitionMigration;
			case OperationType.CompletePartitionMigration:
				return RntbdConstants.RntbdOperationType.CompletePartitionMigration;
			case OperationType.PreReplaceValidation:
				return RntbdConstants.RntbdOperationType.PreReplaceValidation;
			case OperationType.MigratePartition:
				return RntbdConstants.RntbdOperationType.MigratePartition;
			case OperationType.AddComputeGatewayRequestCharges:
				return RntbdConstants.RntbdOperationType.AddComputeGatewayRequestCharges;
			case OperationType.MasterReplaceOfferOperation:
				return RntbdConstants.RntbdOperationType.MasterReplaceOfferOperation;
			case OperationType.ProvisionedCollectionOfferUpdateOperation:
				return RntbdConstants.RntbdOperationType.ProvisionedCollectionOfferUpdateOperation;
			case OperationType.InitiateDatabaseOfferPartitionShrink:
				return RntbdConstants.RntbdOperationType.InitiateDatabaseOfferPartitionShrink;
			case OperationType.CompleteDatabaseOfferPartitionShrink:
				return RntbdConstants.RntbdOperationType.CompleteDatabaseOfferPartitionShrink;
			default:
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid operation type: {0}", operationType), "operationType");
			}
		}

		private static RntbdConstants.RntbdResourceType GetRntbdResourceType(ResourceType resourceType)
		{
			switch (resourceType)
			{
			case ResourceType.Attachment:
				return RntbdConstants.RntbdResourceType.Attachment;
			case ResourceType.Collection:
				return RntbdConstants.RntbdResourceType.Collection;
			case ResourceType.Conflict:
				return RntbdConstants.RntbdResourceType.Conflict;
			case ResourceType.Database:
				return RntbdConstants.RntbdResourceType.Database;
			case ResourceType.Document:
				return RntbdConstants.RntbdResourceType.Document;
			case ResourceType.Record:
				return RntbdConstants.RntbdResourceType.Record;
			case ResourceType.Permission:
				return RntbdConstants.RntbdResourceType.Permission;
			case ResourceType.StoredProcedure:
				return RntbdConstants.RntbdResourceType.StoredProcedure;
			case ResourceType.Trigger:
				return RntbdConstants.RntbdResourceType.Trigger;
			case ResourceType.User:
				return RntbdConstants.RntbdResourceType.User;
			case ResourceType.UserDefinedType:
				return RntbdConstants.RntbdResourceType.UserDefinedType;
			case ResourceType.UserDefinedFunction:
				return RntbdConstants.RntbdResourceType.UserDefinedFunction;
			case ResourceType.Offer:
				return RntbdConstants.RntbdResourceType.Offer;
			case ResourceType.DatabaseAccount:
				return RntbdConstants.RntbdResourceType.DatabaseAccount;
			case ResourceType.PartitionKeyRange:
				return RntbdConstants.RntbdResourceType.PartitionKeyRange;
			case ResourceType.Schema:
				return RntbdConstants.RntbdResourceType.Schema;
			case ResourceType.BatchApply:
				return RntbdConstants.RntbdResourceType.BatchApply;
			case ResourceType.ComputeGatewayCharges:
				return RntbdConstants.RntbdResourceType.ComputeGatewayCharges;
			case ResourceType.Module:
				return RntbdConstants.RntbdResourceType.Module;
			case ResourceType.ModuleCommand:
				return RntbdConstants.RntbdResourceType.ModuleCommand;
			case ResourceType.Replica:
				return RntbdConstants.RntbdResourceType.Replica;
			case ResourceType.PartitionSetInformation:
				return RntbdConstants.RntbdResourceType.PartitionSetInformation;
			case ResourceType.XPReplicatorAddress:
				return RntbdConstants.RntbdResourceType.XPReplicatorAddress;
			case ResourceType.MasterPartition:
				return RntbdConstants.RntbdResourceType.MasterPartition;
			case ResourceType.ServerPartition:
				return RntbdConstants.RntbdResourceType.ServerPartition;
			case ResourceType.Topology:
				return RntbdConstants.RntbdResourceType.Topology;
			case ResourceType.RestoreMetadata:
				return RntbdConstants.RntbdResourceType.RestoreMetadata;
			case ResourceType.RidRange:
				return RntbdConstants.RntbdResourceType.RidRange;
			case ResourceType.VectorClock:
				return RntbdConstants.RntbdResourceType.VectorClock;
			default:
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid resource type: {0}", resourceType), "resourceType");
			}
		}

		private static void AddMatchHeader(DocumentServiceRequest request, RntbdConstants.RntbdOperationType operationType, RntbdConstants.Request rntbdRequest)
		{
			string text = null;
			text = ((operationType - 3 > RntbdConstants.RntbdOperationType.Create) ? request.Headers["If-Match"] : request.Headers["If-None-Match"]);
			if (!string.IsNullOrEmpty(text))
			{
				rntbdRequest.match.value.valueBytes = Encoding.UTF8.GetBytes(text);
				rntbdRequest.match.isPresent = true;
			}
		}

		private static void AddIfModifiedSinceHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string text = request.Headers["If-Modified-Since"];
			if (!string.IsNullOrEmpty(text))
			{
				rntbdRequest.ifModifiedSince.value.valueBytes = Encoding.UTF8.GetBytes(text);
				rntbdRequest.ifModifiedSince.isPresent = true;
			}
		}

		private static void AddA_IMHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string text = request.Headers["A-IM"];
			if (!string.IsNullOrEmpty(text))
			{
				rntbdRequest.a_IM.value.valueBytes = Encoding.UTF8.GetBytes(text);
				rntbdRequest.a_IM.isPresent = true;
			}
		}

		private static void AddDateHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string dateHeader = Helpers.GetDateHeader(request.Headers);
			if (!string.IsNullOrEmpty(dateHeader))
			{
				rntbdRequest.date.value.valueBytes = Encoding.UTF8.GetBytes(dateHeader);
				rntbdRequest.date.isPresent = true;
			}
		}

		private static void AddContinuation(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Continuation))
			{
				rntbdRequest.continuationToken.value.valueBytes = Encoding.UTF8.GetBytes(request.Continuation);
				rntbdRequest.continuationToken.isPresent = true;
			}
		}

		private static void AddResourceIdOrPathHeaders(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.ResourceId))
			{
				rntbdRequest.resourceId.value.valueBytes = ResourceId.Parse(request.ResourceType, request.ResourceId);
				rntbdRequest.resourceId.isPresent = true;
			}
			if (!request.IsNameBased)
			{
				return;
			}
			string[] array = request.ResourceAddress.Split(UrlTrim, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length >= 2)
			{
				string a = array[0];
				if (!(a == "dbs"))
				{
					throw new BadRequestException();
				}
				rntbdRequest.databaseName.value.valueBytes = Encoding.UTF8.GetBytes(array[1]);
				rntbdRequest.databaseName.isPresent = true;
			}
			if (array.Length >= 4)
			{
				switch (array[2])
				{
				case "colls":
					rntbdRequest.collectionName.value.valueBytes = Encoding.UTF8.GetBytes(array[3]);
					rntbdRequest.collectionName.isPresent = true;
					break;
				case "users":
					rntbdRequest.userName.value.valueBytes = Encoding.UTF8.GetBytes(array[3]);
					rntbdRequest.userName.isPresent = true;
					break;
				case "udts":
					rntbdRequest.userDefinedTypeName.value.valueBytes = Encoding.UTF8.GetBytes(array[3]);
					rntbdRequest.userDefinedTypeName.isPresent = true;
					break;
				}
			}
			if (array.Length >= 6)
			{
				switch (array[4])
				{
				case "docs":
					rntbdRequest.documentName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.documentName.isPresent = true;
					break;
				case "sprocs":
					rntbdRequest.storedProcedureName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.storedProcedureName.isPresent = true;
					break;
				case "permissions":
					rntbdRequest.permissionName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.permissionName.isPresent = true;
					break;
				case "udfs":
					rntbdRequest.userDefinedFunctionName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.userDefinedFunctionName.isPresent = true;
					break;
				case "triggers":
					rntbdRequest.triggerName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.triggerName.isPresent = true;
					break;
				case "conflicts":
					rntbdRequest.conflictName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.conflictName.isPresent = true;
					break;
				case "pkranges":
					rntbdRequest.partitionKeyRangeName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.partitionKeyRangeName.isPresent = true;
					break;
				case "schemas":
					rntbdRequest.schemaName.value.valueBytes = Encoding.UTF8.GetBytes(array[5]);
					rntbdRequest.schemaName.isPresent = true;
					break;
				}
			}
			if (array.Length >= 8)
			{
				string a = array[6];
				if (a == "attachments")
				{
					rntbdRequest.attachmentName.value.valueBytes = Encoding.UTF8.GetBytes(array[7]);
					rntbdRequest.attachmentName.isPresent = true;
				}
			}
		}

		private static void AddBinaryIdIfPresent(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (request.Properties != null && request.Properties.TryGetValue("x-ms-binary-id", out object value))
			{
				byte[] array = value as byte[];
				if (array == null)
				{
					throw new ArgumentOutOfRangeException("x-ms-binary-id");
				}
				rntbdRequest.binaryId.value.valueBytes = array;
				rntbdRequest.binaryId.isPresent = true;
			}
		}

		private static void AddEffectivePartitionKeyIfPresent(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (request.Properties != null && request.Properties.TryGetValue("x-ms-effective-partition-key", out object value))
			{
				byte[] array = value as byte[];
				if (array == null)
				{
					throw new ArgumentOutOfRangeException("x-ms-effective-partition-key");
				}
				rntbdRequest.effectivePartitionKey.value.valueBytes = array;
				rntbdRequest.effectivePartitionKey.isPresent = true;
			}
		}

		private static void AddMergeStaticIdIfPresent(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (request.Properties != null && request.Properties.TryGetValue("x-ms-cosmos-merge-static-id", out object value))
			{
				byte[] array = value as byte[];
				if (array == null)
				{
					throw new ArgumentOutOfRangeException("x-ms-cosmos-merge-static-id");
				}
				rntbdRequest.mergeStaticId.value.valueBytes = array;
				rntbdRequest.mergeStaticId.isPresent = true;
			}
		}

		private static void AddEntityId(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.EntityId))
			{
				rntbdRequest.entityId.value.valueBytes = Encoding.UTF8.GetBytes(request.EntityId);
				rntbdRequest.entityId.isPresent = true;
			}
		}

		private static void AddIndexingDirectiveHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-indexing-directive"]))
			{
				RntbdConstants.RntbdIndexingDirective rntbdIndexingDirective = RntbdConstants.RntbdIndexingDirective.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-indexing-directive"], ignoreCase: true, out IndexingDirective result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-indexing-directive"], typeof(IndexingDirective).Name));
				}
				switch (result)
				{
				case IndexingDirective.Default:
					rntbdIndexingDirective = RntbdConstants.RntbdIndexingDirective.Default;
					break;
				case IndexingDirective.Exclude:
					rntbdIndexingDirective = RntbdConstants.RntbdIndexingDirective.Exclude;
					break;
				case IndexingDirective.Include:
					rntbdIndexingDirective = RntbdConstants.RntbdIndexingDirective.Include;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-indexing-directive"], typeof(IndexingDirective).Name));
				}
				rntbdRequest.indexingDirective.value.valueByte = (byte)rntbdIndexingDirective;
				rntbdRequest.indexingDirective.isPresent = true;
			}
		}

		private static void AddMigrateCollectionDirectiveHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-migratecollection-directive"]))
			{
				RntbdConstants.RntbdMigrateCollectionDirective rntbdMigrateCollectionDirective = RntbdConstants.RntbdMigrateCollectionDirective.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-migratecollection-directive"], ignoreCase: true, out MigrateCollectionDirective result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-migratecollection-directive"], typeof(MigrateCollectionDirective).Name));
				}
				switch (result)
				{
				case MigrateCollectionDirective.Freeze:
					rntbdMigrateCollectionDirective = RntbdConstants.RntbdMigrateCollectionDirective.Freeze;
					break;
				case MigrateCollectionDirective.Thaw:
					rntbdMigrateCollectionDirective = RntbdConstants.RntbdMigrateCollectionDirective.Thaw;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-migratecollection-directive"], typeof(MigrateCollectionDirective).Name));
				}
				rntbdRequest.migrateCollectionDirective.value.valueByte = (byte)rntbdMigrateCollectionDirective;
				rntbdRequest.migrateCollectionDirective.isPresent = true;
			}
		}

		private static void AddConsistencyLevelHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-consistency-level"]))
			{
				RntbdConstants.RntbdConsistencyLevel rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-consistency-level"], ignoreCase: true, out ConsistencyLevel result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-consistency-level"], typeof(ConsistencyLevel).Name));
				}
				switch (result)
				{
				case ConsistencyLevel.Strong:
					rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.Strong;
					break;
				case ConsistencyLevel.BoundedStaleness:
					rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.BoundedStaleness;
					break;
				case ConsistencyLevel.Session:
					rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.Session;
					break;
				case ConsistencyLevel.Eventual:
					rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.Eventual;
					break;
				case ConsistencyLevel.ConsistentPrefix:
					rntbdConsistencyLevel = RntbdConstants.RntbdConsistencyLevel.ConsistentPrefix;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-consistency-level"], typeof(ConsistencyLevel).Name));
				}
				rntbdRequest.consistencyLevel.value.valueByte = (byte)rntbdConsistencyLevel;
				rntbdRequest.consistencyLevel.isPresent = true;
			}
		}

		private static void AddIsFanout(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-is-fanout-request"]))
			{
				rntbdRequest.isFanout.value.valueByte = (byte)(request.Headers["x-ms-is-fanout-request"].Equals(bool.TrueString) ? 1 : 0);
				rntbdRequest.isFanout.isPresent = true;
			}
		}

		private static void AddAllowScanOnQuery(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-query-enable-scan"]))
			{
				rntbdRequest.enableScanInQuery.value.valueByte = (byte)(request.Headers["x-ms-documentdb-query-enable-scan"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.enableScanInQuery.isPresent = true;
			}
		}

		private static void AddEnableLowPrecisionOrderBy(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-query-enable-low-precision-order-by"]))
			{
				rntbdRequest.enableLowPrecisionOrderBy.value.valueByte = (byte)(request.Headers["x-ms-documentdb-query-enable-low-precision-order-by"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.enableLowPrecisionOrderBy.isPresent = true;
			}
		}

		private static void AddEmitVerboseTracesInQuery(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-query-emit-traces"]))
			{
				rntbdRequest.emitVerboseTracesInQuery.value.valueByte = (byte)(request.Headers["x-ms-documentdb-query-emit-traces"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.emitVerboseTracesInQuery.isPresent = true;
			}
		}

		private static void AddCanCharge(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-cancharge"]))
			{
				rntbdRequest.canCharge.value.valueByte = (byte)(request.Headers["x-ms-cancharge"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.canCharge.isPresent = true;
			}
		}

		private static void AddCanThrottle(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-canthrottle"]))
			{
				rntbdRequest.canThrottle.value.valueByte = (byte)(request.Headers["x-ms-canthrottle"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.canThrottle.isPresent = true;
			}
		}

		private static void AddProfileRequest(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-profile-request"]))
			{
				rntbdRequest.profileRequest.value.valueByte = (byte)(request.Headers["x-ms-profile-request"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.profileRequest.isPresent = true;
			}
		}

		private static void AddPageSize(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string text = request.Headers["x-ms-max-item-count"];
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidPageSize, text));
			}
			if (result == -1)
			{
				rntbdRequest.pageSize.value.valueULong = uint.MaxValue;
			}
			else
			{
				if (result < 0)
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidPageSize, text));
				}
				rntbdRequest.pageSize.value.valueULong = (uint)result;
			}
			rntbdRequest.pageSize.isPresent = true;
		}

		private static void AddEnableLogging(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-script-enable-logging"]))
			{
				rntbdRequest.enableLogging.value.valueByte = (byte)(request.Headers["x-ms-documentdb-script-enable-logging"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.enableLogging.isPresent = true;
			}
		}

		private static void AddSupportSpatialLegacyCoordinates(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-supportspatiallegacycoordinates"]))
			{
				rntbdRequest.supportSpatialLegacyCoordinates.value.valueByte = (byte)(request.Headers["x-ms-documentdb-supportspatiallegacycoordinates"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.supportSpatialLegacyCoordinates.isPresent = true;
			}
		}

		private static void AddUsePolygonsSmallerThanAHemisphere(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-usepolygonssmallerthanahemisphere"]))
			{
				rntbdRequest.usePolygonsSmallerThanAHemisphere.value.valueByte = (byte)(request.Headers["x-ms-documentdb-usepolygonssmallerthanahemisphere"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.usePolygonsSmallerThanAHemisphere.isPresent = true;
			}
		}

		private static void AddPopulateQuotaInfo(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-populatequotainfo"]))
			{
				rntbdRequest.populateQuotaInfo.value.valueByte = (byte)(request.Headers["x-ms-documentdb-populatequotainfo"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.populateQuotaInfo.isPresent = true;
			}
		}

		private static void AddPopulateResourceCount(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-populateresourcecount"]))
			{
				rntbdRequest.populateResourceCount.value.valueByte = (byte)(request.Headers["x-ms-documentdb-populateresourcecount"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.populateResourceCount.isPresent = true;
			}
		}

		private static void AddPopulatePartitionStatistics(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-populatepartitionstatistics"]))
			{
				rntbdRequest.populatePartitionStatistics.value.valueByte = (byte)(request.Headers["x-ms-documentdb-populatepartitionstatistics"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.populatePartitionStatistics.isPresent = true;
			}
		}

		private static void AddDisableRUPerMinuteUsage(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-disable-ru-per-minute-usage"]))
			{
				rntbdRequest.disableRUPerMinuteUsage.value.valueByte = (byte)(request.Headers["x-ms-documentdb-disable-ru-per-minute-usage"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.disableRUPerMinuteUsage.isPresent = true;
			}
		}

		private static void AddPopulateQueryMetrics(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-populatequerymetrics"]))
			{
				rntbdRequest.populateQueryMetrics.value.valueByte = (byte)(request.Headers["x-ms-documentdb-populatequerymetrics"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.populateQueryMetrics.isPresent = true;
			}
		}

		private static void AddQueryForceScan(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-force-query-scan"]))
			{
				rntbdRequest.forceQueryScan.value.valueByte = (byte)(request.Headers["x-ms-documentdb-force-query-scan"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.forceQueryScan.isPresent = true;
			}
		}

		private static void AddPopulateCollectionThroughputInfo(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-populatecollectionthroughputinfo"]))
			{
				rntbdRequest.populateCollectionThroughputInfo.value.valueByte = (byte)(request.Headers["x-ms-documentdb-populatecollectionthroughputinfo"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.populateCollectionThroughputInfo.isPresent = true;
			}
		}

		private static void AddShareThroughput(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-share-throughput"]))
			{
				rntbdRequest.shareThroughput.value.valueByte = (byte)(request.Headers["x-ms-share-throughput"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.shareThroughput.isPresent = true;
			}
		}

		private static void AddIsReadOnlyScript(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-is-readonly-script"]))
			{
				rntbdRequest.isReadOnlyScript.value.valueByte = (byte)(request.Headers["x-ms-is-readonly-script"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.isReadOnlyScript.isPresent = true;
			}
		}

		private static void AddIsAutoScaleRequest(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-is-auto-scale"]))
			{
				rntbdRequest.isAutoScaleRequest.value.valueByte = (byte)(request.Headers["x-ms-is-auto-scale"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.isAutoScaleRequest.isPresent = true;
			}
		}

		private static void AddCanOfferReplaceComplete(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-can-offer-replace-complete"]))
			{
				rntbdRequest.canOfferReplaceComplete.value.valueByte = (byte)(request.Headers["x-ms-can-offer-replace-complete"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.canOfferReplaceComplete.isPresent = true;
			}
		}

		private static void AddResponseContinuationTokenLimitInKb(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-responsecontinuationtokenlimitinkb"]))
			{
				string text = request.Headers["x-ms-documentdb-responsecontinuationtokenlimitinkb"];
				if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidPageSize, text));
				}
				if (result < 0)
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResponseContinuationTokenLimit, text));
				}
				rntbdRequest.responseContinuationTokenLimitInKb.value.valueULong = (uint)result;
				rntbdRequest.responseContinuationTokenLimitInKb.isPresent = true;
			}
		}

		private static void AddRemoteStorageType(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-remote-storage-type"]))
			{
				RntbdConstants.RntbdRemoteStorageType rntbdRemoteStorageType = RntbdConstants.RntbdRemoteStorageType.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-remote-storage-type"], ignoreCase: true, out RemoteStorageType result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-remote-storage-type"], typeof(RemoteStorageType).Name));
				}
				switch (result)
				{
				case RemoteStorageType.Standard:
					rntbdRemoteStorageType = RntbdConstants.RntbdRemoteStorageType.Standard;
					break;
				case RemoteStorageType.Premium:
					rntbdRemoteStorageType = RntbdConstants.RntbdRemoteStorageType.Premium;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-remote-storage-type"], typeof(RemoteStorageType).Name));
				}
				rntbdRequest.remoteStorageType.value.valueByte = (byte)rntbdRemoteStorageType;
				rntbdRequest.remoteStorageType.isPresent = true;
			}
		}

		private static void AddCollectionRemoteStorageSecurityIdentifier(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string text = request.Headers["x-ms-collection-security-identifier"];
			if (!string.IsNullOrEmpty(text))
			{
				rntbdRequest.collectionRemoteStorageSecurityIdentifier.value.valueBytes = Encoding.UTF8.GetBytes(text);
				rntbdRequest.collectionRemoteStorageSecurityIdentifier.isPresent = true;
			}
		}

		private static void AddIsUserRequest(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-cosmos-internal-is-user-request"]))
			{
				rntbdRequest.isUserRequest.value.valueByte = (byte)(request.Headers["x-ms-cosmos-internal-is-user-request"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.isUserRequest.isPresent = true;
			}
		}

		private static void AddPreserveFullContent(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-cosmos-preserve-full-content"]))
			{
				rntbdRequest.preserveFullContent.value.valueByte = (byte)(request.Headers["x-ms-cosmos-preserve-full-content"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.preserveFullContent.isPresent = true;
			}
		}

		private static void AddEnumerationDirection(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			object value = null;
			if (request.Properties != null && request.Properties.TryGetValue("x-ms-enumeration-direction", out value))
			{
				byte? b = value as byte?;
				if (!b.HasValue)
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, "x-ms-enumeration-direction", typeof(EnumerationDirection).Name));
				}
				rntbdRequest.enumerationDirection.value.valueByte = b.Value;
				rntbdRequest.enumerationDirection.isPresent = true;
			}
			else if (!string.IsNullOrEmpty(request.Headers["x-ms-enumeration-direction"]))
			{
				RntbdConstants.RntdbEnumerationDirection rntdbEnumerationDirection = RntbdConstants.RntdbEnumerationDirection.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-enumeration-direction"], ignoreCase: true, out EnumerationDirection result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-enumeration-direction"], typeof(EnumerationDirection).Name));
				}
				switch (result)
				{
				case EnumerationDirection.Forward:
					rntdbEnumerationDirection = RntbdConstants.RntdbEnumerationDirection.Forward;
					break;
				case EnumerationDirection.Reverse:
					rntdbEnumerationDirection = RntbdConstants.RntdbEnumerationDirection.Reverse;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-enumeration-direction"], typeof(EnumerationDirection).Name));
				}
				rntbdRequest.enumerationDirection.value.valueByte = (byte)rntdbEnumerationDirection;
				rntbdRequest.enumerationDirection.isPresent = true;
			}
		}

		private static void AddStartAndEndKeys(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (request.Properties == null)
			{
				AddStartAndEndKeysFromHeaders(request, rntbdRequest);
				return;
			}
			RntbdConstants.RntdbReadFeedKeyType? rntdbReadFeedKeyType = null;
			if (request.Properties.TryGetValue("x-ms-read-key-type", out object value))
			{
				if (!(value is byte))
				{
					throw new ArgumentOutOfRangeException("x-ms-read-key-type");
				}
				rntbdRequest.readFeedKeyType.value.valueByte = (byte)value;
				rntbdRequest.readFeedKeyType.isPresent = true;
				rntdbReadFeedKeyType = (RntbdConstants.RntdbReadFeedKeyType)value;
			}
			if (rntdbReadFeedKeyType == RntbdConstants.RntdbReadFeedKeyType.ResourceId)
			{
				SetBytesValue(request, "x-ms-start-id", rntbdRequest.StartId);
				SetBytesValue(request, "x-ms-end-id", rntbdRequest.EndId);
			}
			else if (rntdbReadFeedKeyType == RntbdConstants.RntdbReadFeedKeyType.EffectivePartitionKey)
			{
				SetBytesValue(request, "x-ms-start-epk", rntbdRequest.StartEpk);
				SetBytesValue(request, "x-ms-end-epk", rntbdRequest.EndEpk);
			}
		}

		private static void AddStartAndEndKeysFromHeaders(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-read-key-type"]))
			{
				RntbdConstants.RntdbReadFeedKeyType rntdbReadFeedKeyType = RntbdConstants.RntdbReadFeedKeyType.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-read-key-type"], ignoreCase: true, out ReadFeedKeyType result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-read-key-type"], typeof(ReadFeedKeyType).Name));
				}
				switch (result)
				{
				case ReadFeedKeyType.ResourceId:
					rntdbReadFeedKeyType = RntbdConstants.RntdbReadFeedKeyType.ResourceId;
					break;
				case ReadFeedKeyType.EffectivePartitionKey:
					rntdbReadFeedKeyType = RntbdConstants.RntdbReadFeedKeyType.EffectivePartitionKey;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-read-key-type"], typeof(ReadFeedKeyType).Name));
				}
				rntbdRequest.readFeedKeyType.value.valueByte = (byte)rntdbReadFeedKeyType;
				rntbdRequest.readFeedKeyType.isPresent = true;
			}
			string text = request.Headers["x-ms-start-id"];
			if (!string.IsNullOrEmpty(text))
			{
				rntbdRequest.StartId.value.valueBytes = Convert.FromBase64String(text);
				rntbdRequest.StartId.isPresent = true;
			}
			string text2 = request.Headers["x-ms-end-id"];
			if (!string.IsNullOrEmpty(text2))
			{
				rntbdRequest.EndId.value.valueBytes = Convert.FromBase64String(text2);
				rntbdRequest.EndId.isPresent = true;
			}
			string text3 = request.Headers["x-ms-start-epk"];
			if (!string.IsNullOrEmpty(text3))
			{
				rntbdRequest.StartEpk.value.valueBytes = Convert.FromBase64String(text3);
				rntbdRequest.StartEpk.isPresent = true;
			}
			string text4 = request.Headers["x-ms-end-epk"];
			if (!string.IsNullOrEmpty(text4))
			{
				rntbdRequest.EndEpk.value.valueBytes = Convert.FromBase64String(text4);
				rntbdRequest.EndEpk.isPresent = true;
			}
		}

		private static void SetBytesValue(DocumentServiceRequest request, string headerName, RntbdToken token)
		{
			if (request.Properties.TryGetValue(headerName, out object value))
			{
				byte[] array = value as byte[];
				if (array == null)
				{
					throw new ArgumentOutOfRangeException(headerName);
				}
				token.value.valueBytes = array;
				token.isPresent = true;
			}
		}

		private static void AddContentSerializationFormat(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-documentdb-content-serialization-format"]))
			{
				RntbdConstants.RntbdContentSerializationFormat rntbdContentSerializationFormat = RntbdConstants.RntbdContentSerializationFormat.Invalid;
				if (!Enum.TryParse(request.Headers["x-ms-documentdb-content-serialization-format"], ignoreCase: true, out ContentSerializationFormat result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-documentdb-content-serialization-format"], typeof(ContentSerializationFormat).Name));
				}
				switch (result)
				{
				case ContentSerializationFormat.JsonText:
					rntbdContentSerializationFormat = RntbdConstants.RntbdContentSerializationFormat.JsonText;
					break;
				case ContentSerializationFormat.CosmosBinary:
					rntbdContentSerializationFormat = RntbdConstants.RntbdContentSerializationFormat.CosmosBinary;
					break;
				case ContentSerializationFormat.HybridRow:
					rntbdContentSerializationFormat = RntbdConstants.RntbdContentSerializationFormat.HybridRow;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, request.Headers["x-ms-documentdb-content-serialization-format"], typeof(ContentSerializationFormat).Name));
				}
				rntbdRequest.contentSerializationFormat.value.valueByte = (byte)rntbdContentSerializationFormat;
				rntbdRequest.contentSerializationFormat.isPresent = true;
			}
		}

		private static void FillTokenFromHeader(DocumentServiceRequest request, string headerName, RntbdToken token)
		{
			string text = request.Headers[headerName];
			if (string.IsNullOrEmpty(text) && request.Properties != null && request.Properties.TryGetValue(headerName, out object value))
			{
				text = (string)value;
			}
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			switch (token.GetTokenType())
			{
			case RntbdTokenTypes.SmallString:
			case RntbdTokenTypes.String:
			case RntbdTokenTypes.ULongString:
				token.value.valueBytes = Encoding.UTF8.GetBytes(text);
				break;
			case RntbdTokenTypes.ULong:
				if (!uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint result3))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, headerName));
				}
				token.value.valueULong = result3;
				break;
			case RntbdTokenTypes.Long:
				if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, headerName));
				}
				token.value.valueLong = result;
				break;
			case RntbdTokenTypes.Double:
				token.value.valueDouble = double.Parse(text, CultureInfo.InvariantCulture);
				break;
			case RntbdTokenTypes.LongLong:
				if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result2))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, headerName));
				}
				token.value.valueLongLong = result2;
				break;
			case RntbdTokenTypes.Byte:
				token.value.valueByte = (byte)(text.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				break;
			default:
				throw new BadRequestException();
			}
			token.isPresent = true;
		}

		private static void AddExcludeSystemProperties(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			if (!string.IsNullOrEmpty(request.Headers["x-ms-exclude-system-properties"]))
			{
				rntbdRequest.excludeSystemProperties.value.valueByte = (byte)(request.Headers["x-ms-exclude-system-properties"].Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
				rntbdRequest.excludeSystemProperties.isPresent = true;
			}
		}

		private static void AddFanoutOperationStateHeader(DocumentServiceRequest request, RntbdConstants.Request rntbdRequest)
		{
			string text = request.Headers["x-ms-fanout-operation-state"];
			if (!string.IsNullOrEmpty(text))
			{
				if (!Enum.TryParse(text, ignoreCase: true, out FanoutOperationState result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, text, "FanoutOperationState"));
				}
				RntbdConstants.RntbdFanoutOperationState valueByte;
				switch (result)
				{
				case FanoutOperationState.Started:
					valueByte = RntbdConstants.RntbdFanoutOperationState.Started;
					break;
				case FanoutOperationState.Completed:
					valueByte = RntbdConstants.RntbdFanoutOperationState.Completed;
					break;
				default:
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidEnumValue, text, "FanoutOperationState"));
				}
				rntbdRequest.FanoutOperationState.value.valueByte = (byte)valueByte;
				rntbdRequest.FanoutOperationState.isPresent = true;
			}
		}
	}
}
