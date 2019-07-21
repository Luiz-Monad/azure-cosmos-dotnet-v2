using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Client
{
	internal sealed class GatewayServiceConfigurationReader : IServiceConfigurationReader
	{
		private ReplicationPolicy userReplicationPolicy;

		private ReplicationPolicy systemReplicationPolicy;

		private ConsistencyLevel consistencyLevel;

		private ReadPolicy readPolicy;

		private bool initialized;

		private Uri serviceEndpoint;

		private ApiType apiType;

		private readonly ConnectionPolicy connectionPolicy;

		private IDictionary<string, object> queryEngineConfiguration;

		private readonly IComputeHash authKeyHashFunction;

		private readonly bool hasAuthKeyResourceToken;

		private readonly string authKeyResourceToken = string.Empty;

		private readonly HttpMessageHandler messageHandler;

		public string DatabaseAccountId
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public Uri DatabaseAccountApiEndpoint
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public ReplicationPolicy UserReplicationPolicy
		{
			get
			{
				ThrowIfNotInitialized();
				return userReplicationPolicy;
			}
		}

		public ReplicationPolicy SystemReplicationPolicy
		{
			get
			{
				ThrowIfNotInitialized();
				return systemReplicationPolicy;
			}
		}

		public ReadPolicy ReadPolicy
		{
			get
			{
				ThrowIfNotInitialized();
				return readPolicy;
			}
		}

		public string PrimaryMasterKey
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string SecondaryMasterKey
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string PrimaryReadonlyMasterKey
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string SecondaryReadonlyMasterKey
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string ResourceSeedKey
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool EnableAuthorization => true;

		public ConsistencyLevel DefaultConsistencyLevel
		{
			get
			{
				ThrowIfNotInitialized();
				return consistencyLevel;
			}
			set
			{
				ThrowIfNotInitialized();
				consistencyLevel = value;
			}
		}

		public string SubscriptionId
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IDictionary<string, object> QueryEngineConfiguration
		{
			get
			{
				ThrowIfNotInitialized();
				return queryEngineConfiguration;
			}
		}

		public IList<Uri> DatabaseServices
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IList<Uri> CollectionServices
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IList<Uri> UserServices
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IList<Uri> PermissionServices
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IList<Uri> ServerServices
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public GatewayServiceConfigurationReader(Uri serviceEndpoint, IComputeHash stringHMACSHA256Helper, bool hasResourceToken, string resourceToken, ConnectionPolicy connectionPolicy, ApiType apiType, HttpMessageHandler messageHandler = null)
		{
			this.serviceEndpoint = serviceEndpoint;
			authKeyHashFunction = stringHMACSHA256Helper;
			hasAuthKeyResourceToken = hasResourceToken;
			authKeyResourceToken = resourceToken;
			this.connectionPolicy = connectionPolicy;
			this.messageHandler = messageHandler;
			this.apiType = apiType;
		}

		private async Task<DatabaseAccount> GetDatabaseAccountAsync(Uri serviceEndpoint)
		{
			HttpClient httpClient = (messageHandler == null) ? new HttpClient() : new HttpClient(messageHandler);
			httpClient.DefaultRequestHeaders.Add("x-ms-version", HttpConstants.Versions.CurrentVersion);
			httpClient.AddUserAgentHeader(connectionPolicy.UserAgentContainer);
			httpClient.AddApiTypeHeader(apiType);
			string empty = string.Empty;
			string value;
			if (hasAuthKeyResourceToken)
			{
				value = HttpUtility.UrlEncode(authKeyResourceToken);
			}
			else
			{
				string value2 = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
				httpClient.DefaultRequestHeaders.Add("x-ms-date", value2);
				INameValueCollection nameValueCollection = new StringKeyValueCollection();
				nameValueCollection.Add("x-ms-date", value2);
				value = AuthorizationHelper.GenerateKeyAuthorizationSignature("GET", serviceEndpoint, nameValueCollection, authKeyHashFunction);
			}
			httpClient.DefaultRequestHeaders.Add("authorization", value);
			using (HttpResponseMessage responseMessage = await httpClient.GetHttpAsync(serviceEndpoint))
			{
				using (DocumentServiceResponse documentServiceResponse = await ClientExtensions.ParseResponseAsync(responseMessage))
				{
					return documentServiceResponse.GetInternalResource(DatabaseAccount.CreateNewInstance);
				}
			}
		}

		public async Task InitializeAsync()
		{
			if (!initialized)
			{
				await InitializeReaderAsync();
			}
		}

		public async Task<DatabaseAccount> InitializeReaderAsync()
		{
			DatabaseAccount databaseAccount = await GlobalEndpointManager.GetDatabaseAccountFromAnyLocationsAsync(serviceEndpoint, connectionPolicy.PreferredLocations, GetDatabaseAccountAsync);
			userReplicationPolicy = databaseAccount.ReplicationPolicy;
			systemReplicationPolicy = databaseAccount.SystemReplicationPolicy;
			consistencyLevel = databaseAccount.ConsistencyPolicy.DefaultConsistencyLevel;
			readPolicy = databaseAccount.ReadPolicy;
			queryEngineConfiguration = databaseAccount.QueryEngineConfiuration;
			initialized = true;
			return databaseAccount;
		}

		private void ThrowIfNotInitialized()
		{
			if (!initialized)
			{
				throw new InvalidProgramException();
			}
		}
	}
}
