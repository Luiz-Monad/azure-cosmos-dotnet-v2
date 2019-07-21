using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Represents a DatabaseAccount. A DatabaseAccount is the container for databases in the Azure Cosmos DB service.
	/// </summary>
	public class DatabaseAccount : Resource
	{
		private ReplicationPolicy replicationPolicy;

		private ConsistencyPolicy consistencyPolicy;

		private ReplicationPolicy systemReplicationPolicy;

		private ReadPolicy readPolicy;

		private Dictionary<string, object> queryEngineConfiguration;

		private Collection<DatabaseAccountLocation> readLocations;

		private Collection<DatabaseAccountLocation> writeLocations;

		/// <summary>
		/// Gets the self-link for Databases in the databaseAccount from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for Databases in the databaseAccount.
		/// </value>
		[JsonProperty(PropertyName = "_dbs")]
		public string DatabasesLink
		{
			get
			{
				return GetValue<string>("_dbs");
			}
			internal set
			{
				SetValue("_dbs", value);
			}
		}

		/// <summary>
		/// Gets the self-link for Media in the databaseAccount from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for Media in the databaseAccount.
		/// </value>
		[JsonProperty(PropertyName = "media")]
		public string MediaLink
		{
			get
			{
				return GetValue<string>("media");
			}
			internal set
			{
				SetValue("media", value);
			}
		}

		/// <summary>
		/// Gets the list of locations reprsenting the writable regions of
		/// this database account from the Azure Cosmos DB service.
		/// </summary>
		[JsonIgnore]
		public IEnumerable<DatabaseAccountLocation> WritableLocations
		{
			get
			{
				return WriteLocationsInternal;
			}
		}

		[JsonProperty(PropertyName = "writableLocations")]
		internal Collection<DatabaseAccountLocation> WriteLocationsInternal
		{
			get
			{
				if (writeLocations == null)
				{
					writeLocations = GetObjectCollection<DatabaseAccountLocation>("writableLocations");
					if (writeLocations == null)
					{
						writeLocations = new Collection<DatabaseAccountLocation>();
						SetObjectCollection("writableLocations", writeLocations);
					}
				}
				return writeLocations;
			}
			set
			{
				writeLocations = value;
				SetObjectCollection("writableLocations", value);
			}
		}

		/// <summary>
		/// Gets the list of locations reprsenting the readable regions of
		/// this database account from the Azure Cosmos DB service.
		/// </summary>
		[JsonIgnore]
		public IEnumerable<DatabaseAccountLocation> ReadableLocations
		{
			get
			{
				return ReadLocationsInternal;
			}
		}

		[JsonProperty(PropertyName = "readableLocations")]
		internal Collection<DatabaseAccountLocation> ReadLocationsInternal
		{
			get
			{
				if (readLocations == null)
				{
					readLocations = GetObjectCollection<DatabaseAccountLocation>("readableLocations");
					if (readLocations == null)
					{
						readLocations = new Collection<DatabaseAccountLocation>();
						SetObjectCollection("readableLocations", readLocations);
					}
				}
				return readLocations;
			}
			set
			{
				readLocations = value;
				SetObjectCollection("readableLocations", value);
			}
		}

		/// <summary>
		/// Gets the storage quota for media storage in the databaseAccount from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The storage quota in measured MBs.
		/// </value>
		/// <remarks>
		/// This value is retrieved from the gateway.
		/// </remarks>
		public long MaxMediaStorageUsageInMB
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the current attachment content (media) usage in MBs from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The attachment content (media) usage in MBs.
		/// </value>
		/// <remarks>
		/// The value is retrieved from the gateway. The value is returned from cached information updated periodically 
		/// and is not guaranteed to be real time.
		/// </remarks>
		public long MediaStorageUsageInMB
		{
			get;
			internal set;
		}

		/// <summary>
		/// The cumulative sum of current sizes of created collection in MB
		/// Value is returned from cached information which is updated periodically and is not guaranteed to be real time
		/// TODO remove this property tfs 4442779
		/// </summary>
		internal long ConsumedDocumentStorageInMB
		{
			get;
			set;
		}

		/// <summary>
		/// The cumulative sum of maximum sizes of created collection in MB
		/// Value is returned from cached information which is updated periodically and is not guaranteed to be real time
		/// TODO remove this property tfs 4442779
		/// </summary>
		internal long ReservedDocumentStorageInMB
		{
			get;
			set;
		}

		/// <summary>
		/// The provisioned documented storage capacity for the database account
		/// Value is returned from cached information which is updated periodically and is not guaranteed to be real time
		/// TODO remove this property tfs 4442779
		/// </summary>
		internal long ProvisionedDocumentStorageInMB
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DatabaseAccount.ConsistencyPolicy" /> settings from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The ConsistencyPolicy settings.
		/// </value>
		public ConsistencyPolicy ConsistencyPolicy
		{
			get
			{
				if (consistencyPolicy == null)
				{
					consistencyPolicy = GetObject<ConsistencyPolicy>("userConsistencyPolicy");
					if (consistencyPolicy == null)
					{
						consistencyPolicy = new ConsistencyPolicy();
					}
				}
				return consistencyPolicy;
			}
		}

		/// <summary>
		/// Gets the self-link for Address Routing Table in the databaseAccount
		/// </summary>
		[JsonProperty(PropertyName = "addresses")]
		internal string AddressesLink
		{
			get
			{
				return GetValue<string>("addresses");
			}
			set
			{
				SetValue("addresses", value.ToString());
			}
		}

		/// <summary>
		/// Gets the ReplicationPolicy settings
		/// </summary>
		internal ReplicationPolicy ReplicationPolicy
		{
			get
			{
				if (replicationPolicy == null)
				{
					replicationPolicy = GetObject<ReplicationPolicy>("userReplicationPolicy");
					if (replicationPolicy == null)
					{
						replicationPolicy = new ReplicationPolicy();
					}
				}
				return replicationPolicy;
			}
		}

		/// <summary>
		/// Gets the SystemReplicationPolicy settings
		/// </summary>
		internal ReplicationPolicy SystemReplicationPolicy
		{
			get
			{
				if (systemReplicationPolicy == null)
				{
					systemReplicationPolicy = GetObject<ReplicationPolicy>("systemReplicationPolicy");
					if (systemReplicationPolicy == null)
					{
						systemReplicationPolicy = new ReplicationPolicy();
					}
				}
				return systemReplicationPolicy;
			}
		}

		internal ReadPolicy ReadPolicy
		{
			get
			{
				if (readPolicy == null)
				{
					readPolicy = GetObject<ReadPolicy>("readPolicy");
					if (readPolicy == null)
					{
						readPolicy = new ReadPolicy();
					}
				}
				return readPolicy;
			}
		}

		internal IDictionary<string, object> QueryEngineConfiuration
		{
			get
			{
				if (queryEngineConfiguration == null)
				{
					string value = GetValue<string>("queryEngineConfiguration");
					if (!string.IsNullOrEmpty(value))
					{
						queryEngineConfiguration = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
					}
					if (queryEngineConfiguration == null)
					{
						queryEngineConfiguration = new Dictionary<string, object>();
					}
				}
				return queryEngineConfiguration;
			}
		}

		internal bool EnableMultipleWriteLocations
		{
			get
			{
				return GetValue<bool>("enableMultipleWriteLocations");
			}
			set
			{
				SetValue("enableMultipleWriteLocations", value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> class.
		/// </summary>
		internal DatabaseAccount()
		{
			base.SelfLink = string.Empty;
		}

		internal override void OnSave()
		{
			if (replicationPolicy != null)
			{
				replicationPolicy.OnSave();
				SetObject("userReplicationPolicy", replicationPolicy);
			}
			if (consistencyPolicy != null)
			{
				consistencyPolicy.OnSave();
				SetObject("userConsistencyPolicy", consistencyPolicy);
			}
			if (systemReplicationPolicy != null)
			{
				systemReplicationPolicy.OnSave();
				SetObject("systemReplicationPolicy", systemReplicationPolicy);
			}
			if (readPolicy != null)
			{
				readPolicy.OnSave();
				SetObject("readPolicy", readPolicy);
			}
			if (readLocations != null)
			{
				SetObjectCollection("readableLocations", readLocations);
			}
			if (writeLocations != null)
			{
				SetObjectCollection("writableLocations", writeLocations);
			}
			if (queryEngineConfiguration != null)
			{
				SetValue("queryEngineConfiguration", JsonConvert.SerializeObject(queryEngineConfiguration));
			}
		}

		internal static DatabaseAccount CreateNewInstance()
		{
			return new DatabaseAccount();
		}
	}
}
