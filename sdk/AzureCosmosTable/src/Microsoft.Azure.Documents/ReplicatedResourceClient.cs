using Microsoft.Azure.Documents.Client;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// ReplicatedResourceClient uses the ConsistencyReader to make requests to backend
	/// </summary>
	internal sealed class ReplicatedResourceClient
	{
		private const string EnableGlobalStrongConfigurationName = "EnableGlobalStrong";

		private const int GoneAndRetryWithRetryTimeoutInSeconds = 30;

		private const int StrongGoneAndRetryWithRetryTimeoutInSeconds = 60;

		private readonly TimeSpan minBackoffForFallingBackToOtherRegions = TimeSpan.FromSeconds(1.0);

		private readonly AddressSelector addressSelector;

		private readonly IAddressResolver addressResolver;

		private readonly ConsistencyReader consistencyReader;

		private readonly ConsistencyWriter consistencyWriter;

		private readonly Protocol protocol;

		private readonly TransportClient transportClient;

		private readonly IServiceConfigurationReader serviceConfigReader;

		private readonly bool enableReadRequestsFallback;

		private readonly bool useMultipleWriteLocations;

		private readonly bool detectClientConnectivityIssues;

		private static readonly Lazy<bool> enableGlobalStrong = new Lazy<bool>(() => true);

		public string LastReadAddress
		{
			get
			{
				return consistencyReader.LastReadAddress;
			}
			set
			{
				consistencyReader.LastReadAddress = value;
			}
		}

		public string LastWriteAddress => consistencyWriter.LastWriteAddress;

		public bool ForceAddressRefresh
		{
			get;
			set;
		}

		public ReplicatedResourceClient(IAddressResolver addressResolver, ISessionContainer sessionContainer, Protocol protocol, TransportClient transportClient, IServiceConfigurationReader serviceConfigReader, IAuthorizationTokenProvider authorizationTokenProvider, bool enableReadRequestsFallback, bool useMultipleWriteLocations, bool detectClientConnectivityIssues)
		{
			this.addressResolver = addressResolver;
			addressSelector = new AddressSelector(addressResolver, protocol);
			if (protocol != 0 && protocol != Protocol.Tcp)
			{
				throw new ArgumentOutOfRangeException("protocol");
			}
			this.protocol = protocol;
			this.transportClient = transportClient;
			this.serviceConfigReader = serviceConfigReader;
			consistencyReader = new ConsistencyReader(addressSelector, sessionContainer, transportClient, serviceConfigReader, authorizationTokenProvider);
			consistencyWriter = new ConsistencyWriter(addressSelector, sessionContainer, transportClient, serviceConfigReader, authorizationTokenProvider, useMultipleWriteLocations);
			this.enableReadRequestsFallback = enableReadRequestsFallback;
			this.useMultipleWriteLocations = useMultipleWriteLocations;
			this.detectClientConnectivityIssues = detectClientConnectivityIssues;
		}

		public Task<StoreResponse> InvokeAsync(DocumentServiceRequest request, Func<DocumentServiceRequest, Task> prepareRequestAsyncDelegate = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			Func<GoneAndRetryRequestRetryPolicyContext, Task<StoreResponse>> executeAsync = async delegate(GoneAndRetryRequestRetryPolicyContext contextArguments)
			{
				if (prepareRequestAsyncDelegate != null)
				{
					await prepareRequestAsyncDelegate(request);
				}
				request.Headers["x-ms-client-retry-attempt-count"] = contextArguments.ClientRetryCount.ToString(CultureInfo.InvariantCulture);
				request.Headers["x-ms-remaining-time-in-ms-on-client"] = contextArguments.RemainingTimeInMsOnClientRequest.TotalMilliseconds.ToString();
				return await InvokeAsync(request, new TimeoutHelper(contextArguments.RemainingTimeInMsOnClientRequest, cancellationToken), contextArguments.IsInRetry, contextArguments.ForceRefresh || ForceAddressRefresh, cancellationToken);
			};
			Func<GoneAndRetryRequestRetryPolicyContext, Task<StoreResponse>> inBackoffAlternateCallbackMethod = null;
			if ((request.OperationType.IsReadOperation() && enableReadRequestsFallback) || CheckWriteRetryable(request))
			{
				ClientSideRequestStatistics sharedStatistics = null;
				if (request.RequestContext.ClientRequestStatistics == null)
				{
					sharedStatistics = new ClientSideRequestStatistics();
					request.RequestContext.ClientRequestStatistics = sharedStatistics;
				}
				else
				{
					sharedStatistics = request.RequestContext.ClientRequestStatistics;
				}
				DocumentServiceRequest freshRequest = request.Clone();
				inBackoffAlternateCallbackMethod = async delegate(GoneAndRetryRequestRetryPolicyContext retryContext)
				{
					DocumentServiceRequest requestClone = freshRequest.Clone();
					requestClone.RequestContext.ClientRequestStatistics = sharedStatistics;
					if (prepareRequestAsyncDelegate != null)
					{
						await prepareRequestAsyncDelegate(requestClone);
					}
					DefaultTrace.TraceInformation("Executing inBackoffAlternateCallbackMethod on regionIndex {0}", retryContext.RegionRerouteAttemptCount);
					requestClone.RequestContext.RouteToLocation(retryContext.RegionRerouteAttemptCount, usePreferredLocations: true);
					return await RequestRetryUtility.ProcessRequestAsync((GoneOnlyRequestRetryPolicyContext innerRetryContext) => InvokeAsync(requestClone, new TimeoutHelper(innerRetryContext.RemainingTimeInMsOnClientRequest, cancellationToken), innerRetryContext.IsInRetry, innerRetryContext.ForceRefresh, cancellationToken), () => requestClone, new GoneOnlyRequestRetryPolicy<StoreResponse>(retryContext.TimeoutForInBackoffRetryPolicy), cancellationToken);
				};
			}
			int value = (serviceConfigReader.DefaultConsistencyLevel == ConsistencyLevel.Strong) ? 60 : 30;
			return RequestRetryUtility.ProcessRequestAsync(executeAsync, () => request, new GoneAndRetryWithRequestRetryPolicy<StoreResponse>(value, minBackoffForFallingBackToOtherRegions, detectClientConnectivityIssues), inBackoffAlternateCallbackMethod, minBackoffForFallingBackToOtherRegions, cancellationToken);
		}

		private Task<StoreResponse> InvokeAsync(DocumentServiceRequest request, TimeoutHelper timeout, bool isInRetry, bool forceRefresh, CancellationToken cancellationToken)
		{
			if (request.OperationType == OperationType.ExecuteJavaScript)
			{
				if (request.IsReadOnlyScript)
				{
					return consistencyReader.ReadAsync(request, timeout, isInRetry, forceRefresh);
				}
				return consistencyWriter.WriteAsync(request, timeout, forceRefresh, cancellationToken);
			}
			if (request.OperationType.IsWriteOperation())
			{
				return consistencyWriter.WriteAsync(request, timeout, forceRefresh, cancellationToken);
			}
			if (request.OperationType.IsReadOperation())
			{
				return consistencyReader.ReadAsync(request, timeout, isInRetry, forceRefresh);
			}
			if (request.OperationType == OperationType.Throttle || request.OperationType == OperationType.PreCreateValidation || request.OperationType == OperationType.OfferPreGrowValidation)
			{
				return HandleThrottlePreCreateOrOfferPreGrowAsync(request, forceRefresh);
			}
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unexpected operation type {0}", request.OperationType));
		}

		private async Task<StoreResponse> HandleThrottlePreCreateOrOfferPreGrowAsync(DocumentServiceRequest request, bool forceRefresh)
		{
			DocumentServiceRequest request2 = DocumentServiceRequest.Create(OperationType.Create, ResourceType.Database, request.RequestAuthorizationTokenType);
			Uri physicalAddress = await ResolvePrimaryUriAsync(request2, forceRefresh);
			return await transportClient.InvokeResourceOperationAsync(physicalAddress, request);
		}

		private async Task<Uri> ResolvePrimaryUriAsync(DocumentServiceRequest request, bool forceAddressRefresh)
		{
			return (await addressResolver.ResolveAsync(request, forceAddressRefresh, CancellationToken.None)).GetPrimaryUri(request, protocol);
		}

		private bool CheckWriteRetryable(DocumentServiceRequest request)
		{
			bool result = false;
			if (useMultipleWriteLocations && ((request.OperationType == OperationType.Execute && request.ResourceType == ResourceType.StoredProcedure) || (request.OperationType.IsWriteOperation() && request.ResourceType == ResourceType.Document)))
			{
				result = true;
			}
			return result;
		}

		internal static bool IsGlobalStrongEnabled()
		{
			return true;
		}

		internal static bool IsReadingFromMaster(ResourceType resourceType, OperationType operationType)
		{
			if (resourceType == ResourceType.Offer || resourceType == ResourceType.Database || resourceType == ResourceType.User || resourceType == ResourceType.UserDefinedType || resourceType == ResourceType.Permission || resourceType == ResourceType.DatabaseAccount || resourceType == ResourceType.Topology || (resourceType == ResourceType.PartitionKeyRange && operationType != OperationType.GetSplitPoint && operationType != OperationType.AbortSplit) || (resourceType == ResourceType.Collection && (operationType == OperationType.ReadFeed || operationType == OperationType.Query || operationType == OperationType.SqlQuery)))
			{
				return true;
			}
			return false;
		}

		internal static bool IsMasterResource(ResourceType resourceType)
		{
			if (resourceType == ResourceType.Offer || resourceType == ResourceType.Database || resourceType == ResourceType.User || resourceType == ResourceType.UserDefinedType || resourceType == ResourceType.Permission || resourceType == ResourceType.Topology || resourceType == ResourceType.DatabaseAccount || resourceType == ResourceType.PartitionKeyRange || resourceType == ResourceType.Collection)
			{
				return true;
			}
			return false;
		}
	}
}
