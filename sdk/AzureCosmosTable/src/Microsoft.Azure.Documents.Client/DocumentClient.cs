using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Common;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents.Query;
using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Provides a client-side logical representation for the Azure Cosmos DB service.
	/// This client is used to configure and execute requests against the service.
	/// </summary>
	/// <threadSafety>
	/// This type is thread safe.
	/// </threadSafety>
	/// <remarks>
	/// The service client that encapsulates the endpoint and credentials and connection policy used to access the Azure Cosmos DB service.
	/// It is recommended to cache and reuse this instance within your application rather than creating a new instance for every operation.
	///
	/// <para>
	/// When your app uses DocumentClient, you should call its IDisposable.Dispose implementation when you are finished using it.
	/// Depending on your programming technique, you can do this in one of two ways:
	/// </para>
	///
	/// <para>
	/// 1. By using a language construct such as the using statement in C#.
	/// The using statement is actually a syntactic convenience.
	/// At compile time, the language compiler implements the intermediate language (IL) for a try/catch block.
	/// <code language="c#">
	/// <![CDATA[
	/// using (IDocumentClient client = new DocumentClient(new Uri("endpoint"), "authKey"))
	/// {
	///     ...
	/// }
	/// ]]>
	/// </code>
	/// </para>
	///
	/// <para>
	/// 2. By wrapping the call to the IDisposable.Dispose implementation in a try/catch block.
	/// The following example replaces the using block in the previous example with a try/catch/finally block.
	/// <code language="c#">
	/// <![CDATA[
	/// IDocumentClient client = new DocumentClient(new Uri("endpoint"), "authKey"))
	/// try{
	///     ...
	/// }
	/// finally{
	///     if (client != null) client.Dispose();
	/// }
	/// ]]>
	/// </code>
	/// </para>
	///
	/// </remarks>
	public sealed class DocumentClient : IDisposable, IAuthorizationTokenProvider, IDocumentClient, IDocumentClientInternal
	{
		private class ResetSessionTokenRetryPolicyFactory : IRetryPolicyFactory
		{
			private readonly IRetryPolicyFactory retryPolicy;

			private readonly ISessionContainer sessionContainer;

			private readonly ClientCollectionCache collectionCache;

			public ResetSessionTokenRetryPolicyFactory(ISessionContainer sessionContainer, ClientCollectionCache collectionCache, IRetryPolicyFactory retryPolicy)
			{
				this.retryPolicy = retryPolicy;
				this.sessionContainer = sessionContainer;
				this.collectionCache = collectionCache;
			}

			public IDocumentClientRetryPolicy GetRequestPolicy()
			{
				return new RenameCollectionAwareClientRetryPolicy(sessionContainer, collectionCache, retryPolicy.GetRequestPolicy());
			}
		}

		private class HttpRequestMessageHandler : DelegatingHandler
		{
			private readonly EventHandler<SendingRequestEventArgs> sendingRequest;

			private readonly EventHandler<ReceivedResponseEventArgs> receivedResponse;

			public HttpRequestMessageHandler(EventHandler<SendingRequestEventArgs> sendingRequest, EventHandler<ReceivedResponseEventArgs> receivedResponse, HttpMessageHandler innerHandler)
			{
				this.sendingRequest = sendingRequest;
				this.receivedResponse = receivedResponse;
				base.InnerHandler = (innerHandler ?? new HttpClientHandler());
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				sendingRequest?.Invoke(this, new SendingRequestEventArgs(request));
				HttpResponseMessage httpResponseMessage = await base.SendAsync(request, cancellationToken);
				receivedResponse?.Invoke(this, new ReceivedResponseEventArgs(request, httpResponseMessage));
				return httpResponseMessage;
			}
		}

		private const string AllowOverrideStrongerConsistency = "AllowOverrideStrongerConsistency";

		private const string MaxConcurrentConnectionOpenConfig = "MaxConcurrentConnectionOpenRequests";

		private const string IdleConnectionTimeoutInSecondsConfig = "IdleConnectionTimeoutInSecondsConfig";

		private const string OpenConnectionTimeoutInSecondsConfig = "OpenConnectionTimeoutInSecondsConfig";

		private const string TransportTimerPoolGranularityInSecondsConfig = "TransportTimerPoolGranularityInSecondsConfig";

		private const string EnableTcpChannelConfig = "CosmosDbEnableTcpChannel";

		private const string MaxRequestsPerChannelConfig = "CosmosDbMaxRequestsPerTcpChannel";

		private const string TcpPartitionCount = "CosmosDbTcpPartitionCount";

		private const string MaxChannelsPerHostConfig = "CosmosDbMaxTcpChannelsPerHost";

		private const string RntbdPortReusePolicy = "CosmosDbTcpPortReusePolicy";

		private const string RntbdPortPoolReuseThreshold = "CosmosDbTcpPortReuseThreshold";

		private const string RntbdPortPoolBindAttempts = "CosmosDbTcpPortReuseBindAttempts";

		private const string RntbdReceiveHangDetectionTimeConfig = "CosmosDbTcpReceiveHangDetectionTimeSeconds";

		private const string RntbdSendHangDetectionTimeConfig = "CosmosDbTcpSendHangDetectionTimeSeconds";

		private const string EnableCpuMonitorConfig = "CosmosDbEnableCpuMonitor";

		private const int MaxConcurrentConnectionOpenRequestsPerProcessor = 25;

		private const int DefaultMaxRequestsPerRntbdChannel = 30;

		private const int DefaultRntbdPartitionCount = 1;

		private const int DefaultMaxRntbdChannelsPerHost = 65535;

		private const TcpPortReuse DefaultRntbdPortReusePolicy = TcpPortReuse.ReuseUnicastPort;

		private const int DefaultRntbdPortPoolReuseThreshold = 256;

		private const int DefaultRntbdPortPoolBindAttempts = 5;

		private const int DefaultRntbdReceiveHangDetectionTimeSeconds = 65;

		private const int DefaultRntbdSendHangDetectionTimeSeconds = 10;

		private const bool DefaultEnableCpuMonitor = true;

		private ConnectionPolicy connectionPolicy;

		private RetryPolicy retryPolicy;

		private bool allowOverrideStrongerConsistency = false;

		private int maxConcurrentConnectionOpenRequests = Environment.ProcessorCount * 25;

		private int openConnectionTimeoutInSeconds = 5;

		private int idleConnectionTimeoutInSeconds = -1;

		private int timerPoolGranularityInSeconds = 1;

		private bool enableRntbdChannel = true;

		private int maxRequestsPerRntbdChannel = 30;

		private int rntbdPartitionCount = 1;

		private int maxRntbdChannels = 65535;

		private TcpPortReuse rntbdPortReusePolicy = TcpPortReuse.ReuseUnicastPort;

		private int rntbdPortPoolReuseThreshold = 256;

		private int rntbdPortPoolBindAttempts = 5;

		private int rntbdReceiveHangDetectionTimeSeconds = 65;

		private int rntbdSendHangDetectionTimeSeconds = 10;

		private bool enableCpuMonitor = true;

		private readonly IDictionary<string, List<PartitionKeyAndResourceTokenPair>> resourceTokens;

		private IComputeHash authKeyHashFunction;

		private ConsistencyLevel? desiredConsistencyLevel;

		private GatewayServiceConfigurationReader gatewayConfigurationReader;

		private ClientCollectionCache collectionCache;

		private PartitionKeyRangeCache partitionKeyRangeCache;

		internal HttpMessageHandler httpMessageHandler;

		private bool isSuccessfullyInitialized;

		private bool isDisposed;

		private object initializationSyncLock;

		private IStoreClientFactory storeClientFactory;

		private HttpClient mediaClient;

		private bool isStoreClientFactoryCreatedInternally;

		private IStoreModel storeModel;

		private IStoreModel gatewayStoreModel;

		private static int idCounter;

		private int traceId;

		private ISessionContainer sessionContainer;

		private readonly bool hasAuthKeyResourceToken;

		private readonly string authKeyResourceToken = string.Empty;

		private DocumentClientEventSource eventSource;

		private GlobalEndpointManager globalEndpointManager;

		private bool useMultipleWriteLocations;

		internal Task initializeTask;

		private JsonSerializerSettings serializerSettings;

		private Action<IQueryable> onExecuteScalarQueryCallback;

		internal GlobalAddressResolver AddressResolver
		{
			get;
			private set;
		}

		internal GlobalEndpointManager GlobalEndpointManager => globalEndpointManager;

		/// <summary>
		/// Partition resolvers are a dictionary of database links to IPartitionResolver to be used in partitioning for the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// Support for IPartitionResolver is now obsolete. It's recommended that you use
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
		public IDictionary<string, IPartitionResolver> PartitionResolvers
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the session object used for session consistency version tracking in the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// <value>
		/// The session object used for version tracking when the consistency level is set to Session.
		/// </value>
		/// The session object can be saved and shared between two DocumentClient instances within the same AppDomain.
		/// </remarks>
		public object Session
		{
			get
			{
				return sessionContainer;
			}
			set
			{
				SessionContainer sessionContainer = value as SessionContainer;
				if (sessionContainer == null)
				{
					throw new ArgumentNullException("value");
				}
				if (!string.Equals(ServiceEndpoint.Host, sessionContainer.HostName, StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadSession, sessionContainer.HostName, ServiceEndpoint.Host));
				}
				SessionContainer obj = this.sessionContainer as SessionContainer;
				if (obj == null)
				{
					throw new ArgumentNullException("currentSessionContainer");
				}
				obj.ReplaceCurrrentStateWithStateOf(sessionContainer);
			}
		}

		/// <summary>
		/// Gets or Sets the Api type
		/// </summary>
		internal ApiType ApiType
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the endpoint Uri for the service endpoint from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The Uri for the service endpoint.
		/// </value>
		/// <seealso cref="T:System.Uri" />
		public Uri ServiceEndpoint
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the current write endpoint chosen based on availability and preference from the Azure Cosmos DB service.
		/// </summary>
		public Uri WriteEndpoint => globalEndpointManager.WriteEndpoints.FirstOrDefault();

		/// <summary>
		/// Gets the current read endpoint chosen based on availability and preference from the Azure Cosmos DB service.
		/// </summary>
		public Uri ReadEndpoint => globalEndpointManager.ReadEndpoints.FirstOrDefault();

		/// <summary>
		/// Gets the Connection policy used by the client from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The Connection policy used by the client.
		/// </value>
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ConnectionPolicy" />
		public ConnectionPolicy ConnectionPolicy => connectionPolicy;

		/// <summary>
		/// Gets a dictionary of resource tokens used by the client from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// A dictionary of resource tokens used by the client.
		/// </value>
		/// <seealso cref="T:System.Collections.Generic.IDictionary`2" />
		[Obsolete]
		public IDictionary<string, string> ResourceTokens
		{
			get
			{
				if (resourceTokens == null)
				{
					return null;
				}
				return resourceTokens.ToDictionary((KeyValuePair<string, List<PartitionKeyAndResourceTokenPair>> pair) => pair.Key, (KeyValuePair<string, List<PartitionKeyAndResourceTokenPair>> pair) => pair.Value.First().ResourceToken);
			}
		}

		/// <summary>
		/// Gets the AuthKey used by the client from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The AuthKey used by the client.
		/// </value>
		/// <seealso cref="T:System.Security.SecureString" />
		public SecureString AuthKey
		{
			get
			{
				if (authKeyHashFunction != null)
				{
					return authKeyHashFunction.Key;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the configured consistency level of the client from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The configured <see cref="T:Microsoft.Azure.Documents.ConsistencyLevel" /> of the client.
		/// </value>
		/// <seealso cref="T:Microsoft.Azure.Documents.ConsistencyLevel" />
		public ConsistencyLevel ConsistencyLevel
		{
			get
			{
				TaskHelper.InlineIfPossible(() => EnsureValidClientAsync(), null).Wait();
				if (!desiredConsistencyLevel.HasValue)
				{
					return gatewayConfigurationReader.DefaultConsistencyLevel;
				}
				return desiredConsistencyLevel.Value;
			}
		}

		internal QueryCompatibilityMode QueryCompatibilityMode
		{
			get;
			set;
		}

		/// <summary>
		/// RetryPolicy retries a request when it encounters session unavailable (see ClientRetryPolicy).
		/// Once it exhausts all write regions it clears the session container, then it uses ClientCollectionCache
		/// to resolves the request's collection name. If it differs from the session container's resource id it
		/// explains the session unavailable exception: somebody removed and recreated the collection. In this
		/// case we retry once again (with empty session token) otherwise we return the error to the client
		/// (see RenameCollectionAwareClientRetryPolicy)
		/// </summary>
		internal IRetryPolicyFactory ResetSessionTokenRetryPolicy
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets and sets the IStoreModel object.
		/// </summary>
		/// <remarks>
		/// Test hook to enable unit test of DocumentClient.
		/// </remarks>
		internal IStoreModel StoreModel
		{
			get
			{
				return storeModel;
			}
			set
			{
				storeModel = value;
			}
		}

		/// <summary>
		/// Gets and sets the gateway IStoreModel object.
		/// </summary>
		/// <remarks>
		/// Test hook to enable unit test of DocumentClient.
		/// </remarks>
		internal IStoreModel GatewayStoreModel
		{
			get
			{
				return gatewayStoreModel;
			}
			set
			{
				gatewayStoreModel = value;
			}
		}

		/// <summary>
		/// Gets and sets on execute scalar query callback
		/// </summary>
		/// <remarks>
		/// Test hook to enable unit test for scalar queries
		/// </remarks>
		internal Action<IQueryable> OnExecuteScalarQueryCallback
		{
			get
			{
				return onExecuteScalarQueryCallback;
			}
			set
			{
				onExecuteScalarQueryCallback = value;
			}
		}

		private event EventHandler<SendingRequestEventArgs> sendingRequest;

		private event EventHandler<ReceivedResponseEventArgs> receivedResponse;

		internal event EventHandler<SendingRequestEventArgs> SendingRequest
		{
			add
			{
				sendingRequest += value;
			}
			remove
			{
				sendingRequest -= value;
			}
		}

		static DocumentClient()
		{
			StringKeyValueCollection.SetNameValueCollectionFactory(new DictionaryNameValueCollectionFactory());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified Azure Cosmos DB service endpoint, key, and connection policy for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">
		/// The service endpoint to use to create the client.
		/// </param>
		/// <param name="authKey">
		/// The list of Permission objects to use to create the client.
		/// </param>
		/// <param name="connectionPolicy">
		/// (Optional) The connection policy for the client. If none is passed, the default is used <see cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// </param>
		/// <param name="desiredConsistencyLevel">
		/// (Optional) This can be used to weaken the database account consistency level for read operations.
		/// If this is not set the database account consistency level will be used for all requests.
		/// </param>
		/// <remarks>
		/// The service endpoint and the authorization key can be obtained from the Azure Management Portal.
		/// The authKey used here is encrypted for privacy when being used, and deleted from computer memory when no longer needed
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:System.Security.SecureString" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, SecureString authKey, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
		{
			if (authKey == null)
			{
				throw new ArgumentNullException("authKey");
			}
			if (authKey != null)
			{
				authKeyHashFunction = new SecureStringHMACSHA256Helper(authKey);
			}
			Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified Azure Cosmos DB service endpoint, key, connection policy and a custom JsonSerializerSettings
		/// for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">
		/// The service endpoint to use to create the client.
		/// </param>
		/// <param name="authKey">
		/// The list of Permission objects to use to create the client.
		/// </param>
		/// <param name="connectionPolicy">
		/// The connection policy for the client.
		/// </param>
		/// <param name="desiredConsistencyLevel">
		/// This can be used to weaken the database account consistency level for read operations.
		/// If this is not set the database account consistency level will be used for all requests.
		/// </param>
		/// <param name="serializerSettings">
		/// The custom JsonSerializer settings to be used for serialization/derialization.
		/// </param>
		/// <remarks>
		/// The service endpoint and the authorization key can be obtained from the Azure Management Portal.
		/// The authKey used here is encrypted for privacy when being used, and deleted from computer memory when no longer needed
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:System.Security.SecureString" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		/// <seealso cref="T:Newtonsoft.Json.JsonSerializerSettings" />
		[Obsolete("Please use the constructor that takes JsonSerializerSettings as the third parameter.")]
		public DocumentClient(Uri serviceEndpoint, SecureString authKey, ConnectionPolicy connectionPolicy, ConsistencyLevel? desiredConsistencyLevel, JsonSerializerSettings serializerSettings)
			: this(serviceEndpoint, authKey, connectionPolicy, desiredConsistencyLevel)
		{
			this.serializerSettings = serializerSettings;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified Azure Cosmos DB service endpoint, key, connection policy and a custom JsonSerializerSettings
		/// for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">
		/// The service endpoint to use to create the client.
		/// </param>
		/// <param name="authKey">
		/// The list of Permission objects to use to create the client.
		/// </param>
		/// <param name="serializerSettings">
		/// The custom JsonSerializer settings to be used for serialization/derialization.
		/// </param>
		/// <param name="connectionPolicy">
		/// (Optional) The connection policy for the client. If none is passed, the default is used <see cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// </param>
		/// <param name="desiredConsistencyLevel">
		/// (Optional) This can be used to weaken the database account consistency level for read operations.
		/// If this is not set the database account consistency level will be used for all requests.
		/// </param>
		/// <remarks>
		/// The service endpoint and the authorization key can be obtained from the Azure Management Portal.
		/// The authKey used here is encrypted for privacy when being used, and deleted from computer memory when no longer needed
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:System.Security.SecureString" />
		/// <seealso cref="T:Newtonsoft.Json.JsonSerializerSettings" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, SecureString authKey, JsonSerializerSettings serializerSettings, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
			: this(serviceEndpoint, authKey, connectionPolicy, desiredConsistencyLevel)
		{
			this.serializerSettings = serializerSettings;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified service endpoint, an authorization key (or resource token) and a connection policy
		/// for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="authKeyOrResourceToken">The authorization key or resource token to use to create the client.</param>
		/// <param name="connectionPolicy">(Optional) The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <remarks>
		/// The service endpoint can be obtained from the Azure Management Portal.
		/// If you are connecting using one of the Master Keys, these can be obtained along with the endpoint from the Azure Management Portal
		/// If however you are connecting as a specific Azure Cosmos DB User, the value passed to <paramref name="authKeyOrResourceToken" /> is the ResourceToken obtained from the permission feed for the user.
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
			: this(serviceEndpoint, authKeyOrResourceToken, null, connectionPolicy, desiredConsistencyLevel, null, ApiType.None, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified service endpoint, an authorization key (or resource token) and a connection policy
		/// for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="authKeyOrResourceToken">The authorization key or resource token to use to create the client.</param>
		/// <param name="handler">The HTTP handler stack to use for sending requests (e.g., HttpClientHandler).</param>
		/// <param name="connectionPolicy">(Optional) The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <remarks>
		/// The service endpoint can be obtained from the Azure Management Portal.
		/// If you are connecting using one of the Master Keys, these can be obtained along with the endpoint from the Azure Management Portal
		/// If however you are connecting as a specific Azure Cosmos DB User, the value passed to <paramref name="authKeyOrResourceToken" /> is the ResourceToken obtained from the permission feed for the user.
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, HttpMessageHandler handler, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
			: this(serviceEndpoint, authKeyOrResourceToken, null, connectionPolicy, desiredConsistencyLevel, null, ApiType.None, null, handler)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified service endpoint, an authorization key (or resource token) and a connection policy
		/// for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="authKeyOrResourceToken">The authorization key or resource token to use to create the client.</param>
		/// <param name="sendingRequestEventArgs"> The event handler to be invoked before the request is sent.</param>
		/// <param name="receivedResponseEventArgs"> The event handler to be invoked after a response has been received.</param>
		/// <param name="connectionPolicy">(Optional) The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <param name="serializerSettings">The custom JsonSerializer settings to be used for serialization/derialization.</param>
		/// <param name="apitype">Api type for the account</param>
		/// <param name="handler">The HTTP handler stack to use for sending requests (e.g., HttpClientHandler).</param>
		/// <param name="sessionContainer">The default session container with which DocumentClient is created.</param>
		/// <param name="enableCpuMonitor">Flag that indicates whether client-side CPU monitoring is enabled for improved troubleshooting.</param>
		/// <param name="storeClientFactory">Factory that creates store clients sharing the same transport client to optimize network resource reuse across multiple document clients in the same process.</param>
		/// <remarks>
		/// The service endpoint can be obtained from the Azure Management Portal.
		/// If you are connecting using one of the Master Keys, these can be obtained along with the endpoint from the Azure Management Portal
		/// If however you are connecting as a specific Azure Cosmos DB User, the value passed to <paramref name="authKeyOrResourceToken" /> is the ResourceToken obtained from the permission feed for the user.
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		internal DocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, EventHandler<SendingRequestEventArgs> sendingRequestEventArgs, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?), JsonSerializerSettings serializerSettings = null, ApiType apitype = ApiType.None, EventHandler<ReceivedResponseEventArgs> receivedResponseEventArgs = null, HttpMessageHandler handler = null, ISessionContainer sessionContainer = null, bool? enableCpuMonitor = default(bool?), IStoreClientFactory storeClientFactory = null)
		{
			if (authKeyOrResourceToken == null)
			{
				throw new ArgumentNullException("authKeyOrResourceToken");
			}
			if (sendingRequestEventArgs != null)
			{
				sendingRequest += sendingRequestEventArgs;
			}
			if (serializerSettings != null)
			{
				this.serializerSettings = serializerSettings;
			}
			ApiType = apitype;
			if (receivedResponseEventArgs != null)
			{
				receivedResponse += receivedResponseEventArgs;
			}
			if (AuthorizationHelper.IsResourceToken(authKeyOrResourceToken))
			{
				hasAuthKeyResourceToken = true;
				authKeyResourceToken = authKeyOrResourceToken;
			}
			else
			{
				authKeyHashFunction = new StringHMACSHA256Hash(authKeyOrResourceToken);
			}
			Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel, handler, sessionContainer, enableCpuMonitor, storeClientFactory);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified service endpoint, an authorization key (or resource token), a connection policy
		/// and a custom JsonSerializerSettings for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="authKeyOrResourceToken">The authorization key or resource token to use to create the client.</param>
		/// <param name="connectionPolicy">The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">The default consistency policy for client operations.</param>
		/// <param name="serializerSettings">The custom JsonSerializer settings to be used for serialization/derialization.</param>
		/// <remarks>
		/// The service endpoint can be obtained from the Azure Management Portal.
		/// If you are connecting using one of the Master Keys, these can be obtained along with the endpoint from the Azure Management Portal
		/// If however you are connecting as a specific Azure Cosmos DB User, the value passed to <paramref name="authKeyOrResourceToken" /> is the ResourceToken obtained from the permission feed for the user.
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		/// <seealso cref="T:Newtonsoft.Json.JsonSerializerSettings" />
		[Obsolete("Please use the constructor that takes JsonSerializerSettings as the third parameter.")]
		public DocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, ConnectionPolicy connectionPolicy, ConsistencyLevel? desiredConsistencyLevel, JsonSerializerSettings serializerSettings)
			: this(serviceEndpoint, authKeyOrResourceToken, connectionPolicy, desiredConsistencyLevel)
		{
			this.serializerSettings = serializerSettings;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified service endpoint, an authorization key (or resource token), a connection policy
		/// and a custom JsonSerializerSettings for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="authKeyOrResourceToken">The authorization key or resource token to use to create the client.</param>
		/// <param name="serializerSettings">The custom JsonSerializer settings to be used for serialization/derialization.</param>
		/// <param name="connectionPolicy">(Optional) The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <remarks>
		/// The service endpoint can be obtained from the Azure Management Portal.
		/// If you are connecting using one of the Master Keys, these can be obtained along with the endpoint from the Azure Management Portal
		/// If however you are connecting as a specific Azure Cosmos DB User, the value passed to <paramref name="authKeyOrResourceToken" /> is the ResourceToken obtained from the permission feed for the user.
		/// <para>
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:Newtonsoft.Json.JsonSerializerSettings" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, string authKeyOrResourceToken, JsonSerializerSettings serializerSettings, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
			: this(serviceEndpoint, authKeyOrResourceToken, connectionPolicy, desiredConsistencyLevel)
		{
			this.serializerSettings = serializerSettings;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified Azure Cosmos DB service endpoint for the Azure Cosmos DB service, a list of permission objects and a connection policy.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="permissionFeed">A list of Permission objects to use to create the client.</param>
		/// <param name="connectionPolicy">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.ConnectionPolicy" /> to use for this connection.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permissionFeed" /> is not supplied.</exception>
		/// <exception cref="T:System.ArgumentException">If <paramref name="permissionFeed" /> is not a valid permission link.</exception>
		/// <remarks>
		/// If no <paramref name="connectionPolicy" /> is provided, then the default <see cref="T:Microsoft.Azure.Documents.Client.ConnectionPolicy" /> will be used.
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		public DocumentClient(Uri serviceEndpoint, IList<Permission> permissionFeed, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
			: this(serviceEndpoint, GetResourceTokens(permissionFeed), connectionPolicy, desiredConsistencyLevel)
		{
		}

		private static List<ResourceToken> GetResourceTokens(IList<Permission> permissionFeed)
		{
			if (permissionFeed == null)
			{
				throw new ArgumentNullException("permissionFeed");
			}
			return (from permission in permissionFeed
			select new ResourceToken
			{
				ResourceLink = permission.ResourceLink,
				ResourcePartitionKey = ((permission.ResourcePartitionKey != null) ? permission.ResourcePartitionKey.InternalKey.ToObjectArray() : null),
				Token = permission.Token
			}).ToList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Client.DocumentClient" /> class using the
		/// specified Azure Cosmos DB service endpoint, a list of <see cref="T:Microsoft.Azure.Documents.ResourceToken" /> objects and a connection policy.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="resourceTokens">A list of <see cref="T:Microsoft.Azure.Documents.ResourceToken" /> objects to use to create the client.</param>
		/// <param name="connectionPolicy">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.ConnectionPolicy" /> to use for this connection.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="resourceTokens" /> is not supplied.</exception>
		/// <exception cref="T:System.ArgumentException">If <paramref name="resourceTokens" /> is not a valid permission link.</exception>
		/// <remarks>
		/// If no <paramref name="connectionPolicy" /> is provided, then the default <see cref="T:Microsoft.Azure.Documents.Client.ConnectionPolicy" /> will be used.
		/// Using Direct connectivity, wherever possible, is recommended.
		/// </remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		internal DocumentClient(Uri serviceEndpoint, IList<ResourceToken> resourceTokens, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
		{
			if (resourceTokens == null)
			{
				throw new ArgumentNullException("resourceTokens");
			}
			this.resourceTokens = new Dictionary<string, List<PartitionKeyAndResourceTokenPair>>();
			foreach (ResourceToken resourceToken in resourceTokens)
			{
				bool isNameBased = false;
				bool isFeed = false;
				if (!PathsHelper.TryParsePathSegments(resourceToken.ResourceLink, out isFeed, out string _, out string resourceIdOrFullName, out isNameBased))
				{
					throw new ArgumentException(RMResources.BadUrl, "resourceToken.ResourceLink");
				}
				if (!this.resourceTokens.TryGetValue(resourceIdOrFullName, out List<PartitionKeyAndResourceTokenPair> value))
				{
					value = new List<PartitionKeyAndResourceTokenPair>();
					this.resourceTokens.Add(resourceIdOrFullName, value);
				}
				value.Add(new PartitionKeyAndResourceTokenPair((resourceToken.ResourcePartitionKey != null) ? PartitionKeyInternal.FromObjectArray(resourceToken.ResourcePartitionKey, strict: true) : PartitionKeyInternal.Empty, resourceToken.Token));
			}
			if (!this.resourceTokens.Any())
			{
				throw new ArgumentException("permissionFeed");
			}
			string token = resourceTokens.First().Token;
			if (AuthorizationHelper.IsResourceToken(token))
			{
				hasAuthKeyResourceToken = true;
				authKeyResourceToken = token;
				Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel);
			}
			else
			{
				authKeyHashFunction = new StringHMACSHA256Hash(token);
				Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel);
			}
		}

		/// <summary>
		/// Initializes a new instance of the Microsoft.Azure.Documents.Client.DocumentClient class using the
		/// specified Azure Cosmos DB service endpoint, a dictionary of resource tokens and a connection policy.
		/// </summary>
		/// <param name="serviceEndpoint">The service endpoint to use to create the client.</param>
		/// <param name="resourceTokens">A dictionary of resource ids and resource tokens.</param>
		/// <param name="connectionPolicy">(Optional) The connection policy for the client.</param>
		/// <param name="desiredConsistencyLevel">(Optional) The default consistency policy for client operations.</param>
		/// <remarks>Using Direct connectivity, wherever possible, is recommended</remarks>
		/// <seealso cref="T:System.Uri" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConnectionPolicy" />
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.DocumentClient.ConsistencyLevel" />
		[Obsolete("Please use the constructor that takes a permission list or a resource token list.")]
		public DocumentClient(Uri serviceEndpoint, IDictionary<string, string> resourceTokens, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?))
		{
			if (resourceTokens == null)
			{
				throw new ArgumentNullException("resourceTokens");
			}
			if (resourceTokens.Count() == 0)
			{
				throw new DocumentClientException(RMResources.InsufficientResourceTokens, null, null);
			}
			this.resourceTokens = resourceTokens.ToDictionary((KeyValuePair<string, string> pair) => pair.Key, (KeyValuePair<string, string> pair) => new List<PartitionKeyAndResourceTokenPair>
			{
				new PartitionKeyAndResourceTokenPair(PartitionKeyInternal.Empty, pair.Value)
			});
			string value = resourceTokens.ElementAt(0).Value;
			if (string.IsNullOrEmpty(value))
			{
				throw new DocumentClientException(RMResources.InsufficientResourceTokens, null, null);
			}
			if (AuthorizationHelper.IsResourceToken(value))
			{
				hasAuthKeyResourceToken = true;
				authKeyResourceToken = value;
				Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel);
			}
			else
			{
				authKeyHashFunction = new StringHMACSHA256Hash(value);
				Initialize(serviceEndpoint, connectionPolicy, desiredConsistencyLevel);
			}
		}

		internal async Task<ClientCollectionCache> GetCollectionCacheAsync()
		{
			await EnsureValidClientAsync();
			return collectionCache;
		}

		internal async Task<PartitionKeyRangeCache> GetPartitionKeyRangeCacheAsync()
		{
			await EnsureValidClientAsync();
			return partitionKeyRangeCache;
		}

		/// <summary>
		/// Open the connection to validate that the client initialization is successful in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Threading.Tasks.Task" /> object.
		/// </returns>
		/// <remarks>
		/// This method is recommended to be called, after the constructor, but before calling any other methods on the DocumentClient instance.
		/// If there are any initialization exceptions, this method will throw them (set on the task).
		/// Alternately, calling any API will throw initialization exception at the first call.
		/// </remarks>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     await client.OpenAsync();
		/// }
		/// ]]>
		/// </code>
		/// </example>
		public Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => OpenPrivateInlineAsync(cancellationToken), null, cancellationToken);
		}

		private async Task OpenPrivateInlineAsync(CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			await TaskHelper.InlineIfPossible(() => OpenPrivateAsync(cancellationToken), ResetSessionTokenRetryPolicy.GetRequestPolicy(), cancellationToken);
		}

		private async Task OpenPrivateAsync(CancellationToken cancellationToken)
		{
			ResourceFeedReader<Database> databaseFeedReader = this.CreateDatabaseFeedReader(new FeedOptions
			{
				MaxItemCount = -1
			});
			try
			{
				while (databaseFeedReader.HasMoreResults)
				{
					foreach (Database database in await databaseFeedReader.ExecuteNextAsync(cancellationToken))
					{
						ResourceFeedReader<DocumentCollection> collectionFeedReader = this.CreateDocumentCollectionFeedReader(database.SelfLink, new FeedOptions
						{
							MaxItemCount = -1
						});
						List<Task> tasks = new List<Task>();
						while (collectionFeedReader.HasMoreResults)
						{
							List<Task> list = tasks;
							list.AddRange(from collection in await collectionFeedReader.ExecuteNextAsync(cancellationToken)
							select InitializeCachesAsync(database.Id, collection, cancellationToken));
						}
						await Task.WhenAll(tasks);
					}
				}
			}
			catch (DocumentClientException ex)
			{
				collectionCache = new ClientCollectionCache(sessionContainer, gatewayStoreModel, this, retryPolicy);
				partitionKeyRangeCache = new PartitionKeyRangeCache(this, gatewayStoreModel, collectionCache);
				DefaultTrace.TraceWarning("{0} occurred while OpenAsync. Exception Message: {1}", ex.ToString(), ex.Message);
			}
		}

		private void Initialize(Uri serviceEndpoint, ConnectionPolicy connectionPolicy = null, ConsistencyLevel? desiredConsistencyLevel = default(ConsistencyLevel?), HttpMessageHandler handler = null, ISessionContainer sessionContainer = null, bool? enableCpuMonitor = default(bool?), IStoreClientFactory storeClientFactory = null)
		{
			if (serviceEndpoint == null)
			{
				throw new ArgumentNullException("serviceEndpoint");
			}
			DefaultTrace.InitEventListener();
			if (ConnectionPolicy != null)
			{
				if (ConnectionPolicy.IdleTcpConnectionTimeout.HasValue)
				{
					idleConnectionTimeoutInSeconds = (int)ConnectionPolicy.IdleTcpConnectionTimeout.Value.TotalSeconds;
				}
				if (ConnectionPolicy.OpenTcpConnectionTimeout.HasValue)
				{
					openConnectionTimeoutInSeconds = (int)ConnectionPolicy.OpenTcpConnectionTimeout.Value.TotalSeconds;
				}
				if (ConnectionPolicy.MaxRequestsPerTcpConnection.HasValue)
				{
					maxRequestsPerRntbdChannel = ConnectionPolicy.MaxRequestsPerTcpConnection.Value;
				}
				if (ConnectionPolicy.MaxTcpPartitionCount.HasValue)
				{
					rntbdPartitionCount = ConnectionPolicy.MaxTcpPartitionCount.Value;
				}
				if (ConnectionPolicy.MaxTcpConnectionsPerEndpoint.HasValue)
				{
					maxRntbdChannels = ConnectionPolicy.MaxTcpConnectionsPerEndpoint.Value;
				}
			}
			ServiceEndpoint = (serviceEndpoint.OriginalString.EndsWith("/", StringComparison.Ordinal) ? serviceEndpoint : new Uri(serviceEndpoint.OriginalString + "/"));
			this.connectionPolicy = (connectionPolicy ?? ConnectionPolicy.Default);
			globalEndpointManager = new GlobalEndpointManager(this, this.connectionPolicy);
			httpMessageHandler = new HttpRequestMessageHandler(this.sendingRequest, this.receivedResponse, handler);
			mediaClient = new HttpClient(httpMessageHandler);
			mediaClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
			{
				NoCache = true
			};
			mediaClient.AddUserAgentHeader(this.connectionPolicy.UserAgentContainer);
			mediaClient.AddApiTypeHeader(ApiType);
			mediaClient.DefaultRequestHeaders.Add("x-ms-version", HttpConstants.Versions.CurrentVersion);
			mediaClient.DefaultRequestHeaders.Add("Accept", "*/*");
			if (sessionContainer != null)
			{
				this.sessionContainer = sessionContainer;
			}
			else
			{
				this.sessionContainer = new SessionContainer(ServiceEndpoint.Host);
			}
			retryPolicy = new RetryPolicy(globalEndpointManager, this.connectionPolicy);
			ResetSessionTokenRetryPolicy = retryPolicy;
			mediaClient.Timeout = this.connectionPolicy.MediaRequestTimeout;
			this.desiredConsistencyLevel = desiredConsistencyLevel;
			initializationSyncLock = new object();
        	#pragma warning disable 612, 618
			PartitionResolvers = new ConcurrentDictionary<string, IPartitionResolver>();
        	#pragma warning restore 612, 618
			eventSource = DocumentClientEventSource.Instance;
			initializeTask = TaskHelper.InlineIfPossible(() => GetInitializationTask(storeClientFactory), new ResourceThrottleRetryPolicy(this.connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests, this.connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds));
			initializeTask.ContinueWith(delegate(Task t)
			{
				DefaultTrace.TraceWarning("initializeTask failed {0}", t.Exception);
			}, TaskContinuationOptions.OnlyOnFaulted);
			traceId = Interlocked.Increment(ref idCounter);
			DefaultTrace.TraceInformation(string.Format(CultureInfo.InvariantCulture, "DocumentClient with id {0} initialized at endpoint: {1} with ConnectionMode: {2}, connection Protocol: {3}, and consistency level: {4}", traceId, serviceEndpoint.ToString(), this.connectionPolicy.ConnectionMode.ToString(), this.connectionPolicy.ConnectionProtocol.ToString(), desiredConsistencyLevel.HasValue ? desiredConsistencyLevel.ToString() : "null"));
			QueryCompatibilityMode = QueryCompatibilityMode.Default;
		}

		private async Task GetInitializationTask(IStoreClientFactory storeClientFactory)
		{
			await InitializeGatewayConfigurationReader();
			if (desiredConsistencyLevel.HasValue)
			{
				EnsureValidOverwrite(desiredConsistencyLevel.Value);
			}
			GatewayStoreModel gatewayStoreModel = (GatewayStoreModel)(this.gatewayStoreModel = new GatewayStoreModel(globalEndpointManager, sessionContainer, connectionPolicy.RequestTimeout, gatewayConfigurationReader.DefaultConsistencyLevel, eventSource, serializerSettings, connectionPolicy.UserAgentContainer, ApiType, httpMessageHandler));
			collectionCache = new ClientCollectionCache(sessionContainer, this.gatewayStoreModel, this, retryPolicy);
			partitionKeyRangeCache = new PartitionKeyRangeCache(this, this.gatewayStoreModel, collectionCache);
			ResetSessionTokenRetryPolicy = new ResetSessionTokenRetryPolicyFactory(sessionContainer, collectionCache, retryPolicy);
			if (connectionPolicy.ConnectionMode == ConnectionMode.Gateway)
			{
				storeModel = this.gatewayStoreModel;
			}
			else
			{
				InitializeDirectConnectivity(storeClientFactory);
			}
		}

		private async Task InitializeCachesAsync(string databaseName, DocumentCollection collection, CancellationToken cancellationToken)
		{
			if (databaseName == null)
			{
				throw new ArgumentNullException("databaseName");
			}
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			CollectionCache collectionCache = await GetCollectionCacheAsync();
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Query, ResourceType.Document, collection.SelfLink, AuthorizationTokenType.PrimaryMasterKey))
			{
				collection = await collectionCache.ResolveCollectionAsync(request, CancellationToken.None);
				await partitionKeyRangeCache.TryGetOverlappingRangesAsync(collection.ResourceId, new Range<string>(PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey, PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey, isMinInclusive: true, isMaxInclusive: false));
				if (AddressResolver != null)
				{
					await AddressResolver.OpenAsync(databaseName, collection, cancellationToken);
				}
			}
		}

		/// <summary>
		/// Gets or sets the session object used for session consistency version tracking for a specific collection in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">Collection for which session token must be retrieved.</param>
		/// <value>
		/// The session token used for version tracking when the consistency level is set to Session.
		/// </value>
		/// <remarks>
		/// The session token can be saved and supplied to a request via <see cref="P:Microsoft.Azure.Documents.Client.RequestOptions.SessionToken" />.
		/// </remarks>
		internal string GetSessionToken(string collectionLink)
		{
			SessionContainer obj = sessionContainer as SessionContainer;
			if (obj == null)
			{
				throw new ArgumentNullException("sessionContainerInternal");
			}
			return obj.GetSessionToken(collectionLink);
		}

		/// <summary>
		/// Disposes the client for the Azure Cosmos DB service.
		/// </summary>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key");
		/// if (client != null) client.Dispose();
		/// ]]>
		/// </code>
		/// </example>
		public void Dispose()
		{
			if (isDisposed)
			{
				return;
			}
			if (storeModel != null)
			{
				storeModel.Dispose();
				storeModel = null;
			}
			if (storeClientFactory != null)
			{
				if (isStoreClientFactoryCreatedInternally)
				{
					storeClientFactory.Dispose();
				}
				storeClientFactory = null;
			}
			if (AddressResolver != null)
			{
				AddressResolver.Dispose();
				AddressResolver = null;
			}
			if (mediaClient != null)
			{
				mediaClient.Dispose();
				mediaClient = null;
			}
			if (authKeyHashFunction != null)
			{
				authKeyHashFunction.Dispose();
				authKeyHashFunction = null;
			}
			if (globalEndpointManager != null)
			{
				globalEndpointManager.Dispose();
				globalEndpointManager = null;
			}
			DefaultTrace.TraceInformation("DocumentClient with id {0} disposed.", traceId);
			DefaultTrace.Flush();
			isDisposed = true;
		}

		internal async Task<IDictionary<string, object>> GetQueryEngineConfiguration()
		{
			await EnsureValidClientAsync();
			return gatewayConfigurationReader.QueryEngineConfiguration;
		}

		internal async Task<ConsistencyLevel> GetDefaultConsistencyLevelAsync()
		{
			await EnsureValidClientAsync();
			return gatewayConfigurationReader.DefaultConsistencyLevel;
		}

		internal Task<ConsistencyLevel?> GetDesiredConsistencyLevelAsync()
		{
			return Task.FromResult(desiredConsistencyLevel);
		}

		internal async Task<DocumentServiceResponse> ProcessRequestAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			retryPolicyInstance?.OnBeforeSendRequest(request);
			using (new ActivityScope(Guid.NewGuid()))
			{
				return await GetStoreProxy(request).ProcessMessageAsync(request, cancellationToken);
			}
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("DocumentClient");
			}
		}

		private async Task EnsureValidClientAsync()
		{
			ThrowIfDisposed();
			if (!isSuccessfullyInitialized)
			{
				Task initTask = null;
				lock (initializationSyncLock)
				{
					initTask = initializeTask;
				}
				try
				{
					await initTask;
					isSuccessfullyInitialized = true;
					return;
				}
				catch (Exception ex)
				{
					DefaultTrace.TraceWarning("initializeTask failed {0}", ex.ToString());
				}
				lock (initializationSyncLock)
				{
					if (initializeTask == initTask)
					{
						initializeTask = GetInitializationTask(null);
					}
					initTask = initializeTask;
				}
				await initTask;
				isSuccessfullyInitialized = true;
			}
		}

		/// <summary>
		/// Creates an attachment as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentLink">The link of the parent document for this new attachment. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/ </param>
		/// <param name="attachment">The attachment object.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// The <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.
		/// </returns>
		/// <remarks>
		///
		/// </remarks>
		/// <example>
		/// The example below creates a new document, and then creates a new attachment for that document
		/// <code language="c#">
		/// <![CDATA[
		/// dynamic d = new
		/// {
		///     id = "DOC1800243243470"
		/// };
		///
		/// Document doc = await client.CreateDocumentAsync(collectionSelfLink, d);
		///
		/// //Create an Attachment which links to binary content stored somewhere else
		/// //Use the MediaLink property of Attachment to set where the binary resides
		/// //MediaLink can also point at another Attachment within Azure Cosmos DB.
		/// Attachment a = await client.CreateAttachmentAsync(doc.SelfLink, new Attachment { Id = "foo", ContentType = "text/plain", MediaLink = "link to your media" });
		///
		/// //Because Attachment is a Dynamic object you can use SetPropertyValue method to any property you like
		/// //Even if that property doesn't exist. Here we are creating two new properties on the Attachment we created above.
		/// a.SetPropertyValue("Foo", "some value");
		/// a.SetPropertyValue("Bar", "some value");
		///
		/// //Now update the Attachment object in the database to persist the new properties on the object
		/// client.ReplaceAttachmentAsync(a);
		///
		/// //Let's now create another Attachment except this time we're going to use a Dynamic object instead
		/// //of a <see cref="Microsoft.Azure.Documents.Attachment"/> as we did above.
		/// var b = await client.CreateAttachmentAsync(doc.SelfLink, new { id = "foo", contentType = "text/plain", media="link to your media", a = 5, b = 6 });
		///
		/// //Now you will have a Document in your database with two attachments.
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string documentLink, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateAttachmentPrivateAsync(documentLink, attachment, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> CreateAttachmentPrivateAsync(string documentLink, object attachment, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			if (attachment == null)
			{
				throw new ArgumentNullException("attachment");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			Attachment resource = Attachment.FromObject(attachment);
			ValidateResource(resource);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, documentLink, resource, ResourceType.Attachment, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				if (!request.IsValidAddress(ResourceType.Document))
				{
					throw new ArgumentException(RMResources.BadUrl, "link");
				}
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Attachment>(await CreateAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Creates a database resource as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="database">The specification for the <see cref="T:Microsoft.Azure.Documents.Database" /> to create.</param>
		/// <param name="options">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> for the request.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.Database" /> that was created within a task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="database" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s).</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Database are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the database object supplied. It is likely that an id was not supplied for the new Database.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Database" /> with an id matching the id field of <paramref name="database" /> already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// The example below creates a new <see cref="T:Microsoft.Azure.Documents.Database" /> with an Id property of 'MyDatabase'
		/// This code snippet is intended to be used from within an asynchronous method as it uses the await keyword
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Database db = await client.CreateDatabaseAsync(new Database { Id = "MyDatabase" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// If you would like to construct a <see cref="T:Microsoft.Azure.Documents.Database" /> from within a synchronous method then you need to use the following code
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Database db = client.CreateDatabaseAsync(new Database { Id = "MyDatabase" }).Result;
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Database>> CreateDatabaseAsync(Database database, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateDatabasePrivateAsync(database, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Database>> CreateDatabasePrivateAsync(Database database, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}
			ValidateResource(database);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, "//dbs/", database, ResourceType.Database, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Database>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates(if doesn't exist) or gets(if already exists) a database resource as an asychronous operation in the Azure Cosmos DB service.
		/// You can check the status code from the response to determine whether the database was newly created(201) or existing database was returned(200)
		/// </summary>
		/// <param name="database">The specification for the <see cref="T:Microsoft.Azure.Documents.Database" /> to create.</param>
		/// <param name="options">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> for the request.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.Database" /> that was created within a task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="database" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s).</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property.</exception>
		/// <example>
		/// The example below creates a new <see cref="T:Microsoft.Azure.Documents.Database" /> with an Id property of 'MyDatabase'
		/// This code snippet is intended to be used from within an asynchronous method as it uses the await keyword
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Database db = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "MyDatabase" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// If you would like to construct a <see cref="T:Microsoft.Azure.Documents.Database" /> from within a synchronous method then you need to use the following code
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Database db = client.CreateDatabaseIfNotExistsAsync(new Database { Id = "MyDatabase" }).Result;
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Database>> CreateDatabaseIfNotExistsAsync(Database database, RequestOptions options = null)
		{
			return TaskHelper.InlineIfPossible(() => CreateDatabaseIfNotExistsPrivateAsync(database, options), null);
		}

		private async Task<ResourceResponse<Database>> CreateDatabaseIfNotExistsPrivateAsync(Database database, RequestOptions options)
		{
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}
			try
			{
				return await ReadDatabaseAsync(UriFactory.CreateDatabaseUri(database.Id));
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.NotFound)
				{
					throw;
				}
			}
			try
			{
				return await CreateDatabaseAsync(database, options);
			}
			catch (DocumentClientException ex2)
			{
				if (ex2.StatusCode != HttpStatusCode.Conflict)
				{
					throw;
				}
			}
			return await ReadDatabaseAsync(UriFactory.CreateDatabaseUri(database.Id));
		}

		/// <summary>
		/// Creates a Document as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentsFeedOrDatabaseLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to create the document in. E.g. dbs/db_rid/colls/coll_rid/ </param>
		/// <param name="document">The document object to create.</param>
		/// <param name="options">(Optional) Any request options you wish to set. E.g. Specifying a Trigger to execute when creating the document. <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /></param>
		/// <param name="disableAutomaticIdGeneration">(Optional) Disables the automatic id generation, If this is True the system will throw an exception if the id property is missing from the Document.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.Document" /> that was created contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="documentsFeedOrDatabaseLink" /> or <paramref name="document" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the document supplied. It is likely that <paramref name="disableAutomaticIdGeneration" /> was true and an id was not supplied</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - This likely means the collection in to which you were trying to create the document is full.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Document" /> with an id matching the id field of <paramref name="document" /> already existed</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the <see cref="T:Microsoft.Azure.Documents.Document" /> exceeds the current max entity size. Consult documentation for limits and quotas.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// Azure Cosmos DB supports a number of different ways to work with documents. A document can extend <see cref="T:Microsoft.Azure.Documents.Resource" />
		/// <code language="c#">
		/// <![CDATA[
		/// public class MyObject : Resource
		/// {
		///     public string MyProperty {get; set;}
		/// }
		///
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.CreateDocumentAsync("dbs/db_rid/colls/coll_rid/", new MyObject { MyProperty = "A Value" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// A document can be any POCO object that can be serialized to JSON, even if it doesn't extend from <see cref="T:Microsoft.Azure.Documents.Resource" />
		/// <code language="c#">
		/// <![CDATA[
		/// public class MyPOCO
		/// {
		///     public string MyProperty {get; set;}
		/// }
		///
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.CreateDocumentAsync("dbs/db_rid/colls/coll_rid/", new MyPOCO { MyProperty = "A Value" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// Finally, a Document can also be a dynamic object
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.CreateDocumentAsync("dbs/db_rid/colls/coll_rid/", new { SomeProperty = "A Value" } );
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// Create a Document and execute a Pre and Post Trigger
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.CreateDocumentAsync(
		///         "dbs/db_rid/colls/coll_rid/",
		///         new { id = "DOC123213443" },
		///         new RequestOptions
		///         {
		///             PreTriggerInclude = new List<string> { "MyPreTrigger" },
		///             PostTriggerInclude = new List<string> { "MyPostTrigger" }
		///         });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> CreateDocumentAsync(string documentsFeedOrDatabaseLink, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => CreateDocumentInlineAsync(documentsFeedOrDatabaseLink, document, options, disableAutomaticIdGeneration, cancellationToken), null, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> CreateDocumentInlineAsync(string documentsFeedOrDatabaseLink, object document, RequestOptions options, bool disableAutomaticIdGeneration, CancellationToken cancellationToken)
		{
        	#pragma warning disable 612, 618
			IPartitionResolver value = null;
			IDocumentClientRetryPolicy requestRetryPolicy = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			if (PartitionResolvers.TryGetValue(documentsFeedOrDatabaseLink, out value))
			{
				object partitionKey = value.GetPartitionKey(document);
				string collectionLink = value.ResolveForCreate(partitionKey);
				return await TaskHelper.InlineIfPossible(() => CreateDocumentPrivateAsync(collectionLink, document, options, disableAutomaticIdGeneration, requestRetryPolicy, cancellationToken), requestRetryPolicy);
			}
			if (options == null || options.PartitionKey == null)
			{
				requestRetryPolicy = new PartitionKeyMismatchRetryPolicy(await GetCollectionCacheAsync(), requestRetryPolicy);
			}
        	#pragma warning restore 612, 618
			return await TaskHelper.InlineIfPossible(() => CreateDocumentPrivateAsync(documentsFeedOrDatabaseLink, document, options, disableAutomaticIdGeneration, requestRetryPolicy, cancellationToken), requestRetryPolicy);
		}

		private async Task<ResourceResponse<Document>> CreateDocumentPrivateAsync(string documentCollectionLink, object document, RequestOptions options, bool disableAutomaticIdGeneration, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentCollectionLink))
			{
				throw new ArgumentNullException("documentCollectionLink");
			}
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			Document document2 = Document.FromObject(document, GetSerializerSettingsForRequest(options));
			ValidateResource(document2);
			if (string.IsNullOrEmpty(document2.Id) && !disableAutomaticIdGeneration)
			{
				document2.Id = Guid.NewGuid().ToString();
			}
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, documentCollectionLink, document2, ResourceType.Document, AuthorizationTokenType.PrimaryMasterKey, requestHeaders, SerializationFormattingPolicy.None, GetSerializerSettingsForRequest(options)))
			{
				await AddPartitionKeyInformationAsync(request, document2, options);
				return new ResourceResponse<Document>(await CreateAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Creates a collection as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">The link of the database to create the collection in. E.g. dbs/db_rid/.</param>
		/// <param name="documentCollection">The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> you wish to provide when creating a Collection. E.g. RequestOptions.OfferThroughput = 400. </param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> that was created contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="documentCollection" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s).</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a collection are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an id was not supplied for the new collection.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - This means you attempted to exceed your quota for collections. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     //Create a new collection with an OfferThroughput set to 10000
		///     //Not passing in RequestOptions.OfferThroughput will result in a collection with the default OfferThroughput set.
		///     DocumentCollection coll = await client.CreateDocumentCollectionAsync(databaseLink,
		///         new DocumentCollection { Id = "My Collection" },
		///         new RequestOptions { OfferThroughput = 10000} );
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.OfferV2" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateDocumentCollectionPrivateAsync(databaseLink, documentCollection, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionPrivateAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (documentCollection == null)
			{
				throw new ArgumentNullException("documentCollection");
			}
			ValidateResource(documentCollection);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, databaseLink, documentCollection, ResourceType.Collection, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				ResourceResponse<DocumentCollection> resourceResponse = new ResourceResponse<DocumentCollection>(await CreateAsync(request, retryPolicyInstance));
				sessionContainer.SetSessionToken(resourceResponse.Resource.ResourceId, resourceResponse.Resource.AltLink, resourceResponse.Headers);
				return resourceResponse;
			}
		}

		/// <summary>
		/// Creates (if doesn't exist) or gets (if already exists) a collection as an asychronous operation in the Azure Cosmos DB service.
		/// You can check the status code from the response to determine whether the collection was newly created (201) or existing collection was returned (200).
		/// </summary>
		/// <param name="databaseLink">The link of the database to create the collection in. E.g. dbs/db_rid/.</param>
		/// <param name="documentCollection">The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> you wish to provide when creating a Collection. E.g. RequestOptions.OfferThroughput = 400. </param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> that was created contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="documentCollection" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s).</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a DocumentCollection are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an id was not supplied for the new collection.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - This means you attempted to exceed your quota for collections. Contact support to have this quota increased.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     //Create a new collection with an OfferThroughput set to 10000
		///     //Not passing in RequestOptions.OfferThroughput will result in a collection with the default OfferThroughput set.
		///     DocumentCollection coll = await client.CreateDocumentCollectionIfNotExistsAsync(databaseLink,
		///         new DocumentCollection { Id = "My Collection" },
		///         new RequestOptions { OfferThroughput = 10000} );
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.OfferV2" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options = null)
		{
			return TaskHelper.InlineIfPossible(() => CreateDocumentCollectionIfNotExistsPrivateAsync(databaseLink, documentCollection, options), null);
		}

		private async Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsPrivateAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options)
		{
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (documentCollection == null)
			{
				throw new ArgumentNullException("documentCollection");
			}
			Database database = await ReadDatabaseAsync(databaseLink);
			try
			{
				return await ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(database.Id, documentCollection.Id));
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.NotFound)
				{
					throw;
				}
			}
			try
			{
				return await CreateDocumentCollectionAsync(databaseLink, documentCollection, options);
			}
			catch (DocumentClientException ex2)
			{
				if (ex2.StatusCode != HttpStatusCode.Conflict)
				{
					throw;
				}
			}
			return await ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(database.Id, documentCollection.Id));
		}

		/// <summary>
		/// Restores a collection as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="sourceDocumentCollectionLink">The link to the source <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="targetDocumentCollection">The target <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="restoreTime">(optional)The point in time to restore. If null, use the latest restorable time. </param>
		/// <param name="options">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<ResourceResponse<DocumentCollection>> RestoreDocumentCollectionAsync(string sourceDocumentCollectionLink, DocumentCollection targetDocumentCollection, DateTimeOffset? restoreTime = default(DateTimeOffset?), RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => RestoreDocumentCollectionPrivateAsync(sourceDocumentCollectionLink, targetDocumentCollection, restoreTime, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<DocumentCollection>> RestoreDocumentCollectionPrivateAsync(string sourceDocumentCollectionLink, DocumentCollection targetDocumentCollection, DateTimeOffset? restoreTime, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(sourceDocumentCollectionLink))
			{
				throw new ArgumentNullException("sourceDocumentCollectionLink");
			}
			if (targetDocumentCollection == null)
			{
				throw new ArgumentNullException("targetDocumentCollection");
			}
			string databasePath = PathsHelper.GetDatabasePath(sourceDocumentCollectionLink);
			if (PathsHelper.TryParsePathSegments(databasePath, out bool isFeed, out string resourcePath, out string resourceIdOrFullName, out bool isNameBased) && isNameBased && !isFeed)
			{
				string[] array = resourceIdOrFullName.Split(new char[1]
				{
					'/'
				}, StringSplitOptions.RemoveEmptyEntries);
				string sourceDatabaseId = array[array.Length - 1];
				if (PathsHelper.TryParsePathSegments(sourceDocumentCollectionLink, out isFeed, out resourcePath, out resourceIdOrFullName, out isNameBased) && isNameBased && !isFeed)
				{
					string[] array2 = resourceIdOrFullName.Split(new char[1]
					{
						'/'
					}, StringSplitOptions.RemoveEmptyEntries);
					string sourceCollectionId = array2[array2.Length - 1];
					ValidateResource(targetDocumentCollection);
					if (options == null)
					{
						options = new RequestOptions();
					}
					if (!options.RemoteStorageType.HasValue)
					{
						options.RemoteStorageType = RemoteStorageType.Standard;
					}
					options.SourceDatabaseId = sourceDatabaseId;
					options.SourceCollectionId = sourceCollectionId;
					if (restoreTime.HasValue)
					{
						options.RestorePointInTime = Helpers.ToUnixTime(restoreTime.Value);
					}
					INameValueCollection requestHeaders = GetRequestHeaders(options);
					using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, databasePath, targetDocumentCollection, ResourceType.Collection, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
					{
						ResourceResponse<DocumentCollection> resourceResponse = new ResourceResponse<DocumentCollection>(await CreateAsync(request, retryPolicyInstance));
						sessionContainer.SetSessionToken(resourceResponse.Resource.ResourceId, resourceResponse.Resource.AltLink, resourceResponse.Headers);
						return resourceResponse;
					}
				}
				throw new ArgumentNullException("sourceDocumentCollectionLink");
			}
			throw new ArgumentNullException("sourceDocumentCollectionLink");
		}

		/// <summary>
		/// Get the status of a collection being restored in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="targetDocumentCollectionLink">The link of the document collection being restored.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<DocumentCollectionRestoreStatus> GetDocumentCollectionRestoreStatusAsync(string targetDocumentCollectionLink)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => GetDocumentCollectionRestoreStatusPrivateAsync(targetDocumentCollectionLink, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<DocumentCollectionRestoreStatus> GetDocumentCollectionRestoreStatusPrivateAsync(string targetDocumentCollectionLink, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			if (string.IsNullOrEmpty(targetDocumentCollectionLink))
			{
				throw new ArgumentNullException("targetDocumentCollectionLink");
			}
			string text = (await ReadDocumentCollectionPrivateAsync(targetDocumentCollectionLink, new RequestOptions
			{
				PopulateRestoreStatus = true
			}, retryPolicyInstance)).ResponseHeaders.Get("x-ms-restore-state");
			if (text == null)
			{
				text = RestoreState.RestoreCompleted.ToString();
			}
			return new DocumentCollectionRestoreStatus
			{
				State = text
			};
		}

		/// <summary>
		/// Creates a stored procedure as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the collection to create the stored procedure in. E.g. dbs/db_rid/colls/col_rid/</param>
		/// <param name="storedProcedure">The <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> object to create.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> that was created contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="storedProcedure" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the stored procedure or the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of stored procedures for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> you tried to create was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a new stored procedure called "HelloWorldSproc" that takes in a single param called "name".
		/// StoredProcedure sproc = await client.CreateStoredProcedureAsync(collectionLink, new StoredProcedure
		/// {
		///    Id = "HelloWorldSproc",
		///    Body = @"function (name){
		///                var response = getContext().getResponse();
		///                response.setBody('Hello ' + name);
		///             }"
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<StoredProcedure>> CreateStoredProcedureAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateStoredProcedurePrivateAsync(collectionLink, storedProcedure, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<StoredProcedure>> CreateStoredProcedurePrivateAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (storedProcedure == null)
			{
				throw new ArgumentNullException("storedProcedure");
			}
			ValidateResource(storedProcedure);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, collectionLink, storedProcedure, ResourceType.StoredProcedure, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<StoredProcedure>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates a trigger as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to create the trigger in. E.g. dbs/db_rid/colls/col_rid/ </param>
		/// <param name="trigger">The <see cref="T:Microsoft.Azure.Documents.Trigger" /> object to create.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="trigger" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the new trigger or that the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of triggers for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Trigger" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.Trigger" /> you tried to create was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a trigger that validates the contents of a document as it is created and adds a 'timestamp' property if one was not found.
		/// Trigger trig = await client.CreateTriggerAsync(collectionLink, new Trigger
		/// {
		///     Id = "ValidateDocuments",
		///     Body = @"function validate() {
		///                         var context = getContext();
		///                         var request = context.getRequest();                                                             
		///                         var documentToCreate = request.getBody();
		///
		///                         // validate properties
		///                         if (!('timestamp' in documentToCreate)) {
		///                             var ts = new Date();
		///                             documentToCreate['timestamp'] = ts.getTime();
		///                         }
		///
		///                         // update the document that will be created
		///                         request.setBody(documentToCreate);
		///                       }",
		///     TriggerType = TriggerType.Pre,
		///     TriggerOperation = TriggerOperation.Create
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Trigger>> CreateTriggerAsync(string collectionLink, Trigger trigger, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateTriggerPrivateAsync(collectionLink, trigger, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Trigger>> CreateTriggerPrivateAsync(string collectionLink, Trigger trigger, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (trigger == null)
			{
				throw new ArgumentNullException("trigger");
			}
			ValidateResource(trigger);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, collectionLink, trigger, ResourceType.Trigger, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Trigger>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates a user defined function as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to create the user defined function in. E.g. dbs/db_rid/colls/col_rid/ </param>
		/// <param name="function">The <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> object to create.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="function" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the new user defined function or that the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user defined functions for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> you tried to create was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a user defined function that converts a string to upper case
		/// UserDefinedFunction udf = client.CreateUserDefinedFunctionAsync(collectionLink, new UserDefinedFunction
		/// {
		///    Id = "ToUpper",
		///    Body = @"function toUpper(input) {
		///                        return input.toUpperCase();
		///                     }",
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<UserDefinedFunction>> CreateUserDefinedFunctionAsync(string collectionLink, UserDefinedFunction function, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateUserDefinedFunctionPrivateAsync(collectionLink, function, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedFunction>> CreateUserDefinedFunctionPrivateAsync(string collectionLink, UserDefinedFunction function, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			ValidateResource(function);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, collectionLink, function, ResourceType.UserDefinedFunction, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedFunction>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates a permission on a user object as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userLink">The link of the user to create the permission for. E.g. dbs/db_rid/users/user_rid/ </param>
		/// <param name="permission">The <see cref="T:Microsoft.Azure.Documents.Permission" /> object.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the created <see cref="T:Microsoft.Azure.Documents.Permission" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="userLink" /> or <paramref name="permission" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of permission objects. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Permission" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a read-only permission object for a specific user
		/// Permission p = await client.CreatePermissionAsync(userLink, new Permission { Id = "ReadPermission", PermissionMode = PermissionMode.Read });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Permission>> CreatePermissionAsync(string userLink, Permission permission, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreatePermissionPrivateAsync(userLink, permission, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Permission>> CreatePermissionPrivateAsync(string userLink, Permission permission, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userLink))
			{
				throw new ArgumentNullException("userLink");
			}
			if (permission == null)
			{
				throw new ArgumentNullException("permission");
			}
			ValidateResource(permission);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, userLink, permission, ResourceType.Permission, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Permission>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates a user object as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">The link of the database to create the user in. E.g. dbs/db_rid/ </param>
		/// <param name="user">The <see cref="T:Microsoft.Azure.Documents.User" /> object to create.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the created <see cref="T:Microsoft.Azure.Documents.User" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="user" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user objects for this database. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.User" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a new user called joeBloggs in the specified database
		/// User user = await client.CreateUserAsync(databaseLink, new User { Id = "joeBloggs" });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<User>> CreateUserAsync(string databaseLink, User user, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateUserPrivateAsync(databaseLink, user, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<User>> CreateUserPrivateAsync(string databaseLink, User user, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			ValidateResource(user);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, databaseLink, user, ResourceType.User, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<User>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates a user defined type object as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">The link of the database to create the user defined type in. E.g. dbs/db_rid/ </param>
		/// <param name="userDefinedType">The <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> object to create.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the created <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="userDefinedType" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a UserDefinedType are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user defined type objects for this database. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Create a new user defined type in the specified database
		/// UserDefinedType userDefinedType = await client.CreateUserDefinedTypeAsync(databaseLink, new UserDefinedType { Id = "userDefinedTypeId5" });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<UserDefinedType>> CreateUserDefinedTypeAsync(string databaseLink, UserDefinedType userDefinedType, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateUserDefinedTypePrivateAsync(databaseLink, userDefinedType, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedType>> CreateUserDefinedTypePrivateAsync(string databaseLink, UserDefinedType userDefinedType, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (userDefinedType == null)
			{
				throw new ArgumentNullException("userDefinedType");
			}
			ValidateResource(userDefinedType);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, databaseLink, userDefinedType, ResourceType.UserDefinedType, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedType>(await CreateAsync(request, retryPolicyInstance));
			}
		}

		/// <returns></returns>
		/// <summary>
		/// Delete an <see cref="T:Microsoft.Azure.Documents.Attachment" /> from the the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="attachmentLink">The link of the <see cref="T:Microsoft.Azure.Documents.Attachment" /> to delete. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/attachments/attachment_rid/ </param>
		/// <param name="options">(Optional) Any options you wish to set for this request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attachmentLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete an attachment using its selfLink property
		/// //To get the attachmentLink you would have to query for the Attachment, using CreateAttachmentQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteAttachmentAsync(attachmentLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> DeleteAttachmentAsync(string attachmentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteAttachmentPrivateAsync(attachmentLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> DeleteAttachmentPrivateAsync(string attachmentLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(attachmentLink))
			{
				throw new ArgumentNullException("attachmentLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Attachment, attachmentLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				retryPolicyInstance?.OnBeforeSendRequest(request);
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Attachment>(await DeleteAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.Database" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="databaseLink">The link of the <see cref="T:Microsoft.Azure.Documents.Database" /> to delete. E.g. dbs/db_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="databaseLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a database using its selfLink property
		/// //To get the databaseLink you would have to query for the Database, using CreateDatabaseQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteDatabaseAsync(databaseLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Database>> DeleteDatabaseAsync(string databaseLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteDatabasePrivateAsync(databaseLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Database>> DeleteDatabasePrivateAsync(string databaseLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Database, databaseLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Database>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.Document" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentLink">The link of the <see cref="T:Microsoft.Azure.Documents.Document" /> to delete. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a document using its selfLink property
		/// //To get the documentLink you would have to query for the Document, using CreateDocumentQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteDocumentAsync(documentLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> DeleteDocumentAsync(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteDocumentPrivateAsync(documentLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> DeleteDocumentPrivateAsync(string documentLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Document, documentLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new ResourceResponse<Document>(await DeleteAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentCollectionLink">The link of the <see cref="T:Microsoft.Azure.Documents.Document" /> to delete. E.g. dbs/db_rid/colls/col_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentCollectionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a collection using its selfLink property
		/// //To get the collectionLink you would have to query for the Collection, using CreateDocumentCollectionQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteDocumentCollectionAsync(collectionLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<DocumentCollection>> DeleteDocumentCollectionAsync(string documentCollectionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteDocumentCollectionPrivateAsync(documentCollectionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<DocumentCollection>> DeleteDocumentCollectionPrivateAsync(string documentCollectionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentCollectionLink))
			{
				throw new ArgumentNullException("documentCollectionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Collection, documentCollectionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<DocumentCollection>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="storedProcedureLink">The link of the <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> to delete. E.g. dbs/db_rid/colls/col_rid/sprocs/sproc_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a stored procedure using its selfLink property.
		/// //To get the sprocLink you would have to query for the Stored Procedure, using CreateStoredProcedureQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteStoredProcedureAsync(sprocLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<StoredProcedure>> DeleteStoredProcedureAsync(string storedProcedureLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteStoredProcedurePrivateAsync(storedProcedureLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<StoredProcedure>> DeleteStoredProcedurePrivateAsync(string storedProcedureLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(storedProcedureLink))
			{
				throw new ArgumentNullException("storedProcedureLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.StoredProcedure, storedProcedureLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<StoredProcedure>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.Trigger" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="triggerLink">The link of the <see cref="T:Microsoft.Azure.Documents.Trigger" /> to delete. E.g. dbs/db_rid/colls/col_rid/triggers/trigger_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="triggerLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a trigger using its selfLink property.
		/// //To get the triggerLink you would have to query for the Trigger, using CreateTriggerQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteTriggerAsync(triggerLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Trigger>> DeleteTriggerAsync(string triggerLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteTriggerPrivateAsync(triggerLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Trigger>> DeleteTriggerPrivateAsync(string triggerLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(triggerLink))
			{
				throw new ArgumentNullException("triggerLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Trigger, triggerLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Trigger>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="functionLink">The link of the <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> to delete. E.g. dbs/db_rid/colls/col_rid/udfs/udf_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="functionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a user defined function using its selfLink property.
		/// //To get the functionLink you would have to query for the User Defined Function, using CreateUserDefinedFunctionQuery(),  and then refer to its .SelfLink property
		/// await client.DeleteUserDefinedFunctionAsync(functionLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<UserDefinedFunction>> DeleteUserDefinedFunctionAsync(string functionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteUserDefinedFunctionPrivateAsync(functionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedFunction>> DeleteUserDefinedFunctionPrivateAsync(string functionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(functionLink))
			{
				throw new ArgumentNullException("functionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.UserDefinedFunction, functionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedFunction>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.Permission" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="permissionLink">The link of the <see cref="T:Microsoft.Azure.Documents.Permission" /> to delete. E.g. dbs/db_rid/users/user_rid/permissions/permission_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permissionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a permission using its selfLink property.
		/// //To get the permissionLink you would have to query for the Permission object, using CreateStoredProcedureQuery(), and then refer to its .SelfLink property
		/// await client.DeletePermissionAsync(permissionLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Permission>> DeletePermissionAsync(string permissionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeletePermissionPrivateAsync(permissionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Permission>> DeletePermissionPrivateAsync(string permissionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(permissionLink))
			{
				throw new ArgumentNullException("permissionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Permission, permissionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Permission>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.User" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userLink">The link of the <see cref="T:Microsoft.Azure.Documents.User" /> to delete. E.g. dbs/db_rid/users/user_rid/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a user using its selfLink property.
		/// //To get the userLink you would have to query for the User object, using CreateUserQuery(), and then refer to its .SelfLink property
		/// await client.DeleteUserAsync(userLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<User>> DeleteUserAsync(string userLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteUserPrivateAsync(userLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<User>> DeleteUserPrivateAsync(string userLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userLink))
			{
				throw new ArgumentNullException("userLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.User, userLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<User>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Delete a <see cref="T:Microsoft.Azure.Documents.Conflict" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="conflictLink">The link of the <see cref="T:Microsoft.Azure.Documents.Conflict" /> to delete. E.g. dbs/db_rid/colls/coll_rid/conflicts/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain information about the request issued.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="conflictLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Delete a conflict using its selfLink property.
		/// //To get the conflictLink you would have to query for the Conflict object, using CreateConflictQuery(), and then refer to its .SelfLink property
		/// await client.DeleteConflictAsync(conflictLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Conflict>> DeleteConflictAsync(string conflictLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => DeleteConflictPrivateAsync(conflictLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Conflict>> DeleteConflictPrivateAsync(string conflictLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(conflictLink))
			{
				throw new ArgumentNullException("conflictLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Delete, ResourceType.Conflict, conflictLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Conflict>(await DeleteAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Attachment" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="attachment">The updated <see cref="T:Microsoft.Azure.Documents.Attachment" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Attachment" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attachment" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the item to be updated
		/// Attachment attachment = client.CreateAttachmentQuery(attachmentLink)
		///                             .Where(r => r.Id == "attachment id")
		///                             .AsEnumerable()
		///                             .SingleOrDefault();
		///
		/// //Update some properties on the found resource
		/// attachment.MediaLink = "updated value";
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// Attachment updated = await client.ReplaceAttachmentAsync(attachment);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Attachment attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceAttachmentPrivateAsync(attachment, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> ReplaceAttachmentPrivateAsync(Attachment attachment, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (attachment == null)
			{
				throw new ArgumentNullException("attachment");
			}
			ValidateResource(attachment);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(attachment), attachment, ResourceType.Attachment, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Attachment>(await UpdateAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Replaces a document collection in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentCollection">the updated document collection.</param>
		/// <param name="options">the request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> containing the updated resource record.
		/// </returns>
		public Task<ResourceResponse<DocumentCollection>> ReplaceDocumentCollectionAsync(DocumentCollection documentCollection, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceDocumentCollectionPrivateAsync(documentCollection, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<DocumentCollection>> ReplaceDocumentCollectionPrivateAsync(DocumentCollection documentCollection, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (documentCollection == null)
			{
				throw new ArgumentNullException("documentCollection");
			}
			ValidateResource(documentCollection);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(documentCollection), documentCollection, ResourceType.Collection, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				ResourceResponse<DocumentCollection> resourceResponse = new ResourceResponse<DocumentCollection>(await UpdateAsync(request, retryPolicyInstance));
				if (resourceResponse.Resource != null)
				{
					sessionContainer.SetSessionToken(resourceResponse.Resource.ResourceId, resourceResponse.Resource.AltLink, resourceResponse.Headers);
				}
				return resourceResponse;
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Document" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentLink">The link of the document to be updated. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/ </param>
		/// <param name="document">The updated <see cref="T:Microsoft.Azure.Documents.Document" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="documentLink" /> or <paramref name="document" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// In this example, instead of using a strongly typed <see cref="T:Microsoft.Azure.Documents.Document" />, we will work with our own POCO object and not rely on the dynamic nature of the Document class.
		/// <code language="c#">
		/// <![CDATA[
		/// public class MyPoco
		/// {
		///     public string Id {get; set;}
		///     public string MyProperty {get; set;}
		/// }
		///
		/// //Get the doc back as a Document so you have access to doc.SelfLink
		/// Document doc = client.CreateDocumentQuery<Document>(collectionLink)
		///                        .Where(r => r.Id == "doc id")
		///                        .AsEnumerable()
		///                        .SingleOrDefault();
		///
		/// //Now dynamically cast doc back to your MyPoco
		/// MyPoco poco = (dynamic)doc;
		///
		/// //Update some properties of the poco object
		/// poco.MyProperty = "updated value";
		///
		/// //Now persist these changes to the database using doc.SelLink and the update poco object
		/// Document updated = await client.ReplaceDocumentAsync(doc.SelfLink, poco);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> ReplaceDocumentAsync(string documentLink, object document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => ReplaceDocumentInlineAsync(documentLink, document, options, cancellationToken), null, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> ReplaceDocumentInlineAsync(string documentLink, object document, RequestOptions options, CancellationToken cancellationToken)
		{
			IDocumentClientRetryPolicy requestRetryPolicy = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			if (options == null || options.PartitionKey == null)
			{
				requestRetryPolicy = new PartitionKeyMismatchRetryPolicy(await GetCollectionCacheAsync(), requestRetryPolicy);
			}
			return await TaskHelper.InlineIfPossible(() => ReplaceDocumentPrivateAsync(documentLink, document, options, requestRetryPolicy, cancellationToken), requestRetryPolicy, cancellationToken);
		}

		private Task<ResourceResponse<Document>> ReplaceDocumentPrivateAsync(string documentLink, object document, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			Document document2 = Document.FromObject(document, GetSerializerSettingsForRequest(options));
			ValidateResource(document2);
			return ReplaceDocumentPrivateAsync(documentLink, document2, options, retryPolicyInstance, cancellationToken);
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Document" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="document">The updated <see cref="T:Microsoft.Azure.Documents.Document" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="document" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// This example uses <see cref="T:Microsoft.Azure.Documents.Document" /> and takes advantage of the fact that it is a dynamic object and uses SetProperty to dynamically update properties on the document
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the Document to be updated
		/// Document doc = client.CreateDocumentQuery<Document>(collectionLink)
		///                             .Where(r => r.Id == "doc id")
		///                             .AsEnumerable()
		///                             .SingleOrDefault();
		///
		/// //Update some properties on the found resource
		/// doc.SetPropertyValue("MyProperty", "updated value");
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// Document updated = await client.ReplaceDocumentAsync(doc);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Document document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceDocumentPrivateAsync(GetLinkForRouting(document), document, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> ReplaceDocumentPrivateAsync(string documentLink, Document document, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			ValidateResource(document);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, documentLink, document, ResourceType.Document, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, document, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new ResourceResponse<Document>(await UpdateAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="storedProcedure">The updated <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedure" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// StoredProcedure sproc = client.CreateStoredProcedureQuery(sprocsLink)
		///                                  .Where(r => r.Id == "sproc id")
		///                                  .AsEnumerable()
		///                                  .SingleOrDefault();
		///
		/// //Update some properties on the found resource
		/// sproc.Body = "function () {new javascript body for sproc}";
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// StoredProcedure updated = await client.ReplaceStoredProcedureAsync(sproc);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<StoredProcedure>> ReplaceStoredProcedureAsync(StoredProcedure storedProcedure, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceStoredProcedurePrivateAsync(storedProcedure, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<StoredProcedure>> ReplaceStoredProcedurePrivateAsync(StoredProcedure storedProcedure, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (storedProcedure == null)
			{
				throw new ArgumentNullException("storedProcedure");
			}
			ValidateResource(storedProcedure);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(storedProcedure), storedProcedure, ResourceType.StoredProcedure, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<StoredProcedure>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Trigger" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="trigger">The updated <see cref="T:Microsoft.Azure.Documents.Trigger" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Trigger" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="trigger" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// Trigger trigger = client.CreateTriggerQuery(sprocsLink)
		///                               .Where(r => r.Id == "trigger id")
		///                               .AsEnumerable()
		///                               .SingleOrDefault();
		///
		/// //Update some properties on the found resource
		/// trigger.Body = "function () {new javascript body for trigger}";
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// Trigger updated = await client.ReplaceTriggerAsync(sproc);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Trigger>> ReplaceTriggerAsync(Trigger trigger, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceTriggerPrivateAsync(trigger, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Trigger>> ReplaceTriggerPrivateAsync(Trigger trigger, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (trigger == null)
			{
				throw new ArgumentNullException("trigger");
			}
			ValidateResource(trigger);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(trigger), trigger, ResourceType.Trigger, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Trigger>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="function">The updated <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="function" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// UserDefinedFunction udf = client.CreateUserDefinedFunctionQuery(functionsLink)
		///                                     .Where(r => r.Id == "udf id")
		///                                     .AsEnumerable()
		///                                     .SingleOrDefault();
		///
		/// //Update some properties on the found resource
		/// udf.Body = "function () {new javascript body for udf}";
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// UserDefinedFunction updated = await client.ReplaceUserDefinedFunctionAsync(udf);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<UserDefinedFunction>> ReplaceUserDefinedFunctionAsync(UserDefinedFunction function, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserDefinedFunctionPrivateAsync(function, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedFunction>> ReplaceUserDefinedFunctionPrivateAsync(UserDefinedFunction function, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			ValidateResource(function);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(function), function, ResourceType.UserDefinedFunction, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedFunction>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Permission" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="permission">The updated <see cref="T:Microsoft.Azure.Documents.Permission" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Permission" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permission" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// Permission permission = client.CreatePermissionQuery(permissionsLink)
		///                                     .Where(r => r.Id == "permission id")
		///                                     .AsEnumerable()
		///                                     .SingleOrDefault();
		///
		/// //Change the permission mode to All
		/// permission.PermissionMode = PermissionMode.All;
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// Permission updated = await client.ReplacePermissionAsync(permission);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Permission>> ReplacePermissionAsync(Permission permission, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplacePermissionPrivateAsync(permission, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Permission>> ReplacePermissionPrivateAsync(Permission permission, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (permission == null)
			{
				throw new ArgumentNullException("permission");
			}
			ValidateResource(permission);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(permission), permission, ResourceType.Permission, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Permission>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.User" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="user">The updated <see cref="T:Microsoft.Azure.Documents.User" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.User" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="user" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// User user = client.CreateUserQuery(usersLink)
		///                          .Where(r => r.Id == "user id")
		///                          .AsEnumerable()
		///                          .SingleOrDefault();
		///
		/// //Change the user mode to All
		/// user.Id = "some new method";
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// User updated = await client.ReplaceUserAsync(user);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<User>> ReplaceUserAsync(User user, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserPrivateAsync(user, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<User>> ReplaceUserPrivateAsync(User user, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			ValidateResource(user);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(user), user, ResourceType.User, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<User>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.Offer" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="offer">The updated <see cref="T:Microsoft.Azure.Documents.Offer" /> to replace the existing resource with.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Offer" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="offer" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		///     <item>
		///        <term>423</term><description>Locked - This means the scale operation cannot be performed because another scale operation is in progress.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// Offer offer = client.CreateOfferQuery()
		///                          .Where(r => r.ResourceLink == "collection selfLink")
		///                          .AsEnumerable()
		///                          .SingleOrDefault();
		///
		/// //Create a new offer with the changed throughput
		/// OfferV2 newOffer = new OfferV2(offer, 5000);
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// Offer updated = await client.ReplaceOfferAsync(newOffer);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Offer>> ReplaceOfferAsync(Offer offer)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceOfferPrivateAsync(offer, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Offer>> ReplaceOfferPrivateAsync(Offer offer, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			if (offer == null)
			{
				throw new ArgumentNullException("offer");
			}
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, offer.SelfLink, offer, ResourceType.Offer, AuthorizationTokenType.PrimaryMasterKey))
			{
				return new ResourceResponse<Offer>(await UpdateAsync(request, retryPolicyInstance), OfferTypeResolver.ResponseOfferTypeResolver);
			}
		}

		/// <summary>
		/// Replaces a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> in the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userDefinedType">The updated <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> to replace the existing resource with.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> containing the updated resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userDefinedType" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to delete did not exist.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Fetch the resource to be updated
		/// UserDefinedType userDefinedType = client.CreateUserDefinedTypeQuery(userDefinedTypesLink)
		///                          .Where(r => r.Id == "user defined type id")
		///                          .AsEnumerable()
		///                          .SingleOrDefault();
		///
		/// //Now persist these changes to the database by replacing the original resource
		/// UserDefinedType updated = await client.ReplaceUserDefinedTypeAsync(userDefinedType);
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<UserDefinedType>> ReplaceUserDefinedTypeAsync(UserDefinedType userDefinedType, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserDefinedTypePrivateAsync(userDefinedType, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedType>> ReplaceUserDefinedTypePrivateAsync(UserDefinedType userDefinedType, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, string altLink = null)
		{
			await EnsureValidClientAsync();
			if (userDefinedType == null)
			{
				throw new ArgumentNullException("userDefinedType");
			}
			ValidateResource(userDefinedType);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Replace, altLink ?? GetLinkForRouting(userDefinedType), userDefinedType, ResourceType.UserDefinedType, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedType>(await UpdateAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads an <see cref="T:Microsoft.Azure.Documents.Attachment" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="attachmentLink">The link to the attachment to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Attachment" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attachmentLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads an Attachment resource where
		/// // - sample_db is the ID property of the Database
		/// // - sample_coll is the ID property of the DocumentCollection
		/// // - sample_doc is the ID property of the Document
		/// // - attachment_id is the ID property of the Attachment resource you wish to read.
		/// var attachLink = "/dbs/sample_db/colls/sample_coll/docs/sample_doc/attachments/attachment_id/";
		/// Attachment attachment = await client.ReadAttachmentAsync(attachLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Database if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="attachmentLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}/docs/{doc identifier}/attachments/{attachment identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Attachment>> ReadAttachmentAsync(string attachmentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadAttachmentPrivateAsync(attachmentLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> ReadAttachmentPrivateAsync(string attachmentLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(attachmentLink))
			{
				throw new ArgumentNullException("attachmentLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Attachment, attachmentLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Attachment>(await ReadAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Database" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="databaseLink">The link of the Database resource to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Database" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="databaseLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Database resource where
		/// // - database_id is the ID property of the Database resource you wish to read.
		/// var dbLink = "/dbs/database_id";
		/// Database database = await client.ReadDatabaseAsync(dbLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Database if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="databaseLink" /> is always "/dbs/{db identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Database>> ReadDatabaseAsync(string databaseLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDatabasePrivateAsync(databaseLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Database>> ReadDatabasePrivateAsync(string databaseLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Database, databaseLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Database>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Document" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentLink">The link for the document to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This reads a document record from a database & collection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - document_id is the ID of the document resource
		/// var docLink = "dbs/sample_database/colls/sample_collection/docs/document_id";
		/// Document doc = await client.ReadDocumentAsync(docLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Document if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="documentLink" /> is always "dbs/{db identifier}/colls/{coll identifier}/docs/{doc identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Document>> ReadDocumentAsync(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDocumentPrivateAsync(documentLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> ReadDocumentPrivateAsync(string documentLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Document, documentLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new ResourceResponse<Document>(await ReadAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Document" /> as a generic type T from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentLink">The link for the document to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.DocumentResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This reads a document record from a database & collection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - document_id is the ID of the document resource
		/// var docLink = "dbs/sample_database/colls/sample_collection/docs/document_id";
		/// Customer customer = await client.ReadDocumentAsync<Customer>(docLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Document if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="documentLink" /> is always "dbs/{db identifier}/colls/{coll identifier}/docs/{doc identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.DocumentResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<DocumentResponse<T>> ReadDocumentAsync<T>(string documentLink, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDocumentPrivateAsync<T>(documentLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<DocumentResponse<T>> ReadDocumentPrivateAsync<T>(string documentLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Document, documentLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new DocumentResponse<T>(await ReadAsync(request, retryPolicyInstance, cancellationToken), GetSerializerSettingsForRequest(options));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentCollectionLink">The link for the DocumentCollection to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentCollectionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This reads a DocumentCollection record from a database where
		/// // - sample_database is the ID of the database
		/// // - collection_id is the ID of the collection resource to be read
		/// var collLink = "/dbs/sample_database/colls/collection_id";
		/// DocumentCollection coll = await client.ReadDocumentCollectionAsync(collLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the DocumentCollection if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="documentCollectionLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<DocumentCollection>> ReadDocumentCollectionAsync(string documentCollectionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDocumentCollectionPrivateAsync(documentCollectionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<DocumentCollection>> ReadDocumentCollectionPrivateAsync(string documentCollectionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentCollectionLink))
			{
				throw new ArgumentNullException("documentCollectionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Collection, documentCollectionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<DocumentCollection>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="storedProcedureLink">The link of the stored procedure to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a StoredProcedure from a Database and DocumentCollection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - sproc_id is the ID of the stored procedure to be read
		/// var sprocLink = "/dbs/sample_database/colls/sample_collection/sprocs/sproc_id";
		/// StoredProcedure sproc = await client.ReadStoredProcedureAsync(sprocLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Stored Procedure if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="storedProcedureLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}/sprocs/{sproc identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<StoredProcedure>> ReadStoredProcedureAsync(string storedProcedureLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => _ReadStoredProcedureAsync(storedProcedureLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<StoredProcedure>> _ReadStoredProcedureAsync(string storedProcedureLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(storedProcedureLink))
			{
				throw new ArgumentNullException("storedProcedureLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.StoredProcedure, storedProcedureLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<StoredProcedure>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Trigger" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="triggerLink">The link to the Trigger to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Trigger" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="triggerLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Trigger from a Database and DocumentCollection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - trigger_id is the ID of the trigger to be read
		/// var triggerLink = "/dbs/sample_database/colls/sample_collection/triggers/trigger_id";
		/// Trigger trigger = await client.ReadTriggerAsync(triggerLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Trigger if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="triggerLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}/triggers/{trigger identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Trigger>> ReadTriggerAsync(string triggerLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadTriggerPrivateAsync(triggerLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Trigger>> ReadTriggerPrivateAsync(string triggerLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(triggerLink))
			{
				throw new ArgumentNullException("triggerLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Trigger, triggerLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Trigger>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="functionLink">The link to the User Defined Function to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="functionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a User Defined Function from a Database and DocumentCollection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - udf_id is the ID of the user-defined function to be read
		/// var udfLink = "/dbs/sample_database/colls/sample_collection/udfs/udf_id";
		/// UserDefinedFunction udf = await client.ReadUserDefinedFunctionAsync(udfLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the User Defined Function if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="functionLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}/udfs/{udf identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<UserDefinedFunction>> ReadUserDefinedFunctionAsync(string functionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserDefinedFunctionPrivateAsync(functionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedFunction>> ReadUserDefinedFunctionPrivateAsync(string functionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(functionLink))
			{
				throw new ArgumentNullException("functionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.UserDefinedFunction, functionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedFunction>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Permission" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="permissionLink">The link for the Permission resource to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Permission" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permissionLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Permission resource from a Database and User where
		/// // - sample_database is the ID of the database
		/// // - sample_user is the ID of the user
		/// // - permission_id is the ID of the permission to be read
		/// var permissionLink = "/dbs/sample_database/users/sample_user/permissions/permission_id";
		/// Permission permission = await client.ReadPermissionAsync(permissionLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Permission if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="permissionLink" /> is always "/dbs/{db identifier}/users/{user identifier}/permissions/{permission identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Permission>> ReadPermissionAsync(string permissionLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadPermissionPrivateAsync(permissionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Permission>> ReadPermissionPrivateAsync(string permissionLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(permissionLink))
			{
				throw new ArgumentNullException("permissionLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Permission, permissionLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Permission>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.User" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userLink">The link to the User resource to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.User" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a User resource from a Database
		/// // - sample_database is the ID of the database
		/// // - user_id is the ID of the user to be read
		/// var userLink = "/dbs/sample_database/users/user_id";
		/// User user = await client.ReadUserAsync(userLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the User if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="userLink" /> is always "/dbs/{db identifier}/users/{user identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<User>> ReadUserAsync(string userLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserPrivateAsync(userLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<User>> ReadUserPrivateAsync(string userLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userLink))
			{
				throw new ArgumentNullException("userLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.User, userLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<User>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Conflict" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="conflictLink">The link to the Conflict to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Conflict" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="conflictLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Conflict resource from a Database
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - conflict_id is the ID of the conflict to be read
		/// var conflictLink = "/dbs/sample_database/colls/sample_collection/conflicts/conflict_id";
		/// Conflict conflict = await client.ReadConflictAsync(conflictLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Conflict if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="conflictLink" /> is always "/dbs/{db identifier}/colls/{collectioon identifier}/conflicts/{conflict identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Conflict>> ReadConflictAsync(string conflictLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadConflictPrivateAsync(conflictLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Conflict>> ReadConflictPrivateAsync(string conflictLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(conflictLink))
			{
				throw new ArgumentNullException("conflictLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Conflict, conflictLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Conflict>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads an <see cref="T:Microsoft.Azure.Documents.Offer" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="offerLink">The link to the Offer to be read.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Offer" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="offerLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads an Offer resource from a Database
		/// // - offer_id is the ID of the offer to be read
		/// var offerLink = "/offers/offer_id";
		/// Offer offer = await client.ReadOfferAsync(offerLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// For an Offer, id is always generated internally by the system when the linked resource is created. id and _rid are always the same for Offer.
		/// </para>
		/// <para>
		/// Refer to https://docs.microsoft.com/en-us/azure/cosmos-db/how-to-provision-container-throughput to learn more about 
		/// minimum throughput of a Cosmos container (or a database)
		/// To retrieve the minimum throughput for a collection/database, use the following sample 
		/// <code language="c#">
		/// <![CDATA[
		/// // Find the offer for the collection by SelfLink
		/// Offer offer = client.CreateOfferQuery(
		///     string.Format("SELECT * FROM offers o WHERE o.resource = '{0}'", collectionSelfLink)).AsEnumerable().FirstOrDefault();
		/// ResourceResponse<Offer> response = await client.ReadOfferAsync(offer.SelfLink);
		/// string minimumRUsForCollection = readResponse.Headers["x-ms-cosmos-min-throughput"];
		/// ]]>
		/// </code>
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		public Task<ResourceResponse<Offer>> ReadOfferAsync(string offerLink)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadOfferPrivateAsync(offerLink, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Offer>> ReadOfferPrivateAsync(string offerLink, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(offerLink))
			{
				throw new ArgumentNullException("offerLink");
			}
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Offer, offerLink, null, AuthorizationTokenType.PrimaryMasterKey))
			{
				return new ResourceResponse<Offer>(await ReadAsync(request, retryPolicyInstance), OfferTypeResolver.ResponseOfferTypeResolver);
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Schema" /> as an asynchronous operation.
		/// </summary>
		/// <param name="documentSchemaLink">The link for the schema to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentSchemaLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when reading a Schema are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This reads a schema record from a database & collection where
		/// // - sample_database is the ID of the database
		/// // - sample_collection is the ID of the collection
		/// // - schema_id is the ID of the document resource
		/// var docLink = "/dbs/sample_database/colls/sample_collection/schemas/schemas_id";
		/// Schema schema = await client.ReadSchemaAsync(docLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown uses ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the Document if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="documentSchemaLink" /> is always "/dbs/{db identifier}/colls/{coll identifier}/schema/{schema identifier}" only
		/// the values within the {} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Schema" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		internal Task<ResourceResponse<Schema>> ReadSchemaAsync(string documentSchemaLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadSchemaPrivateAsync(documentSchemaLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Schema>> ReadSchemaPrivateAsync(string documentSchemaLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentSchemaLink))
			{
				throw new ArgumentNullException("documentSchemaLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Schema, documentSchemaLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new ResourceResponse<Schema>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userDefinedTypeLink">The link to the UserDefinedType resource to be read.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userDefinedTypeLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a UserDefinedType are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a User resource from a Database
		/// // - sample_database is the ID of the database
		/// // - userDefinedType_id is the ID of the user defined type to be read
		/// var userDefinedTypeLink = "/dbs/sample_database/udts/userDefinedType_id";
		/// UserDefinedType userDefinedType = await client.ReadUserDefinedTypeAsync(userDefinedTypeLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the Database. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// <para>
		/// The example shown user defined type ID-based links, where the link is composed of the ID properties used when the resources were created.
		/// You can still use the <see cref="P:Microsoft.Azure.Documents.Resource.SelfLink" /> property of the UserDefinedType if you prefer. A self-link is a URI for a resource that is made up of Resource Identifiers  (or the _rid properties).
		/// ID-based links and SelfLink will both work.
		/// The format for <paramref name="userDefinedTypeLink" /> is always "/dbs/{db identifier}/udts/{user defined type identifier}"
		/// only the values within the {...} change depending on which method you wish to use to address the resource.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		/// <seealso cref="T:System.Uri" />
		internal Task<ResourceResponse<UserDefinedType>> ReadUserDefinedTypeAsync(string userDefinedTypeLink, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserDefinedTypePrivateAsync(userDefinedTypeLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedType>> ReadUserDefinedTypePrivateAsync(string userDefinedTypeLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userDefinedTypeLink))
			{
				throw new ArgumentNullException("userDefinedTypeLink");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.UserDefinedType, userDefinedTypeLink, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedType>(await ReadAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Attachment" /> for a document from the Azure Cosmos DB service
		/// as an asynchronous operation.
		/// </summary>
		/// <param name="attachmentsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/coll_rid/docs/doc_rid/attachments/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Attachment" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attachmentsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read Attachment feed 10 items at a time until there are no more to read
		///     FeedResponse<Attachment> response = await client.ReadAttachmentFeedAsync("/dbs/db_rid/colls/coll_rid/docs/doc_rid/attachments/ ",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Attachment>> ReadAttachmentFeedAsync(string attachmentsLink, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadAttachmentFeedPrivateAsync(attachmentsLink, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<FeedResponse<Attachment>> ReadAttachmentFeedPrivateAsync(string attachmentsLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(attachmentsLink))
			{
				throw new ArgumentNullException("attachmentsLink");
			}
			return await this.CreateAttachmentFeedReader(attachmentsLink, options).ExecuteNextAsync(cancellationToken);
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Database" /> for a database account from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Database" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<Database> response = await client.ReadDatabaseFeedAsync(new FeedOptions
		///                                                                 {
		///                                                                     MaxItemCount = 10,
		///                                                                     RequestContinuation = continuation
		///                                                                 });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Database>> ReadDatabaseFeedAsync(FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDatabaseFeedPrivateAsync(options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<Database>> ReadDatabaseFeedPrivateAsync(FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			return await this.CreateDatabaseFeedReader(options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.PartitionKeyRange" /> for a database account from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="partitionKeyRangesOrCollectionLink">The link of the resources to be read, or owner collection link, SelfLink or AltLink. E.g. /dbs/db_rid/colls/coll_rid/pkranges</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Database" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// FeedResponse<PartitionKeyRange> response = null;
		/// List<string> ids = new List<string>();
		/// do
		/// {
		///     response = await client.ReadPartitionKeyRangeFeedAsync(collection.SelfLink, new FeedOptions { MaxItemCount = 1000 });
		///     foreach (var item in response)
		///     {
		///         ids.Add(item.Id);
		///     }
		/// }
		/// while (!string.IsNullOrEmpty(response.ResponseContinuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.PartitionKeyRange" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.FeedOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.FeedResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(string partitionKeyRangesOrCollectionLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadPartitionKeyRangeFeedPrivateAsync(partitionKeyRangesOrCollectionLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedPrivateAsync(string partitionKeyRangesLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(partitionKeyRangesLink))
			{
				throw new ArgumentNullException("partitionKeyRangesLink");
			}
			return await this.CreatePartitionKeyRangeFeedReader(partitionKeyRangesLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> for a database from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="collectionsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="collectionsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<DocumentCollection> response = await client.ReadDocumentCollectionFeedAsync("/dbs/db_rid/colls/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<DocumentCollection>> ReadDocumentCollectionFeedAsync(string collectionsLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDocumentCollectionFeedPrivateAsync(collectionsLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<DocumentCollection>> ReadDocumentCollectionFeedPrivateAsync(string collectionsLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionsLink))
			{
				throw new ArgumentNullException("collectionsLink");
			}
			return await this.CreateDocumentCollectionFeedReader(collectionsLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> for a collection from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="storedProceduresLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/col_rid/sprocs/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProceduresLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<StoredProcedure> response = await client.ReadStoredProcedureFeedAsync("/dbs/db_rid/colls/col_rid/sprocs/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<StoredProcedure>> ReadStoredProcedureFeedAsync(string storedProceduresLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadStoredProcedureFeedPrivateAsync(storedProceduresLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<StoredProcedure>> ReadStoredProcedureFeedPrivateAsync(string storedProceduresLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(storedProceduresLink))
			{
				throw new ArgumentNullException("storedProceduresLink");
			}
			return await this.CreateStoredProcedureFeedReader(storedProceduresLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Trigger" /> for a collection from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="triggersLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/col_rid/triggers/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Trigger" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="triggersLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<Trigger> response = await client.ReadTriggerFeedAsync("/dbs/db_rid/colls/col_rid/triggers/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Trigger>> ReadTriggerFeedAsync(string triggersLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadTriggerFeedPrivateAsync(triggersLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<Trigger>> ReadTriggerFeedPrivateAsync(string triggersLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(triggersLink))
			{
				throw new ArgumentNullException("triggersLink");
			}
			return await this.CreateTriggerFeedReader(triggersLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> for a collection from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userDefinedFunctionsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/col_rid/udfs/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userDefinedFunctionsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<UserDefinedFunction> response = await client.ReadUserDefinedFunctionFeedAsync("/dbs/db_rid/colls/col_rid/udfs/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<UserDefinedFunction>> ReadUserDefinedFunctionFeedAsync(string userDefinedFunctionsLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserDefinedFunctionFeedPrivateAsync(userDefinedFunctionsLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<UserDefinedFunction>> ReadUserDefinedFunctionFeedPrivateAsync(string userDefinedFunctionsLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userDefinedFunctionsLink))
			{
				throw new ArgumentNullException("userDefinedFunctionsLink");
			}
			return await this.CreateUserDefinedFunctionFeedReader(userDefinedFunctionsLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Permission" /> for a user from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="permissionsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/users/user_rid/permissions/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Permission" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permissionsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<Permission> response = await client.ReadPermissionFeedAsync("/dbs/db_rid/users/user_rid/permissions/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Permission>> ReadPermissionFeedAsync(string permissionsLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadPermissionFeedPrivateAsync(permissionsLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<Permission>> ReadPermissionFeedPrivateAsync(string permissionsLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(permissionsLink))
			{
				throw new ArgumentNullException("permissionsLink");
			}
			return await this.CreatePermissionFeedReader(permissionsLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of documents for a specified collection from the Azure Cosmos DB service.
		/// This takes returns a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which will contain an enumerable list of dynamic objects.
		/// </summary>
		/// <param name="documentsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/coll_rid/docs/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> containing dynamic objects representing the items in the feed.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<dynamic> response = await client.ReadDocumentFeedAsync("/dbs/db_rid/colls/coll_rid/docs/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// Instead of FeedResponse{Document} this method takes advantage of dynamic objects in .NET. This way a single feed result can contain any kind of Document, or POCO object.
		/// This is important becuse a DocumentCollection can contain different kinds of documents.
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<dynamic>> ReadDocumentFeedAsync(string documentsLink, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => ReadDocumentFeedInlineAsync(documentsLink, options, cancellationToken), null, cancellationToken);
		}

		private async Task<FeedResponse<dynamic>> ReadDocumentFeedInlineAsync(string documentsLink, FeedOptions options, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentsLink))
			{
				throw new ArgumentNullException("documentsLink");
			}
			FeedResponse<Document> feedResponse = await this.CreateDocumentFeedReader(documentsLink, options).ExecuteNextAsync(cancellationToken);
			return new FeedResponse<object>(feedResponse.Cast<object>(), feedResponse.Count, feedResponse.Headers, feedResponse.UseETagAsContinuation, feedResponse.QueryMetrics, feedResponse.RequestStatistics, null, feedResponse.ResponseLengthBytes);
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.User" /> for a database from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="usersLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/users/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.User" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="usersLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<User> response = await client.ReadUserFeedAsync("/dbs/db_rid/users/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<User>> ReadUserFeedAsync(string usersLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserFeedPrivateAsync(usersLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<User>> ReadUserFeedPrivateAsync(string usersLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(usersLink))
			{
				throw new ArgumentNullException("usersLink");
			}
			return await this.CreateUserFeedReader(usersLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Conflict" /> for a collection from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="conflictsLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/coll_rid/conflicts/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Conflict" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="conflictsLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<Conflict> response = await client.ReadConflictAsync("/dbs/db_rid/colls/coll_rid/conflicts/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Conflict>> ReadConflictFeedAsync(string conflictsLink, FeedOptions options = null)
		{
			return TaskHelper.InlineIfPossible(() => ReadConflictFeedInlineAsync(conflictsLink, options), null);
		}

		private async Task<FeedResponse<Conflict>> ReadConflictFeedInlineAsync(string conflictsLink, FeedOptions options)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(conflictsLink))
			{
				throw new ArgumentNullException("conflictsLink");
			}
			return await this.CreateConflictFeedReader(conflictsLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Offer" /> for a database account from the Azure Cosmos DB service
		/// as an asynchronous operation.
		/// </summary>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Offer" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<Offer> response = await client.ReadOfferAsync(new FeedOptions
		///                                                                 {
		///                                                                     MaxItemCount = 10,
		///                                                                     RequestContinuation = continuation
		///                                                                 });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<Offer>> ReadOffersFeedAsync(FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadOfferFeedPrivateAsync(options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<Offer>> ReadOfferFeedPrivateAsync(FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			return await this.CreateOfferFeedReader(options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.Schema" /> for a collection as an asynchronous operation.
		/// </summary>
		/// <param name="documentCollectionSchemaLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/colls/coll_rid/schemas </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Schema" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<User> response = await client.ReadUserFeedAsync("/dbs/db_rid/colls/coll_rid/schemas",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Schema" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<FeedResponse<Schema>> ReadSchemaFeedAsync(string documentCollectionSchemaLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadSchemaFeedPrivateAsync(documentCollectionSchemaLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<Schema>> ReadSchemaFeedPrivateAsync(string documentCollectionSchemaLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentCollectionSchemaLink))
			{
				throw new ArgumentNullException("documentCollectionSchemaLink");
			}
			return await this.CreateSchemaFeedReader(documentCollectionSchemaLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> for a database from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="userDefinedTypesLink">The SelfLink of the resources to be read. E.g. /dbs/db_rid/udts/ </param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userDefinedTypesLink" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a UserDefinedType are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource feed you tried to read did not exist. Check the parent rids are correct.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// int count = 0;
		/// string continuation = string.Empty;
		/// do
		/// {
		///     // Read the feed 10 items at a time until there are no more items to read
		///     FeedResponse<UserDefinedType> response = await client.ReadUserDefinedTypeFeedAsync("/dbs/db_rid/udts/",
		///                                                     new FeedOptions
		///                                                     {
		///                                                         MaxItemCount = 10,
		///                                                         RequestContinuation = continuation
		///                                                     });
		///
		///     // Append the item count
		///     count += response.Count;
		///
		///     // Get the continuation so that we know when to stop.
		///      continuation = response.ResponseContinuation;
		/// } while (!string.IsNullOrEmpty(continuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<FeedResponse<UserDefinedType>> ReadUserDefinedTypeFeedAsync(string userDefinedTypesLink, FeedOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadUserDefinedTypeFeedPrivateAsync(userDefinedTypesLink, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<FeedResponse<UserDefinedType>> ReadUserDefinedTypeFeedPrivateAsync(string userDefinedTypesLink, FeedOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userDefinedTypesLink))
			{
				throw new ArgumentNullException("userDefinedTypesLink");
			}
			return await this.CreateUserDefinedTypeFeedReader(userDefinedTypesLink, options).ExecuteNextAsync();
		}

		/// <summary>
		/// Executes a stored procedure against a collection as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TValue">The type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureLink">The link to the stored procedure to execute.</param>
		/// <param name="procedureParams">(Optional) An array of dynamic objects representing the parameters for the stored procedure.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureLink" /> is not set.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation which would contain any response set in the stored procedure.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Execute a StoredProcedure with ResourceId of "sproc_rid" that takes two "Player" documents, does some stuff, and returns a bool
		/// StoredProcedureResponse<bool> sprocResponse = await client.ExecuteStoredProcedureAsync<bool>(
		///                                                         "/dbs/db_rid/colls/col_rid/sprocs/sproc_rid/",
		///                                                         new Player { id="1", name="joe" } ,
		///                                                         new Player { id="2", name="john" }
		///                                                     );
		///
		/// if (sprocResponse.Response) Console.WriteLine("Congrats, the stored procedure did some stuff");
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.StoredProcedureResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, params dynamic[] procedureParams)
		{
			return ExecuteStoredProcedureAsync<TValue>(storedProcedureLink, null, default(CancellationToken), procedureParams);
		}

		/// <summary>
		/// Executes a stored procedure against a partitioned collection in the Azure Cosmos DB service as an asynchronous operation, specifiying a target partition.
		/// </summary>
		/// <typeparam name="TValue">The type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureLink">The link to the stored procedure to execute.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="procedureParams">(Optional) An array of dynamic objects representing the parameters for the stored procedure.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureLink" /> is not set.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation which would contain any response set in the stored procedure.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Execute a StoredProcedure with ResourceId of "sproc_rid" that takes two "Player" documents, does some stuff, and returns a bool
		/// StoredProcedureResponse<bool> sprocResponse = await client.ExecuteStoredProcedureAsync<bool>(
		///                                                         "/dbs/db_rid/colls/col_rid/sprocs/sproc_rid/",
		///                                                         new RequestOptions { PartitionKey = new PartitionKey(1) },
		///                                                         new Player { id="1", name="joe" } ,
		///                                                         new Player { id="2", name="john" }
		///                                                     );
		///
		/// if (sprocResponse.Response) Console.WriteLine("Congrats, the stored procedure did some stuff");
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.StoredProcedureResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, RequestOptions options, params dynamic[] procedureParams)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ExecuteStoredProcedurePrivateAsync<TValue>(storedProcedureLink, options, retryPolicyInstance, default(CancellationToken), procedureParams), retryPolicyInstance);
		}

		/// <summary>
		/// Executes a stored procedure against a partitioned collection in the Azure Cosmos DB service as an asynchronous operation, specifiying a target partition.
		/// </summary>
		/// <typeparam name="TValue">The type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureLink">The link to the stored procedure to execute.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <param name="procedureParams">(Optional) An array of dynamic objects representing the parameters for the stored procedure.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureLink" /> is not set.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation which would contain any response set in the stored procedure.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Execute a StoredProcedure with ResourceId of "sproc_rid" that takes two "Player" documents, does some stuff, and returns a bool
		/// StoredProcedureResponse<bool> sprocResponse = await client.ExecuteStoredProcedureAsync<bool>(
		///                                                         "/dbs/db_rid/colls/col_rid/sprocs/sproc_rid/",
		///                                                         new RequestOptions { PartitionKey = new PartitionKey(1) },
		///                                                         new Player { id="1", name="joe" } ,
		///                                                         new Player { id="2", name="john" }
		///                                                     );
		///
		/// if (sprocResponse.Response) Console.WriteLine("Congrats, the stored procedure did some stuff");
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.StoredProcedureResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(string storedProcedureLink, RequestOptions options, CancellationToken cancellationToken, params dynamic[] procedureParams)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ExecuteStoredProcedurePrivateAsync<TValue>(storedProcedureLink, options, retryPolicyInstance, cancellationToken, procedureParams), retryPolicyInstance, cancellationToken);
		}

		private async Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedurePrivateAsync<TValue>(string storedProcedureLink, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken, params dynamic[] procedureParams)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(storedProcedureLink))
			{
				throw new ArgumentNullException("storedProcedureLink");
			}
			JsonSerializerSettings serializerSettingsForRequest = GetSerializerSettingsForRequest(options);
			string value = (serializerSettingsForRequest == null) ? JsonConvert.SerializeObject(procedureParams) : JsonConvert.SerializeObject(procedureParams, serializerSettingsForRequest);
			using (MemoryStream storedProcedureInputStream = new MemoryStream())
			{
				using (StreamWriter writer = new StreamWriter(storedProcedureInputStream))
				{
					writer.Write(value);
					writer.Flush();
					storedProcedureInputStream.Position = 0L;
					using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.ExecuteJavaScript, ResourceType.StoredProcedure, storedProcedureLink, storedProcedureInputStream, AuthorizationTokenType.PrimaryMasterKey, GetRequestHeaders(options)))
					{
						request.Headers["x-ms-date"] = DateTime.UtcNow.ToString("r");
						if (options == null || options.PartitionKeyRangeId == null)
						{
							await AddPartitionKeyInformationAsync(request, options);
						}
						retryPolicyInstance?.OnBeforeSendRequest(request);
						request.SerializerSettings = GetSerializerSettingsForRequest(options);
						return new StoredProcedureResponse<TValue>(await ExecuteProcedureAsync(request, retryPolicyInstance, cancellationToken), GetSerializerSettingsForRequest(options));
					}
				}
			}
		}

		/// <summary>
		/// Upserts an attachment as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentLink">The link of the parent document for this new attachment. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/ </param>
		/// <param name="attachment">The attachment object.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>
		/// The <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.
		/// </returns>
		/// <remarks>
		///
		/// </remarks>
		/// <example>
		/// The example below creates a new document, and then upserts a new attachment for that document
		/// <code language="c#">
		/// <![CDATA[
		/// dynamic d = new
		/// {
		///     id = "DOC1800243243470"
		/// };
		///
		/// Document doc = await client.CreateDocumentAsync(collectionSelfLink, d);
		///
		/// //Upsert an Attachment which links to binary content stored somewhere else
		/// //Use the MediaLink property of Attachment to set where the binary resides
		/// //MediaLink can also point at another Attachment within Azure Cosmos DB.
		/// Attachment a = await client.UpsertAttachmentAsync(doc.SelfLink, new Attachment { Id = "foo", ContentType = "text/plain", MediaLink = "link to your media" });
		///
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(string documentLink, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertAttachmentPrivateAsync(documentLink, attachment, options, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> UpsertAttachmentPrivateAsync(string documentLink, object attachment, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentLink))
			{
				throw new ArgumentNullException("documentLink");
			}
			if (attachment == null)
			{
				throw new ArgumentNullException("attachment");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			Attachment resource = Attachment.FromObject(attachment);
			ValidateResource(resource);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, documentLink, resource, ResourceType.Attachment, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				if (!request.IsValidAddress(ResourceType.Document))
				{
					throw new ArgumentException(RMResources.BadUrl, "link");
				}
				await AddPartitionKeyInformationAsync(request, options);
				return new ResourceResponse<Attachment>(await UpsertAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Upserts a database resource as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="database">The specification for the <see cref="T:Microsoft.Azure.Documents.Database" /> to upsert.</param>
		/// <param name="options">(Optional) The <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> for the request.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.Database" /> that was upserted within a task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="database" /> is not set</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Database are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the database object supplied. It is likely that an id was not supplied for the new Database.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Database" /> with an id matching the id field of <paramref name="database" /> already existed</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// The example below upserts a new <see cref="T:Microsoft.Azure.Documents.Database" /> with an Id property of 'MyDatabase'
		/// This code snippet is intended to be used from within an Asynchronous method as it uses the await keyword
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Database db = await client.UpsertDatabaseAsync(new Database { Id = "MyDatabase" });
		/// }
		/// ]]>
		/// </code>
		///
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<Database>> UpsertDatabaseAsync(Database database, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertDatabasePrivateAsync(database, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Database>> UpsertDatabasePrivateAsync(Database database, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (database == null)
			{
				throw new ArgumentNullException("database");
			}
			ValidateResource(database);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, "//dbs/", database, ResourceType.Database, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Database>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a Document as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentsFeedOrDatabaseLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to upsert the document in. E.g. dbs/db_rid/colls/coll_rid/ </param>
		/// <param name="document">The document object to upsert.</param>
		/// <param name="options">(Optional) Any request options you wish to set. E.g. Specifying a Trigger to execute when creating the document. <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /></param>
		/// <param name="disableAutomaticIdGeneration">(Optional) Disables the automatic id generation, If this is True the system will throw an exception if the id property is missing from the Document.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.Document" /> that was upserted contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="documentsFeedOrDatabaseLink" /> or <paramref name="document" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the document supplied. It is likely that <paramref name="disableAutomaticIdGeneration" /> was true and an id was not supplied</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - This likely means the collection in to which you were trying to upsert the document is full.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Document" /> with an id matching the id field of <paramref name="document" /> already existed</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the <see cref="T:Microsoft.Azure.Documents.Document" /> exceeds the current max entity size. Consult documentation for limits and quotas.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// Azure Cosmos DB supports a number of different ways to work with documents. A document can extend <see cref="T:Microsoft.Azure.Documents.Resource" />
		/// <code language="c#">
		/// <![CDATA[
		/// public class MyObject : Resource
		/// {
		///     public string MyProperty {get; set;}
		/// }
		///
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.UpsertDocumentAsync("dbs/db_rid/colls/coll_rid/", new MyObject { MyProperty = "A Value" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// A document can be any POCO object that can be serialized to JSON, even if it doesn't extend from <see cref="T:Microsoft.Azure.Documents.Resource" />
		/// <code language="c#">
		/// <![CDATA[
		/// public class MyPOCO
		/// {
		///     public string MyProperty {get; set;}
		/// }
		///
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.UpsertDocumentAsync("dbs/db_rid/colls/coll_rid/", new MyPOCO { MyProperty = "A Value" });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// A Document can also be a dynamic object
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.UpsertDocumentAsync("dbs/db_rid/colls/coll_rid/", new { SomeProperty = "A Value" } );
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// Upsert a Document and execute a Pre and Post Trigger
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     Document doc = await client.UpsertDocumentAsync(
		///         "dbs/db_rid/colls/coll_rid/",
		///         new { id = "DOC123213443" },
		///         new RequestOptions
		///         {
		///             PreTriggerInclude = new List<string> { "MyPreTrigger" },
		///             PostTriggerInclude = new List<string> { "MyPostTrigger" }
		///         });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> UpsertDocumentAsync(string documentsFeedOrDatabaseLink, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => UpsertDocumentInlineAsync(documentsFeedOrDatabaseLink, document, options, disableAutomaticIdGeneration, cancellationToken), null, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> UpsertDocumentInlineAsync(string documentsFeedOrDatabaseLink, object document, RequestOptions options, bool disableAutomaticIdGeneration, CancellationToken cancellationToken)
		{
        	#pragma warning disable 612, 618
			IPartitionResolver value = null;
			IDocumentClientRetryPolicy requestRetryPolicy = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			if (PartitionResolvers.TryGetValue(documentsFeedOrDatabaseLink, out value))
			{
				object partitionKey = value.GetPartitionKey(document);
				string collectionLink = value.ResolveForCreate(partitionKey);
				return await TaskHelper.InlineIfPossible(() => UpsertDocumentPrivateAsync(collectionLink, document, options, disableAutomaticIdGeneration, requestRetryPolicy, cancellationToken), requestRetryPolicy, cancellationToken);
			}
			if (options == null || options.PartitionKey == null)
			{
				requestRetryPolicy = new PartitionKeyMismatchRetryPolicy(await GetCollectionCacheAsync(), requestRetryPolicy);
			}
        	#pragma warning restore 612, 618
			return await TaskHelper.InlineIfPossible(() => UpsertDocumentPrivateAsync(documentsFeedOrDatabaseLink, document, options, disableAutomaticIdGeneration, requestRetryPolicy, cancellationToken), requestRetryPolicy, cancellationToken);
		}

		private async Task<ResourceResponse<Document>> UpsertDocumentPrivateAsync(string documentCollectionLink, object document, RequestOptions options, bool disableAutomaticIdGeneration, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(documentCollectionLink))
			{
				throw new ArgumentNullException("documentCollectionLink");
			}
			if (document == null)
			{
				throw new ArgumentNullException("document");
			}
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			Document document2 = Document.FromObject(document, GetSerializerSettingsForRequest(options));
			ValidateResource(document2);
			if (string.IsNullOrEmpty(document2.Id) && !disableAutomaticIdGeneration)
			{
				document2.Id = Guid.NewGuid().ToString();
			}
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, documentCollectionLink, document2, ResourceType.Document, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				await AddPartitionKeyInformationAsync(request, document2, options);
				request.SerializerSettings = GetSerializerSettingsForRequest(options);
				return new ResourceResponse<Document>(await UpsertAsync(request, retryPolicyInstance, cancellationToken));
			}
		}

		/// <summary>
		/// Upserts a collection as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">The link of the database to upsert the collection in. E.g. dbs/db_rid/</param>
		/// <param name="documentCollection">The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> you wish to provide when creating a Collection. E.g. RequestOptions.OfferThroughput = 400. </param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> that was upserted contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="documentCollection" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an id was not supplied for the new collection.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - This means you attempted to exceed your quota for collections. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// using (IDocumentClient client = new DocumentClient(new Uri("service endpoint"), "auth key"))
		/// {
		///     //Upsert a new collection with an OfferThroughput set to 10000
		///     //Not passing in RequestOptions.OfferThroughput will result in a collection with the default OfferThroughput set.
		///     DocumentCollection coll = await client.UpsertDocumentCollectionAsync(databaseLink,
		///         new DocumentCollection { Id = "My Collection" },
		///         new RequestOptions { OfferThroughput = 10000} );
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<DocumentCollection>> UpsertDocumentCollectionAsync(string databaseLink, DocumentCollection documentCollection, RequestOptions options = null)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Upserts a stored procedure as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the collection to upsert the stored procedure in. E.g. dbs/db_rid/colls/col_rid/</param>
		/// <param name="storedProcedure">The <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> object to upsert.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> that was upserted contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="storedProcedure" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the stored procedure or the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of stored procedures for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> you tried to upsert was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a new stored procedure called "HelloWorldSproc" that takes in a single param called "name".
		/// StoredProcedure sproc = await client.UpsertStoredProcedureAsync(collectionLink, new StoredProcedure
		/// {
		///    Id = "HelloWorldSproc",
		///    Body = @"function (name){
		///                var response = getContext().getResponse();
		///                response.setBody('Hello ' + name);
		///             }"
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<StoredProcedure>> UpsertStoredProcedureAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertStoredProcedurePrivateAsync(collectionLink, storedProcedure, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<StoredProcedure>> UpsertStoredProcedurePrivateAsync(string collectionLink, StoredProcedure storedProcedure, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (storedProcedure == null)
			{
				throw new ArgumentNullException("storedProcedure");
			}
			ValidateResource(storedProcedure);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, collectionLink, storedProcedure, ResourceType.StoredProcedure, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<StoredProcedure>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a trigger as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to upsert the trigger in. E.g. dbs/db_rid/colls/col_rid/ </param>
		/// <param name="trigger">The <see cref="T:Microsoft.Azure.Documents.Trigger" /> object to upsert.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="trigger" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the new trigger or that the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of triggers for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Trigger" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.Trigger" /> you tried to upsert was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a trigger that validates the contents of a document as it is created and adds a 'timestamp' property if one was not found.
		/// Trigger trig = await client.UpsertTriggerAsync(collectionLink, new Trigger
		/// {
		///     Id = "ValidateDocuments",
		///     Body = @"function validate() {
		///                         var context = getContext();
		///                         var request = context.getRequest();                                                             
		///                         var documentToCreate = request.getBody();
		///
		///                         // validate properties
		///                         if (!('timestamp' in documentToCreate)) {
		///                             var ts = new Date();
		///                             documentToCreate['timestamp'] = ts.getTime();
		///                         }
		///
		///                         // update the document that will be created
		///                         request.setBody(documentToCreate);
		///                       }",
		///     TriggerType = TriggerType.Pre,
		///     TriggerOperation = TriggerOperation.Create
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Trigger>> UpsertTriggerAsync(string collectionLink, Trigger trigger, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertTriggerPrivateAsync(collectionLink, trigger, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Trigger>> UpsertTriggerPrivateAsync(string collectionLink, Trigger trigger, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (trigger == null)
			{
				throw new ArgumentNullException("trigger");
			}
			ValidateResource(trigger);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, collectionLink, trigger, ResourceType.Trigger, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Trigger>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a user defined function as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">The link of the <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> to upsert the user defined function in. E.g. dbs/db_rid/colls/col_rid/ </param>
		/// <param name="function">The <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> object to upsert.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />for this request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="collectionLink" /> or <paramref name="function" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied. It is likely that an Id was not supplied for the new user defined function or that the Body was malformed.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user defined functions for the collection supplied. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		///     <item>
		///         <term>413</term><description>RequestEntityTooLarge - This means the body of the <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> you tried to upsert was too large.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a user defined function that converts a string to upper case
		/// UserDefinedFunction udf = client.UpsertUserDefinedFunctionAsync(collectionLink, new UserDefinedFunction
		/// {
		///    Id = "ToUpper",
		///    Body = @"function toUpper(input) {
		///                        return input.toUpperCase();
		///                     }",
		/// });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<UserDefinedFunction>> UpsertUserDefinedFunctionAsync(string collectionLink, UserDefinedFunction function, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertUserDefinedFunctionPrivateAsync(collectionLink, function, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedFunction>> UpsertUserDefinedFunctionPrivateAsync(string collectionLink, UserDefinedFunction function, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(collectionLink))
			{
				throw new ArgumentNullException("collectionLink");
			}
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			ValidateResource(function);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, collectionLink, function, ResourceType.UserDefinedFunction, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedFunction>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a permission on a user object in the Azure Cosmos DB service as an asychronous operation.
		/// </summary>
		/// <param name="userLink">The link of the user to Upsert the permission for. E.g. dbs/db_rid/users/user_rid/ </param>
		/// <param name="permission">The <see cref="T:Microsoft.Azure.Documents.Permission" /> object.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the upserted <see cref="T:Microsoft.Azure.Documents.Permission" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="userLink" /> or <paramref name="permission" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of permission objects. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.Permission" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a read-only permission object for a specific user
		/// Permission p = await client.UpsertPermissionAsync(userLink, new Permission { Id = "ReadPermission", PermissionMode = PermissionMode.Read });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Permission>> UpsertPermissionAsync(string userLink, Permission permission, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertPermissionPrivateAsync(userLink, permission, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<Permission>> UpsertPermissionPrivateAsync(string userLink, Permission permission, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(userLink))
			{
				throw new ArgumentNullException("userLink");
			}
			if (permission == null)
			{
				throw new ArgumentNullException("permission");
			}
			ValidateResource(permission);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, userLink, permission, ResourceType.Permission, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<Permission>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a permission on a user object in the Azure Cosmos DB service as an asychronous operation.
		/// </summary>
		/// <param name="databaseLink">The link of the database to upsert the user in. E.g. dbs/db_rid/ </param>
		/// <param name="user">The <see cref="T:Microsoft.Azure.Documents.User" /> object to upsert.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the upserted <see cref="T:Microsoft.Azure.Documents.User" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="user" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user objects for this database. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.User" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a new user called joeBloggs in the specified database
		/// User user = await client.UpsertUserAsync(databaseLink, new User { Id = "joeBloggs" });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<User>> UpsertUserAsync(string databaseLink, User user, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertUserPrivateAsync(databaseLink, user, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<User>> UpsertUserPrivateAsync(string databaseLink, User user, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (user == null)
			{
				throw new ArgumentNullException("user");
			}
			ValidateResource(user);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, databaseLink, user, ResourceType.User, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<User>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Upserts a user defined type object in the Azure Cosmos DB service as an asychronous operation.
		/// </summary>
		/// <param name="databaseLink">The link of the database to upsert the user defined type in. E.g. dbs/db_rid/ </param>
		/// <param name="userDefinedType">The <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> object to upsert.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>A task object representing the service response for the asynchronous operation which contains the upserted <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> object.</returns>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="databaseLink" /> or <paramref name="userDefinedType" /> is not set.</exception>
		/// <exception cref="T:System.AggregateException">Represents a consolidation of failures that occured during async processing. Look within InnerExceptions to find the actual exception(s)</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>400</term><description>BadRequest - This means something was wrong with the request supplied.</description>
		///     </item>
		///     <item>
		///         <term>403</term><description>Forbidden - You have reached your quota of user defined type objects for this database. Contact support to have this quota increased.</description>
		///     </item>
		///     <item>
		///         <term>409</term><description>Conflict - This means a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> with an id matching the id you supplied already existed.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		///
		/// <code language="c#">
		/// <![CDATA[
		/// //Upsert a new user defined type in the specified database
		/// UserDefinedType userDefinedType = await client.UpsertUserDefinedTypeAsync(databaseLink, new UserDefinedType { Id = "userDefinedTypeId5" });
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<UserDefinedType>> UpsertUserDefinedTypeAsync(string databaseLink, UserDefinedType userDefinedType, RequestOptions options = null)
		{
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertUserDefinedTypePrivateAsync(databaseLink, userDefinedType, options, retryPolicyInstance), retryPolicyInstance);
		}

		private async Task<ResourceResponse<UserDefinedType>> UpsertUserDefinedTypePrivateAsync(string databaseLink, UserDefinedType userDefinedType, RequestOptions options, IDocumentClientRetryPolicy retryPolicyInstance)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentNullException("databaseLink");
			}
			if (userDefinedType == null)
			{
				throw new ArgumentNullException("userDefinedType");
			}
			ValidateResource(userDefinedType);
			INameValueCollection requestHeaders = GetRequestHeaders(options);
			using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, databaseLink, userDefinedType, ResourceType.UserDefinedType, AuthorizationTokenType.PrimaryMasterKey, requestHeaders))
			{
				return new ResourceResponse<UserDefinedType>(await UpsertAsync(request, retryPolicyInstance));
			}
		}

		/// <summary>
		/// Creates an <see cref="T:Microsoft.Azure.Documents.Attachment" /> with the contents of the provided <paramref name="mediaStream" /> as an asynchronous operation
		///  in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsLink">The attachments link for the document. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/attachments/ </param>
		/// <param name="mediaStream">the <see cref="T:System.IO.Stream" /> of the attachment media.</param>
		/// <param name="options">the <see cref="T:Microsoft.Azure.Documents.Client.MediaOptions" /> for the request.</param>
		/// <param name="requestOptions">Request options.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="attachmentsLink" /> or <paramref name="mediaStream" /> is not set.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This attachment could be any binary you want to attach. Like images, videos, word documents, pdfs etc. it doesn't matter
		/// using (FileStream fileStream = new FileStream(@".\something.pdf", FileMode.Open))
		/// {
		///     //Create the attachment
		///     Attachment attachment = await client.CreateAttachmentAsync("dbs/db_rid/colls/coll_rid/docs/doc_rid/attachments/",
		///                                         fileStream,
		///                                         new MediaOptions
		///                                         {
		///                                             ContentType = "application/pdf",
		///                                             Slug = "something.pdf"
		///                                         });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.IO.Stream" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string attachmentsLink, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = retryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => CreateAttachmentPrivateAsync(attachmentsLink, mediaStream, options, requestOptions, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> CreateAttachmentPrivateAsync(string attachmentsLink, Stream mediaStream, MediaOptions options, RequestOptions requestOptions, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(attachmentsLink))
			{
				throw new ArgumentNullException("attachmentsLink");
			}
			if (mediaStream == null)
			{
				throw new ArgumentNullException("mediaStream");
			}
			using (StreamContent streamContent = new StreamContent(mediaStream))
			{
				StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
				if (options == null || string.IsNullOrEmpty(options.ContentType))
				{
					streamContent.Headers.Add("Content-Type", "application/octet-stream");
					((INameValueCollection)nameValueCollection).Set("Content-Type", "application/octet-stream");
				}
				if (options != null)
				{
					if (options.ContentType != null)
					{
						streamContent.Headers.Add("Content-Type", options.ContentType);
						((INameValueCollection)nameValueCollection).Set("Content-Type", options.ContentType);
					}
					if (options.Slug != null)
					{
						streamContent.Headers.Add("Slug", options.Slug);
						((INameValueCollection)nameValueCollection).Set("Slug", options.Slug);
					}
				}
				using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, ResourceType.Attachment, attachmentsLink, AuthorizationTokenType.PrimaryMasterKey))
				{
					if (!request.IsValidAddress(ResourceType.Document))
					{
						throw new ArgumentException(RMResources.BadUrl, "link");
					}
					retryPolicyInstance?.OnBeforeSendRequest(request);
					Uri requestUri = new Uri(globalEndpointManager.ResolveServiceEndpoint(request), PathsHelper.GeneratePath(ResourceType.Attachment, request, isFeed: true));
					using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
					{
						string value = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
						requestMessage.Headers.Add("x-ms-date", value);
						((INameValueCollection)nameValueCollection).Set("x-ms-date", value);
						if (requestOptions != null && requestOptions.PartitionKey != null)
						{
							string value2 = requestOptions.PartitionKey.ToString();
							if (!string.IsNullOrEmpty(value2))
							{
								requestMessage.Headers.Add("x-ms-documentdb-partitionkey", value2);
								((INameValueCollection)nameValueCollection).Set("x-ms-documentdb-partitionkey", value2);
							}
						}
						requestMessage.Content = streamContent;
						requestMessage.Headers.Add("authorization", ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(ResourceType.Attachment), "POST", (INameValueCollection)nameValueCollection, AuthorizationTokenType.PrimaryMasterKey));
						using (HttpResponseMessage response = await mediaClient.SendHttpAsync(requestMessage, cancellationToken))
						{
							return new ResourceResponse<Attachment>(await ClientExtensions.ParseResponseAsync(response));
						}
					}
				}
			}
		}

		/// <summary>
		/// Upserts an <see cref="T:Microsoft.Azure.Documents.Attachment" /> with the contents of the provided <paramref name="mediaStream" /> as an asynchronous operation
		///  in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsLink">The attachments link for the document. E.g. dbs/db_rid/colls/col_rid/docs/doc_rid/attachments/ </param>
		/// <param name="mediaStream">the <see cref="T:System.IO.Stream" /> of the attachment media.</param>
		/// <param name="options">the <see cref="T:Microsoft.Azure.Documents.Client.MediaOptions" /> for the request.</param>
		/// <param name="requestOptions">the <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="attachmentsLink" /> or <paramref name="mediaStream" /> is not set.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This attachment could be any binary you want to attach. Like images, videos, word documents, pdfs etc. it doesn't matter
		/// using (FileStream fileStream = new FileStream(@".\something.pdf", FileMode.Open))
		/// {
		///     //Upsert the attachment
		///     Attachment attachment = await client.UpsertAttachmentAsync("dbs/db_rid/colls/coll_rid/docs/doc_rid/attachments/",
		///                                         fileStream,
		///                                         new MediaOptions
		///                                         {
		///                                             ContentType = "application/pdf",
		///                                             Slug = "something.pdf"
		///                                         });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.IO.Stream" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(string attachmentsLink, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			IDocumentClientRetryPolicy retryPolicyInstance = retryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => UpsertAttachmentPrivateAsync(attachmentsLink, mediaStream, options, requestOptions, retryPolicyInstance, cancellationToken), retryPolicyInstance, cancellationToken);
		}

		private async Task<ResourceResponse<Attachment>> UpsertAttachmentPrivateAsync(string attachmentsLink, Stream mediaStream, MediaOptions options, RequestOptions requestOptions, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(attachmentsLink))
			{
				throw new ArgumentNullException("attachmentsLink");
			}
			if (mediaStream == null)
			{
				throw new ArgumentNullException("mediaStream");
			}
			using (StreamContent streamContent = new StreamContent(mediaStream))
			{
				StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
				if (options == null || string.IsNullOrEmpty(options.ContentType))
				{
					streamContent.Headers.Add("Content-Type", "application/octet-stream");
					((INameValueCollection)nameValueCollection).Set("Content-Type", "application/octet-stream");
				}
				if (options != null)
				{
					if (options.ContentType != null)
					{
						streamContent.Headers.Add("Content-Type", options.ContentType);
						((INameValueCollection)nameValueCollection).Set("Content-Type", options.ContentType);
					}
					if (options.Slug != null)
					{
						streamContent.Headers.Add("Slug", options.Slug);
						((INameValueCollection)nameValueCollection).Set("Slug", options.Slug);
					}
				}
				using (DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Upsert, ResourceType.Attachment, attachmentsLink, AuthorizationTokenType.PrimaryMasterKey))
				{
					if (!request.IsValidAddress(ResourceType.Document))
					{
						throw new ArgumentException(RMResources.BadUrl, "link");
					}
					retryPolicyInstance?.OnBeforeSendRequest(request);
					Uri requestUri = new Uri(globalEndpointManager.ResolveServiceEndpoint(request), PathsHelper.GeneratePath(ResourceType.Attachment, request, isFeed: true));
					using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
					{
						string value = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
						requestMessage.Headers.Add("x-ms-date", value);
						requestMessage.Headers.Add("x-ms-documentdb-is-upsert", bool.TrueString);
						((INameValueCollection)nameValueCollection).Set("x-ms-date", value);
						if (requestOptions != null)
						{
							if (requestOptions.PartitionKey != null)
							{
								string value2 = requestOptions.PartitionKey.ToString();
								if (!string.IsNullOrEmpty(value2))
								{
									requestMessage.Headers.Add("x-ms-documentdb-partitionkey", value2);
									((INameValueCollection)nameValueCollection).Set("x-ms-documentdb-partitionkey", value2);
								}
							}
							if (requestOptions.AccessCondition != null)
							{
								if (requestOptions.AccessCondition.Type == AccessConditionType.IfMatch)
								{
									requestMessage.Headers.Add("If-Match", requestOptions.AccessCondition.Condition);
									((INameValueCollection)nameValueCollection).Set("If-Match", requestOptions.AccessCondition.Condition);
								}
								else
								{
									requestMessage.Headers.Add("If-None-Match", requestOptions.AccessCondition.Condition);
									((INameValueCollection)nameValueCollection).Set("If-None-Match", requestOptions.AccessCondition.Condition);
								}
							}
						}
						requestMessage.Content = streamContent;
						requestMessage.Headers.Add("authorization", ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(ResourceType.Attachment), "POST", (INameValueCollection)nameValueCollection, AuthorizationTokenType.PrimaryMasterKey));
						using (HttpResponseMessage response = await mediaClient.SendHttpAsync(requestMessage, cancellationToken))
						{
							return new ResourceResponse<Attachment>(await ClientExtensions.ParseResponseAsync(response));
						}
					}
				}
			}
		}

		/// <summary>
		/// Replaces the specified media's content as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="mediaLink">The link for the media to be updated. /media/media_rid </param>
		/// <param name="mediaStream">The <see cref="T:System.IO.Stream" /> of the attachment media.</param>
		/// <param name="options">The <see cref="T:Microsoft.Azure.Documents.Client.MediaOptions" /> for the request.</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <exception cref="T:System.ArgumentNullException">If either <paramref name="mediaLink" /> or <paramref name="mediaStream" /> is not set.</exception>
		/// <exception cref="T:System.ArgumentException">If <paramref name="mediaLink" /> is not in the form of /media/{mediaId}.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //This attachment could be any binary you want to attach. Like images, videos, word documents, pdfs etc. it doesn't matter
		/// using (FileStream fileStream = new FileStream(@".\something.pdf", FileMode.Open))
		/// {
		///     //Update the attachment media
		///     await client.UpdateMediaAsync("/media/media_rid", fileStream,
		///                     new MediaOptions
		///                     {
		///                         ContentType = "application/pdf",
		///                         Slug = "something.pdf"
		///                     });
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.MediaOptions" />ReadMediaMetadataAsync
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.MediaResponse" />
		/// <seealso cref="T:System.IO.Stream" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<MediaResponse> UpdateMediaAsync(string mediaLink, Stream mediaStream, MediaOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => UpdateMediaPrivateAsync(mediaLink, mediaStream, options, cancellationToken), ResetSessionTokenRetryPolicy.GetRequestPolicy(), cancellationToken);
		}

		private async Task<MediaResponse> UpdateMediaPrivateAsync(string mediaLink, Stream mediaStream, MediaOptions options, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(mediaLink))
			{
				throw new ArgumentNullException("mediaLink");
			}
			if (mediaStream == null)
			{
				throw new ArgumentNullException("mediaStream");
			}
			string[] array = UrlUtility.SplitAndRemoveEmptyEntries(mediaLink, new char[1]
			{
				'/'
			});
			if (array.Length != 2)
			{
				throw new ArgumentException(RMResources.InvalidUrl, "mediaLink");
			}
			using (StreamContent streamContent = new StreamContent(mediaStream))
			{
				if (options != null)
				{
					if (options.ContentType != null)
					{
						streamContent.Headers.Add("Content-Type", options.ContentType);
					}
					if (options.Slug != null)
					{
						streamContent.Headers.Add("Slug", options.Slug);
					}
				}
				using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(WriteEndpoint, mediaLink)))
				{
					string attachmentId = GetAttachmentId(array[1]);
					string value = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
					StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
					((INameValueCollection)nameValueCollection).Set("x-ms-date", value);
					requestMessage.Headers.Add("x-ms-date", value);
					requestMessage.Content = streamContent;
					requestMessage.Headers.Add("authorization", ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(attachmentId, "media", "PUT", (INameValueCollection)nameValueCollection, AuthorizationTokenType.PrimaryMasterKey));
					using (HttpResponseMessage response = await mediaClient.SendHttpAsync(requestMessage, cancellationToken))
					{
						using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(response))
						{
							MediaResponse mediaResponse = new MediaResponse
							{
								ContentType = documentServiceResponse.Headers["Content-Type"],
								Slug = documentServiceResponse.Headers["Slug"],
								ActivityId = documentServiceResponse.Headers["x-ms-activity-id"]
							};
							string text = documentServiceResponse.Headers["Content-Length"];
							if (!string.IsNullOrEmpty(text))
							{
								mediaResponse.ContentLength = long.Parse(text, CultureInfo.InvariantCulture);
							}
							mediaResponse.Headers = documentServiceResponse.Headers.Clone();
							return mediaResponse;
						}
					}
				}
			}
		}

		/// <summary>
		/// Retrieves the metadata associated with the specified attachment content (aka media) as an asynchronous operation
		///  from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="mediaLink">The link for the media to read metadata for. E.g. /media/media_rid </param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="mediaLink" /> is not set.</exception>
		/// <exception cref="T:System.ArgumentException">If <paramref name="mediaLink" /> is not in the form of /media/{mediaId}.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.MediaResponse" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<MediaResponse> ReadMediaMetadataAsync(string mediaLink)
		{
			return TaskHelper.InlineIfPossible(() => ReadMediaMetadataPrivateAsync(mediaLink), ResetSessionTokenRetryPolicy.GetRequestPolicy());
		}

		private async Task<MediaResponse> ReadMediaMetadataPrivateAsync(string mediaLink)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(mediaLink))
			{
				throw new ArgumentNullException("mediaLink");
			}
			string[] array = UrlUtility.SplitAndRemoveEmptyEntries(mediaLink, new char[1]
			{
				'/'
			});
			if (array.Length != 2)
			{
				throw new ArgumentException(RMResources.InvalidUrl, "mediaLink");
			}
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Head, new Uri(WriteEndpoint, mediaLink)))
			{
				string attachmentId = GetAttachmentId(array[1]);
				string value = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
				StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
				((INameValueCollection)nameValueCollection).Set("x-ms-date", value);
				requestMessage.Headers.Add("x-ms-date", value);
				requestMessage.Headers.Add("authorization", ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(attachmentId, "media", "HEAD", (INameValueCollection)nameValueCollection, AuthorizationTokenType.PrimaryMasterKey));
				using (HttpResponseMessage response = await mediaClient.SendHttpAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
				{
					using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(response))
					{
						MediaResponse mediaResponse = new MediaResponse
						{
							ContentType = documentServiceResponse.Headers["Content-Type"],
							Slug = documentServiceResponse.Headers["Slug"],
							ActivityId = documentServiceResponse.Headers["x-ms-activity-id"]
						};
						string text = documentServiceResponse.Headers["Content-Length"];
						if (!string.IsNullOrEmpty(text))
						{
							mediaResponse.ContentLength = long.Parse(text, CultureInfo.InvariantCulture);
						}
						mediaResponse.Headers = documentServiceResponse.Headers.Clone();
						return mediaResponse;
					}
				}
			}
		}

		/// <summary>
		/// Retrieves the specified attachment content (aka media) from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="mediaLink">The link for the media to read. E.g. /media/media_rid</param>
		/// <param name="cancellationToken">(Optional) A <see cref="T:System.Threading.CancellationToken" /> that can be used by other objects or threads to receive notice of cancellation.</param>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="mediaLink" /> is not set.</exception>
		/// <exception cref="T:System.ArgumentException">If <paramref name="mediaLink" /> is not in the form of /media/{mediaId}.</exception>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.MediaResponse" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<MediaResponse> ReadMediaAsync(string mediaLink, CancellationToken cancellationToken = default(CancellationToken))
		{
			return TaskHelper.InlineIfPossible(() => ReadMediaPrivateAsync(mediaLink, cancellationToken), ResetSessionTokenRetryPolicy.GetRequestPolicy(), cancellationToken);
		}

		private async Task<MediaResponse> ReadMediaPrivateAsync(string mediaLink, CancellationToken cancellationToken)
		{
			await EnsureValidClientAsync();
			if (string.IsNullOrEmpty(mediaLink))
			{
				throw new ArgumentNullException("mediaLink");
			}
			string[] array = UrlUtility.SplitAndRemoveEmptyEntries(mediaLink, new char[1]
			{
				'/'
			});
			if (array.Length != 2)
			{
				throw new ArgumentException(RMResources.InvalidUrl, "mediaLink");
			}
			using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(WriteEndpoint, mediaLink)))
			{
				string attachmentId = GetAttachmentId(array[1]);
				string value = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
				StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
				((INameValueCollection)nameValueCollection).Set("x-ms-date", value);
				requestMessage.Headers.Add("x-ms-date", value);
				requestMessage.Headers.Add("authorization", ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(attachmentId, "media", "GET", (INameValueCollection)nameValueCollection, AuthorizationTokenType.PrimaryMasterKey));
				HttpResponseMessage responseMessage = (connectionPolicy.MediaReadMode != MediaReadMode.Streamed) ? (await mediaClient.SendHttpAsync(requestMessage, cancellationToken)) : (await mediaClient.SendHttpAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken));
				try
				{
					DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseMediaResponseAsync(responseMessage, cancellationToken);
					MediaResponse mediaResponse = new MediaResponse
					{
						Media = documentServiceResponse.ResponseBody,
						Slug = documentServiceResponse.Headers["Slug"],
						ContentType = documentServiceResponse.Headers["Content-Type"],
						ActivityId = documentServiceResponse.Headers["x-ms-activity-id"]
					};
					string text = documentServiceResponse.Headers["Content-Length"];
					if (!string.IsNullOrEmpty(text))
					{
						mediaResponse.ContentLength = long.Parse(text, CultureInfo.InvariantCulture);
					}
					mediaResponse.Headers = documentServiceResponse.Headers;
					return mediaResponse;
				}
				catch (Exception)
				{
					responseMessage.Dispose();
					throw;
				}
			}
		}

		private bool TryGetResourceToken(string resourceAddress, PartitionKeyInternal partitionKey, out string resourceToken)
		{
			resourceToken = null;
			if (resourceTokens.TryGetValue(resourceAddress, out List<PartitionKeyAndResourceTokenPair> value))
			{
				PartitionKeyAndResourceTokenPair partitionKeyAndResourceTokenPair = value.FirstOrDefault((PartitionKeyAndResourceTokenPair pair) => pair.PartitionKey.Contains(partitionKey));
				if (partitionKeyAndResourceTokenPair != null)
				{
					resourceToken = partitionKeyAndResourceTokenPair.ResourceToken;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// </summary>
		/// <param name="resourceAddress"></param>
		/// <param name="resourceType"></param>
		/// <param name="requestVerb"></param>
		/// <param name="headers"></param>
		/// <param name="tokenType">unused, use token based upon what is passed in constructor</param>
		/// <returns></returns>
		string IAuthorizationTokenProvider.GetUserAuthorizationToken(string resourceAddress, string resourceType, string requestVerb, INameValueCollection headers, AuthorizationTokenType tokenType)
		{
			if (hasAuthKeyResourceToken && resourceTokens == null)
			{
				return HttpUtility.UrlEncode(authKeyResourceToken);
			}
			if (authKeyHashFunction != null)
			{
				headers["x-ms-date"] = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
				return AuthorizationHelper.GenerateKeyAuthorizationSignature(requestVerb, resourceAddress, resourceType, headers, authKeyHashFunction);
			}
			PartitionKeyInternal partitionKey = PartitionKeyInternal.Empty;
			string text = headers["x-ms-documentdb-partitionkey"];
			if (text != null)
			{
				partitionKey = PartitionKeyInternal.FromJsonString(text);
			}
			if (PathsHelper.IsNameBased(resourceAddress))
			{
				string resourceToken = null;
				bool flag = false;
				for (int i = 2; i < ResourceId.MaxPathFragment; i += 2)
				{
					string parentByIndex = PathsHelper.GetParentByIndex(resourceAddress, i);
					if (parentByIndex == null)
					{
						break;
					}
					flag = TryGetResourceToken(parentByIndex, partitionKey, out resourceToken);
					if (flag)
					{
						break;
					}
				}
				if (!flag && PathsHelper.GetCollectionPath(resourceAddress) == resourceAddress && (requestVerb == "GET" || requestVerb == "HEAD"))
				{
					string value = resourceAddress.EndsWith("/", StringComparison.Ordinal) ? resourceAddress : (resourceAddress + "/");
					foreach (KeyValuePair<string, List<PartitionKeyAndResourceTokenPair>> resourceToken3 in resourceTokens)
					{
						if (resourceToken3.Key.StartsWith(value, StringComparison.Ordinal))
						{
							resourceToken = resourceToken3.Value[0].ResourceToken;
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					throw new UnauthorizedException(string.Format(CultureInfo.InvariantCulture, ClientResources.AuthTokenNotFound, resourceAddress));
				}
				return HttpUtility.UrlEncode(resourceToken);
			}
			string resourceToken2 = null;
			ResourceId resourceId = ResourceId.Parse(resourceAddress);
			bool flag2 = false;
			if (resourceId.Attachment != 0 || resourceId.Permission != 0L || resourceId.StoredProcedure != 0L || resourceId.Trigger != 0L || resourceId.UserDefinedFunction != 0L)
			{
				flag2 = TryGetResourceToken(resourceAddress, partitionKey, out resourceToken2);
			}
			if (!flag2 && (resourceId.Attachment != 0 || resourceId.Document != 0L))
			{
				flag2 = TryGetResourceToken(resourceId.DocumentId.ToString(), partitionKey, out resourceToken2);
			}
			if (!flag2 && (resourceId.Attachment != 0 || resourceId.Document != 0L || resourceId.StoredProcedure != 0L || resourceId.Trigger != 0L || resourceId.UserDefinedFunction != 0L || resourceId.DocumentCollection != 0))
			{
				flag2 = TryGetResourceToken(resourceId.DocumentCollectionId.ToString(), partitionKey, out resourceToken2);
			}
			if (!flag2 && (resourceId.Permission != 0L || resourceId.User != 0))
			{
				flag2 = TryGetResourceToken(resourceId.UserId.ToString(), partitionKey, out resourceToken2);
			}
			if (!flag2)
			{
				flag2 = TryGetResourceToken(resourceId.DatabaseId.ToString(), partitionKey, out resourceToken2);
			}
			if (!flag2 && resourceId.DocumentCollection != 0 && (requestVerb == "GET" || requestVerb == "HEAD"))
			{
				foreach (KeyValuePair<string, List<PartitionKeyAndResourceTokenPair>> resourceToken4 in resourceTokens)
				{
					ResourceId rid;
					if (!PathsHelper.IsNameBased(resourceToken4.Key) && ResourceId.TryParse(resourceToken4.Key, out rid) && rid.DocumentCollectionId.Equals(resourceId))
					{
						resourceToken2 = resourceToken4.Value[0].ResourceToken;
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2)
			{
				throw new UnauthorizedException(string.Format(CultureInfo.InvariantCulture, ClientResources.AuthTokenNotFound, resourceAddress));
			}
			return HttpUtility.UrlEncode(resourceToken2);
		}

		Task<string> IAuthorizationTokenProvider.GetSystemAuthorizationTokenAsync(string federationName, string resourceAddress, string resourceType, string requestVerb, INameValueCollection headers, AuthorizationTokenType tokenType)
		{
			return Task.FromResult(((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(resourceAddress, resourceType, requestVerb, headers, tokenType));
		}

		internal Task<DocumentServiceResponse> CreateAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "POST", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> UpdateAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "PUT", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> ReadAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "GET", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> ReadFeedAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "GET", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> DeleteAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "DELETE", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> ExecuteProcedureAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			request.Headers["Content-Type"] = "application/json";
			request.Headers["authorization"] = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "POST", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> ExecuteQueryAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "POST", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		internal Task<DocumentServiceResponse> UpsertAsync(DocumentServiceRequest request, IDocumentClientRetryPolicy retryPolicy, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			string userAuthorizationToken = ((IAuthorizationTokenProvider)this).GetUserAuthorizationToken(request.ResourceAddress, PathsHelper.GetResourcePath(request.ResourceType), "POST", request.Headers, AuthorizationTokenType.PrimaryMasterKey);
			request.Headers["authorization"] = userAuthorizationToken;
			request.Headers["x-ms-documentdb-is-upsert"] = bool.TrueString;
			return ProcessRequestAsync(request, retryPolicy, cancellationToken);
		}

		/// <summary>
		/// Read the <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <returns>
		/// A <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> wrapped in a <see cref="T:System.Threading.Tasks.Task" /> object.
		/// </returns>
		public Task<DatabaseAccount> GetDatabaseAccountAsync()
		{
			return TaskHelper.InlineIfPossible(() => GetDatabaseAccountPrivateAsync(ReadEndpoint), ResetSessionTokenRetryPolicy.GetRequestPolicy());
		}

		/// <summary>
		/// Read the <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> as an asynchronous operation
		/// given a specific reginal endpoint url.
		/// </summary>
		/// <param name="serviceEndpoint">The reginal url of the serice endpoint.</param>
		/// <param name="cancellationToken">The CancellationToken</param>
		/// <returns>
		/// A <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> wrapped in a <see cref="T:System.Threading.Tasks.Task" /> object.
		/// </returns>
		Task<DatabaseAccount> IDocumentClientInternal.GetDatabaseAccountInternalAsync(Uri serviceEndpoint, CancellationToken cancellationToken)
		{
			return GetDatabaseAccountPrivateAsync(serviceEndpoint, cancellationToken);
		}

		private async Task<DatabaseAccount> GetDatabaseAccountPrivateAsync(Uri serviceEndpoint, CancellationToken cancellationToken = default(CancellationToken))
		{
			await EnsureValidClientAsync();
			GatewayStoreModel gatewayStoreModel = this.gatewayStoreModel as GatewayStoreModel;
			if (gatewayStoreModel != null)
			{
				using (HttpRequestMessage request = new HttpRequestMessage())
				{
					StringKeyValueCollection nameValueCollection = new StringKeyValueCollection();
					string value = DateTime.UtcNow.ToString("r");
					((INameValueCollection)nameValueCollection).Add("x-ms-date", value);
					request.Headers.Add("x-ms-date", value);
					string value2 = (!hasAuthKeyResourceToken) ? AuthorizationHelper.GenerateKeyAuthorizationSignature("GET", serviceEndpoint, nameValueCollection, authKeyHashFunction) : HttpUtility.UrlEncode(authKeyResourceToken);
					request.Headers.Add("authorization", value2);
					request.Method = HttpMethod.Get;
					request.RequestUri = serviceEndpoint;
					DatabaseAccount databaseAccount = await gatewayStoreModel.GetDatabaseAccountAsync(request, cancellationToken);
					useMultipleWriteLocations = (connectionPolicy.UseMultipleWriteLocations && databaseAccount.EnableMultipleWriteLocations);
					return databaseAccount;
				}
			}
			return null;
		}

		/// <summary>
		/// Certain requests must be routed through gateway even when the client connectivity mode is direct.
		/// For e.g., DocumentCollection creation. This method returns the <see cref="T:Microsoft.Azure.Documents.IBackendProxy" /> based
		/// on the input <paramref name="request" />.
		/// </summary>
		/// <param name="request"></param>
		/// <returns>Returns <see cref="T:Microsoft.Azure.Documents.IBackendProxy" /> to which the request must be sent</returns>
		private IStoreModel GetStoreProxy(DocumentServiceRequest request)
		{
			if (request.UseGatewayMode)
			{
				return gatewayStoreModel;
			}
			ResourceType resourceType = request.ResourceType;
			OperationType operationType = request.OperationType;
			if (resourceType == ResourceType.Offer || (resourceType.IsScript() && operationType != OperationType.ExecuteJavaScript) || resourceType == ResourceType.PartitionKeyRange)
			{
				return gatewayStoreModel;
			}
			switch (operationType)
			{
			case OperationType.Create:
			case OperationType.Upsert:
				if (resourceType == ResourceType.Database || resourceType == ResourceType.User || resourceType == ResourceType.Collection || resourceType == ResourceType.Permission)
				{
					return gatewayStoreModel;
				}
				return storeModel;
			case OperationType.Delete:
				if (resourceType == ResourceType.Database || resourceType == ResourceType.User || resourceType == ResourceType.Collection)
				{
					return gatewayStoreModel;
				}
				return storeModel;
			case OperationType.Replace:
				if (resourceType == ResourceType.Collection)
				{
					return gatewayStoreModel;
				}
				return storeModel;
			case OperationType.Read:
				if (resourceType == ResourceType.Collection)
				{
					return gatewayStoreModel;
				}
				return storeModel;
			default:
				return storeModel;
			}
		}

		/// <summary>
		/// The preferred link used in replace operation in SDK.
		/// </summary>
		/// <returns></returns>
		private string GetLinkForRouting(Resource resource)
		{
			return resource.SelfLink ?? resource.AltLink;
		}

		internal void EnsureValidOverwrite(ConsistencyLevel desiredConsistencyLevel)
		{
			ConsistencyLevel defaultConsistencyLevel = gatewayConfigurationReader.DefaultConsistencyLevel;
			if (!IsValidConsistency(defaultConsistencyLevel, desiredConsistencyLevel))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidConsistencyLevel, desiredConsistencyLevel.ToString(), defaultConsistencyLevel.ToString()));
			}
		}

		private bool IsValidConsistency(ConsistencyLevel backendConsistency, ConsistencyLevel desiredConsistency)
		{
			if (allowOverrideStrongerConsistency)
			{
				return true;
			}
			return ValidationHelpers.ValidateConsistencyLevel(backendConsistency, desiredConsistency);
		}

		private void InitializeDirectConnectivity(IStoreClientFactory storeClientFactory)
		{
			if (storeClientFactory != null)
			{
				this.storeClientFactory = storeClientFactory;
				isStoreClientFactoryCreatedInternally = false;
			}
			else
			{
				this.storeClientFactory = new StoreClientFactory(connectionPolicy.ConnectionProtocol, (int)connectionPolicy.RequestTimeout.TotalSeconds, maxConcurrentConnectionOpenRequests, connectionPolicy.UserAgentContainer, eventSource, null, openConnectionTimeoutInSeconds, idleConnectionTimeoutInSeconds, timerPoolGranularityInSeconds, maxRntbdChannels, rntbdPartitionCount, maxRequestsPerRntbdChannel, rntbdPortReusePolicy, rntbdPortPoolReuseThreshold, rntbdPortPoolBindAttempts, rntbdReceiveHangDetectionTimeSeconds, rntbdSendHangDetectionTimeSeconds, enableCpuMonitor);
				isStoreClientFactoryCreatedInternally = true;
			}
			AddressResolver = new GlobalAddressResolver(globalEndpointManager, connectionPolicy.ConnectionProtocol, this, collectionCache, partitionKeyRangeCache, connectionPolicy.UserAgentContainer, gatewayConfigurationReader, httpMessageHandler, connectionPolicy, ApiType);
			CreateStoreModel(subscribeRntbdStatus: true);
		}

		private void CreateStoreModel(bool subscribeRntbdStatus)
		{
			StoreClient storeClient = storeClientFactory.CreateStoreClient(AddressResolver, sessionContainer, gatewayConfigurationReader, this, enableRequestDiagnostics: true, connectionPolicy.EnableReadRequestsFallback ?? (gatewayConfigurationReader.DefaultConsistencyLevel != ConsistencyLevel.BoundedStaleness), !enableRntbdChannel, useMultipleWriteLocations && gatewayConfigurationReader.DefaultConsistencyLevel != ConsistencyLevel.Strong, detectClientConnectivityIssues: true);
			if (subscribeRntbdStatus)
			{
				storeClient.AddDisableRntbdChannelCallback(DisableRntbdChannel);
			}
			storeClient.SerializerSettings = serializerSettings;
			storeModel = new ServerStoreModel(storeClient, this.sendingRequest, this.receivedResponse);
		}

		private void DisableRntbdChannel()
		{
			enableRntbdChannel = false;
			CreateStoreModel(subscribeRntbdStatus: false);
		}

		private async Task InitializeGatewayConfigurationReader()
		{
			gatewayConfigurationReader = new GatewayServiceConfigurationReader(ServiceEndpoint, authKeyHashFunction, hasAuthKeyResourceToken, authKeyResourceToken, connectionPolicy, ApiType, httpMessageHandler);
			DatabaseAccount databaseAccount = await gatewayConfigurationReader.InitializeReaderAsync();
			useMultipleWriteLocations = (connectionPolicy.UseMultipleWriteLocations && databaseAccount.EnableMultipleWriteLocations);
			await globalEndpointManager.RefreshLocationAsync(databaseAccount);
		}

		private void CaptureSessionToken(DocumentServiceRequest request, DocumentServiceResponse response)
		{
			sessionContainer.SetSessionToken(request, response.Headers);
		}

		internal DocumentServiceRequest CreateDocumentServiceRequest(OperationType operationType, string resourceLink, ResourceType resourceType, INameValueCollection headers)
		{
			if (resourceType == ResourceType.Database || resourceType == ResourceType.Offer)
			{
				return DocumentServiceRequest.Create(operationType, (string)null, resourceType, AuthorizationTokenType.PrimaryMasterKey, headers);
			}
			return DocumentServiceRequest.Create(operationType, resourceType, resourceLink, AuthorizationTokenType.PrimaryMasterKey, headers);
		}

		private void ValidateResource(Resource resource)
		{
			if (!string.IsNullOrEmpty(resource.Id))
			{
				int num = resource.Id.IndexOfAny(new char[4]
				{
					'/',
					'\\',
					'?',
					'#'
				});
				if (num != -1)
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidCharacterInResourceName, resource.Id[num]));
				}
				if (resource.Id[resource.Id.Length - 1] == ' ')
				{
					throw new ArgumentException(RMResources.InvalidSpaceEndingInResourceName);
				}
			}
		}

		private async Task AddPartitionKeyInformationAsync(DocumentServiceRequest request, Document document, RequestOptions options)
		{
			DocumentCollection documentCollection = await(await GetCollectionCacheAsync()).ResolveCollectionAsync(request, CancellationToken.None);
			PartitionKeyDefinition partitionKey = documentCollection.PartitionKey;
			PartitionKeyInternal partitionKeyInternal = (options != null && options.PartitionKey != null && options.PartitionKey.Equals(PartitionKey.None)) ? documentCollection.NonePartitionKeyValue : ((options == null || options.PartitionKey == null) ? DocumentAnalyzer.ExtractPartitionKeyValue(document, partitionKey) : options.PartitionKey.InternalKey);
			request.Headers.Set("x-ms-documentdb-partitionkey", partitionKeyInternal.ToJsonString());
		}

		internal async Task AddPartitionKeyInformationAsync(DocumentServiceRequest request, RequestOptions options)
		{
			DocumentCollection documentCollection = await(await GetCollectionCacheAsync()).ResolveCollectionAsync(request, CancellationToken.None);
			PartitionKeyDefinition partitionKey = documentCollection.PartitionKey;
			PartitionKeyInternal partitionKeyInternal;
			if (options != null && options.PartitionKey != null)
			{
				partitionKeyInternal = ((!options.PartitionKey.Equals(PartitionKey.None)) ? options.PartitionKey.InternalKey : documentCollection.NonePartitionKeyValue);
			}
			else
			{
				if (partitionKey != null && partitionKey.Paths.Count != 0)
				{
					throw new InvalidOperationException(RMResources.MissingPartitionKeyValue);
				}
				partitionKeyInternal = PartitionKeyInternal.Empty;
			}
			request.Headers.Set("x-ms-documentdb-partitionkey", partitionKeyInternal.ToJsonString());
		}

		private JsonSerializerSettings GetSerializerSettingsForRequest(RequestOptions requestOptions)
		{
			return requestOptions?.JsonSerializerSettings ?? serializerSettings;
		}

		private INameValueCollection GetRequestHeaders(RequestOptions options)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			if (useMultipleWriteLocations)
			{
				nameValueCollection.Set("x-ms-cosmos-allow-tentative-writes", bool.TrueString);
			}
			if (desiredConsistencyLevel.HasValue)
			{
				if (!IsValidConsistency(gatewayConfigurationReader.DefaultConsistencyLevel, desiredConsistencyLevel.Value))
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidConsistencyLevel, desiredConsistencyLevel.Value.ToString(), gatewayConfigurationReader.DefaultConsistencyLevel));
				}
				nameValueCollection.Set("x-ms-consistency-level", desiredConsistencyLevel.Value.ToString());
			}
			if (options == null)
			{
				return nameValueCollection;
			}
			if (options.AccessCondition != null)
			{
				if (options.AccessCondition.Type == AccessConditionType.IfMatch)
				{
					nameValueCollection.Set("If-Match", options.AccessCondition.Condition);
				}
				else
				{
					nameValueCollection.Set("If-None-Match", options.AccessCondition.Condition);
				}
			}
			if (options.ConsistencyLevel.HasValue)
			{
				if (!IsValidConsistency(gatewayConfigurationReader.DefaultConsistencyLevel, options.ConsistencyLevel.Value))
				{
					throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidConsistencyLevel, options.ConsistencyLevel.Value.ToString(), gatewayConfigurationReader.DefaultConsistencyLevel));
				}
				nameValueCollection.Set("x-ms-consistency-level", options.ConsistencyLevel.ToString());
			}
			if (options.IndexingDirective.HasValue)
			{
				nameValueCollection.Set("x-ms-indexing-directive", options.IndexingDirective.ToString());
			}
			if (options.PostTriggerInclude != null && options.PostTriggerInclude.Count > 0)
			{
				string value = string.Join(",", options.PostTriggerInclude.AsEnumerable());
				nameValueCollection.Set("x-ms-documentdb-post-trigger-include", value);
			}
			if (options.PreTriggerInclude != null && options.PreTriggerInclude.Count > 0)
			{
				string value2 = string.Join(",", options.PreTriggerInclude.AsEnumerable());
				nameValueCollection.Set("x-ms-documentdb-pre-trigger-include", value2);
			}
			if (!string.IsNullOrEmpty(options.SessionToken))
			{
				nameValueCollection["x-ms-session-token"] = options.SessionToken;
			}
			if (options.ResourceTokenExpirySeconds.HasValue)
			{
				nameValueCollection.Set("x-ms-documentdb-expiry-seconds", options.ResourceTokenExpirySeconds.Value.ToString(CultureInfo.InvariantCulture));
			}
			if (options.OfferType != null)
			{
				nameValueCollection.Set("x-ms-offer-type", options.OfferType);
			}
			if (options.OfferThroughput.HasValue)
			{
				nameValueCollection.Set("x-ms-offer-throughput", options.OfferThroughput.Value.ToString(CultureInfo.InvariantCulture));
			}
			if (options.OfferEnableRUPerMinuteThroughput)
			{
				nameValueCollection.Set("x-ms-offer-is-ru-per-minute-throughput-enabled", bool.TrueString);
			}
			if (options.InsertSystemPartitionKey)
			{
				nameValueCollection.Set("x-ms-cosmos-insert-systempartitionkey", bool.TrueString);
			}
			if (options.OfferAutoScaleMode.HasValue)
			{
				nameValueCollection.Set("x-ms-cosmos-offer-autoscale-mode", options.OfferAutoScaleMode.ToString());
			}
			if (options.EnableScriptLogging)
			{
				nameValueCollection.Set("x-ms-documentdb-script-enable-logging", bool.TrueString);
			}
			if (options.PopulateQuotaInfo)
			{
				nameValueCollection.Set("x-ms-documentdb-populatequotainfo", bool.TrueString);
			}
			if (options.PopulateRestoreStatus)
			{
				nameValueCollection.Set("x-ms-cosmosdb-populaterestorestatus", bool.TrueString);
			}
			if (options.PopulatePartitionKeyRangeStatistics)
			{
				nameValueCollection.Set("x-ms-documentdb-populatepartitionstatistics", bool.TrueString);
			}
			if (options.DisableRUPerMinuteUsage)
			{
				nameValueCollection.Set("x-ms-documentdb-disable-ru-per-minute-usage", bool.TrueString);
			}
			if (options.RemoteStorageType.HasValue)
			{
				nameValueCollection.Set("x-ms-remote-storage-type", options.RemoteStorageType.ToString());
			}
			if (options.PartitionKeyRangeId != null)
			{
				nameValueCollection.Set("x-ms-documentdb-partitionkeyrangeid", options.PartitionKeyRangeId);
			}
			if (options.SourceDatabaseId != null)
			{
				nameValueCollection.Set("x-ms-source-database-Id", options.SourceDatabaseId);
			}
			if (options.SourceCollectionId != null)
			{
				nameValueCollection.Set("x-ms-source-collection-Id", options.SourceCollectionId);
			}
			if (options.RestorePointInTime.HasValue)
			{
				nameValueCollection.Set("x-ms-restore-point-in-time", options.RestorePointInTime.Value.ToString(CultureInfo.InvariantCulture));
			}
			if (options.IsReadOnlyScript)
			{
				nameValueCollection.Set("x-ms-is-readonly-script", bool.TrueString);
			}
			if (options.ExcludeSystemProperties.HasValue)
			{
				nameValueCollection.Set("x-ms-exclude-system-properties", options.ExcludeSystemProperties.Value.ToString());
			}
			if (options.MergeStaticId != null)
			{
				nameValueCollection.Set("x-ms-cosmos-merge-static-id", options.MergeStaticId);
			}
			if (options.PreserveFullContent)
			{
				nameValueCollection.Set("x-ms-cosmos-preserve-full-content", bool.TrueString);
			}
			return nameValueCollection;
		}

		private string GetAttachmentId(string mediaId)
		{
			string attachmentId = null;
			byte storageIndex = 0;
			if (!MediaIdHelper.TryParseMediaId(mediaId, out attachmentId, out storageIndex))
			{
				throw new ArgumentException(ClientResources.MediaLinkInvalid);
			}
			return attachmentId;
		}

		/// <summary>
		/// Creates an attachment as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to create an attachment for.</param>
		/// <param name="attachment">the attachment object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return CreateAttachmentAsync(documentUri.OriginalString, attachment, options, cancellationToken);
		}

		/// <summary>
		/// Creates an attachment as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to create an attachment for.</param>
		/// <param name="mediaStream">the stream of the attachment media.</param>
		/// <param name="options">the media options for the request.</param>
		/// <param name="requestOptions">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> CreateAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return CreateAttachmentAsync(documentUri.OriginalString, mediaStream, options, requestOptions, cancellationToken);
		}

		/// <summary>
		/// Creates a document as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to create the document in.</param>
		/// <param name="document">the document object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="disableAutomaticIdGeneration">Disables the automatic id generation, will throw an exception if id is missing.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Document>> CreateDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return CreateDocumentAsync(documentCollectionUri.OriginalString, document, options, disableAutomaticIdGeneration, cancellationToken);
		}

		/// <summary>
		/// Creates a collection as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to create the collection in.</param>
		/// <param name="documentCollection">the Microsoft.Azure.Documents.DocumentCollection object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionAsync(Uri databaseUri, DocumentCollection documentCollection, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateDocumentCollectionAsync(databaseUri.OriginalString, documentCollection, options);
		}

		/// <summary>
		/// Creates(if doesn't exist) or gets(if already exists) a collection as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to create the collection in.</param>
		/// <param name="documentCollection">The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> object.</param>
		/// <param name="options">(Optional) Any <see cref="T:Microsoft.Azure.Documents.Client.RequestOptions" /> you wish to provide when creating a Collection. E.g. RequestOptions.OfferThroughput = 400. </param>
		/// <returns>The <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> that was created contained within a <see cref="T:System.Threading.Tasks.Task" /> object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsAsync(Uri databaseUri, DocumentCollection documentCollection, RequestOptions options = null)
		{
			return TaskHelper.InlineIfPossible(() => CreateDocumentCollectionIfNotExistsPrivateAsync(databaseUri, documentCollection, options), null);
		}

		private async Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsPrivateAsync(Uri databaseUri, DocumentCollection documentCollection, RequestOptions options)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			if (documentCollection == null)
			{
				throw new ArgumentNullException("documentCollection");
			}
			Uri documentCollectionUri = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", databaseUri.OriginalString, "colls", Uri.EscapeUriString(documentCollection.Id)), UriKind.Relative);
			try
			{
				return await ReadDocumentCollectionAsync(documentCollectionUri, options);
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.NotFound)
				{
					throw;
				}
			}
			try
			{
				return await CreateDocumentCollectionAsync(databaseUri, documentCollection, options);
			}
			catch (DocumentClientException ex2)
			{
				if (ex2.StatusCode != HttpStatusCode.Conflict)
				{
					throw;
				}
			}
			return await ReadDocumentCollectionAsync(documentCollectionUri, options);
		}

		/// <summary>
		/// Creates a stored procedure as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to create the stored procedure in.</param>
		/// <param name="storedProcedure">the Microsoft.Azure.Documents.StoredProcedure object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<StoredProcedure>> CreateStoredProcedureAsync(Uri documentCollectionUri, StoredProcedure storedProcedure, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return CreateStoredProcedureAsync(documentCollectionUri.OriginalString, storedProcedure, options);
		}

		/// <summary>
		/// Creates a trigger as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to create the trigger in.</param>
		/// <param name="trigger">the Microsoft.Azure.Documents.Trigger object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Trigger>> CreateTriggerAsync(Uri documentCollectionUri, Trigger trigger, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return CreateTriggerAsync(documentCollectionUri.OriginalString, trigger, options);
		}

		/// <summary>
		/// Creates a user defined function as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to create the user defined function in.</param>
		/// <param name="function">the Microsoft.Azure.Documents.UserDefinedFunction object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<UserDefinedFunction>> CreateUserDefinedFunctionAsync(Uri documentCollectionUri, UserDefinedFunction function, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return CreateUserDefinedFunctionAsync(documentCollectionUri.OriginalString, function, options);
		}

		/// <summary>
		/// Creates a permission as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userUri">the URI of the user to create the permission for.</param>
		/// <param name="permission">the Microsoft.Azure.Documents.Permission object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Permission>> CreatePermissionAsync(Uri userUri, Permission permission, RequestOptions options = null)
		{
			if (userUri == null)
			{
				throw new ArgumentNullException("userUri");
			}
			return CreatePermissionAsync(userUri.OriginalString, permission, options);
		}

		/// <summary>
		/// Creates a user as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to create the user in.</param>
		/// <param name="user">the Microsoft.Azure.Documents.User object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<User>> CreateUserAsync(Uri databaseUri, User user, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateUserAsync(databaseUri.OriginalString, user, options);
		}

		/// <summary>
		/// Creates a user defined type as an asychronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to create the user defined type in.</param>
		/// <param name="userDefinedType">the Microsoft.Azure.Documents.UserDefinedType object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<ResourceResponse<UserDefinedType>> CreateUserDefinedTypeAsync(Uri databaseUri, UserDefinedType userDefinedType, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateUserDefinedTypeAsync(databaseUri.OriginalString, userDefinedType, options);
		}

		/// <summary>
		/// Upserts an attachment as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to upsert an attachment for.</param>
		/// <param name="attachment">the attachment object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, object attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return UpsertAttachmentAsync(documentUri.OriginalString, attachment, options, cancellationToken);
		}

		/// <summary>
		/// Upserts an attachment as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to upsert an attachment for.</param>
		/// <param name="mediaStream">the stream of the attachment media.</param>
		/// <param name="options">the media options for the request.</param>
		/// <param name="requestOptions">Request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> UpsertAttachmentAsync(Uri documentUri, Stream mediaStream, MediaOptions options = null, RequestOptions requestOptions = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return UpsertAttachmentAsync(documentUri.OriginalString, mediaStream, options, requestOptions, cancellationToken);
		}

		/// <summary>
		/// Upserts a document as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to upsert the document in.</param>
		/// <param name="document">the document object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="disableAutomaticIdGeneration">Disables the automatic id generation, will throw an exception if id is missing.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Document>> UpsertDocumentAsync(Uri documentCollectionUri, object document, RequestOptions options = null, bool disableAutomaticIdGeneration = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return UpsertDocumentAsync(documentCollectionUri.OriginalString, document, options, disableAutomaticIdGeneration, cancellationToken);
		}

		/// <summary>
		/// Upserts a stored procedure as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to upsert the stored procedure in.</param>
		/// <param name="storedProcedure">the Microsoft.Azure.Documents.StoredProcedure object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<StoredProcedure>> UpsertStoredProcedureAsync(Uri documentCollectionUri, StoredProcedure storedProcedure, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return UpsertStoredProcedureAsync(documentCollectionUri.OriginalString, storedProcedure, options);
		}

		/// <summary>
		/// Upserts a trigger as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to upsert the trigger in.</param>
		/// <param name="trigger">the Microsoft.Azure.Documents.Trigger object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Trigger>> UpsertTriggerAsync(Uri documentCollectionUri, Trigger trigger, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return UpsertTriggerAsync(documentCollectionUri.OriginalString, trigger, options);
		}

		/// <summary>
		/// Upserts a user defined function as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to upsert the user defined function in.</param>
		/// <param name="function">the Microsoft.Azure.Documents.UserDefinedFunction object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<UserDefinedFunction>> UpsertUserDefinedFunctionAsync(Uri documentCollectionUri, UserDefinedFunction function, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return UpsertUserDefinedFunctionAsync(documentCollectionUri.OriginalString, function, options);
		}

		/// <summary>
		/// Upserts a permission as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userUri">the URI of the user to upsert the permission for.</param>
		/// <param name="permission">the Microsoft.Azure.Documents.Permission object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Permission>> UpsertPermissionAsync(Uri userUri, Permission permission, RequestOptions options = null)
		{
			if (userUri == null)
			{
				throw new ArgumentNullException("userUri");
			}
			return UpsertPermissionAsync(userUri.OriginalString, permission, options);
		}

		/// <summary>
		/// Upserts a user as an asynchronous operation  in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to upsert the user in.</param>
		/// <param name="user">the Microsoft.Azure.Documents.User object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<User>> UpsertUserAsync(Uri databaseUri, User user, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return UpsertUserAsync(databaseUri.OriginalString, user, options);
		}

		/// <summary>
		/// Upserts a user defined type as an asynchronous operation  in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to upsert the user defined type in.</param>
		/// <param name="userDefinedType">the Microsoft.Azure.Documents.UserDefinedType object.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<ResourceResponse<UserDefinedType>> UpsertUserDefinedTypeAsync(Uri databaseUri, UserDefinedType userDefinedType, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return UpsertUserDefinedTypeAsync(databaseUri.OriginalString, userDefinedType, options);
		}

		/// <summary>
		/// Delete an attachment as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentUri">the URI of the attachment to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> DeleteAttachmentAsync(Uri attachmentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (attachmentUri == null)
			{
				throw new ArgumentNullException("attachmentUri");
			}
			return DeleteAttachmentAsync(attachmentUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Delete a database as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI of the database to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Database>> DeleteDatabaseAsync(Uri databaseUri, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return DeleteDatabaseAsync(databaseUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a document as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Document>> DeleteDocumentAsync(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return DeleteDocumentAsync(documentUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Delete a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<DocumentCollection>> DeleteDocumentCollectionAsync(Uri documentCollectionUri, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return DeleteDocumentCollectionAsync(documentCollectionUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a stored procedure as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProcedureUri">the URI of the stored procedure to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<StoredProcedure>> DeleteStoredProcedureAsync(Uri storedProcedureUri, RequestOptions options = null)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			return DeleteStoredProcedureAsync(storedProcedureUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a trigger as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggerUri">the URI of the trigger to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Trigger>> DeleteTriggerAsync(Uri triggerUri, RequestOptions options = null)
		{
			if (triggerUri == null)
			{
				throw new ArgumentNullException("triggerUri");
			}
			return DeleteTriggerAsync(triggerUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a user defined function as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="functionUri">the URI of the user defined function to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<UserDefinedFunction>> DeleteUserDefinedFunctionAsync(Uri functionUri, RequestOptions options = null)
		{
			if (functionUri == null)
			{
				throw new ArgumentNullException("functionUri");
			}
			return DeleteUserDefinedFunctionAsync(functionUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a permission as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionUri">the URI of the permission to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Permission>> DeletePermissionAsync(Uri permissionUri, RequestOptions options = null)
		{
			if (permissionUri == null)
			{
				throw new ArgumentNullException("permissionUri");
			}
			return DeletePermissionAsync(permissionUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a user as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userUri">the URI of the user to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<User>> DeleteUserAsync(Uri userUri, RequestOptions options = null)
		{
			if (userUri == null)
			{
				throw new ArgumentNullException("userUri");
			}
			return DeleteUserAsync(userUri.OriginalString, options);
		}

		/// <summary>
		/// Delete a conflict as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictUri">the URI of the conflict to delete.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Conflict>> DeleteConflictAsync(Uri conflictUri, RequestOptions options = null)
		{
			if (conflictUri == null)
			{
				throw new ArgumentNullException("conflictUri");
			}
			return DeleteConflictAsync(conflictUri.OriginalString, options);
		}

		/// <summary>
		/// Replaces an attachment as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentUri">the URI of the attachment to be updated.</param>
		/// <param name="attachment">the attachment resource.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Uri attachmentUri, Attachment attachment, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (attachmentUri == null)
			{
				throw new ArgumentNullException("attachmentUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceAttachmentPrivateAsync(attachment, options, retryPolicyInstance, cancellationToken, attachmentUri.OriginalString), retryPolicyInstance, cancellationToken);
		}

		/// <summary>
		/// Replaces a document as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">the URI of the document to be updated.</param>
		/// <param name="document">the updated document.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Document>> ReplaceDocumentAsync(Uri documentUri, object document, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return ReplaceDocumentAsync(documentUri.OriginalString, document, options, cancellationToken);
		}

		/// <summary>
		/// Replaces a document collection as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">the URI of the document collection to be updated.</param>
		/// <param name="documentCollection">the updated document collection.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<DocumentCollection>> ReplaceDocumentCollectionAsync(Uri documentCollectionUri, DocumentCollection documentCollection, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceDocumentCollectionPrivateAsync(documentCollection, options, retryPolicyInstance, documentCollectionUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replace the specified stored procedure in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProcedureUri">the URI for the stored procedure to be updated.</param>
		/// <param name="storedProcedure">the updated stored procedure.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<StoredProcedure>> ReplaceStoredProcedureAsync(Uri storedProcedureUri, StoredProcedure storedProcedure, RequestOptions options = null)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceStoredProcedurePrivateAsync(storedProcedure, options, retryPolicyInstance, storedProcedureUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replaces a trigger as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggerUri">the URI for the trigger to be updated.</param>
		/// <param name="trigger">the updated trigger.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Trigger>> ReplaceTriggerAsync(Uri triggerUri, Trigger trigger, RequestOptions options = null)
		{
			if (triggerUri == null)
			{
				throw new ArgumentNullException("triggerUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceTriggerPrivateAsync(trigger, options, retryPolicyInstance, triggerUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replaces a user defined function as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedFunctionUri">the URI for the user defined function to be updated.</param>
		/// <param name="function">the updated user defined function.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<UserDefinedFunction>> ReplaceUserDefinedFunctionAsync(Uri userDefinedFunctionUri, UserDefinedFunction function, RequestOptions options = null)
		{
			if (userDefinedFunctionUri == null)
			{
				throw new ArgumentNullException("userDefinedFunctionUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserDefinedFunctionPrivateAsync(function, options, retryPolicyInstance, userDefinedFunctionUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replaces a permission as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionUri">the URI for the permission to be updated.</param>
		/// <param name="permission">the updated permission.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<Permission>> ReplacePermissionAsync(Uri permissionUri, Permission permission, RequestOptions options = null)
		{
			if (permissionUri == null)
			{
				throw new ArgumentNullException("permissionUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplacePermissionPrivateAsync(permission, options, retryPolicyInstance, permissionUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replaces a user as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userUri">the URI for the user to be updated.</param>
		/// <param name="user">the updated user.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<ResourceResponse<User>> ReplaceUserAsync(Uri userUri, User user, RequestOptions options = null)
		{
			if (userUri == null)
			{
				throw new ArgumentNullException("userUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserPrivateAsync(user, options, retryPolicyInstance, userUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Replaces a user defined type as an asynchronous operation in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypeUri">the URI for the user defined type to be updated.</param>
		/// <param name="userDefinedType">the updated user defined type.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<ResourceResponse<UserDefinedType>> ReplaceUserDefinedTypeAsync(Uri userDefinedTypeUri, UserDefinedType userDefinedType, RequestOptions options = null)
		{
			if (userDefinedTypeUri == null)
			{
				throw new ArgumentNullException("userDefinedTypeUri");
			}
			IDocumentClientRetryPolicy retryPolicyInstance = ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReplaceUserDefinedTypePrivateAsync(userDefinedType, options, retryPolicyInstance, userDefinedTypeUri.OriginalString), retryPolicyInstance);
		}

		/// <summary>
		/// Reads an <see cref="T:Microsoft.Azure.Documents.Attachment" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentUri">A URI to the Attachment resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps an <see cref="T:Microsoft.Azure.Documents.Attachment" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="attachmentUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads an Attachment resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection
		/// // - doc_id is the ID property of the Document
		/// // - attachment_id is the ID property of the Attachment resource you wish to read. 
		/// var attachLink = UriFactory.CreateAttachmentUri("db_id", "coll_id", "doc_id", "attachment_id");
		/// Attachment attachment = await client.ReadAttachmentAsync(attachLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Attachment>> ReadAttachmentAsync(Uri attachmentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (attachmentUri == null)
			{
				throw new ArgumentNullException("attachmentUri");
			}
			return ReadAttachmentAsync(attachmentUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Database" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">A URI to the Database resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Database" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="databaseUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Database resource where 
		/// // - db_id is the ID property of the Database you wish to read. 
		/// var dbLink = UriFactory.CreateDatabaseUri("db_id");
		/// Database database = await client.ReadDatabaseAsync(dbLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Database>> ReadDatabaseAsync(Uri databaseUri, RequestOptions options = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return ReadDatabaseAsync(databaseUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Document" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentUri">A URI to the Document resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when reading a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Document resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection
		/// // - doc_id is the ID property of the Document you wish to read. 
		/// var docUri = UriFactory.CreateDocumentUri("db_id", "coll_id", "doc_id");
		/// Document document = await client.ReadDocumentAsync(docUri);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Document>> ReadDocumentAsync(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return ReadDocumentAsync(documentUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Document" /> as a generic type T from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="documentUri">A URI to the Document resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.DocumentResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Document" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when reading a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Document resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection
		/// // - doc_id is the ID property of the Document you wish to read. 
		/// var docUri = UriFactory.CreateDocumentUri("db_id", "coll_id", "doc_id");
		/// Customer customer = await client.ReadDocumentAsync<Customer>(docUri);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.DocumentResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<DocumentResponse<T>> ReadDocumentAsync<T>(Uri documentUri, RequestOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentUri == null)
			{
				throw new ArgumentNullException("documentUri");
			}
			return ReadDocumentAsync<T>(documentUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionUri">A URI to the DocumentCollection resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.DocumentCollection" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="documentCollectionUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Document resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection you wish to read. 
		/// var collLink = UriFactory.CreateCollectionUri("db_id", "coll_id");
		/// DocumentCollection coll = await client.ReadDocumentCollectionAsync(collLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<DocumentCollection>> ReadDocumentCollectionAsync(Uri documentCollectionUri, RequestOptions options = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return ReadDocumentCollectionAsync(documentCollectionUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProcedureUri">A URI to the StoredProcedure resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.StoredProcedure" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="storedProcedureUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a StoredProcedure resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection 
		/// // - sproc_id is the ID property of the StoredProcedure you wish to read. 
		/// var sprocLink = UriFactory.CreateStoredProcedureUri("db_id", "coll_id", "sproc_id");
		/// StoredProcedure sproc = await client.ReadStoredProcedureAsync(sprocLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<StoredProcedure>> ReadStoredProcedureAsync(Uri storedProcedureUri, RequestOptions options = null)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			return ReadStoredProcedureAsync(storedProcedureUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Trigger" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggerUri">A URI to the Trigger resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Trigger" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="triggerUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Trigger resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection 
		/// // - trigger_id is the ID property of the Trigger you wish to read. 
		/// var triggerLink = UriFactory.CreateTriggerUri("db_id", "coll_id", "trigger_id");
		/// Trigger trigger = await client.ReadTriggerAsync(triggerLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Trigger>> ReadTriggerAsync(Uri triggerUri, RequestOptions options = null)
		{
			if (triggerUri == null)
			{
				throw new ArgumentNullException("triggerUri");
			}
			return ReadTriggerAsync(triggerUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="functionUri">A URI to the User Defined Function resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="functionUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a UserDefinedFunction resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection 
		/// // - udf_id is the ID property of the UserDefinedFunction you wish to read. 
		/// var udfLink = UriFactory.CreateUserDefinedFunctionUri("db_id", "coll_id", "udf_id");
		/// UserDefinedFunction udf = await client.ReadUserDefinedFunctionAsync(udfLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<UserDefinedFunction>> ReadUserDefinedFunctionAsync(Uri functionUri, RequestOptions options = null)
		{
			if (functionUri == null)
			{
				throw new ArgumentNullException("functionUri");
			}
			return ReadUserDefinedFunctionAsync(functionUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Permission" /> resource as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionUri">A URI to the Permission resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Permission" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="permissionUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Permission resource where 
		/// // - db_id is the ID property of the Database
		/// // - user_id is the ID property of the User 
		/// // - permission_id is the ID property of the Permission you wish to read. 
		/// var permissionLink = UriFactory.CreatePermissionUri("db_id", "coll_id", "user_id");
		/// Permission permission = await client.ReadPermissionAsync(permissionLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Permission>> ReadPermissionAsync(Uri permissionUri, RequestOptions options = null)
		{
			if (permissionUri == null)
			{
				throw new ArgumentNullException("permissionUri");
			}
			return ReadPermissionAsync(permissionUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.User" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userUri">A URI to the User resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.User" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a User resource where 
		/// // - db_id is the ID property of the Database
		/// // - user_id is the ID property of the User you wish to read. 
		/// var userLink = UriFactory.CreateUserUri("db_id", "user_id");
		/// User user = await client.ReadUserAsync(userLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<User>> ReadUserAsync(Uri userUri, RequestOptions options = null)
		{
			if (userUri == null)
			{
				throw new ArgumentNullException("userUri");
			}
			return ReadUserAsync(userUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Conflict" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictUri">A URI to the Conflict resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Conflict" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="conflictUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Conflict resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection
		/// // - conflict_id is the ID property of the Conflict you wish to read. 
		/// var conflictLink = UriFactory.CreateConflictUri("db_id", "coll_id", "conflict_id");
		/// Conflict conflict = await client.ReadConflictAsync(conflictLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<ResourceResponse<Conflict>> ReadConflictAsync(Uri conflictUri, RequestOptions options = null)
		{
			if (conflictUri == null)
			{
				throw new ArgumentNullException("conflictUri");
			}
			return ReadConflictAsync(conflictUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.Schema" /> as an asynchronous operation.
		/// </summary>
		/// <param name="schemaUri">A URI to the Schema resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.Schema" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="schemaUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when reading a Schema are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a Document resource where 
		/// // - db_id is the ID property of the Database
		/// // - coll_id is the ID property of the DocumentCollection
		/// // - schema_id is the ID property of the Document you wish to read. 
		/// var docLink = UriFactory.CreateDocumentUri("db_id", "coll_id", "schema_id");
		/// Schema schema = await client.ReadSchemaAsync(schemaLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Schema" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<Schema>> ReadSchemaAsync(Uri schemaUri, RequestOptions options = null)
		{
			if (schemaUri == null)
			{
				throw new ArgumentNullException("schemaUri");
			}
			return ReadSchemaAsync(schemaUri.OriginalString, options);
		}

		/// <summary>
		/// Reads a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypeUri">A URI to the UserDefinedType resource to be read.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.UserDefinedType" /> containing the read resource record.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">If <paramref name="userDefinedTypeUri" /> is not set.</exception>
		/// <exception cref="T:Microsoft.Azure.Documents.DocumentClientException">This exception can encapsulate many different types of errors. To determine the specific error always look at the StatusCode property. Some common codes you may get when creating a Document are:
		/// <list type="table">
		///     <listheader>
		///         <term>StatusCode</term><description>Reason for exception</description>
		///     </listheader>
		///     <item>
		///         <term>404</term><description>NotFound - This means the resource you tried to read did not exist.</description>
		///     </item>
		///     <item>
		///         <term>429</term><description>TooManyRequests - This means you have exceeded the number of request units per second. Consult the DocumentClientException.RetryAfter value to see how long you should wait before retrying this operation.</description>
		///     </item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// //Reads a UserDefinedType resource where 
		/// // - db_id is the ID property of the Database
		/// // - userDefinedType_id is the ID property of the UserDefinedType you wish to read. 
		/// var userDefinedTypeLink = UriFactory.CreateUserDefinedTypeUri("db_id", "userDefinedType_id");
		/// UserDefinedType userDefinedType = await client.ReadUserDefinedTypeAsync(userDefinedTypeLink);
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// <para>
		/// Doing a read of a resource is the most efficient way to get a resource from the service. If you know the resource's ID, do a read instead of a query by ID.
		/// </para>
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" /> 
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		internal Task<ResourceResponse<UserDefinedType>> ReadUserDefinedTypeAsync(Uri userDefinedTypeUri, RequestOptions options = null)
		{
			if (userDefinedTypeUri == null)
			{
				throw new ArgumentNullException("userDefinedTypeUri");
			}
			return ReadUserDefinedTypeAsync(userDefinedTypeUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of attachments for a document as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI for the attachments.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<Attachment>> ReadAttachmentFeedAsync(Uri attachmentsUri, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return ReadAttachmentFeedAsync(attachmentsUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Reads the feed (sequence) of collections for a database as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionsUri">the URI for the document collections.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<DocumentCollection>> ReadDocumentCollectionFeedAsync(Uri documentCollectionsUri, FeedOptions options = null)
		{
			if (documentCollectionsUri == null)
			{
				throw new ArgumentNullException("documentCollectionsUri");
			}
			return ReadDocumentCollectionFeedAsync(documentCollectionsUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of stored procedures for a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProceduresUri">the URI for the stored procedures.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<StoredProcedure>> ReadStoredProcedureFeedAsync(Uri storedProceduresUri, FeedOptions options = null)
		{
			if (storedProceduresUri == null)
			{
				throw new ArgumentNullException("storedProceduresUri");
			}
			return ReadStoredProcedureFeedAsync(storedProceduresUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of triggers for a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggersUri">the URI for the triggers.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<Trigger>> ReadTriggerFeedAsync(Uri triggersUri, FeedOptions options = null)
		{
			if (triggersUri == null)
			{
				throw new ArgumentNullException("triggersUri");
			}
			return ReadTriggerFeedAsync(triggersUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of user defined functions for a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedFunctionsUri">the URI for the user defined functions.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<UserDefinedFunction>> ReadUserDefinedFunctionFeedAsync(Uri userDefinedFunctionsUri, FeedOptions options = null)
		{
			if (userDefinedFunctionsUri == null)
			{
				throw new ArgumentNullException("userDefinedFunctionsUri");
			}
			return ReadUserDefinedFunctionFeedAsync(userDefinedFunctionsUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of permissions for a user as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionsUri">the URI for the permissions.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<Permission>> ReadPermissionFeedAsync(Uri permissionsUri, FeedOptions options = null)
		{
			if (permissionsUri == null)
			{
				throw new ArgumentNullException("permissionsUri");
			}
			return ReadPermissionFeedAsync(permissionsUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of documents for a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentsUri">the URI for the documents.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<dynamic>> ReadDocumentFeedAsync(Uri documentsUri, FeedOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (documentsUri == null)
			{
				throw new ArgumentNullException("documentsUri");
			}
			return ReadDocumentFeedAsync(documentsUri.OriginalString, options, cancellationToken);
		}

		/// <summary>
		/// Reads the feed (sequence) of users for a database as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="usersUri">the URI for the users.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<User>> ReadUserFeedAsync(Uri usersUri, FeedOptions options = null)
		{
			if (usersUri == null)
			{
				throw new ArgumentNullException("usersUri");
			}
			return ReadUserFeedAsync(usersUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of conflicts for a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictsUri">the URI for the conflicts.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<FeedResponse<Conflict>> ReadConflictFeedAsync(Uri conflictsUri, FeedOptions options = null)
		{
			if (conflictsUri == null)
			{
				throw new ArgumentNullException("conflictsUri");
			}
			return ReadConflictFeedAsync(conflictsUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of <see cref="T:Microsoft.Azure.Documents.PartitionKeyRange" /> for a database account from the Azure Cosmos DB service as an asynchronous operation.
		/// </summary>
		/// <param name="partitionKeyRangesOrCollectionUri">The Uri for partition key ranges, or owner collection.</param>
		/// <param name="options">(Optional) The request options for the request.</param>
		/// <returns>
		/// A <see cref="N:System.Threading.Tasks" /> containing a <see cref="T:Microsoft.Azure.Documents.Client.ResourceResponse`1" /> which wraps a <see cref="T:Microsoft.Azure.Documents.PartitionKeyRange" /> containing the read resource record.
		/// </returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// Uri partitionKeyRangesUri = UriFactory.CreatePartitionKeyRangesUri(database.Id, collection.Id);
		/// FeedResponse<PartitionKeyRange> response = null;
		/// List<string> ids = new List<string>();
		/// do
		/// {
		///     response = await client.ReadPartitionKeyRangeFeedAsync(partitionKeyRangesUri, new FeedOptions { MaxItemCount = 1000 });
		///     foreach (var item in response)
		///     {
		///         ids.Add(item.Id);
		///     }
		/// }
		/// while (!string.IsNullOrEmpty(response.ResponseContinuation));
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.PartitionKeyRange" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.FeedOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.FeedResponse`1" />
		/// <seealso cref="M:Microsoft.Azure.Documents.Client.UriFactory.CreatePartitionKeyRangesUri(System.String,System.String)" />
		/// <seealso cref="T:System.Threading.Tasks.Task" />
		public Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(Uri partitionKeyRangesOrCollectionUri, FeedOptions options = null)
		{
			if (partitionKeyRangesOrCollectionUri == null)
			{
				throw new ArgumentNullException("partitionKeyRangesOrCollectionUri");
			}
			return ReadPartitionKeyRangeFeedAsync(partitionKeyRangesOrCollectionUri.OriginalString, options);
		}

		/// <summary>
		/// Reads the feed (sequence) of user defined types for a database as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypesUri">the URI for the user defined types.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<FeedResponse<UserDefinedType>> ReadUserDefinedTypeFeedAsync(Uri userDefinedTypesUri, FeedOptions options = null)
		{
			if (userDefinedTypesUri == null)
			{
				throw new ArgumentNullException("userDefinedTypesUri");
			}
			return ReadUserDefinedTypeFeedAsync(userDefinedTypesUri.OriginalString, options);
		}

		/// <summary>
		/// Executes a stored procedure against a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TValue">the type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureUri">the URI of the stored procedure to be executed.</param>
		/// <param name="procedureParams">the parameters for the stored procedure execution.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, params dynamic[] procedureParams)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			return ExecuteStoredProcedureAsync<TValue>(storedProcedureUri.OriginalString, procedureParams);
		}

		/// <summary>
		/// Executes a stored procedure against a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TValue">the type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureUri">the URI of the stored procedure to be executed.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="procedureParams">the parameters for the stored procedure execution.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, RequestOptions options, params dynamic[] procedureParams)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			return ExecuteStoredProcedureAsync<TValue>(storedProcedureUri.OriginalString, options, procedureParams);
		}

		/// <summary>
		/// Executes a stored procedure against a collection as an asynchronous operation from the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TValue">the type of the stored procedure's return value.</typeparam>
		/// <param name="storedProcedureUri">the URI of the stored procedure to be executed.</param>
		/// <param name="options">The request options for the request.</param>
		/// <param name="cancellationToken">(Optional) <see cref="T:System.Threading.CancellationToken" /> representing request cancellation.</param>
		/// <param name="procedureParams">the parameters for the stored procedure execution.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		public Task<StoredProcedureResponse<TValue>> ExecuteStoredProcedureAsync<TValue>(Uri storedProcedureUri, RequestOptions options, CancellationToken cancellationToken = default(CancellationToken), params dynamic[] procedureParams)
		{
			if (storedProcedureUri == null)
			{
				throw new ArgumentNullException("storedProcedureUri");
			}
			return ExecuteStoredProcedureAsync<TValue>(storedProcedureUri.OriginalString, options, cancellationToken, procedureParams);
		}

		/// <summary>
		/// Reads the feed (sequence) of schemas for a collection as an asynchronous operation.
		/// </summary>
		/// <param name="schemasUri">the link for the schemas.</param>
		/// <param name="options">The request options for the request.</param>
		/// <returns>The task object representing the service response for the asynchronous operation.</returns>
		internal Task<FeedResponse<Schema>> ReadSchemaFeedAsync(Uri schemasUri, FeedOptions options = null)
		{
			if (schemasUri == null)
			{
				throw new ArgumentNullException("schemasUri");
			}
			return ReadSchemaFeedAsync(schemasUri.OriginalString, options);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">the type of object to query.</typeparam>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<T> CreateAttachmentQuery<T>(Uri attachmentsUri, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery<T>(attachmentsUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<T> CreateAttachmentQuery<T>(Uri attachmentsUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery<T>(attachmentsUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<T> CreateAttachmentQuery<T>(Uri attachmentsUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery<T>(attachmentsUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<Attachment> CreateAttachmentQuery(Uri attachmentsUri, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery(attachmentsUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateAttachmentQuery(Uri attachmentsUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery(attachmentsUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="attachmentsUri">the URI to the attachments.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateAttachmentQuery(Uri attachmentsUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (attachmentsUri == null)
			{
				throw new ArgumentNullException("attachmentsUri");
			}
			return CreateAttachmentQuery(attachmentsUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for document collections in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI to the database.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<DocumentCollection> CreateDocumentCollectionQuery(Uri databaseUri, FeedOptions feedOptions = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateDocumentCollectionQuery(databaseUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for document collections in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI to the database.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateDocumentCollectionQuery(Uri databaseUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateDocumentCollectionQuery(databaseUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for document collections in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">the URI to the database.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateDocumentCollectionQuery(Uri databaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateDocumentCollectionQuery(databaseUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for collections under an Azure Cosmos DB database account
		/// in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">Specifies the database to read collections from.</param>
		/// <param name="feedOptions">Specifies the options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		internal IDocumentQuery<DocumentCollection> CreateDocumentCollectionChangeFeedQuery(Uri databaseUri, ChangeFeedOptions feedOptions)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateDocumentCollectionChangeFeedQuery(databaseUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create query for stored procedures in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProceduresUri">the URI to the stored procedures.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<StoredProcedure> CreateStoredProcedureQuery(Uri storedProceduresUri, FeedOptions feedOptions = null)
		{
			if (storedProceduresUri == null)
			{
				throw new ArgumentNullException("storedProceduresUri");
			}
			return CreateStoredProcedureQuery(storedProceduresUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create query for stored procedures in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProceduresUri">the URI to the stored procedures.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateStoredProcedureQuery(Uri storedProceduresUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (storedProceduresUri == null)
			{
				throw new ArgumentNullException("storedProceduresUri");
			}
			return CreateStoredProcedureQuery(storedProceduresUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create query for stored procedures in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="storedProceduresUri">the URI to the stored procedures.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateStoredProcedureQuery(Uri storedProceduresUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (storedProceduresUri == null)
			{
				throw new ArgumentNullException("storedProceduresUri");
			}
			return CreateStoredProcedureQuery(storedProceduresUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create query for triggers in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggersUri">the URI to the triggers.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<Trigger> CreateTriggerQuery(Uri triggersUri, FeedOptions feedOptions = null)
		{
			if (triggersUri == null)
			{
				throw new ArgumentNullException("triggersUri");
			}
			return CreateTriggerQuery(triggersUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create query for triggers in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggersUri">the URI to the triggers.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateTriggerQuery(Uri triggersUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (triggersUri == null)
			{
				throw new ArgumentNullException("triggersUri");
			}
			return CreateTriggerQuery(triggersUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create query for triggers in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="triggersUri">the URI to the triggers.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateTriggerQuery(Uri triggersUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (triggersUri == null)
			{
				throw new ArgumentNullException("triggersUri");
			}
			return CreateTriggerQuery(triggersUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user-defined functions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedFunctionsUri">the URI to the user-defined functions.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<UserDefinedFunction> CreateUserDefinedFunctionQuery(Uri userDefinedFunctionsUri, FeedOptions feedOptions = null)
		{
			if (userDefinedFunctionsUri == null)
			{
				throw new ArgumentNullException("userDefinedFunctionsUri");
			}
			return CreateUserDefinedFunctionQuery(userDefinedFunctionsUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user-defined functions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedFunctionsUri">the URI to the user-defined functions.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateUserDefinedFunctionQuery(Uri userDefinedFunctionsUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (userDefinedFunctionsUri == null)
			{
				throw new ArgumentNullException("userDefinedFunctionsUri");
			}
			return CreateUserDefinedFunctionQuery(userDefinedFunctionsUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user-defined functions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedFunctionsUri">the URI to the user-defined functions.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateUserDefinedFunctionQuery(Uri userDefinedFunctionsUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (userDefinedFunctionsUri == null)
			{
				throw new ArgumentNullException("userDefinedFunctionsUri");
			}
			return CreateUserDefinedFunctionQuery(userDefinedFunctionsUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for conflicts in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictsUri">the URI to the conflicts.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<Conflict> CreateConflictQuery(Uri conflictsUri, FeedOptions feedOptions = null)
		{
			if (conflictsUri == null)
			{
				throw new ArgumentNullException("conflictsUri");
			}
			return CreateConflictQuery(conflictsUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for conflicts in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictsUri">the URI to the conflicts.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateConflictQuery(Uri conflictsUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (conflictsUri == null)
			{
				throw new ArgumentNullException("conflictsUri");
			}
			return CreateConflictQuery(conflictsUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for conflicts in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="conflictsUri">the URI to the conflicts.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateConflictQuery(Uri conflictsUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (conflictsUri == null)
			{
				throw new ArgumentNullException("conflictsUri");
			}
			return CreateConflictQuery(conflictsUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">the type of object to query.</typeparam>
		/// <param name="documentCollectionUri">The URI of the document collection.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionUri, FeedOptions feedOptions = null)
		{
			if (documentCollectionUri == null)
			{
				throw new ArgumentNullException("documentCollectionUri");
			}
			return CreateDocumentQuery<T>(documentCollectionUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">the type of object to query.</typeparam>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. Please use the override that does not take a partitionKey parameter.")]
		public IOrderedQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionOrDatabaseUri, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery<T>(documentCollectionOrDatabaseUri.OriginalString, feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">the type of object to query.</typeparam>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionOrDatabaseUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery<T>(documentCollectionOrDatabaseUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionOrDatabaseUri, string sqlExpression, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery<T>(documentCollectionOrDatabaseUri, new SqlQuerySpec(sqlExpression), feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionOrDatabaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery<T>(documentCollectionOrDatabaseUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents for the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<T> CreateDocumentQuery<T>(Uri documentCollectionOrDatabaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery<T>(documentCollectionOrDatabaseUri.OriginalString, querySpec, feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<Document> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, FeedOptions feedOptions = null)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IOrderedQueryable<Document> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri.OriginalString, feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, string sqlExpression, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri, new SqlQuerySpec(sqlExpression), feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="documentCollectionOrDatabaseUri">The URI of the document collection, e.g. dbs/db_rid/colls/coll_rid/. 
		/// Alternatively, this can be a URI of the database when using an <see cref="T:Microsoft.Azure.Documents.Client.IPartitionResolver" />, e.g. dbs/db_rid/</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>The query result set.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<dynamic> CreateDocumentQuery(Uri documentCollectionOrDatabaseUri, SqlQuerySpec querySpec, FeedOptions feedOptions, object partitionKey)
		{
			if (documentCollectionOrDatabaseUri == null)
			{
				throw new ArgumentNullException("documentCollectionOrDatabaseUri");
			}
			return CreateDocumentQuery(documentCollectionOrDatabaseUri.OriginalString, querySpec, feedOptions, partitionKey);
		}

		/// <summary>
		/// Extension method to create a change feed query for documents in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">Specifies the collection to read documents from.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		public IDocumentQuery<Document> CreateDocumentChangeFeedQuery(Uri collectionLink, ChangeFeedOptions feedOptions)
		{
			if (collectionLink == null)
			{
				throw new ArgumentNullException("collectionLink");
			}
			return CreateDocumentChangeFeedQuery(collectionLink.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for users in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="usersUri">the URI to the users.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<User> CreateUserQuery(Uri usersUri, FeedOptions feedOptions = null)
		{
			if (usersUri == null)
			{
				throw new ArgumentNullException("usersUri");
			}
			return CreateUserQuery(usersUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for users in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="usersUri">the URI to the users.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateUserQuery(Uri usersUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (usersUri == null)
			{
				throw new ArgumentNullException("usersUri");
			}
			return CreateUserQuery(usersUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for users in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="usersUri">the URI to the users.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreateUserQuery(Uri usersUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (usersUri == null)
			{
				throw new ArgumentNullException("usersUri");
			}
			return CreateUserQuery(usersUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for permissions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionsUri">the URI to the permissions.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IOrderedQueryable<Permission> CreatePermissionQuery(Uri permissionsUri, FeedOptions feedOptions = null)
		{
			if (permissionsUri == null)
			{
				throw new ArgumentNullException("permissionsUri");
			}
			return CreatePermissionQuery(permissionsUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for permissions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionsUri">the URI to the permissions.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreatePermissionQuery(Uri permissionsUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (permissionsUri == null)
			{
				throw new ArgumentNullException("permissionsUri");
			}
			return CreatePermissionQuery(permissionsUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for permissions in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="permissionsUri">the URI to the permissions.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		public IQueryable<dynamic> CreatePermissionQuery(Uri permissionsUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (permissionsUri == null)
			{
				throw new ArgumentNullException("permissionsUri");
			}
			return CreatePermissionQuery(permissionsUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user defined types in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypesUri">the URI to the user defined types.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		internal IOrderedQueryable<UserDefinedType> CreateUserDefinedTypeQuery(Uri userDefinedTypesUri, FeedOptions feedOptions = null)
		{
			if (userDefinedTypesUri == null)
			{
				throw new ArgumentNullException("userDefinedTypesUri");
			}
			return CreateUserDefinedTypeQuery(userDefinedTypesUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user defined types in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypesUri">the URI to the user defined types.</param>
		/// <param name="sqlExpression">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		internal IQueryable<dynamic> CreateUserDefinedTypeQuery(Uri userDefinedTypesUri, string sqlExpression, FeedOptions feedOptions = null)
		{
			if (userDefinedTypesUri == null)
			{
				throw new ArgumentNullException("userDefinedTypesUri");
			}
			return CreateUserDefinedTypeQuery(userDefinedTypesUri, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Extension method to create a query for user defined types in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="userDefinedTypesUri">the URI to the user defined types.</param>
		/// <param name="querySpec">The sql query.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>The query result set.</returns>
		internal IQueryable<dynamic> CreateUserDefinedTypeQuery(Uri userDefinedTypesUri, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			if (userDefinedTypesUri == null)
			{
				throw new ArgumentNullException("userDefinedTypesUri");
			}
			return CreateUserDefinedTypeQuery(userDefinedTypesUri.OriginalString, querySpec, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for user defined types under an Azure Cosmos DB database account
		/// in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseUri">Specifies the database to read user defined types from.</param>
		/// <param name="feedOptions">Specifies the options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		internal IDocumentQuery<UserDefinedType> CreateUserDefinedTypeChangeFeedQuery(Uri databaseUri, ChangeFeedOptions feedOptions)
		{
			if (databaseUri == null)
			{
				throw new ArgumentNullException("databaseUri");
			}
			return CreateUserDefinedTypeChangeFeedQuery(databaseUri.OriginalString, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for attachments in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="documentLink">The link of the parent document.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{T} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries against attachments of custom types.
		/// <code language="c#">
		/// <![CDATA[
		/// public class PriorityAttachment : Attachment
		/// {
		///     [JsonProperty("priority")]
		///     public int Priority;
		/// }
		///
		/// foreach (PriorityAttachment attachment in 
		///     client.CreateAttachmentQuery<PriorityAttachment>(document.SelfLink).Where(a => a.Priority == 0))
		/// {
		///     Console.WriteLine("Id: {0}, MediaLink:{1}", attachment.Id, attachment.MediaLink);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<T> CreateAttachmentQuery<T>(string documentLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<T>(this, ResourceType.Attachment, typeof(Attachment), documentLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for attachments in the Azure Cosmos DB service by using a SQL statement. 
		/// </summary>
		/// <param name="documentLink">The link of the parent document.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{T} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for plain text attachments using a SQL query string.
		/// <code language="c#">
		/// <![CDATA[
		/// foreach (Attachment attachment in client.CreateAttachmentQuery(
		///     document.SelfLink, 
		///     "SELECT * FROM attachments a WHERE a.contentType = 'text/plain'"))
		/// {
		///     Console.WriteLine("Id: {0}, MediaLink:{1}", attachment.Id, attachment.MediaLink);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<T> CreateAttachmentQuery<T>(string documentLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateAttachmentQuery<T>(documentLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		///  Overloaded. This method creates a query for attachments in the Azure Cosmos DB service by using a SQL statement with parameterized values.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="documentLink">The link of the parent document.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{T} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for plain text attachments using a parameterized SQL query string.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM attachments a WHERE a.contentType = @contentType", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@contentType", Value = "text/plain" } }));
		///
		/// foreach (Attachment attachment in client.CreateAttachmentQuery(document.SelfLink, query))
		/// {
		///     Console.WriteLine("Id: {0}, MediaLink:{1}", attachment.Id, attachment.MediaLink);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<T> CreateAttachmentQuery<T>(string documentLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<T>(this, ResourceType.Attachment, typeof(Attachment), documentLink, feedOptions).AsSQL<T, T>(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for attachments in the Azure Cosmos DB service. It returns an IOrderedQueryable{Attachment}.
		/// </summary>
		/// <param name="documentLink">The link to the parent document</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Attachments} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for plain text attachments using LINQ.
		/// <code language="c#">
		/// <![CDATA[
		/// foreach (Attachment attachment in client.CreateAttachmentQuery(document.SelfLink).Where(a => a.ContentType == "text/plain"))
		/// {
		///     Console.WriteLine("Id: {0}, MediaLink:{1}", attachment.Id, attachment.MediaLink);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Attachment> CreateAttachmentQuery(string documentLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Attachment>(this, ResourceType.Attachment, typeof(Attachment), documentLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for attachments in the Azure Cosmos DB service by using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="documentLink">The link to the parent document.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// foreach (Attachment attachment in client.CreateAttachmentQuery<dynamic>(
		///     document.SelfLink, 
		///     "SELECT * FROM attachments a WHERE a.priority = 0"))
		/// {
		///     Console.WriteLine("Id: {0}, Priority:{1}", attachment.id, attachment.priority);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateAttachmentQuery(string documentLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateAttachmentQuery(documentLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		///  Overloaded. This method creates a query for attachments in the Azure Cosmos DB service by using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="documentLink">The link to the parent document resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for plain text attachments using a parameterized SQL query string.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM attachments a WHERE a.priority = @priority", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@priority", Value = 0 } }));
		///
		/// foreach (dynamic attachment in client.CreateAttachmentQuery<dynamic>(document.SelfLink, query))
		/// {
		///     Console.WriteLine("Id: {0}, Priority:{1}", attachment.id, attachment.priority);
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Attachment" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateAttachmentQuery(string documentLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Attachment>(this, ResourceType.Attachment, typeof(Attachment), documentLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for database resources under an account in the Azure Cosmos DB service. It returns An IOrderedQueryable{Database}.
		/// </summary>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Database} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for databases by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Database database = client.CreateDatabaseQuery().Where(d => d.Id == "mydb").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Database> CreateDatabaseQuery(FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Database>(this, ResourceType.Database, typeof(Database), "//dbs/", feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for database resources under an Azure Cosmos DB database account by using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for databases by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Database database = client.CreateDatabaseQuery("SELECT * FROM dbs d WHERE d.id = 'mydb'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDatabaseQuery(string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateDatabaseQuery(new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for database resources under an Azure Cosmos DB database account by using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for databases by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM dbs d WHERE d.id = @id",
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "mydb" }}));
		/// dynamic database = client.CreateDatabaseQuery<dynamic>(query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDatabaseQuery(SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Database>(this, ResourceType.Database, typeof(Database), "//dbs/", feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for databases under an Azure Cosmos DB database account
		/// in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		internal IDocumentQuery<Database> CreateDatabaseChangeFeedQuery(ChangeFeedOptions feedOptions)
		{
			ValidateChangeFeedOptionsForNotPartitionedResource(feedOptions);
			return new ChangeFeedQuery<Database>(this, ResourceType.Database, null, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for collections under an Azure Cosmos DB database. It returns An IOrderedQueryable{DocumentCollection}.
		/// </summary>
		/// <param name="databaseLink">The link to the parent database resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{DocumentCollection} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for collections by id.
		/// <code language="c#">
		/// <![CDATA[
		/// DocumentCollection collection = client.CreateDocumentCollectionQuery(databaseLink).Where(c => c.Id == "myColl").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<DocumentCollection> CreateDocumentCollectionQuery(string databaseLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<DocumentCollection>(this, ResourceType.Collection, typeof(DocumentCollection), databaseLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for collections under an Azure Cosmos DB database using a SQL statement.   It returns an IQueryable{DocumentCollection}.
		/// </summary>
		/// <param name="databaseLink">The link to the parent database resource.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for collections by id.
		/// <code language="c#">
		/// <![CDATA[
		/// DocumentCollection collection = client.CreateDocumentCollectionQuery(databaseLink, "SELECT * FROM colls c WHERE c.id = 'mycoll'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDocumentCollectionQuery(string databaseLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateDocumentCollectionQuery(databaseLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for collections under an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="databaseLink">The link to the parent database resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for collections by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM colls c WHERE c.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "mycoll" }}));
		/// DocumentCollection collection = client.CreateDocumentCollectionQuery(databaseLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDocumentCollectionQuery(string databaseLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<DocumentCollection>(this, ResourceType.Collection, typeof(DocumentCollection), databaseLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for collections under an Azure Cosmos DB database account
		/// in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">Specifies the database to read collections from.</param>
		/// <param name="feedOptions">Specifies the options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		internal IDocumentQuery<DocumentCollection> CreateDocumentCollectionChangeFeedQuery(string databaseLink, ChangeFeedOptions feedOptions)
		{
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentException("databaseLink");
			}
			ValidateChangeFeedOptionsForNotPartitionedResource(feedOptions);
			return new ChangeFeedQuery<DocumentCollection>(this, ResourceType.Collection, databaseLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for stored procedures under a collection in an Azure Cosmos DB service. It returns An IOrderedQueryable{StoredProcedure}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{StoredProcedure} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for stored procedures by id.
		/// <code language="c#">
		/// <![CDATA[
		/// StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collectionLink).Where(c => c.Id == "helloWorld").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<StoredProcedure> CreateStoredProcedureQuery(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<StoredProcedure>(this, ResourceType.StoredProcedure, typeof(StoredProcedure), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for stored procedures under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for stored procedures by id.
		/// <code language="c#">
		/// <![CDATA[
		/// StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collectionLink, "SELECT * FROM sprocs s WHERE s.id = 'HelloWorld'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		public IQueryable<dynamic> CreateStoredProcedureQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateStoredProcedureQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for stored procedures under a collection in an Azure Cosmos DB database using a SQL statement using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for stored procedures by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM sprocs s WHERE s.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "HelloWorld" }}));
		/// StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.StoredProcedure" />
		public IQueryable<dynamic> CreateStoredProcedureQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<StoredProcedure>(this, ResourceType.StoredProcedure, typeof(StoredProcedure), collectionLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for triggers under a collection in an Azure Cosmos DB service. It returns An IOrderedQueryable{Trigger}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Trigger} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for triggers by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Trigger trigger = client.CreateTriggerQuery(collectionLink).Where(t => t.Id == "validate").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Trigger> CreateTriggerQuery(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Trigger>(this, ResourceType.Trigger, typeof(Trigger), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for triggers under a collection in an Azure Cosmos DB service. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for triggers by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Trigger trigger = client.CreateTriggerQuery(collectionLink, "SELECT * FROM triggers t WHERE t.id = 'validate'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateTriggerQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateTriggerQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for triggers under a collection in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{Trigger} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for triggers by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM triggers t WHERE t.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "HelloWorld" }}));
		/// Trigger trigger = client.CreateTriggerQuery(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Trigger" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateTriggerQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Trigger>(this, ResourceType.Trigger, typeof(Trigger), collectionLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for udfs under a collection in an Azure Cosmos DB service. It returns An IOrderedQueryable{UserDefinedFunction}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{UserDefinedFunction} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for user-defined functions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// UserDefinedFunction udf = client.CreateUserDefinedFunctionQuery(collectionLink).Where(u => u.Id == "sqrt").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<UserDefinedFunction> CreateUserDefinedFunctionQuery(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<UserDefinedFunction>(this, ResourceType.UserDefinedFunction, typeof(UserDefinedFunction), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for udfs under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for user-defined functions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// UserDefinedFunction udf = client.CreateUserDefinedFunctionQuery(collectionLink, "SELECT * FROM udfs u WHERE u.id = 'sqrt'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateUserDefinedFunctionQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateUserDefinedFunctionQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for udfs under a collection in an Azure Cosmos DB database with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for user-defined functions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM udfs u WHERE u.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "sqrt" }}));
		/// UserDefinedFunction udf = client.CreateUserDefinedFunctionQuery(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateUserDefinedFunctionQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<UserDefinedFunction>(this, ResourceType.UserDefinedFunction, typeof(UserDefinedFunction), collectionLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for conflicts under a collection in an Azure Cosmos DB service. It returns An IOrderedQueryable{Conflict}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Conflict} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for conflicts by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Conflict conflict = client.CreateConflictQuery(collectionLink).Where(c => c.Id == "summary").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Conflict> CreateConflictQuery(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Conflict>(this, ResourceType.Conflict, typeof(Conflict), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for conflicts under a collection in an Azure Cosmos DB service. It returns an IQueryable{Conflict}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for conflicts by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM conflicts c WHERE c.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "summary" }}));
		/// Conflict conflict = client.CreateConflictQuery(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Conflict" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateConflictQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateConflictQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for conflicts under a collection in an Azure Cosmos DB database with parameterized values. It returns an IQueryable{dynamic}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{dynamic} that can evaluate the query with the provided SQL statement.</returns>
		/// <example>
		/// This example below queries for conflicts by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec("SELECT * FROM conflicts c WHERE c.id = @id", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "summary" }}));
		/// dynamic conflict = client.CreateConflictQuery<dynamic>(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateConflictQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Conflict>(this, ResourceType.Conflict, typeof(Conflict), collectionLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="collectionLink">The link to the parent collection resource.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{T} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for some book documents.
		/// <code language="c#">
		/// <![CDATA[
		/// public class Book 
		/// {
		///     [JsonProperty("title")]
		///     public string Title {get; set;}
		///
		///     public Author Author {get; set;}
		///
		///     public int Price {get; set;}
		/// }
		///
		/// public class Author
		/// {
		///     public string FirstName {get; set;}
		///     public string LastName {get; set;}
		/// }
		///
		/// // Query by the Title property
		/// Book book = client.CreateDocumentQuery<Book>(collectionLink).Where(b => b.Title == "War and Peace").AsEnumerable().FirstOrDefault();
		///
		/// // Query a nested property
		/// Book otherBook = client.CreateDocumentQuery<Book>(collectionLink).Where(b => b.Author.FirstName == "Leo").AsEnumerable().FirstOrDefault();
		///
		/// // Perform a range query (needs an IndexType.Range on price or FeedOptions.EnableScansInQuery)
		/// foreach (Book matchingBook in client.CreateDocumentQuery<Book>(collectionLink).Where(b => b.Price > 100))
		/// {
		///     // Iterate through books
		/// }
		///
		/// // Query asychronously. Optionally set FeedOptions.MaxItemCount to control page size
		/// using (var queryable = client.CreateDocumentQuery<Book>(
		///     collectionLink,
		///     new FeedOptions { MaxItemCount = 10 })
		///     .Where(b => b.Title == "War and Peace")
		///     .AsDocumentQuery())
		/// {
		///     while (queryable.HasMoreResults) 
		///     {
		///         foreach(Book b in await queryable.ExecuteNextAsync<Book>())
		///         {
		///             // Iterate through books
		///         }
		///     }
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// The Azure Cosmos DB LINQ provider compiles LINQ to SQL statements. Refer to http://azure.microsoft.com/documentation/articles/documentdb-sql-query/#linq-to-documentdb-sql for the list of expressions supported by the Azure Cosmos DB LINQ provider. ToString() on the generated IQueryable returns the translated SQL statement. The Azure Cosmos DB provider translates JSON.NET and DataContract serialization attributes for members to their JSON property names.
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<T> CreateDocumentQuery<T>(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<T>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="documentsFeedOrDatabaseLink">The path link for the documents under a collection, e.g. dbs/db_rid/colls/coll_rid/docs/. 
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver, e.g. dbs/db_rid/</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>An IOrderedQueryable{T} that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IOrderedQueryable<T> CreateDocumentQuery<T>(string documentsFeedOrDatabaseLink, FeedOptions feedOptions, object partitionKey)
		{
			return new DocumentQuery<T>(this, ResourceType.Document, typeof(Document), documentsFeedOrDatabaseLink, feedOptions, partitionKey);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{T}.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="collectionLink">The link to the parent collection.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{T} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for some book documents.
		/// <code language="c#">
		/// <![CDATA[
		/// public class Book 
		/// {
		///     [JsonProperty("title")]
		///     public string Title {get; set;}
		///
		///     public Author Author {get; set;}
		///
		///     public int Price {get; set;}
		/// }
		///
		/// public class Author
		/// {
		///     public string FirstName {get; set;}
		///     public string LastName {get; set;}
		/// }
		///
		/// // Query by the Title property
		/// Book book = client.CreateDocumentQuery<Book>(collectionLink, 
		///     "SELECT * FROM books b WHERE b.title  = 'War and Peace'").AsEnumerable().FirstOrDefault();
		///
		/// // Query a nested property
		/// Book otherBook = client.CreateDocumentQuery<Book>(collectionLink,
		///     "SELECT * FROM books b WHERE b.Author.FirstName = 'Leo'").AsEnumerable().FirstOrDefault();
		///
		/// // Perform a range query (needs an IndexType.Range on price or FeedOptions.EnableScansInQuery)
		/// foreach (Book matchingBook in client.CreateDocumentQuery<Book>(
		///     collectionLink, "SELECT * FROM books b where b.Price > 1000"))
		/// {
		///     // Iterate through books
		/// }
		///
		/// // Query asychronously. Optionally set FeedOptions.MaxItemCount to control page size
		/// using (var queryable = client.CreateDocumentQuery<Book>(collectionLink, 
		///     "SELECT * FROM books b WHERE b.title  = 'War and Peace'", 
		///     new FeedOptions { MaxItemCount = 10 }).AsDocumentQuery())
		/// {
		///     while (queryable.HasMoreResults) 
		///     {
		///         foreach(Book b in await queryable.ExecuteNextAsync<Book>())
		///         {
		///             // Iterate through books
		///         }
		///     }
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateDocumentQuery<T>(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{T}.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="collectionLink">The path link for the documents under a collection, e.g. dbs/db_rid/colls/coll_rid/docs/. 
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver, e.g. dbs/db_rid/</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>An IQueryable{T} that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, string sqlExpression, FeedOptions feedOptions, object partitionKey)
		{
			return CreateDocumentQuery<T>(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions, partitionKey);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{T}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="collectionLink">The link to the parent document collection.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IQueryable{T} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for some book documents.
		/// <code language="c#">
		/// <![CDATA[
		/// public class Book 
		/// {
		///     [JsonProperty("title")]
		///     public string Title {get; set;}
		///
		///     public Author Author {get; set;}
		///
		///     public int Price {get; set;}
		/// }
		///
		/// public class Author
		/// {
		///     public string FirstName {get; set;}
		///     public string LastName {get; set;}
		/// }
		///
		/// // Query using Title
		/// Book book, otherBook;
		///
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM books b WHERE b.title = @title", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@title", Value = "War and Peace" }}));
		/// book = client.CreateDocumentQuery<Book>(collectionLink, query).AsEnumerable().FirstOrDefault();
		///
		/// // Query a nested property
		/// query = new SqlQuerySpec(
		///     "SELECT * FROM books b WHERE b.Author.FirstName = @firstName", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@firstName", Value = "Leo" }}));
		/// otherBook = client.CreateDocumentQuery<Book>(collectionLink, query).AsEnumerable().FirstOrDefault();
		///
		/// // Perform a range query (needs an IndexType.Range on price or FeedOptions.EnableScansInQuery)
		/// query = new SqlQuerySpec(
		///     "SELECT * FROM books b WHERE b.Price > @minPrice", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@minPrice", Value = 1000 }}));
		/// foreach (Book b in client.CreateDocumentQuery<Book>(
		///     collectionLink, query))
		/// {
		///     // Iterate through books
		/// }
		///
		/// // Query asychronously. Optionally set FeedOptions.MaxItemCount to control page size
		/// query = new SqlQuerySpec(
		///     "SELECT * FROM books b WHERE b.title = @title", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@title", Value = "War and Peace" }}));
		///
		/// using (var queryable = client.CreateDocumentQuery<Book>(collectionLink, query, 
		///     new FeedOptions { MaxItemCount = 10 }).AsDocumentQuery())
		/// {
		///     while (queryable.HasMoreResults) 
		///     {
		///         foreach(Book b in await queryable.ExecuteNextAsync<Book>())
		///         {
		///             // Iterate through books
		///         }
		///     }
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<T>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions).AsSQL<T, T>(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{T}.
		///  For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <typeparam name="T">The type of object to query.</typeparam>
		/// <param name="collectionLink">The link to the parent document collection.
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>An IQueryable{T} that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<T> CreateDocumentQuery<T>(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions, object partitionKey)
		{
			return new DocumentQuery<T>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions, partitionKey).AsSQL<T, T>(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB service. It returns IOrderedQueryable{Document}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent document collection.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Document} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for documents by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Document document = client.CreateDocumentQuery<Document>(collectionLink)
		///     .Where(d => d.Id == "War and Peace").AsEnumerable().FirstOrDefault();
		///
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>
		/// This overload should be used when the schema of the queried documents is unknown or when querying by ID and replacing/deleting documents.
		/// Since Document is a DynamicObject, it can be dynamically cast back to the original C# object.
		/// </remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Document> CreateDocumentQuery(string collectionLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Document>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB service. It returns IOrderedQueryable{Document}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent document collection.
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">Optional partition key that can be used with an IPartitionResolver.</param>
		/// <returns>An IOrderedQueryable{Document} that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IOrderedQueryable<Document> CreateDocumentQuery(string collectionLink, FeedOptions feedOptions, object partitionKey)
		{
			return new DocumentQuery<Document>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions, partitionKey);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="collectionLink">The link to the parent document collection.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic&gt; that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for book documents.
		/// <code language="c#">
		/// <![CDATA[
		/// // SQL querying allows dynamic property access
		/// dynamic document = client.CreateDocumentQuery<dynamic>(collectionLink,
		///     "SELECT * FROM books b WHERE b.title == 'War and Peace'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateDocumentQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement. It returns an IQueryable{dynamic}.
		/// </summary>
		/// <param name="collectionLink">The link of the parent document collection.
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver, e.g. dbs/db_rid/</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>an IQueryable{dynamic&gt; that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, string sqlExpression, FeedOptions feedOptions, object partitionKey)
		{
			return CreateDocumentQuery(collectionLink, new SqlQuerySpec(sqlExpression), feedOptions, partitionKey);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent document collection.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic&gt; that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for book documents.
		/// <code language="c#">
		/// <![CDATA[
		/// // SQL querying allows dynamic property access
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM books b WHERE b.title = @title", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@title", Value = "War and Peace" }}));
		///
		/// dynamic document = client.CreateDocumentQuery<dynamic>(collectionLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Document>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for documents under a collection in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="collectionLink">The link to the parent document collection.
		/// Alternatively, this can be a path link to the database when using an IPartitionResolver, e.g. dbs/db_rid/</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <param name="partitionKey">The partition key that can be used with an IPartitionResolver.</param>
		/// <returns>an IQueryable{dynamic&gt; that can evaluate the query.</returns>
		/// <remarks>
		/// Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use 
		/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
		/// </remarks>
		[Obsolete("Support for IPartitionResolver based method overloads is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput. Please use the override that does not take a partitionKey parameter.")]
		public IQueryable<dynamic> CreateDocumentQuery(string collectionLink, SqlQuerySpec querySpec, FeedOptions feedOptions, object partitionKey)
		{
			return new DocumentQuery<Document>(this, ResourceType.Document, typeof(Document), collectionLink, feedOptions, partitionKey).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for documents under a collection in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="collectionLink">Specifies the collection to read documents from.</param>
		/// <param name="feedOptions">The options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		/// <remarks>ChangeFeedOptions.PartitionKeyRangeId must be provided.</remarks>
		/// <example>
		/// <code language="c#">
		/// <![CDATA[
		/// string partitionKeyRangeId = "0";   // Use client.ReadPartitionKeyRangeFeedAsync() to obtain the ranges.
		/// string checkpointContinuation = null;
		/// ChangeFeedOptions options = new ChangeFeedOptions
		/// {
		///     PartitionKeyRangeId = partitionKeyRangeId,
		///     RequestContinuation = checkpointContinuation,
		///     StartFromBeginning = true,
		/// };
		/// using(var query = client.CreateDocumentChangeFeedQuery(collection.SelfLink, options))
		/// {
		///     while (true)
		///     {
		///         do
		///         {
		///             var response = await query.ExecuteNextAsync<Document>();
		///             if (response.Count > 0)
		///             {
		///                 var docs = new List<Document>();
		///                 docs.AddRange(response);
		///                 // Process the documents.
		///                 // Checkpoint response.ResponseContinuation.
		///             }
		///         }
		///         while (query.HasMoreResults);
		///         Task.Delay(TimeSpan.FromMilliseconds(500)); // Or break here and use checkpointed continuation token later.
		///     }       
		/// }
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery`1" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Client.ChangeFeedOptions" />
		/// <seealso cref="T:Microsoft.Azure.Documents.PartitionKeyRange" />
		public IDocumentQuery<Document> CreateDocumentChangeFeedQuery(string collectionLink, ChangeFeedOptions feedOptions)
		{
			if (collectionLink == null)
			{
				throw new ArgumentNullException("collectionLink");
			}
			return new ChangeFeedQuery<Document>(this, ResourceType.Document, collectionLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for users under an Azure Cosmos DB service. It returns IOrderedQueryable{User}.
		/// </summary>
		/// <param name="usersLink">The path link for the users under a database, e.g. dbs/db_rid/users/.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{User} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for users by id.
		/// <code language="c#">
		/// <![CDATA[
		/// User user = client.CreateUserQuery(usersLink).Where(u => u.Id == "userid5").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<User> CreateUserQuery(string usersLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<User>(this, ResourceType.User, typeof(User), usersLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for users under an Azure Cosmos DB service. It returns IQueryable{dynamic}.
		/// </summary>
		/// <param name="usersLink">The path link for the users under a database, e.g. dbs/db_rid/users/.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for users by id.
		/// <code language="c#">
		/// <![CDATA[
		/// User user = client.CreateUserQuery(usersLink, "SELECT * FROM users u WHERE u.id = 'userid5'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateUserQuery(string usersLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateUserQuery(usersLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for users under an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="usersLink">The path link for the users under a database, e.g. dbs/db_rid/users/.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic&gt; that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for users by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM users u WHERE u.id = @id", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "userid5" }}));
		///
		/// User user = client.CreateUserQuery(usersLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.User" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateUserQuery(string usersLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<User>(this, ResourceType.User, typeof(User), usersLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for permissions under a user in an Azure Cosmos DB service. It returns IOrderedQueryable{Permission}.
		/// </summary>
		/// <param name="permissionsLink">The path link for the persmissions under a user, e.g. dbs/db_rid/users/user_rid/permissions/.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Permission} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for permissions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Permission perm = client.CreatePermissionQuery(userLink).Where(p => p.id == "readonly").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Permission> CreatePermissionQuery(string permissionsLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Permission>(this, ResourceType.Permission, typeof(Permission), permissionsLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for permissions under a user in an Azure Cosmos DB database using a SQL statement. It returns IQueryable{dynamic}.
		/// </summary>
		/// <param name="permissionsLink">The path link for the persmissions under a user, e.g. dbs/db_rid/users/user_rid/permissions/.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for permissions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// Permission perm = client.CreatePermissionQuery(userLink, 
		///     "SELECT * FROM perms p WHERE p.id = 'readonly'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreatePermissionQuery(string permissionsLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreatePermissionQuery(permissionsLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for permissions under a user in an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="permissionsLink">The path link for the persmissions under a user, e.g. dbs/db_rid/users/user_rid/permissions/.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for permissions by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM perms p WHERE p.id = @id", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "readonly" }}));
		///
		/// Permission perm = client.CreatePermissionQuery(usersLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Permission" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreatePermissionQuery(string permissionsLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Permission>(this, ResourceType.Permission, typeof(Permission), permissionsLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for offers under an Azure Cosmos DB database account. It returns IOrderedQueryable{Offer}.
		/// </summary>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{Offer} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for offers
		/// <code language="c#">
		/// <![CDATA[
		/// // Find the offer for the collection by SelfLink
		/// Offer offer = client.CreateOfferQuery().Where(o => o.Resource == collectionSelfLink).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IOrderedQueryable<Offer> CreateOfferQuery(FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Offer>(this, ResourceType.Offer, typeof(Offer), "//offers/", feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for offers under an Azure Cosmos DB database account using a SQL statement. It returns IQueryable{dynamic}.
		/// </summary>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for offers
		/// <code language="c#">
		/// <![CDATA[
		/// // Find the offer for the collection by SelfLink
		/// Offer offer = client.CreateOfferQuery(
		///     string.Format("SELECT * FROM offers o WHERE o.resource = '{0}'", collectionSelfLink)).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateOfferQuery(string sqlExpression, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Offer>(this, ResourceType.Offer, typeof(Offer), "//offers/", feedOptions).AsSQL(new SqlQuerySpec(sqlExpression));
		}

		/// <summary>
		/// Overloaded. This method creates a query for offers under an Azure Cosmos DB database account using a SQL statement with parameterized values. It returns IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for offers
		/// <code language="c#">
		/// <![CDATA[
		/// // Find the offer for the collection by SelfLink
		/// Offer offer = client.CreateOfferQuery("SELECT * FROM offers o WHERE o.resource = @collectionSelfLink",
		/// new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@collectionSelfLink", Value = collection.SelfLink }}))
		/// .AsEnumerable().FirstOrDefault();
		///
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		public IQueryable<dynamic> CreateOfferQuery(SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<Offer>(this, ResourceType.Offer, typeof(Offer), "//offers/", feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a query for user defined types under an Azure Cosmos DB service. It returns IOrderedQueryable{UserDefinedType}.
		/// </summary>
		/// <param name="userDefinedTypesLink">The path link for the user defined types under a database, e.g. dbs/db_rid/udts/.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>An IOrderedQueryable{UserDefinedType} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for user defined types by id.
		/// <code language="c#">
		/// <![CDATA[
		/// UserDefinedType userDefinedTypes = client.CreateUserDefinedTypeQuery(userDefinedTypesLink).Where(u => u.Id == "userDefinedTypeId5").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		internal IOrderedQueryable<UserDefinedType> CreateUserDefinedTypeQuery(string userDefinedTypesLink, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<UserDefinedType>(this, ResourceType.UserDefinedType, typeof(UserDefinedType), userDefinedTypesLink, feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for user defined types under an Azure Cosmos DB service. It returns IQueryable{dynamic}.
		/// </summary>
		/// <param name="userDefinedTypesLink">The path link for the user defined types under a database, e.g. dbs/db_rid/udts/.</param>
		/// <param name="sqlExpression">The SQL statement.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for user defined types by id.
		/// <code language="c#">
		/// <![CDATA[
		/// UserDefinedType userDefinedTypes = client.CreateUserDefinedTypeQuery(userDefinedTypesLink, "SELECT * FROM userDefinedTypes u WHERE u.id = 'userDefinedTypeId5'").AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		internal IQueryable<dynamic> CreateUserDefinedTypeQuery(string userDefinedTypesLink, string sqlExpression, FeedOptions feedOptions = null)
		{
			return CreateUserDefinedTypeQuery(userDefinedTypesLink, new SqlQuerySpec(sqlExpression), feedOptions);
		}

		/// <summary>
		/// Overloaded. This method creates a query for user defined types under an Azure Cosmos DB database using a SQL statement with parameterized values. It returns an IQueryable{dynamic}.
		/// For more information on preparing SQL statements with parameterized values, please see <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" />.
		/// </summary>
		/// <param name="userDefinedTypesLink">The path link for the user defined types under a database, e.g. dbs/db_rid/udts/.</param>
		/// <param name="querySpec">The SqlQuerySpec instance containing the SQL expression.</param>
		/// <param name="feedOptions">The options for processing the query result feed. For details, see <see cref="T:Microsoft.Azure.Documents.Client.FeedOptions" /></param>
		/// <returns>an IQueryable{dynamic} that can evaluate the query.</returns>
		/// <example>
		/// This example below queries for user defined types by id.
		/// <code language="c#">
		/// <![CDATA[
		/// var query = new SqlQuerySpec(
		///     "SELECT * FROM userDefinedTypes u WHERE u.id = @id", 
		///     new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@id", Value = "userDefinedTypeId5" }}));
		///
		/// UserDefinedType userDefinedType = client.CreateUserDefinedTypeQuery(userDefinedTypesLink, query).AsEnumerable().FirstOrDefault();
		/// ]]>
		/// </code>
		/// </example>
		/// <remarks>Refer to https://msdn.microsoft.com/en-us/library/azure/dn782250.aspx and http://azure.microsoft.com/documentation/articles/documentdb-sql-query/ for syntax and examples.</remarks>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedType" />
		/// <seealso cref="T:Microsoft.Azure.Documents.Linq.IDocumentQuery" />
		internal IQueryable<dynamic> CreateUserDefinedTypeQuery(string userDefinedTypesLink, SqlQuerySpec querySpec, FeedOptions feedOptions = null)
		{
			return new DocumentQuery<UserDefinedType>(this, ResourceType.UserDefinedType, typeof(UserDefinedType), userDefinedTypesLink, feedOptions).AsSQL(querySpec);
		}

		/// <summary>
		/// Overloaded. This method creates a change feed query for user defined types under an Azure Cosmos DB database account
		/// in an Azure Cosmos DB service.
		/// </summary>
		/// <param name="databaseLink">Specifies the database to read user defined types from.</param>
		/// <param name="feedOptions">Specifies the options for processing the query results feed.</param>
		/// <returns>the query result set.</returns>
		internal IDocumentQuery<UserDefinedType> CreateUserDefinedTypeChangeFeedQuery(string databaseLink, ChangeFeedOptions feedOptions)
		{
			if (string.IsNullOrEmpty(databaseLink))
			{
				throw new ArgumentException("databaseLink");
			}
			ValidateChangeFeedOptionsForNotPartitionedResource(feedOptions);
			return new ChangeFeedQuery<UserDefinedType>(this, ResourceType.UserDefinedType, databaseLink, feedOptions);
		}

		private static void ValidateChangeFeedOptionsForNotPartitionedResource(ChangeFeedOptions feedOptions)
		{
			if (feedOptions != null && (feedOptions.PartitionKey != null || !string.IsNullOrEmpty(feedOptions.PartitionKeyRangeId)))
			{
				throw new ArgumentException(RMResources.CannotSpecifyPKRangeForNonPartitionedResource);
			}
		}
	}
}
