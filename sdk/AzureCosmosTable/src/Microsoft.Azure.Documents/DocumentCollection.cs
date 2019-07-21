using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a document collection in the Azure Cosmos DB service. A collection is a named logical container for documents. 
	/// </summary>
	/// <remarks>
	/// A database may contain zero or more named collections and each collection consists of zero or more JSON documents. 
	/// Being schema-free, the documents in a collection do not need to share the same structure or fields. Since collections are application resources, 
	/// they can be authorized using either the master key or resource keys.
	/// Refer to <see>http://azure.microsoft.com/documentation/articles/documentdb-resources/#collections</see> for more details on collections.
	/// </remarks>
	/// <example>
	/// The example below creates a new partitioned collection with 50000 Request-per-Unit throughput.
	/// The partition key is the first level 'country' property in all the documents within this collection.
	/// <code language="c#">
	/// <![CDATA[
	/// DocumentCollection collection = await client.CreateDocumentCollectionAsync(
	///     databaseLink,
	///     new DocumentCollection 
	///     { 
	///         Id = "MyCollection",
	///         PartitionKey = new PartitionKeyDefinition
	///         {
	///             Paths = new Collection<string> { "/country" }
	///         }
	///     }, 
	///     new RequestOptions { OfferThroughput = 50000} ).Result;
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The example below creates a new collection with OfferThroughput set to 10000.
	/// <code language="c#">
	/// <![CDATA[
	/// DocumentCollection collection = await client.CreateDocumentCollectionAsync(
	///     databaseLink,
	///     new DocumentCollection { Id = "MyCollection" }, 
	///     new RequestOptions { OfferThroughput = 10000} ).Result;
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The example below creates a new collection with a custom indexing policy.
	/// <code language="c#">
	/// <![CDATA[
	/// DocumentCollection collectionSpec = new DocumentCollection { Id ="MyCollection" };
	/// collectionSpec.IndexingPolicy.Automatic = true;
	/// collectionSpec.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
	/// collection = await client.CreateDocumentCollectionAsync(database.SelfLink, collectionSpec);
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The example below creates a document of type Book inside this collection.
	/// <code language="c#">
	/// <![CDATA[
	/// Document doc = await client.CreateDocumentAsync(collection.SelfLink, new Book { Title = "War and Peace" });
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The example below queries for a Database by Id to retrieve the SelfLink.
	/// <code language="c#">
	/// <![CDATA[
	/// using Microsoft.Azure.Documents.Linq;
	/// DocumentCollection collection = client.CreateDocumentCollectionQuery(databaseLink).Where(c => c.Id == "myColl").AsEnumerable().FirstOrDefault();
	/// string collectionLink = collection.SelfLink;
	/// ]]>
	/// </code>
	/// </example>
	/// <example>
	/// The example below deletes this collection.
	/// <code language="c#">
	/// <![CDATA[
	/// await client.DeleteDocumentCollectionAsync(collection.SelfLink);
	/// ]]>
	/// </code>
	/// </example>
	/// <seealso cref="T:Microsoft.Azure.Documents.IndexingPolicy" />
	/// <seealso cref="T:Microsoft.Azure.Documents.PartitionKeyDefinition" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Database" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Offer" />
	public class DocumentCollection : Resource
	{
		private IndexingPolicy indexingPolicy;

		private PartitionKeyDefinition partitionKey;

		private SchemaDiscoveryPolicy schemaDiscoveryPolicy;

		private UniqueKeyPolicy uniqueKeyPolicy;

		private ConflictResolutionPolicy conflictResolutionPolicy;

		private ChangeFeedPolicy changeFeedPolicy;

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.IndexingPolicy" /> associated with the collection from the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// The indexing policy associated with the collection.
		/// </value>
		public IndexingPolicy IndexingPolicy
		{
			get
			{
				if (indexingPolicy == null)
				{
					indexingPolicy = (GetObject<IndexingPolicy>("indexingPolicy") ?? new IndexingPolicy());
				}
				return indexingPolicy;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "IndexingPolicy"));
				}
				indexingPolicy = value;
				SetObject("indexingPolicy", value);
			}
		}

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.GeospatialType" /> associated with the collection from the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// The geospatial type associated with this collection.
		/// </value>
		[JsonProperty(PropertyName = "geospatialType")]
		internal GeospatialType GeospatialType
		{
			get
			{
				GeospatialType geospatialType = GeospatialType.Geography;
				string value = GetValue<string>("geospatialType");
				if (string.IsNullOrEmpty(value))
				{
					value = geospatialType.ToString();
				}
				return (GeospatialType)Enum.Parse(typeof(GeospatialType), value, ignoreCase: true);
			}
			set
			{
				SetValue("geospatialType", value.ToString());
			}
		}

		/// <summary>
		/// Gets the self-link for documents in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for documents in a collection.
		/// </value>
		[JsonProperty(PropertyName = "_docs")]
		public string DocumentsLink
		{
			get
			{
				return base.SelfLink.TrimEnd(new char[1]
				{
					'/'
				}) + "/" + GetValue<string>("_docs");
			}
		}

		/// <summary>
		/// Gets the self-link for stored procedures in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for stored procedures in a collection.
		/// </value>
		[JsonProperty(PropertyName = "_sprocs")]
		public string StoredProceduresLink
		{
			get
			{
				return base.SelfLink.TrimEnd(new char[1]
				{
					'/'
				}) + "/" + GetValue<string>("_sprocs");
			}
		}

		/// <summary>
		/// Gets the self-link for triggers in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for triggers in a collection.
		/// </value>
		public string TriggersLink => base.SelfLink.TrimEnd(new char[1]
		{
			'/'
		}) + "/" + GetValue<string>("_triggers");

		/// <summary>
		/// Gets the self-link for user defined functions in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for user defined functions in a collection.
		/// </value>
		public string UserDefinedFunctionsLink => base.SelfLink.TrimEnd(new char[1]
		{
			'/'
		}) + "/" + GetValue<string>("_udfs");

		/// <summary>
		/// Gets the self-link for conflicts in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link for conflicts in a collection.
		/// </value>
		public string ConflictsLink => base.SelfLink.TrimEnd(new char[1]
		{
			'/'
		}) + "/" + GetValue<string>("_conflicts");

		/// <summary>
		/// Gets or sets <see cref="T:Microsoft.Azure.Documents.PartitionKeyDefinition" /> object in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// <see cref="T:Microsoft.Azure.Documents.PartitionKeyDefinition" /> object.
		/// </value>
		[JsonProperty(PropertyName = "partitionKey")]
		public PartitionKeyDefinition PartitionKey
		{
			get
			{
				if (partitionKey == null)
				{
					partitionKey = (GetValue<PartitionKeyDefinition>("partitionKey") ?? new PartitionKeyDefinition());
				}
				return partitionKey;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "PartitionKey"));
				}
				partitionKey = value;
				SetValue("partitionKey", partitionKey);
			}
		}

		/// <summary>
		/// Gets the default time to live in seconds for documents in a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// It is an optional property.
		/// A valid value must be either a nonzero positive integer, '-1', or <c>null</c>.
		/// By default, DefaultTimeToLive is set to null meaning the time to live is turned off for the collection.
		/// The unit of measurement is seconds. The maximum allowed value is 2147483647.
		/// </value>
		/// <remarks>
		/// <para>
		/// The <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> will be applied to all the documents in the collection as the default time-to-live policy.
		/// The individual document could override the default time-to-live policy by setting its <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" />.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> is <c>null</c>, the time-to-live will be turned off for the collection.
		/// It means all the documents will never expire. The individual document's <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> will be disregarded.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> is '-1', the time-to-live will be turned on for the collection.
		/// By default, all the documents will never expire. The individual document could be given a specific time-to-live value by setting its
		/// <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" />. The document's <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> will be honored, and the expired documents
		/// will be deleted in background.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> is a nonzero positive integer, the time-to-live will be turned on for the collection.
		/// And a default time-to-live in seconds will be applied to all the documents. A document will be expired after the
		/// specified <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> value in seconds since its last write time.
		/// The individual document could override the default time-to-live policy by setting its <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" />.
		/// Please refer to the <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> for more details about evaluating the final time-to-live policy of a document.
		/// </para>
		/// </remarks>
		/// <example>
		/// The example below disables time-to-live on a collection.
		/// <code language="c#">
		/// <![CDATA[
		///     collection.DefaultTimeToLive = null;
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// The example below enables time-to-live on a collection. By default, all the documents never expire.
		/// <code language="c#">
		/// <![CDATA[
		///     collection.DefaultTimeToLive = -1;
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// The example below enables time-to-live on a collection. By default, the document will expire after 1000 seconds
		/// since its last write time.
		/// <code language="c#">
		/// <![CDATA[
		///     collection.DefaultTimeToLive = 1000;
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.Document" />
		[JsonProperty(PropertyName = "defaultTtl", NullValueHandling = NullValueHandling.Ignore)]
		public int? DefaultTimeToLive
		{
			get
			{
				return GetValue<int?>("defaultTtl");
			}
			set
			{
				SetValue("defaultTtl", value);
			}
		}

		/// <summary>
		/// Gets or sets the time to live base timestamp property path.
		/// </summary>
		/// <value>
		/// It is an optional property.
		/// This property should be only present when DefaultTimeToLive is set. When this property is present, time to live
		/// for a document is decided based on the value of this property in document.
		/// By default, TimeToLivePropertyPath is set to null meaning the time to live is based on the _ts property in document.
		/// </value>
		[JsonProperty(PropertyName = "ttlPropertyPath", NullValueHandling = NullValueHandling.Ignore)]
		public string TimeToLivePropertyPath
		{
			get
			{
				return GetValue<string>("ttlPropertyPath");
			}
			set
			{
				SetValue("ttlPropertyPath", value);
			}
		}

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.SchemaDiscoveryPolicy" /> associated with the collection. 
		/// </summary>
		/// <value>
		/// The schema discovery policy associated with the collection.
		/// </value>
		internal SchemaDiscoveryPolicy SchemaDiscoveryPolicy
		{
			get
			{
				if (schemaDiscoveryPolicy == null)
				{
					schemaDiscoveryPolicy = (GetObject<SchemaDiscoveryPolicy>("schemaDiscoveryPolicy") ?? new SchemaDiscoveryPolicy());
				}
				return schemaDiscoveryPolicy;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "SchemaDiscoveryPolicy"));
				}
				schemaDiscoveryPolicy = value;
				SetObject("schemaDiscoveryPolicy", value);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.UniqueKeyPolicy" /> that guarantees uniqueness of documents in collection in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "uniqueKeyPolicy")]
		public UniqueKeyPolicy UniqueKeyPolicy
		{
			get
			{
				if (uniqueKeyPolicy == null)
				{
					uniqueKeyPolicy = (GetObject<UniqueKeyPolicy>("uniqueKeyPolicy") ?? new UniqueKeyPolicy());
				}
				return uniqueKeyPolicy;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "UniqueKeyPolicy"));
				}
				uniqueKeyPolicy = value;
				SetObject("uniqueKeyPolicy", value);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.ConflictResolutionPolicy" /> that is used for resolving conflicting writes on documents in different regions, in a collection in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "conflictResolutionPolicy")]
		public ConflictResolutionPolicy ConflictResolutionPolicy
		{
			get
			{
				if (conflictResolutionPolicy == null)
				{
					conflictResolutionPolicy = (GetObject<ConflictResolutionPolicy>("conflictResolutionPolicy") ?? new ConflictResolutionPolicy());
				}
				return conflictResolutionPolicy;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "ConflictResolutionPolicy"));
				}
				conflictResolutionPolicy = value;
				SetObject("conflictResolutionPolicy", value);
			}
		}

		/// <summary>
		/// Gets a collection of <see cref="P:Microsoft.Azure.Documents.DocumentCollection.PartitionKeyRangeStatistics" /> object in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.PartitionKeyRangeStatistics" /> object.
		/// </value>
		/// <remarks>
		/// This is reported based on a sub-sampling of partition keys within the collection and hence these are approximate. If your partition keys are below 1GB of storage, they may not show up in the reported statistics.
		/// </remarks>
		/// <example>
		/// The following code shows how to log statistics for all partition key ranges as a string:
		/// <code language="c#">
		/// <![CDATA[
		/// var collection = await client.ReadDocumentCollectionAsync(
		///     collectionUri,
		///     new RequestOptions { PopulatePartitionKeyRangeStatistics = true } );
		///
		/// Console.WriteLine(collection.PartitionKeyRangeStatistics.ToString());
		/// ]]>
		/// </code>
		/// To log individual partition key range statistics, use the following code:
		/// <code language="c#">
		/// <![CDATA[
		/// var collection = await client.ReadDocumentCollectionAsync(
		///     collectionUri,
		///     new RequestOptions { PopulatePartitionKeyRangeStatistics = true } );
		///
		/// foreach(var partitionKeyRangeStatistics in collection.PartitionKeyRangeStatistics)
		/// {
		///     Console.WriteLine(partitionKeyRangeStatistics.PartitionKeyRangeId);
		///     Console.WriteLine(partitionKeyRangeStatistics.DocumentCount);
		///     Console.WriteLine(partitionKeyRangeStatistics.SizeInKB);
		///
		///     foreach(var partitionKeyStatistics in partitionKeyRangeStatistics.PartitionKeyStatistics)
		///     {
		///         Console.WriteLine(partitionKeyStatistics.PartitionKey);
		///         Console.WriteLine(partitionKeyStatistics.SizeInKB);
		///     }
		///  }
		/// ]]>
		/// </code>
		/// The output will look something like that:
		/// "statistics": [
		/// {"id":"0","sizeInKB":1410184,"documentCount":42807,"partitionKeys":[]},
		/// {"id":"1","sizeInKB":3803113,"documentCount":150530,"partitionKeys":[{"partitionKey":["4009696"],"sizeInKB":3731654}]},
		/// {"id":"2","sizeInKB":1447855,"documentCount":59056,"partitionKeys":[{"partitionKey":["4009633"],"sizeInKB":2861210},{"partitionKey":["4004207"],"sizeInKB":2293163}]},
		/// {"id":"3","sizeInKB":1026254,"documentCount":44241,"partitionKeys":[]},
		/// {"id":"4","sizeInKB":3250973,"documentCount":124959,"partitionKeys":[]}
		/// ]
		/// </example>
		/// <seealso cref="P:Microsoft.Azure.Documents.Client.RequestOptions.PopulatePartitionKeyRangeStatistics" />
		/// <seealso cref="T:Microsoft.Azure.Documents.PartitionKeyStatistics" />
		[JsonIgnore]
		public IReadOnlyList<PartitionKeyRangeStatistics> PartitionKeyRangeStatistics
		{
			get
			{
				return new JsonSerializableList<PartitionKeyRangeStatistics>((from jraw in StatisticsJRaw
				where jraw != null
				select JsonConvert.DeserializeObject<PartitionKeyRangeStatistics>((string)jraw.Value)).ToList());
			}
		}

		[JsonProperty(PropertyName = "statistics")]
		internal IReadOnlyList<JRaw> StatisticsJRaw
		{
			get
			{
				return GetValue<IReadOnlyList<JRaw>>("statistics") ?? new Collection<JRaw>();
			}
			set
			{
				SetValue("statistics", value);
			}
		}

		internal bool HasPartitionKey
		{
			get
			{
				if (partitionKey != null)
				{
					return true;
				}
				return GetValue<object>("partitionKey") != null;
			}
		}

		/// <summary>
		/// Instantiates a new instance of the <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" /> object.
		/// </summary>
		/// <remarks>
		/// The function selects the right partition key constant for inserting documents that don't have
		/// a value for partition key. The constant selection is based on whether the collection is migrated
		/// or user partitioned
		/// </remarks>
		internal PartitionKeyInternal NonePartitionKeyValue
		{
			get
			{
				if (PartitionKey.Paths.Count == 0 || PartitionKey.IsSystemKey.GetValueOrDefault(false))
				{
					return PartitionKeyInternal.Empty;
				}
				return PartitionKeyInternal.Undefined;
			}
		}

		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DocumentCollection.ChangeFeedPolicy" /> associated with the collection from the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// The change feed policy associated with the collection.
		/// </value>
		[JsonProperty(PropertyName = "changeFeedPolicy")]
		internal ChangeFeedPolicy ChangeFeedPolicy
		{
			get
			{
				if (changeFeedPolicy == null)
				{
					changeFeedPolicy = (GetObject<ChangeFeedPolicy>("changeFeedPolicy") ?? new ChangeFeedPolicy());
				}
				return changeFeedPolicy;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, RMResources.PropertyCannotBeNull, "ChangeFeedPolicy"));
				}
				changeFeedPolicy = value;
				SetObject("changeFeedPolicy", value);
			}
		}

		internal override void OnSave()
		{
			if (indexingPolicy != null)
			{
				indexingPolicy.OnSave();
				SetObject("indexingPolicy", indexingPolicy);
			}
			if (partitionKey != null)
			{
				SetValue("partitionKey", partitionKey);
			}
			if (uniqueKeyPolicy != null)
			{
				uniqueKeyPolicy.OnSave();
				SetObject("uniqueKeyPolicy", uniqueKeyPolicy);
			}
			if (conflictResolutionPolicy != null)
			{
				conflictResolutionPolicy.OnSave();
				SetObject("conflictResolutionPolicy", conflictResolutionPolicy);
			}
			if (schemaDiscoveryPolicy != null)
			{
				SetObject("schemaDiscoveryPolicy", schemaDiscoveryPolicy);
			}
			if (changeFeedPolicy != null)
			{
				SetObject("changeFeedPolicy", changeFeedPolicy);
			}
		}
	}
}
