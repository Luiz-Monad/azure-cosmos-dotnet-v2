using Microsoft.Azure.Documents.Routing;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
	/// </summary>
	public sealed class PartitionKey
	{
		/// <summary>
		/// The tag name to use in the documents for specifying a partition key value
		/// when inserting such documents into a migrated collection
		/// </summary>
		public const string SystemKeyName = "_partitionKey";

		/// <summary>
		/// The partition key path in the collection definition for migrated collections
		/// </summary>
		public const string SystemKeyPath = "/_partitionKey";

		/// <summary>
		/// Instantiates a new instance of the <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> object.
		/// </summary>
		/// <remarks>
		/// The returned object represents a partition key value that allows creating and accessing documents
		/// without a value for partition key
		/// </remarks>
		public static PartitionKey None => new PartitionKey
		{
			InternalKey = PartitionKeyInternal.None
		};

		/// <summary>
		/// Gets the internal <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" /> object;
		/// </summary>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		internal PartitionKeyInternal InternalKey
		{
			get;
			private set;
		}

		/// <summary>
		/// Instantiate a new instance of the <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> object.
		/// </summary>
		/// <remarks>
		/// Private constructor used internal to create an instance from a JSON string.
		/// </remarks>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		private PartitionKey()
		{
		}

		/// <summary>
		/// Instantiate a new instance of the <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> object.
		/// </summary>
		/// <param name="keyValue">
		/// The value of the document property that is specified as the partition key when a collection is created.
		/// </param>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		public PartitionKey(object keyValue)
		{
			InternalKey = PartitionKeyInternal.FromObjectArray(new object[1]
			{
				keyValue
			}, strict: true);
		}

		/// <summary>
		/// Instantiate a new instance of the <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> object.
		/// </summary>
		/// <param name="keyValue">
		/// The value of the document property that is specified as the partition key
		/// when a collection is created, in serialized JSON form.
		/// </param>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		public static PartitionKey FromJsonString(string keyValue)
		{
			if (string.IsNullOrEmpty(keyValue))
			{
				throw new ArgumentException("keyValue must not be null or empty.");
			}
			return new PartitionKey
			{
				InternalKey = PartitionKeyInternal.FromJsonString(keyValue)
			};
		}

		/// <summary>
		/// Instantiate a new instance of the <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> object.
		/// </summary>
		/// <param name="keyValue">
		/// The value of the document property that is specified as the partition key
		/// when a collection is created, in PartitionKeyInternal format.
		/// </param>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		internal static PartitionKey FromInternalKey(PartitionKeyInternal keyValue)
		{
			if (keyValue == null)
			{
				throw new ArgumentException("keyValue must not be null or empty.");
			}
			return new PartitionKey
			{
				InternalKey = keyValue
			};
		}

		/// <summary>
		/// Override the base ToString method to output the value of each key component, separated by a space.
		/// </summary>
		/// <returns>The string representation of all the key component values.</returns>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		public override string ToString()
		{
			return InternalKey.ToJsonString();
		}

		/// <summary>
		/// Overrides the Equal operator for object comparisons between two instances of <see cref="T:Microsoft.Azure.Documents.PartitionKey" />.
		/// </summary>
		/// <param name="other">The object to compare with.</param>
		/// <returns>True if two object instance are considered equal.</returns>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		public override bool Equals(object other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			PartitionKey partitionKey = other as PartitionKey;
			if (partitionKey != null)
			{
				return InternalKey.Equals(partitionKey.InternalKey);
			}
			return false;
		}

		/// <summary>
		/// Hash function to return the hash code for the object.
		/// </summary>
		/// <returns>The hash code for this <see cref="T:Microsoft.Azure.Documents.PartitionKey" /> instance</returns>
		/// <remarks>
		/// This class represents a partition key value that identifies the target partition of a collection in the Azure Cosmos DB service.
		/// </remarks>
		public override int GetHashCode()
		{
			if (InternalKey == null)
			{
				return base.GetHashCode();
			}
			return InternalKey.GetHashCode();
		}
	}
}
