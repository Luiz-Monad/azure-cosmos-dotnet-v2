using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents.Partitioning
{
	[Obsolete("Support for classes used with IPartitionResolver is now obsolete.")]
	internal sealed class ConsistentHashRing
	{
		private class Partition : IComparable<Partition>
		{
			public byte[] HashValue
			{
				get;
				private set;
			}

			public string Node
			{
				get;
				private set;
			}

			public Partition(byte[] hashValue, string node)
			{
				HashValue = hashValue;
				Node = node;
			}

			[SuppressMessage("Microsoft.Usage", "#pw26506")]
			public int CompareTo(Partition other)
			{
				return CompareTo(other.HashValue);
			}

			public int CompareTo(byte[] otherHashValue)
			{
				return CompareHashValues(HashValue, otherHashValue);
			}

			public static int CompareHashValues(byte[] hash1, byte[] hash2)
			{
				if (hash1.Length != hash2.Length)
				{
					throw new ArgumentException("Length does not match", "hash2");
				}
				for (int i = 0; i < hash1.Length; i++)
				{
					if (hash1[i] < hash2[i])
					{
						return -1;
					}
					if (hash1[i] > hash2[i])
					{
						return 1;
					}
				}
				return 0;
			}

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder(32);
				for (int i = 0; i < HashValue.Length; i++)
				{
					stringBuilder.AppendFormat("{0:x2}", HashValue[i]);
				}
				return stringBuilder.ToString();
			}
		}

		private IHashGenerator hashGenerator;

		private Partition[] partitions;

		private int totalPartitions;

		/// <summary>  
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.ConsistentHashRing" /> class.
		/// </summary>  
		/// <param name="hashGenerator">A hash generation algorithm specified as an <see cref="T:Microsoft.Azure.Documents.Partitioning.IHashGenerator" /> implementation. </param>
		/// <param name="nodes">Collection of nodes. The node type (T) must have a stable implementation of <see cref="M:System.Object.GetHashCode" /></param>
		/// <param name="totalPartitions">Total number of desired partitions; must be greater than the number of nodes</param>
		public ConsistentHashRing(IHashGenerator hashGenerator, IEnumerable<string> nodes, int totalPartitions)
		{
			if (hashGenerator == null)
			{
				throw new ArgumentNullException("hash");
			}
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}
			int num = nodes.Count();
			if (totalPartitions < num)
			{
				throw new ArgumentException("The total number of partitions must be at least the number of nodes");
			}
			this.hashGenerator = hashGenerator;
			this.totalPartitions = totalPartitions;
			partitions = ConstructPartitions(this.hashGenerator, nodes, this.totalPartitions);
		}

		public string GetNode(string key)
		{
			int partition = ComputePartition(key);
			return GetNode(partition, 0);
		}

		public IHashGenerator GetHashGenerator()
		{
			return hashGenerator;
		}

		/// <summary>  
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Partitioning.ConsistentHashRing" /> class.
		/// </summary>  
		/// <param name="hashGenerator">A hash generation algorithm specified as an <see cref="T:Microsoft.Azure.Documents.Partitioning.IHashGenerator" /> implementation. </param>
		/// <param name="nodes">The nodes to use for the ring.</param>
		/// <param name="totalPartitions">Total number of partitions; must be &gt;= number of nodes</param>  
		/// <returns>Sorted array of Partition objects</returns>  
		private static Partition[] ConstructPartitions(IHashGenerator hashGenerator, IEnumerable<string> nodes, int totalPartitions)
		{
			int num = nodes.Count();
			Partition[] array = new Partition[totalPartitions];
			int num2 = totalPartitions / num;
			int num3 = totalPartitions - num2 * num;
			int num4 = 0;
			foreach (string node in nodes)
			{
				byte[] array2 = hashGenerator.ComputeHash(BitConverter.GetBytes(node.GetHashCode()));
				for (int i = 0; i < num2 + ((num3 > 0) ? 1 : 0); i++)
				{
					array[num4++] = new Partition(array2, node);
					array2 = hashGenerator.ComputeHash(array2);
				}
				num3--;
			}
			Array.Sort(array);
			return array;
		}

		/// <summary>  
		/// Consistently hash the string key, and return the primary partition  
		/// </summary>  
		/// <param name="key">Key</param>  
		/// <returns>Primary partition index</returns>  
		private int ComputePartition(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return ComputePartition(Encoding.UTF8.GetBytes(key));
		}

		private int ComputePartition(byte[] key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			byte[] hashValue = hashGenerator.ComputeHash(key);
			int num = LowerBound(partitions, hashValue);
			if (num == partitions.Length)
			{
				num = 0;
			}
			return num;
		}

		/// <summary>  
		/// Given a partition index, return the node hosting the Nth replica  
		/// </summary>  
		/// <param name="partition">Partition index</param>  
		/// <param name="replica">Desired replica (primary is 0)</param>  
		/// <returns>Node</returns>  
		private string GetNode(int partition, int replica)
		{
			int num = SkipRelicas(partitions, partition, replica);
			return partitions[num].Node;
		}

		/// <summary>  
		/// Return the partition with the lowest hash value that is equal  
		/// or greater to the hashValue parameter (lower bound)  
		/// </summary>  
		/// <param name="partitions">Array of partitions; must be sorted by their hash value</param>  
		/// <param name="hashValue">Hash value to lookup</param>  
		/// <returns>Index into the partition array, or partitions.Length if value is greater than last partition</returns>  
		private static int LowerBound(Partition[] partitions, byte[] hashValue)
		{
			int num = 0;
			int num2 = partitions.Length - num;
			while (num2 > 0)
			{
				int num3 = num2 / 2;
				int num4 = num + num3;
				if (partitions[num4].CompareTo(hashValue) < 0)
				{
					num = ++num4;
					num2 -= num3 + 1;
				}
				else
				{
					num2 = num3;
				}
			}
			return num;
		}

		/// <summary>  
		/// Given a particular partition, skip "around" the circle until we find  
		/// the Nth partition, only counting unique nodes.  
		/// </summary>  
		/// <param name="partitions">Array of partitions</param>  
		/// <param name="partition">Starting index into the partition array</param>  
		/// <param name="replica">Nth partition requested (primary == 0)</param>  
		/// <returns>Index into the partition array</returns>  
		private static int SkipRelicas(Partition[] partitions, int partition, int replica)
		{
			string[] array = new string[replica];
			int num = partition;
			while (replica > 0)
			{
				array[array.Length - replica] = partitions[num].Node;
				do
				{
					num = (num + 1) % partitions.Length;
					if (num == partition)
					{
						throw new InvalidOperationException("Not enough nodes for the requested replica");
					}
				}
				while (array.Take(array.Length - replica + 1).Contains(partitions[num].Node));
				replica--;
			}
			return num;
		}
	}
}
