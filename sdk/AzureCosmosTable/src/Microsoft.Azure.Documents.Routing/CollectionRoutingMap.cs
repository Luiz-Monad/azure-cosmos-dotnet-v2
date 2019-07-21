using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// Stored partition key ranges in an efficient way with some additional information and provides
	/// convenience methods for working with set of ranges.
	/// </summary>
	internal sealed class CollectionRoutingMap
	{
		private class MinPartitionKeyTupleComparer : IComparer<Tuple<PartitionKeyRange, ServiceIdentity>>
		{
			public int Compare(Tuple<PartitionKeyRange, ServiceIdentity> left, Tuple<PartitionKeyRange, ServiceIdentity> right)
			{
				return string.CompareOrdinal(left.Item1.MinInclusive, right.Item1.MinInclusive);
			}
		}

		/// <summary>
		/// Partition key range id to partition address and range.
		/// </summary>
		private readonly Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>> rangeById;

		private readonly List<PartitionKeyRange> orderedPartitionKeyRanges;

		private readonly List<Range<string>> orderedRanges;

		private readonly HashSet<string> goneRanges;

		public string CollectionUniqueId
		{
			get;
			private set;
		}

		public string ChangeFeedNextIfNoneMatch
		{
			get;
			private set;
		}

		/// <summary>
		/// Ranges in increasing order.
		/// </summary>
		public IReadOnlyList<PartitionKeyRange> OrderedPartitionKeyRanges => orderedPartitionKeyRanges;

		public CollectionRoutingMap(CollectionRoutingMap collectionRoutingMap, string changeFeedNextIfNoneMatch)
		{
			rangeById = new Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>>(collectionRoutingMap.rangeById);
			orderedPartitionKeyRanges = new List<PartitionKeyRange>(collectionRoutingMap.orderedPartitionKeyRanges);
			orderedRanges = new List<Range<string>>(collectionRoutingMap.orderedRanges);
			goneRanges = new HashSet<string>(collectionRoutingMap.goneRanges);
			CollectionUniqueId = collectionRoutingMap.CollectionUniqueId;
			ChangeFeedNextIfNoneMatch = changeFeedNextIfNoneMatch;
		}

		private CollectionRoutingMap(Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>> rangeById, List<PartitionKeyRange> orderedPartitionKeyRanges, string collectionUniqueId, string changeFeedNextIfNoneMatch)
		{
			this.rangeById = rangeById;
			this.orderedPartitionKeyRanges = orderedPartitionKeyRanges;
			orderedRanges = (from range in orderedPartitionKeyRanges
			select new Range<string>(range.MinInclusive, range.MaxExclusive, isMinInclusive: true, isMaxInclusive: false)).ToList();
			CollectionUniqueId = collectionUniqueId;
			ChangeFeedNextIfNoneMatch = changeFeedNextIfNoneMatch;
			goneRanges = new HashSet<string>(orderedPartitionKeyRanges.SelectMany(delegate(PartitionKeyRange r)
			{
				IEnumerable<string> parents = r.Parents;
				return parents ?? Enumerable.Empty<string>();
			}));
		}

		public static CollectionRoutingMap TryCreateCompleteRoutingMap(IEnumerable<Tuple<PartitionKeyRange, ServiceIdentity>> ranges, string collectionUniqueId, string changeFeedNextIfNoneMatch = null)
		{
			Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>> dictionary = new Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>>(StringComparer.Ordinal);
			foreach (Tuple<PartitionKeyRange, ServiceIdentity> range in ranges)
			{
				dictionary[range.Item1.Id] = range;
			}
			List<Tuple<PartitionKeyRange, ServiceIdentity>> list = dictionary.Values.ToList();
			list.Sort(new MinPartitionKeyTupleComparer());
			List<PartitionKeyRange> list2 = (from range in list
			select range.Item1).ToList();
			if (!IsCompleteSetOfRanges(list2))
			{
				return null;
			}
			return new CollectionRoutingMap(dictionary, list2, collectionUniqueId, changeFeedNextIfNoneMatch);
		}

		public IReadOnlyList<PartitionKeyRange> GetOverlappingRanges(Range<string> range)
		{
			return GetOverlappingRanges(new Range<string>[1]
			{
				range
			});
		}

		public IReadOnlyList<PartitionKeyRange> GetOverlappingRanges(IReadOnlyList<Range<string>> providedPartitionKeyRanges)
		{
			if (providedPartitionKeyRanges == null)
			{
				throw new ArgumentNullException("providedPartitionKeyRanges");
			}
			SortedList<string, PartitionKeyRange> sortedList = new SortedList<string, PartitionKeyRange>();
			foreach (Range<string> providedPartitionKeyRange in providedPartitionKeyRanges)
			{
				int num = orderedRanges.BinarySearch(providedPartitionKeyRange, Range<string>.MinComparer.Instance);
				if (num < 0)
				{
					num = Math.Max(0, ~num - 1);
				}
				int num2 = orderedRanges.BinarySearch(providedPartitionKeyRange, Range<string>.MaxComparer.Instance);
				if (num2 < 0)
				{
					num2 = Math.Min(OrderedPartitionKeyRanges.Count - 1, ~num2);
				}
				for (int i = num; i <= num2; i++)
				{
					if (Range<string>.CheckOverlapping(orderedRanges[i], providedPartitionKeyRange))
					{
						sortedList[OrderedPartitionKeyRanges[i].MinInclusive] = OrderedPartitionKeyRanges[i];
					}
				}
			}
			return new ReadOnlyCollection<PartitionKeyRange>(sortedList.Values);
		}

		public PartitionKeyRange GetRangeByEffectivePartitionKey(string effectivePartitionKeyValue)
		{
			if (string.CompareOrdinal(effectivePartitionKeyValue, PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey) >= 0)
			{
				throw new ArgumentException("effectivePartitionKeyValue");
			}
			if (string.CompareOrdinal(PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey, effectivePartitionKeyValue) == 0)
			{
				return orderedPartitionKeyRanges[0];
			}
			int num = orderedRanges.BinarySearch(new Range<string>(effectivePartitionKeyValue, effectivePartitionKeyValue, isMinInclusive: true, isMaxInclusive: true), Range<string>.MinComparer.Instance);
			if (num < 0)
			{
				num = ~num - 1;
			}
			return orderedPartitionKeyRanges[num];
		}

		public PartitionKeyRange TryGetRangeByPartitionKeyRangeId(string partitionKeyRangeId)
		{
			if (rangeById.TryGetValue(partitionKeyRangeId, out Tuple<PartitionKeyRange, ServiceIdentity> value))
			{
				return value.Item1;
			}
			return null;
		}

		public ServiceIdentity TryGetInfoByPartitionKeyRangeId(string partitionKeyRangeId)
		{
			if (rangeById.TryGetValue(partitionKeyRangeId, out Tuple<PartitionKeyRange, ServiceIdentity> value))
			{
				return value.Item2;
			}
			return null;
		}

		public CollectionRoutingMap TryCombine(IEnumerable<Tuple<PartitionKeyRange, ServiceIdentity>> ranges, string changeFeedNextIfNoneMatch)
		{
			HashSet<string> newGoneRanges = new HashSet<string>(ranges.SelectMany(delegate(Tuple<PartitionKeyRange, ServiceIdentity> tuple)
			{
				IEnumerable<string> parents = tuple.Item1.Parents;
				return parents ?? Enumerable.Empty<string>();
			}));
			newGoneRanges.UnionWith(goneRanges);
			Dictionary<string, Tuple<PartitionKeyRange, ServiceIdentity>> dictionary = (from tuple in rangeById.Values
			where !newGoneRanges.Contains(tuple.Item1.Id)
			select tuple).ToDictionary((Tuple<PartitionKeyRange, ServiceIdentity> tuple) => tuple.Item1.Id, StringComparer.Ordinal);
			foreach (Tuple<PartitionKeyRange, ServiceIdentity> item in from tuple in ranges
			where !newGoneRanges.Contains(tuple.Item1.Id)
			select tuple)
			{
				dictionary[item.Item1.Id] = item;
				DefaultTrace.TraceInformation("CollectionRoutingMap.TryCombine newRangeById[{0}] = {1}", item.Item1.Id, item);
			}
			List<Tuple<PartitionKeyRange, ServiceIdentity>> list = dictionary.Values.ToList();
			list.Sort(new MinPartitionKeyTupleComparer());
			List<PartitionKeyRange> list2 = (from range in list
			select range.Item1).ToList();
			if (!IsCompleteSetOfRanges(list2))
			{
				return null;
			}
			return new CollectionRoutingMap(dictionary, list2, CollectionUniqueId, changeFeedNextIfNoneMatch);
		}

		private static bool IsCompleteSetOfRanges(IList<PartitionKeyRange> orderedRanges)
		{
			bool flag = false;
			if (orderedRanges.Count > 0)
			{
				PartitionKeyRange partitionKeyRange = orderedRanges[0];
				PartitionKeyRange partitionKeyRange2 = orderedRanges[orderedRanges.Count - 1];
				flag = (string.CompareOrdinal(partitionKeyRange.MinInclusive, PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey) == 0);
				flag &= (string.CompareOrdinal(partitionKeyRange2.MaxExclusive, PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey) == 0);
				for (int i = 1; i < orderedRanges.Count; i++)
				{
					PartitionKeyRange partitionKeyRange3 = orderedRanges[i - 1];
					PartitionKeyRange partitionKeyRange4 = orderedRanges[i];
					flag &= partitionKeyRange3.MaxExclusive.Equals(partitionKeyRange4.MinInclusive);
					if (!flag)
					{
						if (string.CompareOrdinal(partitionKeyRange3.MaxExclusive, partitionKeyRange4.MinInclusive) <= 0)
						{
							break;
						}
						throw new InvalidOperationException("Ranges overlap");
					}
				}
			}
			return flag;
		}

		public bool IsGone(string partitionKeyRangeId)
		{
			return goneRanges.Contains(partitionKeyRangeId);
		}
	}
}
