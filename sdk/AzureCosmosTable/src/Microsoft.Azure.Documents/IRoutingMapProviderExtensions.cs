using Microsoft.Azure.Documents.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal static class IRoutingMapProviderExtensions
	{
		private static string Max(string left, string right)
		{
			if (StringComparer.Ordinal.Compare(left, right) >= 0)
			{
				return left;
			}
			return right;
		}

		private static bool IsSortedAndNonOverlapping<T>(IList<Range<T>> list) where T : IComparable<T>
		{
			object obj;
			if ((object)typeof(T) != typeof(string))
			{
				IComparer<T> @default = Comparer<T>.Default;
				obj = @default;
			}
			else
			{
				obj = (IComparer<T>)StringComparer.Ordinal;
			}
			IComparer<T> comparer = (IComparer<T>)obj;
			for (int i = 1; i < list.Count; i++)
			{
				Range<T> range = list[i - 1];
				Range<T> range2 = list[i];
				int num = comparer.Compare(range.Max, range2.Min);
				if (num > 0)
				{
					return false;
				}
				if (num == 0 && range.IsMaxInclusive && range2.IsMinInclusive)
				{
					return false;
				}
			}
			return true;
		}

		public static async Task<PartitionKeyRange> TryGetRangeByEffectivePartitionKey(this IRoutingMapProvider routingMapProvider, string collectionResourceId, string effectivePartitionKey)
		{
			return (await routingMapProvider.TryGetOverlappingRangesAsync(collectionResourceId, Range<string>.GetPointRange(effectivePartitionKey)))?.Single();
		}

		public static async Task<List<PartitionKeyRange>> TryGetOverlappingRangesAsync(this IRoutingMapProvider routingMapProvider, string collectionResourceId, IList<Range<string>> sortedRanges, bool forceRefresh = false)
		{
			if (!IsSortedAndNonOverlapping(sortedRanges))
			{
				throw new ArgumentException("sortedRanges");
			}
			List<PartitionKeyRange> targetRanges = new List<PartitionKeyRange>();
			int currentProvidedRange = 0;
			while (currentProvidedRange < sortedRanges.Count)
			{
				if (sortedRanges[currentProvidedRange].IsEmpty)
				{
					currentProvidedRange++;
					continue;
				}
				Range<string> range;
				if (targetRanges.Count > 0)
				{
					string text = Max(targetRanges[targetRanges.Count - 1].MaxExclusive, sortedRanges[currentProvidedRange].Min);
					range = new Range<string>(isMinInclusive: string.CompareOrdinal(text, sortedRanges[currentProvidedRange].Min) == 0 && sortedRanges[currentProvidedRange].IsMinInclusive, min: text, max: sortedRanges[currentProvidedRange].Max, isMaxInclusive: sortedRanges[currentProvidedRange].IsMaxInclusive);
				}
				else
				{
					range = sortedRanges[currentProvidedRange];
				}
				IReadOnlyList<PartitionKeyRange> readOnlyList = await routingMapProvider.TryGetOverlappingRangesAsync(collectionResourceId, range, forceRefresh);
				if (readOnlyList == null)
				{
					return null;
				}
				targetRanges.AddRange(readOnlyList);
				for (Range<string> right = targetRanges[targetRanges.Count - 1].ToRange(); currentProvidedRange < sortedRanges.Count && Range<string>.MaxComparer.Instance.Compare(sortedRanges[currentProvidedRange], right) <= 0; currentProvidedRange++)
				{
				}
			}
			return targetRanges;
		}
	}
}
