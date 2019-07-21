using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents.Partitioning
{
	/// <summary>
	/// RangePartitionResolver implements partitioning in Azure Cosmos DB service by using a partition map of ranges of values to a collection self-link.
	/// This works well when the data is naturally ordered and commonly queried upon using ranges of values, e.g., for 
	/// time series data or alphabetical ranges of strings.
	/// </summary>
	/// <typeparam name="T">The type of value to use for range partitioning.</typeparam>
	/// <remarks>
	/// <para>
	/// Support for IPartitionResolver based classes is now obsolete. It's recommended that you use 
	/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
	/// </para>
	/// <para>
	/// In range partitioning, partitions are assigned based on whether the partition key is within a certain range. The 
	/// RangePartitionResolver class helps you maintain a mapping between a <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> and collection self-link.
	/// </para>
	/// <para>
	/// <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> is a simple class for specifying ranges of any types that implement <see cref="T:System.IComparable`1" /> and <see cref="T:System.IEquatable`1" /> 
	/// like strings or numbers. For reads and creates, you can pass in any arbitrary range, and the resolver identifies all the candidate collections by 
	/// identifying the ranges of the partitions that intersect twith the requested range.
	/// </para>
	/// <para>
	/// A special case of range partitioning is when the range is just a single discrete value, sometimes called Lookup Partitioning. This is commonly used 
	/// for partitioning by discrete values like Region or Type or for partitioning tenants in a multi-tenant application.
	/// </para>
	/// </remarks>
	[Obsolete("Support for IPartitionResolver based classes is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
	public class RangePartitionResolver<T> : IPartitionResolver where T : IComparable<T>, IEquatable<T>
	{
		/// <summary>
		/// The name of the property in the document to execute the hashing on in the Azure Cosmos DB service.
		/// </summary>
		public string PartitionKeyPropertyName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the map from range to collection-link that is used for partitioning requests in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The map from range to collection-link that is used for partitioning requests.
		/// </value>
		public IDictionary<Range<T>, string> PartitionMap
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the function to extract the partition key from any object in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The function to extract the partition key from any object.
		/// </value>
		public Func<object, object> PartitionKeyExtractor
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.RangePartitionResolver`1" /> class in the Azure Cosmos DB service using the specified <paramref name="partitionKeyPropertyName" /> value.
		/// </summary>
		/// <param name="partitionKeyPropertyName">The name of the property in the document to execute the hashing on.</param>
		/// <param name="partitionMap">A map from range to collection-link that is used for partitioning requests.</param>
		/// <remarks>
		/// Use when you want to partition based on a single property name. For other partitioning schemes, use the constructor 
		/// with partitionKeyExtractor instead.
		/// </remarks>
		/// <exception cref="T:System.ArgumentNullException">Thrown if one of the parameters is null.</exception>
		public RangePartitionResolver(string partitionKeyPropertyName, IDictionary<Range<T>, string> partitionMap)
		{
			if (string.IsNullOrEmpty(partitionKeyPropertyName))
			{
				throw new ArgumentNullException("partitionKeyPropertyName");
			}
			if (partitionMap == null)
			{
				throw new ArgumentNullException("partitionMap");
			}
			PartitionKeyPropertyName = partitionKeyPropertyName;
			PartitionMap = partitionMap;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver" /> in the Azure Cosmos DB service using the specified <paramref name="partitionKeyExtractor" /> value.
		/// </summary>
		/// <param name="partitionKeyExtractor">The name of the property in the document to execute the hashing on.</param>
		/// <param name="partitionMap">A map from range to collection-link that is used for partitioning requests.</param>
		/// <exception cref="T:System.ArgumentNullException">Thrown if one of the parameters is null.</exception>
		public RangePartitionResolver(Func<object, object> partitionKeyExtractor, IDictionary<Range<T>, string> partitionMap)
		{
			if (partitionKeyExtractor == null)
			{
				throw new ArgumentNullException("partitionKeyExtractor");
			}
			if (partitionMap == null)
			{
				throw new ArgumentNullException("partitionMap");
			}
			PartitionKeyExtractor = partitionKeyExtractor;
			PartitionMap = partitionMap;
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
		/// Given a partition key, returns the correct collection self-link for creating a document using the range partition map in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collection for create</param>
		/// <returns>The target collection link that will be used for document creation.</returns>
		/// <remarks>
		/// If multiple ranges match the specified partitionKey, then the resolver returns one of the matching ranges. If none of the
		/// ranges match, then the method throws a <see cref="T:System.InvalidOperationException" />. If partitionKey is null, then all collections
		/// are returned.
		/// </remarks>
		/// <exception cref="T:System.ArgumentNullException">Thrown if <paramref name="partitionKey" /> is null.</exception>
		/// <exception cref="T:System.InvalidOperationException">
		/// Thrown if the <paramref name="partitionKey" /> is an invalid type or if none of the ranges match the specified partition key.
		/// </exception>
		public virtual string ResolveForCreate(object partitionKey)
		{
			Range<T> range = new Range<T>((T)Convert.ChangeType(partitionKey, typeof(T), CultureInfo.InvariantCulture));
			Range<T> containingRange = null;
			if (TryGetContainingRange(range, out containingRange))
			{
				return PartitionMap[containingRange];
			}
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ClientResources.RangeNotFoundError, range.ToString()));
		}

		/// <summary>
		/// Given a partition key, returns a list of collection links to read from using the range partition map in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="partitionKey">The partition key used to determine the target collections for query</param>
		/// <returns>The list of target collection links.</returns>
		/// <remarks>
		/// The <paramref name="partitionKey" /> must be an instance of <typeparamref name="T" />, <see cref="T:Microsoft.Azure.Documents.Partitioning.Range`1" /> or an <see cref="T:System.Collections.Generic.IEnumerable`1" />."/&gt;. 
		/// This method returns all the collections corresponding to the ranges that intersect with the specified <paramref name="partitionKey" />.
		/// </remarks>
		/// <exception cref="T:System.InvalidOperationException">
		/// Thrown if the <paramref name="partitionKey" /> is an invalid type.
		/// </exception>
		public virtual IEnumerable<string> ResolveForRead(object partitionKey)
		{
			ICollection<Range<T>> collection = null;
			collection = ((partitionKey != null) ? GetIntersectingRanges(ProcessPartitionKey(partitionKey)) : PartitionMap.Keys);
			List<string> list = new List<string>();
			foreach (Range<T> item in collection)
			{
				list.Add(PartitionMap[item]);
			}
			return list;
		}

		private IEnumerable<Range<T>> ProcessPartitionKey(object partitionKey)
		{
			List<Range<T>> list = new List<Range<T>>();
			if (partitionKey == null)
			{
				return null;
			}
			if (partitionKey is Range<T>)
			{
				list.Add((Range<T>)partitionKey);
			}
			else
			{
				if (partitionKey is IEnumerable<T>)
				{
					{
						foreach (T item in (IEnumerable<T>)partitionKey)
						{
							list.Add(new Range<T>(item));
						}
						return list;
					}
				}
				if (!(partitionKey is IEnumerable<Range<T>>))
				{
					try
					{
						T point = (T)Convert.ChangeType(partitionKey, typeof(T), CultureInfo.InvariantCulture);
						list.Add(new Range<T>(point));
						return list;
					}
					catch (Exception innerException)
					{
						throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ClientResources.UnsupportedPartitionKey, partitionKey.GetType()), innerException);
					}
				}
				list.AddRange((IEnumerable<Range<T>>)partitionKey);
			}
			return list;
		}

		private List<Range<T>> GetIntersectingRanges(IEnumerable<Range<T>> inRanges)
		{
			List<Range<T>> list = new List<Range<T>>();
			ICollection<Range<T>> keys = PartitionMap.Keys;
			foreach (Range<T> inRange in inRanges)
			{
				foreach (Range<T> item in keys)
				{
					if (item.Intersect(inRange))
					{
						list.Add(item);
					}
				}
			}
			return list;
		}

		private bool TryGetContainingRange(Range<T> inRange, out Range<T> containingRange)
		{
			foreach (Range<T> key in PartitionMap.Keys)
			{
				if (key.Contains(inRange))
				{
					containingRange = key;
					return true;
				}
			}
			containingRange = null;
			return false;
		}
	}
}
