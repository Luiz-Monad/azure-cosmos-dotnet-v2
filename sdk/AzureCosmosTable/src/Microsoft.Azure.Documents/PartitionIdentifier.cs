using System;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal sealed class PartitionIdentifier
	{
		public int PartitionIndex
		{
			get;
			set;
		}

		public int ServiceIndex
		{
			get;
			set;
		}

		public override bool Equals(object obj)
		{
			PartitionIdentifier partitionIdentifier = obj as PartitionIdentifier;
			if (partitionIdentifier != null)
			{
				if (partitionIdentifier.PartitionIndex == PartitionIndex)
				{
					return partitionIdentifier.ServiceIndex == ServiceIndex;
				}
				return false;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}@{1}", PartitionIndex, ServiceIndex);
		}

		public static PartitionIdentifier FromPartitionInfo(PartitionInfo partitionInfo)
		{
			return new PartitionIdentifier
			{
				PartitionIndex = partitionInfo.PartitionIndex,
				ServiceIndex = partitionInfo.ServiceIndex
			};
		}

		public static bool TryParse(string partitionIdentifierString, out PartitionIdentifier partitionIdentifier)
		{
			partitionIdentifier = null;
			if (!string.IsNullOrEmpty(partitionIdentifierString))
			{
				string[] array = partitionIdentifierString.Split(new char[1]
				{
					'@'
				}, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length == 2 && int.TryParse(array[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) && int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result2))
				{
					partitionIdentifier = new PartitionIdentifier
					{
						PartitionIndex = result,
						ServiceIndex = result2
					};
					return true;
				}
			}
			return false;
		}
	}
}
