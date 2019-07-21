using System;

namespace Microsoft.Azure.Documents.Partitioning
{
	/// <summary>
	/// An interface used by the <see cref="T:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver" /> to partition data using consistent hashing in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Support for interfaces used with IPartitionResolver is now obsolete. It's recommended that you use 
	/// <a href="https://azure.microsoft.com/documentation/articles/documentdb-partition-data">Partitioned Collections</a> for higher storage and throughput.
	/// </para>
	/// </remarks>
	/// <seealso cref="T:Microsoft.Azure.Documents.Partitioning.HashPartitionResolver" />
	[Obsolete("Support for interfaces used with IPartitionResolver is now obsolete. It's recommended that you use partitioned collections for higher storage and throughput.")]
	public interface IHashGenerator
	{
		/// <summary>
		/// Hashes an array of bytes into a new array of bytes that represents the output hash in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="key">A key represented by an array of bytes</param>
		/// <returns>An array of bytes that represents the output hash.</returns>
		byte[] ComputeHash(byte[] key);
	}
}
