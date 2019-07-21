namespace Microsoft.Azure.Documents
{
	internal static class RntbdConstants
	{
		public enum RntbdResourceType : ushort
		{
			Connection = 0,
			Database = 1,
			Collection = 2,
			Document = 3,
			Attachment = 4,
			User = 5,
			Permission = 6,
			StoredProcedure = 7,
			Conflict = 8,
			Trigger = 9,
			UserDefinedFunction = 10,
			Module = 11,
			Replica = 12,
			ModuleCommand = 13,
			Record = 14,
			Offer = 0xF,
			PartitionSetInformation = 0x10,
			XPReplicatorAddress = 17,
			MasterPartition = 18,
			ServerPartition = 19,
			DatabaseAccount = 20,
			Topology = 21,
			PartitionKeyRange = 22,
			Schema = 24,
			BatchApply = 25,
			RestoreMetadata = 26,
			ComputeGatewayCharges = 27,
			RidRange = 28,
			UserDefinedType = 29,
			VectorClock = 0x1F
		}

		public enum RntbdOperationType : ushort
		{
			Connection = 0,
			Create = 1,
			Patch = 2,
			Read = 3,
			ReadFeed = 4,
			Delete = 5,
			Replace = 6,
			ExecuteJavaScript = 8,
			SQLQuery = 9,
			Pause = 10,
			Resume = 11,
			Stop = 12,
			Recycle = 13,
			Crash = 14,
			Query = 0xF,
			ForceConfigRefresh = 0x10,
			Head = 17,
			HeadFeed = 18,
			Upsert = 19,
			Recreate = 20,
			Throttle = 21,
			GetSplitPoint = 22,
			PreCreateValidation = 23,
			BatchApply = 24,
			AbortSplit = 25,
			CompleteSplit = 26,
			OfferUpdateOperation = 27,
			OfferPreGrowValidation = 28,
			BatchReportThroughputUtilization = 29,
			CompletePartitionMigration = 30,
			AbortPartitionMigration = 0x1F,
			PreReplaceValidation = 0x20,
			AddComputeGatewayRequestCharges = 33,
			MigratePartition = 34,
			MasterReplaceOfferOperation = 35,
			ProvisionedCollectionOfferUpdateOperation = 36,
			Batch = 37,
			InitiateDatabaseOfferPartitionShrink = 38,
			CompleteDatabaseOfferPartitionShrink = 39
		}

		public enum ConnectionContextRequestTokenIdentifiers : ushort
		{
			ProtocolVersion,
			ClientVersion,
			UserAgent
		}

		public sealed class ConnectionContextRequest : RntbdTokenStream
		{
			public RntbdToken protocolVersion;

			public RntbdToken clientVersion;

			public RntbdToken userAgent;

			public ConnectionContextRequest()
			{
				protocolVersion = new RntbdToken(isRequired: true, RntbdTokenTypes.ULong, 0);
				clientVersion = new RntbdToken(isRequired: true, RntbdTokenTypes.SmallString, 1);
				userAgent = new RntbdToken(isRequired: true, RntbdTokenTypes.SmallString, 2);
				SetTokens(new RntbdToken[3]
				{
					protocolVersion,
					clientVersion,
					userAgent
				});
			}
		}

		public enum ConnectionContextResponseTokenIdentifiers : ushort
		{
			ProtocolVersion,
			ClientVersion,
			ServerAgent,
			ServerVersion,
			IdleTimeoutInSeconds,
			UnauthenticatedTimeoutInSeconds
		}

		public sealed class ConnectionContextResponse : RntbdTokenStream
		{
			public RntbdToken protocolVersion;

			public RntbdToken clientVersion;

			public RntbdToken serverAgent;

			public RntbdToken serverVersion;

			public RntbdToken idleTimeoutInSeconds;

			public RntbdToken unauthenticatedTimeoutInSeconds;

			public ConnectionContextResponse()
			{
				protocolVersion = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 0);
				clientVersion = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 1);
				serverAgent = new RntbdToken(isRequired: true, RntbdTokenTypes.SmallString, 2);
				serverVersion = new RntbdToken(isRequired: true, RntbdTokenTypes.SmallString, 3);
				idleTimeoutInSeconds = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 4);
				unauthenticatedTimeoutInSeconds = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 5);
				SetTokens(new RntbdToken[6]
				{
					protocolVersion,
					clientVersion,
					serverAgent,
					serverVersion,
					idleTimeoutInSeconds,
					unauthenticatedTimeoutInSeconds
				});
			}
		}

		public enum RntbdIndexingDirective : byte
		{
			Default = 0,
			Include = 1,
			Exclude = 2,
			Invalid = byte.MaxValue
		}

		public enum RntbdMigrateCollectionDirective : byte
		{
			Thaw = 0,
			Freeze = 1,
			Invalid = byte.MaxValue
		}

		public enum RntbdRemoteStorageType : byte
		{
			Invalid,
			NotSpecified,
			Standard,
			Premium
		}

		public enum RntbdConsistencyLevel : byte
		{
			Strong = 0,
			BoundedStaleness = 1,
			Session = 2,
			Eventual = 3,
			ConsistentPrefix = 4,
			Invalid = byte.MaxValue
		}

		public enum RntdbEnumerationDirection : byte
		{
			Invalid,
			Forward,
			Reverse
		}

		public enum RntbdFanoutOperationState : byte
		{
			Started = 1,
			Completed
		}

		public enum RntdbReadFeedKeyType : byte
		{
			Invalid,
			ResourceId,
			EffectivePartitionKey
		}

		public enum RntbdContentSerializationFormat : byte
		{
			JsonText = 0,
			CosmosBinary = 1,
			HybridRow = 2,
			Invalid = byte.MaxValue
		}

		public enum RequestIdentifiers : ushort
		{
			ResourceId = 0,
			AuthorizationToken = 1,
			PayloadPresent = 2,
			Date = 3,
			PageSize = 4,
			SessionToken = 5,
			ContinuationToken = 6,
			IndexingDirective = 7,
			Match = 8,
			PreTriggerInclude = 9,
			PostTriggerInclude = 10,
			IsFanout = 11,
			CollectionPartitionIndex = 12,
			CollectionServiceIndex = 13,
			PreTriggerExclude = 14,
			PostTriggerExclude = 0xF,
			ConsistencyLevel = 0x10,
			EntityId = 17,
			ResourceSchemaName = 18,
			ReplicaPath = 19,
			ResourceTokenExpiry = 20,
			DatabaseName = 21,
			CollectionName = 22,
			DocumentName = 23,
			AttachmentName = 24,
			UserName = 25,
			PermissionName = 26,
			StoredProcedureName = 27,
			UserDefinedFunctionName = 28,
			TriggerName = 29,
			EnableScanInQuery = 30,
			EmitVerboseTracesInQuery = 0x1F,
			ConflictName = 0x20,
			BindReplicaDirective = 33,
			PrimaryMasterKey = 34,
			SecondaryMasterKey = 35,
			PrimaryReadonlyKey = 36,
			SecondaryReadonlyKey = 37,
			ProfileRequest = 38,
			EnableLowPrecisionOrderBy = 39,
			ClientVersion = 40,
			CanCharge = 41,
			CanThrottle = 42,
			PartitionKey = 43,
			PartitionKeyRangeId = 44,
			NotUsed2D = 45,
			NotUsed2E = 46,
			NotUsed2F = 47,
			MigrateCollectionDirective = 49,
			NotUsed32 = 50,
			SupportSpatialLegacyCoordinates = 51,
			PartitionCount = 52,
			CollectionRid = 53,
			PartitionKeyRangeName = 54,
			SchemaName = 58,
			FilterBySchemaRid = 59,
			UsePolygonsSmallerThanAHemisphere = 60,
			GatewaySignature = 61,
			EnableLogging = 62,
			A_IM = 0x3F,
			PopulateQuotaInfo = 0x40,
			DisableRUPerMinuteUsage = 65,
			PopulateQueryMetrics = 66,
			ResponseContinuationTokenLimitInKb = 67,
			PopulatePartitionStatistics = 68,
			RemoteStorageType = 69,
			CollectionRemoteStorageSecurityIdentifier = 70,
			IfModifiedSince = 71,
			PopulateCollectionThroughputInfo = 72,
			RemainingTimeInMsOnClientRequest = 73,
			ClientRetryAttemptCount = 74,
			TargetLsn = 75,
			TargetGlobalCommittedLsn = 76,
			TransportRequestID = 77,
			RestoreMetadaFilter = 78,
			RestoreParams = 79,
			ShareThroughput = 80,
			PartitionResourceFilter = 81,
			IsReadOnlyScript = 82,
			IsAutoScaleRequest = 83,
			ForceQueryScan = 84,
			CanOfferReplaceComplete = 86,
			ExcludeSystemProperties = 87,
			BinaryId = 88,
			TimeToLiveInSeconds = 89,
			EffectivePartitionKey = 90,
			BinaryPassthroughRequest = 91,
			UserDefinedTypeName = 92,
			EnableDynamicRidRangeAllocation = 93,
			EnumerationDirection = 94,
			StartId = 95,
			EndId = 96,
			FanoutOperationState = 97,
			StartEpk = 98,
			EndEpk = 99,
			ReadFeedKeyType = 100,
			ContentSerializationFormat = 101,
			AllowTentativeWrites = 102,
			IsUserRequest = 103,
			SharedOfferthroughput = 104,
			PreserveFullContent = 105,
			IncludeTentativeWrites = 112,
			PopulateResourceCount = 113,
			MergeStaticId = 114,
			IsBatchAtomic = 115,
			ShouldBatchContinueOnError = 116,
			IsBatchOrdered = 117
		}

		public sealed class Request : RntbdTokenStream
		{
			public RntbdToken resourceId;

			public RntbdToken authorizationToken;

			public RntbdToken payloadPresent;

			public RntbdToken date;

			public RntbdToken pageSize;

			public RntbdToken sessionToken;

			public RntbdToken continuationToken;

			public RntbdToken indexingDirective;

			public RntbdToken match;

			public RntbdToken preTriggerInclude;

			public RntbdToken postTriggerInclude;

			public RntbdToken isFanout;

			public RntbdToken collectionPartitionIndex;

			public RntbdToken collectionServiceIndex;

			public RntbdToken preTriggerExclude;

			public RntbdToken postTriggerExclude;

			public RntbdToken consistencyLevel;

			public RntbdToken entityId;

			public RntbdToken resourceSchemaName;

			public RntbdToken replicaPath;

			public RntbdToken resourceTokenExpiry;

			public RntbdToken databaseName;

			public RntbdToken collectionName;

			public RntbdToken documentName;

			public RntbdToken attachmentName;

			public RntbdToken userName;

			public RntbdToken permissionName;

			public RntbdToken storedProcedureName;

			public RntbdToken userDefinedFunctionName;

			public RntbdToken triggerName;

			public RntbdToken enableScanInQuery;

			public RntbdToken emitVerboseTracesInQuery;

			public RntbdToken conflictName;

			public RntbdToken bindReplicaDirective;

			public RntbdToken primaryMasterKey;

			public RntbdToken secondaryMasterKey;

			public RntbdToken primaryReadonlyKey;

			public RntbdToken secondaryReadonlyKey;

			public RntbdToken profileRequest;

			public RntbdToken enableLowPrecisionOrderBy;

			public RntbdToken clientVersion;

			public RntbdToken canCharge;

			public RntbdToken canThrottle;

			public RntbdToken partitionKey;

			public RntbdToken partitionKeyRangeId;

			public RntbdToken migrateCollectionDirective;

			public RntbdToken supportSpatialLegacyCoordinates;

			public RntbdToken partitionCount;

			public RntbdToken collectionRid;

			public RntbdToken partitionKeyRangeName;

			public RntbdToken schemaName;

			public RntbdToken filterBySchemaRid;

			public RntbdToken usePolygonsSmallerThanAHemisphere;

			public RntbdToken gatewaySignature;

			public RntbdToken enableLogging;

			public RntbdToken a_IM;

			public RntbdToken ifModifiedSince;

			public RntbdToken populateQuotaInfo;

			public RntbdToken disableRUPerMinuteUsage;

			public RntbdToken populateQueryMetrics;

			public RntbdToken responseContinuationTokenLimitInKb;

			public RntbdToken populatePartitionStatistics;

			public RntbdToken remoteStorageType;

			public RntbdToken remainingTimeInMsOnClientRequest;

			public RntbdToken clientRetryAttemptCount;

			public RntbdToken targetLsn;

			public RntbdToken targetGlobalCommittedLsn;

			public RntbdToken transportRequestID;

			public RntbdToken collectionRemoteStorageSecurityIdentifier;

			public RntbdToken populateCollectionThroughputInfo;

			public RntbdToken restoreMetadataFilter;

			public RntbdToken restoreParams;

			public RntbdToken shareThroughput;

			public RntbdToken partitionResourceFilter;

			public RntbdToken isReadOnlyScript;

			public RntbdToken isAutoScaleRequest;

			public RntbdToken forceQueryScan;

			public RntbdToken canOfferReplaceComplete;

			public RntbdToken excludeSystemProperties;

			public RntbdToken binaryId;

			public RntbdToken timeToLiveInSeconds;

			public RntbdToken effectivePartitionKey;

			public RntbdToken binaryPassthroughRequest;

			public RntbdToken userDefinedTypeName;

			public RntbdToken enableDynamicRidRangeAllocation;

			public RntbdToken enumerationDirection;

			public RntbdToken StartId;

			public RntbdToken EndId;

			public RntbdToken FanoutOperationState;

			public RntbdToken StartEpk;

			public RntbdToken EndEpk;

			public RntbdToken readFeedKeyType;

			public RntbdToken contentSerializationFormat;

			public RntbdToken allowTentativeWrites;

			public RntbdToken isUserRequest;

			public RntbdToken preserveFullContent;

			public RntbdToken includeTentativeWrites;

			public RntbdToken populateResourceCount;

			public RntbdToken mergeStaticId;

			public RntbdToken isBatchAtomic;

			public RntbdToken shouldBatchContinueOnError;

			public RntbdToken isBatchOrdered;

			public Request()
			{
				resourceId = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 0);
				authorizationToken = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 1);
				payloadPresent = new RntbdToken(isRequired: true, RntbdTokenTypes.Byte, 2);
				date = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 3);
				pageSize = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 4);
				sessionToken = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 5);
				continuationToken = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 6);
				indexingDirective = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 7);
				match = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 8);
				preTriggerInclude = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 9);
				postTriggerInclude = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 10);
				isFanout = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 11);
				collectionPartitionIndex = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 12);
				collectionServiceIndex = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 13);
				preTriggerExclude = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 14);
				postTriggerExclude = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 15);
				consistencyLevel = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 16);
				entityId = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 17);
				resourceSchemaName = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 18);
				replicaPath = new RntbdToken(isRequired: true, RntbdTokenTypes.String, 19);
				resourceTokenExpiry = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 20);
				databaseName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 21);
				collectionName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 22);
				documentName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 23);
				attachmentName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 24);
				userName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 25);
				permissionName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 26);
				storedProcedureName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 27);
				userDefinedFunctionName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 28);
				triggerName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 29);
				enableScanInQuery = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 30);
				emitVerboseTracesInQuery = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 31);
				conflictName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 32);
				bindReplicaDirective = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 33);
				primaryMasterKey = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 34);
				secondaryMasterKey = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 35);
				primaryReadonlyKey = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 36);
				secondaryReadonlyKey = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 37);
				profileRequest = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 38);
				enableLowPrecisionOrderBy = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 39);
				clientVersion = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 40);
				canCharge = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 41);
				canThrottle = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 42);
				partitionKey = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 43);
				partitionKeyRangeId = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 44);
				migrateCollectionDirective = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 49);
				supportSpatialLegacyCoordinates = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 51);
				partitionCount = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 52);
				collectionRid = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 53);
				partitionKeyRangeName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 54);
				schemaName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 58);
				filterBySchemaRid = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 59);
				usePolygonsSmallerThanAHemisphere = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 60);
				gatewaySignature = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 61);
				enableLogging = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 62);
				a_IM = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 63);
				ifModifiedSince = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 71);
				populateQuotaInfo = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 64);
				disableRUPerMinuteUsage = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 65);
				populateQueryMetrics = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 66);
				responseContinuationTokenLimitInKb = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 67);
				populatePartitionStatistics = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 68);
				remoteStorageType = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 69);
				collectionRemoteStorageSecurityIdentifier = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 70);
				populateCollectionThroughputInfo = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 72);
				remainingTimeInMsOnClientRequest = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 73);
				clientRetryAttemptCount = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 74);
				targetLsn = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 75);
				targetGlobalCommittedLsn = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 76);
				transportRequestID = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 77);
				restoreMetadataFilter = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 78);
				restoreParams = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 79);
				shareThroughput = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 80);
				partitionResourceFilter = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 81);
				isReadOnlyScript = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 82);
				isAutoScaleRequest = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 83);
				forceQueryScan = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 84);
				canOfferReplaceComplete = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 86);
				excludeSystemProperties = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 87);
				binaryId = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 88);
				timeToLiveInSeconds = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 89);
				effectivePartitionKey = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 90);
				binaryPassthroughRequest = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 91);
				userDefinedTypeName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 92);
				enableDynamicRidRangeAllocation = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 93);
				enumerationDirection = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 94);
				StartId = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 95);
				EndId = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 96);
				FanoutOperationState = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 97);
				StartEpk = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 98);
				EndEpk = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 99);
				readFeedKeyType = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 100);
				contentSerializationFormat = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 101);
				allowTentativeWrites = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 102);
				isUserRequest = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 103);
				preserveFullContent = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 105);
				includeTentativeWrites = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 112);
				populateResourceCount = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 113);
				mergeStaticId = new RntbdToken(isRequired: false, RntbdTokenTypes.Bytes, 114);
				isBatchAtomic = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 115);
				shouldBatchContinueOnError = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 116);
				isBatchOrdered = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 117);
				SetTokens(new RntbdToken[102]
				{
					resourceId,
					authorizationToken,
					payloadPresent,
					date,
					pageSize,
					sessionToken,
					continuationToken,
					indexingDirective,
					match,
					preTriggerInclude,
					postTriggerInclude,
					isFanout,
					collectionPartitionIndex,
					collectionServiceIndex,
					preTriggerExclude,
					postTriggerExclude,
					consistencyLevel,
					entityId,
					resourceSchemaName,
					replicaPath,
					resourceTokenExpiry,
					databaseName,
					collectionName,
					documentName,
					attachmentName,
					userName,
					permissionName,
					storedProcedureName,
					userDefinedFunctionName,
					triggerName,
					enableScanInQuery,
					emitVerboseTracesInQuery,
					conflictName,
					bindReplicaDirective,
					primaryMasterKey,
					secondaryMasterKey,
					primaryReadonlyKey,
					secondaryReadonlyKey,
					profileRequest,
					enableLowPrecisionOrderBy,
					clientVersion,
					canCharge,
					canThrottle,
					partitionKey,
					partitionKeyRangeId,
					migrateCollectionDirective,
					supportSpatialLegacyCoordinates,
					partitionCount,
					collectionRid,
					partitionKeyRangeName,
					schemaName,
					filterBySchemaRid,
					usePolygonsSmallerThanAHemisphere,
					gatewaySignature,
					enableLogging,
					a_IM,
					ifModifiedSince,
					populateQuotaInfo,
					disableRUPerMinuteUsage,
					populateQueryMetrics,
					responseContinuationTokenLimitInKb,
					populatePartitionStatistics,
					remoteStorageType,
					collectionRemoteStorageSecurityIdentifier,
					populateCollectionThroughputInfo,
					remainingTimeInMsOnClientRequest,
					clientRetryAttemptCount,
					targetLsn,
					targetGlobalCommittedLsn,
					transportRequestID,
					restoreMetadataFilter,
					restoreParams,
					shareThroughput,
					partitionResourceFilter,
					isReadOnlyScript,
					isAutoScaleRequest,
					forceQueryScan,
					canOfferReplaceComplete,
					excludeSystemProperties,
					binaryId,
					timeToLiveInSeconds,
					effectivePartitionKey,
					binaryPassthroughRequest,
					userDefinedTypeName,
					enableDynamicRidRangeAllocation,
					enumerationDirection,
					StartId,
					EndId,
					FanoutOperationState,
					StartEpk,
					EndEpk,
					readFeedKeyType,
					contentSerializationFormat,
					allowTentativeWrites,
					isUserRequest,
					preserveFullContent,
					includeTentativeWrites,
					populateResourceCount,
					mergeStaticId,
					isBatchAtomic,
					shouldBatchContinueOnError,
					isBatchOrdered
				});
			}
		}

		public enum ResponseIdentifiers : ushort
		{
			PayloadPresent = 0,
			LastStateChangeDateTime = 2,
			ContinuationToken = 3,
			ETag = 4,
			ReadsPerformed = 7,
			WritesPerformed = 8,
			QueriesPerformed = 9,
			IndexTermsGenerated = 10,
			ScriptsExecuted = 11,
			RetryAfterMilliseconds = 12,
			IndexingDirective = 13,
			StorageMaxResoureQuota = 14,
			StorageResourceQuotaUsage = 0xF,
			SchemaVersion = 0x10,
			CollectionPartitionIndex = 17,
			CollectionServiceIndex = 18,
			LSN = 19,
			ItemCount = 20,
			RequestCharge = 21,
			OwnerFullName = 23,
			OwnerId = 24,
			DatabaseAccountId = 25,
			QuorumAckedLSN = 26,
			RequestValidationFailure = 27,
			SubStatus = 28,
			CollectionUpdateProgress = 29,
			CurrentWriteQuorum = 30,
			CurrentReplicaSetSize = 0x1F,
			CollectionLazyIndexProgress = 0x20,
			PartitionKeyRangeId = 33,
			LogResults = 37,
			XPRole = 38,
			IsRUPerMinuteUsed = 39,
			QueryMetrics = 40,
			GlobalCommittedLSN = 41,
			NumberOfReadRegions = 48,
			OfferReplacePending = 49,
			ItemLSN = 50,
			RestoreState = 51,
			CollectionSecurityIdentifier = 52,
			TransportRequestID = 53,
			ShareThroughput = 54,
			DisableRntbdChannel = 56,
			ServerDateTimeUtc = 57,
			LocalLSN = 58,
			QuorumAckedLocalLSN = 59,
			ItemLocalLSN = 60,
			HasTentativeWrites = 61,
			SessionToken = 62,
			ReplicatorLSNToGLSNDelta = 0x3F,
			ReplicatorLSNToLLSNDelta = 0x40,
			VectorClockLocalProgress = 65,
			MinimumRUsForOffer = 66
		}

		public sealed class Response : RntbdTokenStream
		{
			public RntbdToken payloadPresent;

			public RntbdToken lastStateChangeDateTime;

			public RntbdToken continuationToken;

			public RntbdToken eTag;

			public RntbdToken readsPerformed;

			public RntbdToken writesPerformed;

			public RntbdToken queriesPerformed;

			public RntbdToken indexTermsGenerated;

			public RntbdToken scriptsExecuted;

			public RntbdToken retryAfterMilliseconds;

			public RntbdToken indexingDirective;

			public RntbdToken storageMaxResoureQuota;

			public RntbdToken storageResourceQuotaUsage;

			public RntbdToken schemaVersion;

			public RntbdToken collectionPartitionIndex;

			public RntbdToken collectionServiceIndex;

			public RntbdToken LSN;

			public RntbdToken itemCount;

			public RntbdToken requestCharge;

			public RntbdToken ownerFullName;

			public RntbdToken ownerId;

			public RntbdToken databaseAccountId;

			public RntbdToken quorumAckedLSN;

			public RntbdToken requestValidationFailure;

			public RntbdToken subStatus;

			public RntbdToken collectionUpdateProgress;

			public RntbdToken currentWriteQuorum;

			public RntbdToken currentReplicaSetSize;

			public RntbdToken collectionLazyIndexProgress;

			public RntbdToken partitionKeyRangeId;

			public RntbdToken logResults;

			public RntbdToken xpRole;

			public RntbdToken isRUPerMinuteUsed;

			public RntbdToken queryMetrics;

			public RntbdToken globalCommittedLSN;

			public RntbdToken numberOfReadRegions;

			public RntbdToken offerReplacePending;

			public RntbdToken itemLSN;

			public RntbdToken restoreState;

			public RntbdToken collectionSecurityIdentifier;

			public RntbdToken transportRequestID;

			public RntbdToken shareThroughput;

			public RntbdToken disableRntbdChannel;

			public RntbdToken serverDateTimeUtc;

			public RntbdToken localLSN;

			public RntbdToken quorumAckedLocalLSN;

			public RntbdToken itemLocalLSN;

			public RntbdToken hasTentativeWrites;

			public RntbdToken sessionToken;

			public RntbdToken replicatorLSNToGLSNDelta;

			public RntbdToken replicatorLSNToLLSNDelta;

			public RntbdToken vectorClockLocalProgress;

			public RntbdToken minimumRUsForOffer;

			public Response()
			{
				payloadPresent = new RntbdToken(isRequired: true, RntbdTokenTypes.Byte, 0);
				lastStateChangeDateTime = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 2);
				continuationToken = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 3);
				eTag = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 4);
				readsPerformed = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 7);
				writesPerformed = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 8);
				queriesPerformed = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 9);
				indexTermsGenerated = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 10);
				scriptsExecuted = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 11);
				retryAfterMilliseconds = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 12);
				indexingDirective = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 13);
				storageMaxResoureQuota = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 14);
				storageResourceQuotaUsage = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 15);
				schemaVersion = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 16);
				collectionPartitionIndex = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 17);
				collectionServiceIndex = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 18);
				LSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 19);
				itemCount = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 20);
				requestCharge = new RntbdToken(isRequired: false, RntbdTokenTypes.Double, 21);
				ownerFullName = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 23);
				ownerId = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 24);
				databaseAccountId = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 25);
				quorumAckedLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 26);
				requestValidationFailure = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 27);
				subStatus = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 28);
				collectionUpdateProgress = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 29);
				currentWriteQuorum = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 30);
				currentReplicaSetSize = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 31);
				collectionLazyIndexProgress = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 32);
				partitionKeyRangeId = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 33);
				logResults = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 37);
				xpRole = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 38);
				isRUPerMinuteUsed = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 39);
				queryMetrics = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 40);
				globalCommittedLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 41);
				numberOfReadRegions = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 48);
				offerReplacePending = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 49);
				itemLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 50);
				restoreState = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 51);
				collectionSecurityIdentifier = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 52);
				transportRequestID = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 53);
				shareThroughput = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 54);
				disableRntbdChannel = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 56);
				serverDateTimeUtc = new RntbdToken(isRequired: false, RntbdTokenTypes.SmallString, 57);
				localLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 58);
				quorumAckedLocalLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 59);
				itemLocalLSN = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 60);
				hasTentativeWrites = new RntbdToken(isRequired: false, RntbdTokenTypes.Byte, 61);
				sessionToken = new RntbdToken(isRequired: false, RntbdTokenTypes.String, 62);
				replicatorLSNToGLSNDelta = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 63);
				replicatorLSNToLLSNDelta = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 64);
				vectorClockLocalProgress = new RntbdToken(isRequired: false, RntbdTokenTypes.LongLong, 65);
				minimumRUsForOffer = new RntbdToken(isRequired: false, RntbdTokenTypes.ULong, 66);
				SetTokens(new RntbdToken[53]
				{
					payloadPresent,
					lastStateChangeDateTime,
					continuationToken,
					eTag,
					readsPerformed,
					writesPerformed,
					queriesPerformed,
					indexTermsGenerated,
					scriptsExecuted,
					retryAfterMilliseconds,
					indexingDirective,
					storageMaxResoureQuota,
					storageResourceQuotaUsage,
					schemaVersion,
					collectionPartitionIndex,
					collectionServiceIndex,
					LSN,
					itemCount,
					requestCharge,
					ownerFullName,
					ownerId,
					databaseAccountId,
					quorumAckedLSN,
					requestValidationFailure,
					subStatus,
					collectionUpdateProgress,
					currentWriteQuorum,
					currentReplicaSetSize,
					collectionLazyIndexProgress,
					partitionKeyRangeId,
					logResults,
					xpRole,
					isRUPerMinuteUsed,
					queryMetrics,
					globalCommittedLSN,
					numberOfReadRegions,
					offerReplacePending,
					itemLSN,
					restoreState,
					collectionSecurityIdentifier,
					transportRequestID,
					shareThroughput,
					disableRntbdChannel,
					serverDateTimeUtc,
					localLSN,
					quorumAckedLocalLSN,
					itemLocalLSN,
					hasTentativeWrites,
					sessionToken,
					replicatorLSNToGLSNDelta,
					replicatorLSNToLLSNDelta,
					vectorClockLocalProgress,
					minimumRUsForOffer
				});
			}
		}

		public const uint CurrentProtocolVersion = 1u;
	}
}
