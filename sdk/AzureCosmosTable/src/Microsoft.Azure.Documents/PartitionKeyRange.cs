using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a partition key range in the Azure Cosmos DB service.
	/// </summary>
	public sealed class PartitionKeyRange : Resource, IEquatable<PartitionKeyRange>
	{
		internal const string MasterPartitionKeyRangeId = "M";

		/// <summary>
		/// Represents the minimum possible value of a PartitionKeyRange in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "minInclusive")]
		internal string MinInclusive
		{
			get
			{
				return GetValue<string>("minInclusive");
			}
			set
			{
				SetValue("minInclusive", value);
			}
		}

		/// <summary>
		/// Represents maximum exclusive value of a PartitionKeyRange (the upper, but not including this value, boundary of PartitionKeyRange)
		/// in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "maxExclusive")]
		internal string MaxExclusive
		{
			get
			{
				return GetValue<string>("maxExclusive");
			}
			set
			{
				SetValue("maxExclusive", value);
			}
		}

		[JsonProperty(PropertyName = "ridPrefix")]
		internal int? RidPrefix
		{
			get
			{
				return GetValue<int?>("ridPrefix");
			}
			set
			{
				SetValue("ridPrefix", value);
			}
		}

		[JsonProperty(PropertyName = "throughputFraction")]
		internal double ThroughputFraction
		{
			get
			{
				return GetValue<double>("throughputFraction");
			}
			set
			{
				SetValue("throughputFraction", value);
			}
		}

		[JsonProperty(PropertyName = "status")]
		internal PartitionKeyRangeStatus Status
		{
			get
			{
				return GetValue<PartitionKeyRangeStatus>("status");
			}
			set
			{
				SetValue("status", value);
			}
		}

		/// <summary>
		/// Contains ids or parent ranges in the Azure Cosmos DB service.
		/// For example if range with id '1' splits into '2' and '3',
		/// then Parents for ranges '2' and '3' will be ['1'].
		/// If range '3' splits into '4' and '5', then parents for ranges '4' and '5'
		/// will be ['1', '3'].
		/// </summary>
		[JsonProperty(PropertyName = "parents")]
		public Collection<string> Parents
		{
			get
			{
				return GetValue<Collection<string>>("parents");
			}
			set
			{
				SetValue("parents", value);
			}
		}

		internal Range<string> ToRange()
		{
			return new Range<string>(MinInclusive, MaxExclusive, isMinInclusive: true, isMaxInclusive: false);
		}

		/// <summary>
		/// Determines whether this instance in the Azure Cosmos DB service and a specified object have the same value.
		/// </summary>
		/// <param name="obj">The object to compare to this instance</param>
		public override bool Equals(object obj)
		{
			return Equals(obj as PartitionKeyRange);
		}

		/// <summary>
		/// Returns the hash code for this instance in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer hash code.
		/// </returns>
		public override int GetHashCode()
		{
			return (((((0 * 397) ^ Id.GetHashCode()) * 397) ^ MinInclusive.GetHashCode()) * 397) ^ MaxExclusive.GetHashCode();
		}

		/// <summary>
		/// Determines whether this instance in the Azure Cosmos DB service and a specified PartitionKeyRange object have the same value.
		/// </summary>
		/// <param name="other">The PartitionKeyRange object to compare to this instance</param>
		public bool Equals(PartitionKeyRange other)
		{
			if (other == null)
			{
				return false;
			}
			if (Id == other.Id && MinInclusive.Equals(other.MinInclusive) && MaxExclusive.Equals(other.MaxExclusive))
			{
				return ThroughputFraction == other.ThroughputFraction;
			}
			return false;
		}
	}
}
