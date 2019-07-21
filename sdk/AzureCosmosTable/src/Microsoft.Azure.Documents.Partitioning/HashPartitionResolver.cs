using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Azure.Documents.Partitioning
{
	/// <summary>
	/// HashPartitionResolver implements partitioning based on the value of a hash function, allowing you to evenly 
	/// distribute requests and data across a number of partitions in the Azure Cosmos DB service. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Support for IPartitionResolver based classes is now obsolete. It's recommended that you use 
	/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
	/// </para>
	/// <para>
	/// The HashPartitionResolver class internally implements a consistent hash ring over the hash function specified in the 
	/// <see cref="T:Microsoft.Azure.Documents.Partitioning.IHashGenerator" /> interface. By default, the HashPartitionResolver provides an MD5 hash function, but this can be 
	/// swapped out with a different hashing implementation. The consistent hash ring creates 16 replicas for each collection in order 
	/// to achieve a more uniform distribution of documents across collections.
	/// </para>
	/// <para>
	/// The hash partitioning is most suitable for partitioning when the partition key has a high cardinality because it will distribute 
	/// the data evenly across collections. Typically hash partitioning uses the id property. A common use cases for hash partitioning is data 
	/// produced or consumed from a large number of distinct clients or for storing user profiles, catalog items, and telemetry data.
	/// </para>
	/// </remarks>
	[Obsolete("Support for IPartitionResolver based classes is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
	public class HashPartitionResolver : IPartitionResolver, IDisposable
	{
		private class Md5HashGenerator : IHashGenerator, IDisposable
		{
			private HashAlgorithm hashAlgorithm;

			[SuppressMessage("Microsoft.Security.Cryptography", "CA5350:SHA-2 (SHA256, SHA384, SHA512) must be used", Justification = "Secutiry is not a concern here since we need a good hash function only.")]
			public Md5HashGenerator()
			{
				hashAlgorithm = MD5.Create();
			}

			public byte[] ComputeHash(byte[] key)
			{
				lock (this)
				{
					return hashAlgorithm.ComputeHash(key);
				}
			}

			public void Dispose()
			{
				hashAlgorithm.Dispose();
			}
		}

		private ConsistentHashRing consistentHashRing;

		private const int defaultNumberOfVirtualNodesPerCollection = 128;

		private bool isDisposed;

		/// <summary>
		/// Gets the HashGenerator used in consistent hashing.
		/// </summary>
		[JsonIgnore]
		public IHashGenerator HashGenerator
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the property in the document to execute the hashing on in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The name of the property in the document to execute the hashing on.</value>
		/// <remarks>
		/// HashPartitionResolver supports two modes - one using PartitionKeyPropertyName and the other using PartitionKeyExtractor.
		/// PartitionKeyPropertyName is extracted using Reflection, so use the C# property name, not the JSON representation.
		/// </remarks>
		public string PartitionKeyPropertyName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the IEnumerable of collection links used for hashing in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The IEnumerable of collection links used for hashing.</value>
		public IEnumerable<string> CollectionLinks
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the function to extract the partition key from an object in the Azure Cosmos DB service.
		/// </summary>
		/// <value>The function to extract the partition key from an object.</value>
		public Func<object, string> PartitionKeyExtractor
		{
			get;
			private set;
		}

		/// <summary>
		/// The number of virtual nodes per collection in the conisistent hash ring in the Azure Cosmos DB service. This controls the compromise of skewness of documents accross collections vs the consistent hashing latency.
		/// </summary>
		public int NumberOfVirtualNodesPerCollection
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver" /> in the Azure Cosmos DB service using the specified <paramref name="partitionKeyPropertyName" /> value.
		/// </summary>
		/// <param name="partitionKeyPropertyName">The name of the property in the document to execute the hashing on.</param>
		/// <param name="collectionLinks">The list of collection links used for hashing.</param>
		/// <param name="numberOfVirtualNodesPerCollection">The number of virtual nodes per collection in the conisistent hash ring.</param>
		/// <param name="hashGenerator">The <see cref="T:Microsoft.Azure.Documents.Partitioning.IHashGenerator" /> to use in consistent hashing. If null, the default MD5 hash generator is used.</param>
		/// <remarks>
		/// Use when you want to partition based on a single property name. For other partitioning schemes, use the constructor 
		/// with partitionKeyExtractor instead.
		/// </remarks>
		public HashPartitionResolver(string partitionKeyPropertyName, IEnumerable<string> collectionLinks, int numberOfVirtualNodesPerCollection = 128, IHashGenerator hashGenerator = null)
		{
			if (string.IsNullOrEmpty(partitionKeyPropertyName))
			{
				throw new ArgumentException("partitionKeyPropertyName");
			}
			if (numberOfVirtualNodesPerCollection <= 0)
			{
				throw new ArgumentException("numberOfVirtualNodesPerCollection");
			}
			PartitionKeyPropertyName = partitionKeyPropertyName;
			Initialize(collectionLinks, hashGenerator, numberOfVirtualNodesPerCollection);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver" /> in the Azure Cosmos DB service using the specified <paramref name="partitionKeyExtractor" /> value.
		/// </summary>
		/// <param name="partitionKeyExtractor">A function to extract the partitionKey from the document</param>
		/// <param name="collectionLinks">The list of collection links used for hashing.</param>
		/// <param name="numberOfVirtualNodesPerCollection">The number of virtual nodes per collection in the conisistent hash ring.</param>
		/// <param name="hashGenerator">The <see cref="T:Microsoft.Azure.Documents.Partitioning.IHashGenerator" /> to use in consistent hashing. If null, the default MD5 hash generator is used.</param>
		public HashPartitionResolver(Func<object, string> partitionKeyExtractor, IEnumerable<string> collectionLinks, int numberOfVirtualNodesPerCollection = 128, IHashGenerator hashGenerator = null)
		{
			if (partitionKeyExtractor == null)
			{
				throw new ArgumentNullException("partitionKeyExtractor");
			}
			if (numberOfVirtualNodesPerCollection <= 0)
			{
				throw new ArgumentException("numberOfVirtualNodesPerCollection");
			}
			PartitionKeyExtractor = partitionKeyExtractor;
			Initialize(collectionLinks, hashGenerator, numberOfVirtualNodesPerCollection);
		}

		private void Initialize(IEnumerable<string> collectionLinks, IHashGenerator hashGenerator, int numberOfVirtualNodesPerCollection)
		{
			if (collectionLinks == null)
			{
				throw new ArgumentNullException("collectionLinks");
			}
			CollectionLinks = collectionLinks;
			HashGenerator = (hashGenerator ?? new Md5HashGenerator());
			NumberOfVirtualNodesPerCollection = numberOfVirtualNodesPerCollection;
			consistentHashRing = new ConsistentHashRing(HashGenerator, CollectionLinks, CollectionLinks.Count() * NumberOfVirtualNodesPerCollection);
		}

		/// <summary>
		/// Extracts the partition key from the specified document using the specified <see cref="P:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver.PartitionKeyPropertyName" /> 
		/// property or <see cref="P:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver.PartitionKeyExtractor" /> function in order of preference in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="document">A document object</param>
		/// <returns>object used as the partition key.</returns>
		/// <exception cref="T:System.InvalidOperationException">Thrown if unable to extract the partition key.</exception>
		public virtual object GetPartitionKey(object document)
		{
			if (PartitionKeyPropertyName != null)
			{
				return PartitionResolverUtils.ExtractPartitionKeyFromDocument(document, PartitionKeyPropertyName);
			}
			if (PartitionKeyExtractor != null)
			{
				return PartitionKeyExtractor(document);
			}
			throw new InvalidOperationException(ClientResources.PartitionKeyExtractError);
		}

		/// <summary>
		/// Given a partition key, returns the collection self-link for creating a document in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collection for creates.</param>
		/// <returns>The target collection link that will be used for document creation.</returns>
		/// <exception cref="T:System.ArgumentNullException">Thrown if the specified <paramref name="partitionKey" /> is null.</exception>
		public virtual string ResolveForCreate(object partitionKey)
		{
			if (partitionKey == null)
			{
				throw new ArgumentNullException("partitionKey");
			}
			string key = ProcessPartitionKey(partitionKey);
			return consistentHashRing.GetNode(key);
		}

		/// <summary>
		/// Given a partition key, returns a list of collection links to read from using its hash in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collections for reading. Must be a string.</param>
		/// <returns>The list of target collection links.</returns>
		/// <remarks>
		/// If partitionKey is null, then all collections are returned. The HashPartitionResolver supports only strings as partitionKeys.
		/// For other types, use ToString() or JsonConvert.SerializeObject() to convert to string.
		/// </remarks>
		/// <exception cref="T:System.InvalidOperationException">Thrown if the partition key is not a string.</exception>
		public virtual IEnumerable<string> ResolveForRead(object partitionKey)
		{
			if (partitionKey == null)
			{
				return CollectionLinks;
			}
			string key = ProcessPartitionKey(partitionKey);
			return new List<string>
			{
				consistentHashRing.GetNode(key)
			};
		}

		/// <summary>
		/// Disposes the resolver in the Azure Cosmos DB service.
		/// </summary>
		public void Dispose()
		{
			if (!isDisposed)
			{
				(consistentHashRing.GetHashGenerator() as Md5HashGenerator)?.Dispose();
				isDisposed = true;
			}
		}

		internal string ProcessPartitionKey(object partitionKey)
		{
			if (partitionKey == null)
			{
				return null;
			}
			if (partitionKey is string)
			{
				return (string)partitionKey;
			}
			throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ClientResources.UnsupportedPartitionKey, partitionKey.GetType()));
		}
	}
}
