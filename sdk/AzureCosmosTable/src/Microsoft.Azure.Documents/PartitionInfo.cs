using Newtonsoft.Json;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal sealed class PartitionInfo : Resource
	{
		[JsonProperty(PropertyName = "resourceType")]
		public string ResourceType
		{
			get
			{
				return GetValue<string>("resourceType");
			}
			set
			{
				SetValue("resourceType", value);
			}
		}

		[JsonProperty(PropertyName = "serviceIndex")]
		public int ServiceIndex
		{
			get
			{
				return GetValue<int>("serviceIndex");
			}
			set
			{
				SetValue("serviceIndex", value);
			}
		}

		[JsonProperty(PropertyName = "partitionIndex")]
		public int PartitionIndex
		{
			get
			{
				return GetValue<int>("partitionIndex");
			}
			set
			{
				SetValue("partitionIndex", value);
			}
		}

		public override bool Equals(object obj)
		{
			PartitionInfo partitionInfo = obj as PartitionInfo;
			if (partitionInfo != null)
			{
				if (partitionInfo.ResourceType == ResourceType && partitionInfo.PartitionIndex == PartitionIndex)
				{
					return partitionInfo.ServiceIndex == ServiceIndex;
				}
				return false;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}@{1}@{2}", ResourceType, PartitionIndex, ServiceIndex).GetHashCode();
		}
	}
}
