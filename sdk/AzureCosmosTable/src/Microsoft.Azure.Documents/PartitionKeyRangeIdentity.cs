using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal sealed class PartitionKeyRangeIdentity : IEquatable<PartitionKeyRangeIdentity>
	{
		public string CollectionRid
		{
			get;
			private set;
		}

		public string PartitionKeyRangeId
		{
			get;
			private set;
		}

		public PartitionKeyRangeIdentity(string collectionRid, string partitionKeyRangeId)
		{
			if (collectionRid == null)
			{
				throw new ArgumentNullException("collectionRid");
			}
			if (partitionKeyRangeId == null)
			{
				throw new ArgumentNullException("partitionKeyRangeId");
			}
			CollectionRid = collectionRid;
			PartitionKeyRangeId = partitionKeyRangeId;
		}

		/// <summary>
		/// This should only be used for user provided partitionKeyRangeId, because in this case
		/// he knows what he is doing. If collection was deleted/created with same name - it is his responsibility.
		///
		/// If our code infers partitionKeyRangeId automatically and uses collection information from collection cache,
		/// we need to ensure that request will reach correct collection. In this case constructor which takes collectionRid MUST
		/// be used.
		/// </summary>
		public PartitionKeyRangeIdentity(string partitionKeyRangeId)
		{
			if (partitionKeyRangeId == null)
			{
				throw new ArgumentNullException("partitionKeyRangeId");
			}
			PartitionKeyRangeId = partitionKeyRangeId;
		}

		public static PartitionKeyRangeIdentity FromHeader(string header)
		{
			string[] array = header.Split(new char[1]
			{
				','
			});
			if (array.Length == 2)
			{
				return new PartitionKeyRangeIdentity(array[0], array[1]);
			}
			if (array.Length == 1)
			{
				return new PartitionKeyRangeIdentity(array[0]);
			}
			throw new BadRequestException(RMResources.InvalidPartitionKeyRangeIdHeader);
		}

		public string ToHeader()
		{
			if (CollectionRid != null)
			{
				return string.Format(CultureInfo.InvariantCulture, "{0},{1}", CollectionRid, PartitionKeyRangeId);
			}
			return string.Format(CultureInfo.InvariantCulture, "{0}", PartitionKeyRangeId);
		}

		public bool Equals(PartitionKeyRangeIdentity other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (StringComparer.Ordinal.Equals(CollectionRid, other.CollectionRid))
			{
				return StringComparer.Ordinal.Equals(PartitionKeyRangeId, other.PartitionKeyRangeId);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj is PartitionKeyRangeIdentity)
			{
				return Equals((PartitionKeyRangeIdentity)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((CollectionRid != null) ? CollectionRid.GetHashCode() : 0) * 397) ^ ((PartitionKeyRangeId != null) ? PartitionKeyRangeId.GetHashCode() : 0);
		}
	}
}
