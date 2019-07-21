using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	///   A strongly-typed resource class, for looking up localized strings, etc.
	/// </summary>
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class RMResources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		/// <summary>
		///   Returns the cached ResourceManager instance used by this class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("Microsoft.Azure.Documents.RMResources", typeof(RMResources).GetAssembly());
				}
				return resourceMan;
			}
		}

		/// <summary>
		///   Overrides the current thread's CurrentUICulture property for all
		///   resource lookups using this strongly typed resource class.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		/// <summary>
		///   Looks up a localized string similar to {0} api is not supported for this database account.
		/// </summary>
		internal static string ApiTypeForbidden => ResourceManager.GetString("ApiTypeForbidden", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} should have {1} argument..
		/// </summary>
		internal static string ArgumentRequired => ResourceManager.GetString("ArgumentRequired", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Auto-Scale Setting Change With User Auth Is Disallowed..
		/// </summary>
		internal static string AutoScaleSettingChangeWithUserAuthIsDisallowed => ResourceManager.GetString("AutoScaleSettingChangeWithUserAuthIsDisallowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to At this time, only write operations made from the MongoDB SDKs are supported. Modifications to MongoDB collections using other SDKs is temporarily blocked..
		/// </summary>
		internal static string BadClientMongo => ResourceManager.GetString("BadClientMongo", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid response from upstream server or upstream server request failed..
		/// </summary>
		internal static string BadGateway => ResourceManager.GetString("BadGateway", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to One of the input values is invalid..
		/// </summary>
		internal static string BadRequest => ResourceManager.GetString("BadRequest", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Request url is invalid..
		/// </summary>
		internal static string BadUrl => ResourceManager.GetString("BadUrl", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot offline a write region that has zero read regions configured. There needs to be atleast one read region to failover to..
		/// </summary>
		internal static string CannotOfflineWriteRegionWithNoReadRegions => ResourceManager.GetString("CannotOfflineWriteRegionWithNoReadRegions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot specify partition key range or partition key for resource that is not partitioned..
		/// </summary>
		internal static string CannotSpecifyPKRangeForNonPartitionedResource => ResourceManager.GetString("CannotSpecifyPKRangeForNonPartitionedResource", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to StartTime cannot have DateTimeKind.Unspecified..
		/// </summary>
		internal static string ChangeFeedOptionsStartTimeWithUnspecifiedDateTimeKind => ResourceManager.GetString("ChangeFeedOptionsStartTimeWithUnspecifiedDateTimeKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Channel is closed.
		/// </summary>
		internal static string ChannelClosed => ResourceManager.GetString("ChannelClosed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The channel multiplexer has shut down..
		/// </summary>
		internal static string ChannelMultiplexerClosedTransportError => ResourceManager.GetString("ChannelMultiplexerClosedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The transport client failed to open a connection. See the inner exception for details..
		/// </summary>
		internal static string ChannelOpenFailedTransportError => ResourceManager.GetString("ChannelOpenFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The transport client timed out when opening a connection. See the inner exception for details..
		/// </summary>
		internal static string ChannelOpenTimeoutTransportError => ResourceManager.GetString("ChannelOpenTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The request failed because the client was unable to establish connections to {0} endpoints across {1} regions. The client CPU was overloaded during the attempted request..
		/// </summary>
		internal static string ClientCpuOverload => ResourceManager.GetString("ClientCpuOverload", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The request failed because the client was unable to establish connections to {0} endpoints across {1} regions. Please check for client resource starvation issues and verify connectivity between client and server..
		/// </summary>
		internal static string ClientUnavailable => ResourceManager.GetString("ClientUnavailable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Collection create request conflicted with ongoing add region or failover operation. Ensure to complete region configuration before creating collection..
		/// </summary>
		internal static string CollectionCreateTopologyConflict => ResourceManager.GetString("CollectionCreateTopologyConflict", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Throughput of this collection cannot be more than {0}, as was provisioned during collection creation time..
		/// </summary>
		internal static string CollectionThroughputCannotBeMoreThan => ResourceManager.GetString("CollectionThroughputCannotBeMoreThan", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to connect to the remote endpoint..
		/// </summary>
		internal static string ConnectFailedTransportError => ResourceManager.GetString("ConnectFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The connection failed..
		/// </summary>
		internal static string ConnectionBrokenTransportError => ResourceManager.GetString("ConnectionBrokenTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The connection attempt timed out..
		/// </summary>
		internal static string ConnectTimeoutTransportError => ResourceManager.GetString("ConnectTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Correlation ID not found in response.
		/// </summary>
		internal static string CorrelationIDNotFoundInResponse => ResourceManager.GetString("CorrelationIDNotFoundInResponse", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid CORS rule. At least one allowed origin is required..
		/// </summary>
		internal static string CorsAllowedOriginsEmptyList => ResourceManager.GetString("CorsAllowedOriginsEmptyList", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to InvalidCORS rule. The origin '{0}' is an invalid origin. A valid rigin should not include a uri path..
		/// </summary>
		internal static string CorsAllowedOriginsInvalidPath => ResourceManager.GetString("CorsAllowedOriginsInvalidPath", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid CORS rule. The origin '{0}' is not a well formed origin...
		/// </summary>
		internal static string CorsAllowedOriginsMalformedUri => ResourceManager.GetString("CorsAllowedOriginsMalformedUri", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid CORS rule. Wildcards are not supported..
		/// </summary>
		internal static string CorsAllowedOriginsWildcardsNotSupported => ResourceManager.GetString("CorsAllowedOriginsWildcardsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid CORS rules. Only one rule is supported..
		/// </summary>
		internal static string CorsTooManyRules => ResourceManager.GetString("CorsTooManyRules", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value specified in the "{0}" header is incompatible with value specified in the "{1}" header of the request..
		/// </summary>
		internal static string CrossPartitionContinuationAndIndex => ResourceManager.GetString("CrossPartitionContinuationAndIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cross partition query is required but disabled. Please set x-ms-documentdb-query-enablecrosspartition to true, specify x-ms-documentdb-partitionkey, or revise your query to avoid this exception..
		/// </summary>
		internal static string CrossPartitionQueryDisabled => ResourceManager.GetString("CrossPartitionQueryDisabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Database Account {0} does not exist.
		/// </summary>
		internal static string DatabaseAccountNotFound => ResourceManager.GetString("DatabaseAccountNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Database create request conflicted with ongoing add region or failover operation. Ensure to complete region configuration before creating collection...
		/// </summary>
		internal static string DatabaseCreateTopologyConflict => ResourceManager.GetString("DatabaseCreateTopologyConflict", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expecting DateTime value..
		/// </summary>
		internal static string DateTimeConverterInvalidDateTime => ResourceManager.GetString("DateTimeConverterInvalidDateTime", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expecting reader to read Integer.
		/// </summary>
		internal static string DateTimeConverterInvalidReaderValue => ResourceManager.GetString("DateTimeConverterInvalidReaderValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Expecting reader value to be compatible with double conversion..
		/// </summary>
		internal static string DateTimeConveterInvalidReaderDoubleValue => ResourceManager.GetString("DateTimeConveterInvalidReaderDoubleValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Error occurred while deserializing content '{0}'..
		/// </summary>
		internal static string DeserializationError => ResourceManager.GetString("DeserializationError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to DNS resolution failed..
		/// </summary>
		internal static string DnsResolutionFailedTransportError => ResourceManager.GetString("DnsResolutionFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to DNS resolution timed out..
		/// </summary>
		internal static string DnsResolutionTimeoutTransportError => ResourceManager.GetString("DnsResolutionTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Document Query Execution Context is done..
		/// </summary>
		internal static string DocumentQueryExecutionContextIsDone => ResourceManager.GetString("DocumentQueryExecutionContextIsDone", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Service is currently unavailable, please retry after a while. If this problem persists please contact support..
		/// </summary>
		internal static string DocumentServiceUnavailable => ResourceManager.GetString("DocumentServiceUnavailable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Duplicate correlation id generated.
		/// </summary>
		internal static string DuplicateCorrelationIdGenerated => ResourceManager.GetString("DuplicateCorrelationIdGenerated", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Empty Virtual Network Resource Guid.
		/// </summary>
		internal static string EmptyVirtualNetworkResourceGuid => ResourceManager.GetString("EmptyVirtualNetworkResourceGuid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Specified Virtual Network Rules list is empty.
		/// </summary>
		internal static string EmptyVirtualNetworkRulesSpecified => ResourceManager.GetString("EmptyVirtualNetworkRulesSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Availability Zone on a multi region account requires multi master to be enabled. Please enable Multi-Master on the database account to proceed..
		/// </summary>
		internal static string EnableMultipleWriteLocationsBeforeAddingRegion => ResourceManager.GetString("EnableMultipleWriteLocationsBeforeAddingRegion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to retrieve address of endpoint '{0}' from the address '{1}'.
		/// </summary>
		internal static string EndpointNotFound => ResourceManager.GetString("EndpointNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Entity with the specified id already exists in the system..
		/// </summary>
		internal static string EntityAlreadyExists => ResourceManager.GetString("EntityAlreadyExists", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Message: {0}.
		/// </summary>
		internal static string ExceptionMessage => ResourceManager.GetString("ExceptionMessage", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0}, Local IP: {1}.
		/// </summary>
		internal static string ExceptionMessageAddIpAddress => ResourceManager.GetString("ExceptionMessageAddIpAddress", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0}, Request URI: {1}, RequestStats: {2}, SDK: {3}.
		/// </summary>
		internal static string ExceptionMessageAddRequestUri => ResourceManager.GetString("ExceptionMessageAddRequestUri", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Feature {0} is not supported for Multi-region Account.
		/// </summary>
		internal static string FeatureNotSupportedForMultiRegionAccount => ResourceManager.GetString("FeatureNotSupportedForMultiRegionAccount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Feature {0} is not supported in {1} region.
		/// </summary>
		internal static string FeatureNotSupportedInRegion => ResourceManager.GetString("FeatureNotSupportedInRegion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to {0} is not supported for the target subscription.
		/// </summary>
		internal static string FeatureNotSupportedOnSubscription => ResourceManager.GetString("FeatureNotSupportedOnSubscription", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Federation {0} in region {1} is not found.
		/// </summary>
		internal static string FederationEntityNotFound => ResourceManager.GetString("FederationEntityNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to proceed with the request. Please check the authorization claims to ensure the required permissions to process the request..
		/// </summary>
		internal static string Forbidden => ResourceManager.GetString("Forbidden", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Attempt to process the request timed out at remote server..
		/// </summary>
		internal static string GatewayTimedout => ResourceManager.GetString("GatewayTimedout", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Database and Write Location are not matching.
		/// </summary>
		internal static string GlobalAndWriteRegionMisMatch => ResourceManager.GetString("GlobalAndWriteRegionMisMatch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Global Strong write barrier has not been met for the request..
		/// </summary>
		internal static string GlobalStrongWriteBarrierNotMet => ResourceManager.GetString("GlobalStrongWriteBarrierNotMet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The requested resource is no longer available at the server..
		/// </summary>
		internal static string Gone => ResourceManager.GetString("Gone", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to generate id for resourceType = {0}, partitionIndex = {1}, serviceIndex = {2}, partitionCount = {3}..
		/// </summary>
		internal static string IdGenerationFailed => ResourceManager.GetString("IdGenerationFailed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Routing map is not complete..
		/// </summary>
		internal static string IncompleteRoutingMap => ResourceManager.GetString("IncompleteRoutingMap", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Insufficient permissions provided in the authorization header for the corresponding request. Please retry with another authorization header..
		/// </summary>
		internal static string InsufficientPermissions => ResourceManager.GetString("InsufficientPermissions", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to continue without atleast a single token in the resource tokens input collection..
		/// </summary>
		internal static string InsufficientResourceTokens => ResourceManager.GetString("InsufficientResourceTokens", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unknown server error occurred when processing this request..
		/// </summary>
		internal static string InternalServerError => ResourceManager.GetString("InternalServerError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid API version. Ensure a valid x-ms-version header value is passed. Please update to the latest version of Azure Cosmos DB SDK..
		/// </summary>
		internal static string InvalidAPIVersion => ResourceManager.GetString("InvalidAPIVersion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid API version for {0}. Ensure a valid x-ms-version header value is passed. Please update to the latest version of Azure Cosmos DB SDK..
		/// </summary>
		internal static string InvalidAPIVersionForFeature => ResourceManager.GetString("InvalidAPIVersionForFeature", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to AudienceKind is Invalid.
		/// </summary>
		internal static string InvalidAudienceKind => ResourceManager.GetString("InvalidAudienceKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Audience Resource Type.
		/// </summary>
		internal static string InvalidAudienceResourceType => ResourceManager.GetString("InvalidAudienceResourceType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Authorization header doesn't confirm to the required format. Please verify and try again..
		/// </summary>
		internal static string InvalidAuthHeaderFormat => ResourceManager.GetString("InvalidAuthHeaderFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The backend response was not in the correct format..
		/// </summary>
		internal static string InvalidBackendResponse => ResourceManager.GetString("InvalidBackendResponse", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The resource name presented contains invalid character '{0}'..
		/// </summary>
		internal static string InvalidCharacterInResourceName => ResourceManager.GetString("InvalidCharacterInResourceName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid mode '{0}' for setting '{1}'. Mode expected is '{2}'..
		/// </summary>
		internal static string InvalidConflictResolutionMode => ResourceManager.GetString("InvalidConflictResolutionMode", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ConsistencyLevel {0} specified in the request is invalid when service is configured with consistency level {1}. Ensure the request consistency level is not stronger than the service consistency level..
		/// </summary>
		internal static string InvalidConsistencyLevel => ResourceManager.GetString("InvalidConsistencyLevel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Continuation Token.
		/// </summary>
		internal static string InvalidContinuationToken => ResourceManager.GetString("InvalidContinuationToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The specified document collection is invalid...
		/// </summary>
		internal static string InvalidDatabase => ResourceManager.GetString("InvalidDatabase", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input date header is invalid format. Please pass in RFC 1123 style date format..
		/// </summary>
		internal static string InvalidDateHeader => ResourceManager.GetString("InvalidDateHeader", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The specified document collection is invalid..
		/// </summary>
		internal static string InvalidDocumentCollection => ResourceManager.GetString("InvalidDocumentCollection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot enable 'enableMultipleWriteLocations' without also enabling 'canEnableMultipleWriteLocations'..
		/// </summary>
		internal static string InvalidEnableMultipleWriteLocations => ResourceManager.GetString("InvalidEnableMultipleWriteLocations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid value {0} passed for enum {1}.
		/// </summary>
		internal static string InvalidEnumValue => ResourceManager.GetString("InvalidEnumValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failover priority value {0} supplied for region {1} is invalid.
		/// </summary>
		internal static string InvalidFailoverPriority => ResourceManager.GetString("InvalidFailoverPriority", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' specified for the header '{1}' is invalid. .
		/// </summary>
		internal static string InvalidHeaderValue => ResourceManager.GetString("InvalidHeaderValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The specified value {0} of the index kind is invalid..
		/// </summary>
		internal static string InvalidIndexKindValue => ResourceManager.GetString("InvalidIndexKindValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The index spec format is invalid..
		/// </summary>
		internal static string InvalidIndexSpecFormat => ResourceManager.GetString("InvalidIndexSpecFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Received invalid index transformation progress values from all the replicas..
		/// </summary>
		internal static string InvalidIndexTransformationProgressValues => ResourceManager.GetString("InvalidIndexTransformationProgressValues", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to List of supplied locations is invalid.
		/// </summary>
		internal static string InvalidLocations => ResourceManager.GetString("InvalidLocations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one manualPrivateLinkServiceConnection or one privateLinkServiceConnection is supported.
		/// </summary>
		internal static string InvalidPrivateLinkServiceConnections => ResourceManager.GetString("InvalidPrivateLinkServiceConnections", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one privateLinkServiceProxy is supported.
		/// </summary>
		internal static string InvalidPrivateLinkServiceProxies => ResourceManager.GetString("InvalidPrivateLinkServiceProxies", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Only one groupId is supported.
		/// </summary>
		internal static string InvalidGroupIdCount => ResourceManager.GetString("InvalidGroupIdCount", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to GroupId {0} is not supported.
		/// </summary>
		internal static string InvalidGroupId => ResourceManager.GetString("InvalidGroupId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to MaxStalenessInterval should be greater than or equal to {0} sec and less than or equal to {1} sec.
		/// </summary>
		internal static string InvalidMaxStalenessInterval => ResourceManager.GetString("InvalidMaxStalenessInterval", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to MaxStalenessPrefix should be greater than or equal to {0} and less than or equal to {1}.
		/// </summary>
		internal static string InvalidMaxStalenessPrefix => ResourceManager.GetString("InvalidMaxStalenessPrefix", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid supplied IpRangeFilter: {0}.
		/// </summary>
		internal static string InvalidIpRangeFilter => ResourceManager.GetString("InvalidIpRangeFilter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer throughput specified exceeded supported maximum throughput for Fixed size container. Please enter value less than {0}..
		/// </summary>
		internal static string InvalidNonPartitionedOfferThroughput => ResourceManager.GetString("InvalidNonPartitionedOfferThroughput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer IsAutoScaleEnabled specified is invalid. Please specify a boolean value..
		/// </summary>
		internal static string InvalidOfferIsAutoScaleEnabled => ResourceManager.GetString("InvalidOfferIsAutoScaleEnabled", resourceCulture);

		/// <summary>
		/// Looks up a localized string similar to The value of OfferAutoScaleMode specified is invalid.  Please specfy a valid auto scale mode.
		/// </summary>
		internal static string InvalidOfferAutoScaleMode => ResourceManager.GetString("InvalidOfferAutoScaleMode", resourceCulture);

		internal static string OfferAutoScaleNotSupportedForNonPartitionedCollections => ResourceManager.GetString("OfferAutoScaleNotSupportedForNonPartitionedCollections", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer IsRUPerMinuteThroughputEnabled specified is invalid. Please specify a boolean value..
		/// </summary>
		internal static string InvalidOfferIsRUPerMinuteThroughputEnabled => ResourceManager.GetString("InvalidOfferIsRUPerMinuteThroughputEnabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer throughput specified is invalid. Please enter valid positive integer..
		/// </summary>
		internal static string InvalidOfferThroughput => ResourceManager.GetString("InvalidOfferThroughput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to OfferType {0} specified in the request is invalid. Please refer to offer documentation and specify a valid offer type..
		/// </summary>
		internal static string InvalidOfferType => ResourceManager.GetString("InvalidOfferType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The required field Content is missing in Offer version v2..
		/// </summary>
		internal static string InvalidOfferV2Input => ResourceManager.GetString("InvalidOfferV2Input", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource {0} is invalid for adding owner resource record.
		/// </summary>
		internal static string InvalidOwnerResourceType => ResourceManager.GetString("InvalidOwnerResourceType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input PageSize {0} is invalid. Ensure to pass a valid page size which must be a positive integer or -1 for a dynamic page size..
		/// </summary>
		internal static string InvalidPageSize => ResourceManager.GetString("InvalidPageSize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Partition key {0} is invalid..
		/// </summary>
		internal static string InvalidPartitionKey => ResourceManager.GetString("InvalidPartitionKey", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to x-ms-documentdb-partitionkeyrangeid header contains invalid value '{0}'..
		/// </summary>
		internal static string InvalidPartitionKeyRangeIdHeader => ResourceManager.GetString("InvalidPartitionKeyRangeIdHeader", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The permission mode provided in the authorization token doesn't provide sufficient permissions..
		/// </summary>
		internal static string InvalidPermissionMode => ResourceManager.GetString("InvalidPermissionMode", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Command is not supported by backend.
		/// </summary>
		internal static string InvalidProxyCommand => ResourceManager.GetString("InvalidProxyCommand", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query '{0}' specified is either invalid or unsupported..
		/// </summary>
		internal static string InvalidQuery => ResourceManager.GetString("InvalidQuery", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' specified for the query '{1}' is invalid..
		/// </summary>
		internal static string InvalidQueryValue => ResourceManager.GetString("InvalidQueryValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Compared session tokens '{0}' and '{1}' has unexpected regions. .
		/// </summary>
		internal static string InvalidRegionsInSessionToken => ResourceManager.GetString("InvalidRegionsInSessionToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Strong consistency level cannot be specified with async replication. Either change the replication policy 'AsyncReplication' to false or relax the consistency level..
		/// </summary>
		internal static string InvalidReplicationAndConsistencyCombination => ResourceManager.GetString("InvalidReplicationAndConsistencyCombination", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to parse the value '{0}' as ResourceId..
		/// </summary>
		internal static string InvalidResourceID => ResourceManager.GetString("InvalidResourceID", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value for {0} should be greater than 0.
		/// </summary>
		internal static string InvalidResourceIdBatchSize => ResourceManager.GetString("InvalidResourceIdBatchSize", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource kind {0} is invalid.
		/// </summary>
		internal static string InvalidResourceKind => ResourceManager.GetString("InvalidResourceKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Requested ResourceType {0} passed as generic argument should be same as the one specified by ResourceType member {1}.
		/// </summary>
		internal static string InvalidResourceType => ResourceManager.GetString("InvalidResourceType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource Url path {0} is invalid..
		/// </summary>
		internal static string InvalidResourceUrlPath => ResourceManager.GetString("InvalidResourceUrlPath", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value '{0}' specified  for query '{1}' is invalid..
		/// </summary>
		internal static string InvalidResourceUrlQuery => ResourceManager.GetString("InvalidResourceUrlQuery", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input continuation token size limit {0} is invalid. Please pass in a valid continuation token size limit which must be a positive integer..
		/// </summary>
		internal static string InvalidResponseContinuationTokenLimit => ResourceManager.GetString("InvalidResponseContinuationTokenLimit", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Script request has invalid body..
		/// </summary>
		internal static string InvalidScriptResource => ResourceManager.GetString("InvalidScriptResource", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The session token provided '{0}' is invalid..
		/// </summary>
		internal static string InvalidSessionToken => ResourceManager.GetString("InvalidSessionToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The resource name can't end with space..
		/// </summary>
		internal static string InvalidSpaceEndingInResourceName => ResourceManager.GetString("InvalidSpaceEndingInResourceName", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Staleness Policy specified is invalid. Ensure both MaxPrefix and MaxStalenessIntervalInSeconds are both 0 or both not zero..
		/// </summary>
		internal static string InvalidStalenessPolicy => ResourceManager.GetString("InvalidStalenessPolicy", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Storage Service index {0} for media account {1} must be within byte range (inclusive)..
		/// </summary>
		internal static string InvalidStorageServiceMediaIndex => ResourceManager.GetString("InvalidStorageServiceMediaIndex", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot disable 'canEnableMultipleWriteLocations' flag once it has been enabled..
		/// </summary>
		internal static string InvalidSwitchOffCanEnableMultipleWriteLocations => ResourceManager.GetString("InvalidSwitchOffCanEnableMultipleWriteLocations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot enable 'canEnableMultipleWriteLocations' flag once account has been created with it disabled..
		/// </summary>
		internal static string InvalidSwitchOnCanEnableMultipleWriteLocations => ResourceManager.GetString("InvalidSwitchOnCanEnableMultipleWriteLocations", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Target for the request is invalid.
		/// </summary>
		internal static string InvalidTarget => ResourceManager.GetString("InvalidTarget", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The authorization token is not valid at the current time. Please create another token and retry (token start time: {0}, token expiry time: {1}, current server time: {2})..
		/// </summary>
		internal static string InvalidTokenTimeRange => ResourceManager.GetString("InvalidTokenTimeRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Request url is invalid..
		/// </summary>
		internal static string InvalidUrl => ResourceManager.GetString("InvalidUrl", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to System key can only access replica root.
		/// </summary>
		internal static string InvalidUseSystemKey => ResourceManager.GetString("InvalidUseSystemKey", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid version format for {0}. Input Version {1}.
		/// </summary>
		internal static string InvalidVersionFormat => ResourceManager.GetString("InvalidVersionFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to IpAddress {0} is blocked by Policy.
		/// </summary>
		internal static string IpAddressBlockedByPolicy => ResourceManager.GetString("IpAddressBlockedByPolicy", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Not allowed to force delete federation in this environment..
		/// </summary>
		internal static string IsForceDeleteFederationAllowed => ResourceManager.GetString("IsForceDeleteFederationAllowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tried to write a JSON end array (“]“) symbol without a matching array start symbol (“[“)..
		/// </summary>
		internal static string JsonArrayNotStarted => ResourceManager.GetString("JsonArrayNotStarted", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid escape character in JSON..
		/// </summary>
		internal static string JsonInvalidEscapedCharacter => ResourceManager.GetString("JsonInvalidEscapedCharacter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid number in JSON..
		/// </summary>
		internal static string JsonInvalidNumber => ResourceManager.GetString("JsonInvalidNumber", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid parameter in JSON..
		/// </summary>
		internal static string JsonInvalidParameter => ResourceManager.GetString("JsonInvalidParameter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid string character in JSON..
		/// </summary>
		internal static string JsonInvalidStringCharacter => ResourceManager.GetString("JsonInvalidStringCharacter", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered an element that is not a valid JSON value (false / null / true / object / array / number / string).
		/// </summary>
		internal static string JsonInvalidToken => ResourceManager.GetString("JsonInvalidToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid Unicode escape sequence in JSON..
		/// </summary>
		internal static string JsonInvalidUnicodeEscape => ResourceManager.GetString("JsonInvalidUnicodeEscape", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Exceeded the maximum level of nesting for JSON..
		/// </summary>
		internal static string JsonMaxNestingExceeded => ResourceManager.GetString("JsonMaxNestingExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing a closing quote (") in JSON..
		/// </summary>
		internal static string JsonMissingClosingQuote => ResourceManager.GetString("JsonMissingClosingQuote", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing an end array ("]") symbol in JSON..
		/// </summary>
		internal static string JsonMissingEndArray => ResourceManager.GetString("JsonMissingEndArray", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing an end object ("}") symbol in JSON..
		/// </summary>
		internal static string JsonMissingEndObject => ResourceManager.GetString("JsonMissingEndObject", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing a name separator (":") in JSON..
		/// </summary>
		internal static string JsonMissingNameSeparator => ResourceManager.GetString("JsonMissingNameSeparator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Missing a JSON property..
		/// </summary>
		internal static string JsonMissingProperty => ResourceManager.GetString("JsonMissingProperty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a JSON property name without a corresponding property value.
		/// </summary>
		internal static string JsonNotComplete => ResourceManager.GetString("JsonNotComplete", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a value that was not a JSON field name..
		/// </summary>
		internal static string JsonNotFieldnameToken => ResourceManager.GetString("JsonNotFieldnameToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a value that was not a JSON number..
		/// </summary>
		internal static string JsonNotNumberToken => ResourceManager.GetString("JsonNotNumberToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a value that was not a JSON string..
		/// </summary>
		internal static string JsonNotStringToken => ResourceManager.GetString("JsonNotStringToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a number that exceeded the range for JSON numbers..
		/// </summary>
		internal static string JsonNumberOutOfRange => ResourceManager.GetString("JsonNumberOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a number that was too long for a JSON number..
		/// </summary>
		internal static string JsonNumberTooLong => ResourceManager.GetString("JsonNumberTooLong", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Tried to write a JSON object end symbol ("}") without first opening with a JSON object start symbol ("{")..
		/// </summary>
		internal static string JsonObjectNotStarted => ResourceManager.GetString("JsonObjectNotStarted", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered a JSON property name after another JSON property name..
		/// </summary>
		internal static string JsonPropertyAlreadyAdded => ResourceManager.GetString("JsonPropertyAlreadyAdded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Either a JSON property array or object was not started..
		/// </summary>
		internal static string JsonPropertyArrayOrObjectNotStarted => ResourceManager.GetString("JsonPropertyArrayOrObjectNotStarted", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Read a JSON end array ("]") symbol without a matching JSON start array symbol ("[")..
		/// </summary>
		internal static string JsonUnexpectedEndArray => ResourceManager.GetString("JsonUnexpectedEndArray", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Read a JSON end object ("}") symbol without a matching JSON start object symbol ("{")..
		/// </summary>
		internal static string JsonUnexpectedEndObject => ResourceManager.GetString("JsonUnexpectedEndObject", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Read a JSON name separator (":") symbol without a corresponding field name..
		/// </summary>
		internal static string JsonUnexpectedNameSeparator => ResourceManager.GetString("JsonUnexpectedNameSeparator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Encountered an unexpected JSON token..
		/// </summary>
		internal static string JsonUnexpectedToken => ResourceManager.GetString("JsonUnexpectedToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Read a JSON name separator (",") symbol without a preceding JSON value..
		/// </summary>
		internal static string JsonUnexpectedValueSeparator => ResourceManager.GetString("JsonUnexpectedValueSeparator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The resource that is being accessed is locked..
		/// </summary>
		internal static string Locked => ResourceManager.GetString("Locked", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Current maximum throughput per collection is {0}. Please contact Azure support to increase it..
		/// </summary>
		internal static string MaximumRULimitExceeded => ResourceManager.GetString("MaximumRULimitExceeded", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot find messageId header.
		/// </summary>
		internal static string MessageIdHeaderMissing => ResourceManager.GetString("MessageIdHeaderMissing", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The requested verb is not supported..
		/// </summary>
		internal static string MethodNotAllowed => ResourceManager.GetString("MethodNotAllowed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The input authorization token can't serve the request. Please check that the expected payload is built as per the protocol, and check the key being used. Server used the following payload to sign: '{0}'.
		/// </summary>
		internal static string MismatchToken => ResourceManager.GetString("MismatchToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Required Header authorization is missing. Ensure a valid Authorization token is passed..
		/// </summary>
		internal static string MissingAuthHeader => ResourceManager.GetString("MissingAuthHeader", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Authorization token mandates Date headers. Please pass in RFC 1123 style date format..
		/// </summary>
		internal static string MissingDateForAuthorization => ResourceManager.GetString("MissingDateForAuthorization", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKey value must be supplied for this operation..
		/// </summary>
		internal static string MissingPartitionKeyValue => ResourceManager.GetString("MissingPartitionKeyValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Required property {0} is not specified in the request..
		/// </summary>
		internal static string MissingProperty => ResourceManager.GetString("MissingProperty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Required header '{0}' is not specified in the request..
		/// </summary>
		internal static string MissingRequiredHeader => ResourceManager.GetString("MissingRequiredHeader", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Required query variable '{0}' is not specified in the request..
		/// </summary>
		internal static string MissingRequiredQuery => ResourceManager.GetString("MissingRequiredQuery", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This account already has one backup interval capability..
		/// </summary>
		internal static string MoreThanOneBackupIntervalCapability => ResourceManager.GetString("MoreThanOneBackupIntervalCapability", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to This account already has one backup retention capability..
		/// </summary>
		internal static string MoreThanOneBackupRetentionCapability => ResourceManager.GetString("MoreThanOneBackupRetentionCapability", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Atleast single region must be specified in PreferredLocation list when automatic failover is disabled..
		/// </summary>
		internal static string MustHaveNonZeroPreferredRegionWhenAutomaticFailoverDisabled => ResourceManager.GetString("MustHaveNonZeroPreferredRegionWhenAutomaticFailoverDisabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to NamingProperty {0} not found.
		/// </summary>
		internal static string NamingPropertyNotFound => ResourceManager.GetString("NamingPropertyNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property '{0}' integer value must be greater than or equal to zero..
		/// </summary>
		internal static string NegativeInteger => ResourceManager.GetString("NegativeInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to &lt;?xml version="1.0" encoding="utf-8"?&gt;
		///             &lt;xs:schema id="DataCenterRegions" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"&gt;
		///
		///  &lt;xs:element name="DataCenterRegions" msdata:IsDataSet="true" msdata:UseCurrentLocale="true"&gt;
		///    &lt;xs:complexType&gt;
		///      &lt;xs:sequence&gt;
		///        &lt;xs:element name="Network" type="Network" minOccurs="1" maxOccurs="unbounded" /&gt;
		///        &lt;xs:element name="MeteringTier" type="MeteringTier" minOccurs="1" maxOccurs="unbounded" /&gt; [rest of string was truncated]";.
		/// </summary>
		internal static string networks_xsd => ResourceManager.GetString("networks.xsd", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to No graft point.
		/// </summary>
		internal static string NoGraftPoint => ResourceManager.GetString("NoGraftPoint", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Entity with the specified id does not exist in the system..
		/// </summary>
		internal static string NotFound => ResourceManager.GetString("NotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Offer replace request conflicted..
		/// </summary>
		internal static string OfferReplaceTopologyConflict => ResourceManager.GetString("OfferReplaceTopologyConflict", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot replace an offer with version {0} with version {1}.
		/// </summary>
		internal static string OfferReplaceWithSpecifiedVersionsNotSupported => ResourceManager.GetString("OfferReplaceWithSpecifiedVersionsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Offer type and throughput cannot both be specified..
		/// </summary>
		internal static string OfferTypeAndThroughputCannotBeSpecifiedBoth => ResourceManager.GetString("OfferTypeAndThroughputCannotBeSpecifiedBoth", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Requested Operation Status = {0} is invalid..
		/// </summary>
		internal static string OperationRequestedStatusIsInvalid => ResourceManager.GetString("OperationRequestedStatusIsInvalid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Selected partition is full, please try insert in different partition..
		/// </summary>
		internal static string PartitionIsFull => ResourceManager.GetString("PartitionIsFull", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Either PartitionKey or EffectivePartitionKey are expected..
		/// </summary>
		internal static string PartitionKeyAndEffectivePartitionKeyBothSpecified => ResourceManager.GetString("PartitionKeyAndEffectivePartitionKeyBothSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKey and PartitionKeyRangeId cannot be specified at the same time in ChangeFeedOptions..
		/// </summary>
		internal static string PartitionKeyAndPartitionKeyRangeRangeIdBothSpecified => ResourceManager.GetString("PartitionKeyAndPartitionKeyRangeRangeIdBothSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Partition key provided either doesn't correspond to definition in the collection or doesn't match partition key field values specified in the document..
		/// </summary>
		internal static string PartitionKeyMismatch => ResourceManager.GetString("PartitionKeyMismatch", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKeyRangeId is absent in the context..
		/// </summary>
		internal static string PartitionKeyRangeIdAbsentInContext => ResourceManager.GetString("PartitionKeyRangeIdAbsentInContext", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to For partitioned collection, either ChangeFeedOptions.PartitionKeyRangeId or ChangeFeedOptions.PartitionKey must be specified..
		/// </summary>
		internal static string PartitionKeyRangeIdOrPartitionKeyMustBeSpecified => ResourceManager.GetString("PartitionKeyRangeIdOrPartitionKeyMustBeSpecified", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKeyRange with id '{0}' in collection '{1}' doesn't exist..
		/// </summary>
		internal static string PartitionKeyRangeNotFound => ResourceManager.GetString("PartitionKeyRangeNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property '{0}' integer value must be greater than zero..
		/// </summary>
		internal static string PositiveInteger => ResourceManager.GetString("PositiveInteger", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Operation cannot be performed because one of the specified precondition is not met..
		/// </summary>
		internal static string PreconditionFailed => ResourceManager.GetString("PreconditionFailed", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to resolve primary endpoint for partition {0} for service {1}..
		/// </summary>
		internal static string PrimaryNotFound => ResourceManager.GetString("PrimaryNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property {0} can not be assigned to null..
		/// </summary>
		internal static string PropertyCannotBeNull => ResourceManager.GetString("PropertyCannotBeNull", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Property '{0}' is not found in the document..
		/// </summary>
		internal static string PropertyNotFound => ResourceManager.GetString("PropertyNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Reached the pre-approved storage limit for the database account. Please contact Azure support to increase this limit..
		/// </summary>
		internal static string ProvisionLimit => ResourceManager.GetString("ProvisionLimit", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Read Quorum size of {0} is not met for the request..
		/// </summary>
		internal static string ReadQuorumNotMet => ResourceManager.GetString("ReadQuorumNotMet", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The read session is not available for the input session token..
		/// </summary>
		internal static string ReadSessionNotAvailable => ResourceManager.GetString("ReadSessionNotAvailable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to read the server response..
		/// </summary>
		internal static string ReceiveFailedTransportError => ResourceManager.GetString("ReceiveFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The remote system closed the connection..
		/// </summary>
		internal static string ReceiveStreamClosedTransportError => ResourceManager.GetString("ReceiveStreamClosedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The request timed out while waiting for a server response..
		/// </summary>
		internal static string ReceiveTimeoutTransportError => ResourceManager.GetString("ReceiveTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot remove write region for account {0}. Please issue delete on the account to remove write region.
		/// </summary>
		internal static string RemoveWriteRegionNotSupported => ResourceManager.GetString("RemoveWriteRegionNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Replica at index '{0}' is currently unavailable..
		/// </summary>
		internal static string ReplicaAtIndexNotAvailable => ResourceManager.GetString("ReplicaAtIndexNotAvailable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Consistency Level '{0}'  requested via header '{1}' is not supported by this service endpoint. Please contact the service administrator..
		/// </summary>
		internal static string RequestConsistencyLevelNotSupported => ResourceManager.GetString("RequestConsistencyLevelNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The size of the response exceeded the maximum allowed size, limit the  response size by specifying smaller value for '{0}' header..
		/// </summary>
		internal static string RequestEntityTooLarge => ResourceManager.GetString("RequestEntityTooLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Request timed out..
		/// </summary>
		internal static string RequestTimeout => ResourceManager.GetString("RequestTimeout", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The request timed out. See the inner exception for details..
		/// </summary>
		internal static string RequestTimeoutTransportError => ResourceManager.GetString("RequestTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The size of the request exceeded the maximum allowed size..
		/// </summary>
		internal static string RequestTooLarge => ResourceManager.GetString("RequestTooLarge", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ResourceId {0} of type {1} is not a valid resource Id..
		/// </summary>
		internal static string ResourceIdNotValid => ResourceManager.GetString("ResourceIdNotValid", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ResourceIdPolicy {0} is not supported.
		/// </summary>
		internal static string ResourceIdPolicyNotSupported => ResourceManager.GetString("ResourceIdPolicyNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource type {0} is not supported by ResourceIdPartitioner.
		/// </summary>
		internal static string ResourceTypeNotSupported => ResourceManager.GetString("ResourceTypeNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Retry the request..
		/// </summary>
		internal static string RetryWith => ResourceManager.GetString("RetryWith", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Changing id of a script in collections with multiple partitions is not supported..
		/// </summary>
		internal static string ScriptRenameInMultiplePartitionsIsNotSupported => ResourceManager.GetString("ScriptRenameInMultiplePartitionsIsNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to resolve secondary endpoint for partition {0} for service {1}..
		/// </summary>
		internal static string SecondariesNotFound => ResourceManager.GetString("SecondariesNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sending the request failed..
		/// </summary>
		internal static string SendFailedTransportError => ResourceManager.GetString("SendFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Acquiring the send stream lock timed out..
		/// </summary>
		internal static string SendLockTimeoutTransportError => ResourceManager.GetString("SendLockTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Sending the request timed out..
		/// </summary>
		internal static string SendTimeoutTransportError => ResourceManager.GetString("SendTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The response body length is too large. Body length: {0} bytes. Connection: {1}.
		/// </summary>
		internal static string ServerResponseBodyTooLargeError => ResourceManager.GetString("ServerResponseBodyTooLargeError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The response total header length is too large. Header length: {0} bytes. Connection: {1}.
		/// </summary>
		internal static string ServerResponseHeaderTooLargeError => ResourceManager.GetString("ServerResponseHeaderTooLargeError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Invalid response total header length. Expected {0} bytes. Received {1} bytes. Connection: {2}.
		/// </summary>
		internal static string ServerResponseInvalidHeaderLengthError => ResourceManager.GetString("ServerResponseInvalidHeaderLengthError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The transport request ID is missing from the server response..
		/// </summary>
		internal static string ServerResponseTransportRequestIdMissingError => ResourceManager.GetString("ServerResponseTransportRequestIdMissingError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Service at index {0} not found..
		/// </summary>
		internal static string ServiceNotFound => ResourceManager.GetString("ServiceNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Service reserved bits can not be more than 24. Otherwise it overlaps with the collection/user multiplexing bit.
		/// </summary>
		internal static string ServiceReservedBitsOutOfRange => ResourceManager.GetString("ServiceReservedBitsOutOfRange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Service is currently unavailable..
		/// </summary>
		internal static string ServiceUnavailable => ResourceManager.GetString("ServiceUnavailable", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Could not find service hosting DocumentCollection with ResourceId {0}.
		/// </summary>
		internal static string ServiceWithResourceIdNotFound => ResourceManager.GetString("ServiceWithResourceIdNotFound", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Bounding box must have an even number of coordinates and more than 3..
		/// </summary>
		internal static string SpatialBoundingBoxInvalidCoordinates => ResourceManager.GetString("SpatialBoundingBoxInvalidCoordinates", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Spatial operations can be used in Linq expressions only and are evaluated in Azure CosmosDB server..
		/// </summary>
		internal static string SpatialExtensionMethodsNotImplemented => ResourceManager.GetString("SpatialExtensionMethodsNotImplemented", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Coordinate Reference System specified in GeoJSON is invalid..
		/// </summary>
		internal static string SpatialFailedToDeserializeCrs => ResourceManager.GetString("SpatialFailedToDeserializeCrs", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to deserialize Geometry object because 'type' property is either absent or has invalid value..
		/// </summary>
		internal static string SpatialInvalidGeometryType => ResourceManager.GetString("SpatialInvalidGeometryType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Spatial position must have at least two coordinates..
		/// </summary>
		internal static string SpatialInvalidPosition => ResourceManager.GetString("SpatialInvalidPosition", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SSL negotiation failed..
		/// </summary>
		internal static string SslNegotiationFailedTransportError => ResourceManager.GetString("SslNegotiationFailedTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to SSL negotiation timed out..
		/// </summary>
		internal static string SslNegotiationTimeoutTransportError => ResourceManager.GetString("SslNegotiationTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Star and slash should have no arguments..
		/// </summary>
		internal static string StarSlashArgumentError => ResourceManager.GetString("StarSlashArgumentError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to String agument {0} is null or empty.
		/// </summary>
		internal static string StringArgumentNullOrEmpty => ResourceManager.GetString("StringArgumentNullOrEmpty", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKey has fewer components than defined the collection resource..
		/// </summary>
		internal static string TooFewPartitionKeyComponents => ResourceManager.GetString("TooFewPartitionKeyComponents", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKey has more components than defined the collection resource..
		/// </summary>
		internal static string TooManyPartitionKeyComponents => ResourceManager.GetString("TooManyPartitionKeyComponents", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The request rate is too large. Please retry after sometime..
		/// </summary>
		internal static string TooManyRequests => ResourceManager.GetString("TooManyRequests", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to A client transport error occurred: {0}.
		/// </summary>
		internal static string TransportExceptionMessage => ResourceManager.GetString("TransportExceptionMessage", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The transport protocol negotiation timed out. See the inner exception for details..
		/// </summary>
		internal static string TransportNegotiationTimeoutTransportError => ResourceManager.GetString("TransportNegotiationTimeoutTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot deserialize PartitionKey value '{0}'.
		/// </summary>
		internal static string UnableToDeserializePartitionKeyValue => ResourceManager.GetString("UnableToDeserializePartitionKeyValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to find free connection.
		/// </summary>
		internal static string UnableToFindFreeConnection => ResourceManager.GetString("UnableToFindFreeConnection", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unable to authenticate the request. The request requires valid user authentication..
		/// </summary>
		internal static string Unauthorized => ResourceManager.GetString("Unauthorized", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unauthorized Offer Replace Request..
		/// </summary>
		internal static string UnauthorizedOfferReplaceRequest => ResourceManager.GetString("UnauthorizedOfferReplaceRequest", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unauthorized Auto-Scale Request..
		/// </summary>
		internal static string UnauthorizedRequestForAutoScale => ResourceManager.GetString("UnauthorizedRequestForAutoScale", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Operation not permitted as consistency level is set to {0}. Expected {1}..
		/// </summary>
		internal static string UnexpectedConsistencyLevel => ResourceManager.GetString("UnexpectedConsistencyLevel", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected JsonSerializationFormat: {0}.
		/// </summary>
		internal static string UnexpectedJsonSerializationFormat => ResourceManager.GetString("UnexpectedJsonSerializationFormat", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Failed to register JsonTokenType: {0}.
		/// </summary>
		internal static string UnexpectedJsonTokenType => ResourceManager.GetString("UnexpectedJsonTokenType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected offer version {0} from store..
		/// </summary>
		internal static string UnexpectedOfferVersion => ResourceManager.GetString("UnexpectedOfferVersion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected operation type {0} for routing requests for multiple partitions..
		/// </summary>
		internal static string UnexpectedOperationTypeForRoutingRequest => ResourceManager.GetString("UnexpectedOperationTypeForRoutingRequest", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unexpected operator {0} ..
		/// </summary>
		internal static string UnexpectedOperator => ResourceManager.GetString("UnexpectedOperator", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to PartitionKeyRangeId is not expected..
		/// </summary>
		internal static string UnexpectedPartitionKeyRangeId => ResourceManager.GetString("UnexpectedPartitionKeyRangeId", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to ResourceType {0} is unexpected..
		/// </summary>
		internal static string UnexpectedResourceType => ResourceManager.GetString("UnexpectedResourceType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource kind {0} is unknown.
		/// </summary>
		internal static string UnknownResourceKind => ResourceManager.GetString("UnknownResourceKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Resource type {0} is unknown.
		/// </summary>
		internal static string UnknownResourceType => ResourceManager.GetString("UnknownResourceType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to An unknown client transport error has occurred..
		/// </summary>
		internal static string UnknownTransportError => ResourceManager.GetString("UnknownTransportError", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Distict query requires a matching order by in order to return a continuation token.
		///             If you would like to serve this query through continuation tokens, then please rewrite the query in the form 'SELECT DISTINCT VALUE c.blah FROM c ORDER BY c.blah' and please make sure that there is a range index on 'c.blah'..
		/// </summary>
		internal static string UnorderedDistinctQueryContinuationToken => ResourceManager.GetString("UnorderedDistinctQueryContinuationToken", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot execute cross partition order-by queries on mix types. Consider using IS_STRING/IS_NUMBER to get around this exception. Expect type: {0}. Actual type: {1}. Item value: {2}..
		/// </summary>
		internal static string UnsupportedCrossPartitionOrderByQueryOnMixedTypes => ResourceManager.GetString("UnsupportedCrossPartitionOrderByQueryOnMixedTypes", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The provided cross partition query can not be directly served by the gateway. This is a first chance (internal) exception that all newer clients will know how to handle gracefully. This exception is traced, but unless you see it bubble up as an exception (which only happens on older SDK clients), then you can safely ignore this message..
		/// </summary>
		internal static string UnsupportedCrossPartitionQuery => ResourceManager.GetString("UnsupportedCrossPartitionQuery", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cross partition query with aggregate functions is not supported..
		/// </summary>
		internal static string UnsupportedCrossPartitionQueryWithAggregate => ResourceManager.GetString("UnsupportedCrossPartitionQueryWithAggregate", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unsupported entity type {0}.
		/// </summary>
		internal static string UnsupportedEntityType => ResourceManager.GetString("UnsupportedEntityType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Indexing Hints are not supported in this deployment. .
		/// </summary>
		internal static string UnsupportedHints => ResourceManager.GetString("UnsupportedHints", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Key type {0} is unsupported.
		/// </summary>
		internal static string UnsupportedKeyType => ResourceManager.GetString("UnsupportedKeyType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer throughput specified is invalid. Please specify a value between {0} and {1} inclusive in increments of {2}. Please contact https://azure.microsoft.com/support to request limit increases beyond {1} RU/s..
		/// </summary>
		internal static string UnSupportedOfferThroughput => ResourceManager.GetString("UnSupportedOfferThroughput", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The value of offer throughput specified is invalid. Please specify a value between {0} and {1}, or between {2} and {3} inclusive in increments of {4}. Please contact https://azure.microsoft.com/support to request limit increases beyond {3} RU/s..
		/// </summary>
		internal static string UnSupportedOfferThroughputWithTwoRanges => ResourceManager.GetString("UnSupportedOfferThroughputWithTwoRanges", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Offer type is not supported with Offer version 'V2' and above..
		/// </summary>
		internal static string UnsupportedOfferTypeWithV2Offer => ResourceManager.GetString("UnsupportedOfferTypeWithV2Offer", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The supplied offer version {0} is not supported. Please specify either a blank version, 'V1' or 'V2'..
		/// </summary>
		internal static string UnsupportedOfferVersion => ResourceManager.GetString("UnsupportedOfferVersion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unsupported PartitionKey value component '{0}'. Numeric, string, bool, null, Undefined are the only supported types..
		/// </summary>
		internal static string UnsupportedPartitionKeyComponentValue => ResourceManager.GetString("UnsupportedPartitionKeyComponentValue", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Program requires to run in 64 bit for elastic query feature to work. Please switch your program to 64 bit or use Gateway connectivity mode..
		/// </summary>
		internal static string UnsupportedProgram => ResourceManager.GetString("UnsupportedProgram", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Value '{0}' specified for protocol is unsupported..
		/// </summary>
		internal static string UnsupportedProtocol => ResourceManager.GetString("UnsupportedProtocol", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Query that expects full results from aggregate functions is not supported..
		/// </summary>
		internal static string UnsupportedQueryWithFullResultAggregate => ResourceManager.GetString("UnsupportedQueryWithFullResultAggregate", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The requested region '{0}' is not supported..
		/// </summary>
		internal static string UnsupportedRegion => ResourceManager.GetString("UnsupportedRegion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The requested rollback kind '{0}' is not supported..
		/// </summary>
		internal static string UnsupportedRollbackKind => ResourceManager.GetString("UnsupportedRollbackKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Changing Root Indexing Policy is not supported in this deployment..
		/// </summary>
		internal static string UnsupportedRootPolicyChange => ResourceManager.GetString("UnsupportedRootPolicyChange", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Federation system key kind {0} is invalid.
		/// </summary>
		internal static string UnsupportedSystemKeyKind => ResourceManager.GetString("UnsupportedSystemKeyKind", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Unsupported token type {0}.
		/// </summary>
		internal static string UnsupportedTokenType => ResourceManager.GetString("UnsupportedTokenType", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to The Offer Version 'V1' is not supported since the associated collection is already a partitioned collection. Please use offer version 'V2'..
		/// </summary>
		internal static string UnsupportedV1OfferVersion => ResourceManager.GetString("UnsupportedV1OfferVersion", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Upserts for scripts in collections with multiple partitions are not supported..
		/// </summary>
		internal static string UpsertsForScriptsWithMultiplePartitionsAreNotSupported => ResourceManager.GetString("UpsertsForScriptsWithMultiplePartitionsAreNotSupported", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot offline write region when automatic failover is not enabled.
		/// </summary>
		internal static string WriteRegionAutomaticFailoverNotEnabled => ResourceManager.GetString("WriteRegionAutomaticFailoverNotEnabled", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Cannot add additional regions, since database account provision failed..
		/// </summary>
		internal static string WriteRegionDoesNotExist => ResourceManager.GetString("WriteRegionDoesNotExist", resourceCulture);

		/// <summary>
		///   Looks up a localized string similar to Zone Redundant Accounts are not supported in {0} Location yet. Please try other locations..
		/// </summary>
		internal static string ZoneRedundantAccountsNotSupportedInLocation => ResourceManager.GetString("ZoneRedundantAccountsNotSupportedInLocation", resourceCulture);

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal RMResources()
		{
		}
	}
}
