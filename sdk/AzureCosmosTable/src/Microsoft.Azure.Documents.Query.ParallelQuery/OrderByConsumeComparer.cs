using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents.Query.ParallelQuery
{
	/// <summary>
	/// For cross partition order by queries we serve documents from the partition
	/// that has the next document in the sort order of the query.
	/// If there is a tie, then we break the tie by picking the leftmost partition.
	/// </summary>
	internal sealed class OrderByConsumeComparer : IComparer<DocumentProducerTree<OrderByQueryResult>>
	{
		/// <summary>
		/// This flag used to determine whether we should support mixed type order by.
		/// For testing purposes we might turn it on to test mixed type order by on index v2.
		/// </summary>
		[ThreadStatic]
		public static bool AllowMixedTypeOrderByTestFlag;

		/// <summary>
		/// The sort orders for the query (1 for each order by in the query).
		/// Until composite indexing is released this will just be an array of length 1.
		/// </summary>
		private readonly IReadOnlyList<SortOrder> sortOrders;

		/// <summary>
		/// Initializes a new instance of the OrderByConsumeComparer class.
		/// </summary>
		/// <param name="sortOrders">The sort orders for the query.</param>
		public OrderByConsumeComparer(SortOrder[] sortOrders)
		{
			if (sortOrders == null)
			{
				throw new ArgumentNullException("Sort Orders array can not be null for an order by comparer.");
			}
			if (sortOrders.Length == 0)
			{
				throw new ArgumentException("Sort Orders array can not be empty for an order by comparerer.");
			}
			this.sortOrders = new List<SortOrder>(sortOrders);
		}

		/// <summary>
		/// Compares two document producer trees and returns an integer with the relation of which has the document that comes first in the sort order.
		/// </summary>
		/// <param name="producer1">The first document producer tree.</param>
		/// <param name="producer2">The second document producer tree.</param>
		/// <returns>
		/// Less than zero if the document in the first document producer comes first.
		/// Zero if the documents are equivalent.
		/// Greater than zero if the document in the second document producer comes first.
		/// </returns>
		public int Compare(DocumentProducerTree<OrderByQueryResult> producer1, DocumentProducerTree<OrderByQueryResult> producer2)
		{
			if (producer1 == producer2)
			{
				return 0;
			}
			if (producer1.HasMoreResults && !producer2.HasMoreResults)
			{
				return -1;
			}
			if (!producer1.HasMoreResults && producer2.HasMoreResults)
			{
				return 1;
			}
			if (!producer1.HasMoreResults && !producer2.HasMoreResults)
			{
				return string.CompareOrdinal(producer1.PartitionKeyRange.MinInclusive, producer2.PartitionKeyRange.MinInclusive);
			}
			OrderByQueryResult current = producer1.Current;
			OrderByQueryResult current2 = producer2.Current;
			if (current == null)
			{
				throw new InvalidOperationException(string.Format("DEBUG: {0} == null", "result1"));
			}
			if (current2 == null)
			{
				throw new InvalidOperationException(string.Format("DEBUG: {0} == null", "result2"));
			}
			int num = CompareOrderByItems(current.OrderByItems, current2.OrderByItems);
			if (num != 0)
			{
				return num;
			}
			return string.CompareOrdinal(producer1.PartitionKeyRange.MinInclusive, producer2.PartitionKeyRange.MinInclusive);
		}

		/// <summary>
		/// Takes the items relevant to the sort and return an integer defining the relationship.
		/// </summary>
		/// <param name="items1">The items relevant to the sort from the first partition.</param>
		/// <param name="items2">The items relevant to the sort from the second partition.</param>
		/// <returns>The sort relationship.</returns>
		/// <example>
		/// Suppose the query was "SELECT * FROM c ORDER BY c.name asc, c.age desc",
		/// then items1 could be ["Brandon", 22] and items2 could be ["Felix", 28]
		/// Then we would first compare "Brandon" to "Felix" and say that "Brandon" comes first in an ascending lex order (we don't even have to look at age).
		/// If items1 was ["Brandon", 22] and items2 was ["Brandon", 23] then we would say have to look at the age to break the tie and in this case 23 comes first in a descending order.
		/// Some examples of composite order by: http://www.dofactory.com/sql/order-by
		/// </example>
		public int CompareOrderByItems(QueryItem[] items1, QueryItem[] items2)
		{
			if (items1 == items2)
			{
				return 0;
			}
			if (!AllowMixedTypeOrderByTestFlag)
			{
				CheckTypeMatching(items1, items2);
			}
			for (int i = 0; i < sortOrders.Count; i++)
			{
				int num = ItemComparer.Instance.Compare(items1[i].GetItem(), items2[i].GetItem());
				if (num != 0)
				{
					if (sortOrders[i] == SortOrder.Descending)
					{
						return -num;
					}
					return num;
				}
			}
			return 0;
		}

		/// <summary>
		/// With index V1 collections we have the check the types of the items since it is impossible to support mixed typed order by for V1 collections.
		/// The reason for this is, since V1 does not order types.
		/// The only constraint is that all the numbers will be sorted with respect to each other and same for the strings, but strings and numbers might get interleaved.
		/// Take the following example:
		/// Partition 1: "A", 1, "B", 2
		/// Partition 2: 42, "Z", 0x5F3759DF
		/// Step 1: Compare "A" to 42 and WLOG 42 comes first
		/// Step 2: Compare "A" to "Z" and "A" comes first
		/// Step 3: Compare "Z" to 1 and WLOG 1 comes first
		/// Whoops: We have 42, "A", 1 and 1 should come before 42.
		/// </summary>
		/// <param name="items1">The items relevant to the sort for the first partition.</param>
		/// <param name="items2">The items relevant to the sort for the second partition.</param>
		private void CheckTypeMatching(QueryItem[] items1, QueryItem[] items2)
		{
			int num = 0;
			ItemType itemType;
			while (true)
			{
				if (num < items1.Length)
				{
					itemType = ItemTypeHelper.GetItemType(items1[num].GetItem());
					ItemType itemType2 = ItemTypeHelper.GetItemType(items1[num].GetItem());
					if (itemType != itemType2)
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, RMResources.UnsupportedCrossPartitionOrderByQueryOnMixedTypes, itemType, ItemTypeHelper.GetItemType(items1[num].GetItem()), items1[num].GetItem()));
		}
	}
}
