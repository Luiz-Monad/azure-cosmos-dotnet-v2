using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	internal sealed class Address : Resource
	{
		[JsonProperty(PropertyName = "isPrimary")]
		public bool IsPrimary
		{
			get
			{
				return GetValue<bool>("isPrimary");
			}
			internal set
			{
				SetValue("isPrimary", value);
			}
		}

		[JsonProperty(PropertyName = "protocol")]
		public string Protocol
		{
			get
			{
				return GetValue<string>("protocol");
			}
			internal set
			{
				SetValue("protocol", value);
			}
		}

		[JsonProperty(PropertyName = "logicalUri")]
		public string LogicalUri
		{
			get
			{
				return GetValue<string>("logicalUri");
			}
			internal set
			{
				SetValue("logicalUri", value);
			}
		}

		[JsonProperty(PropertyName = "physcialUri")]
		public string PhysicalUri
		{
			get
			{
				return GetValue<string>("physcialUri");
			}
			internal set
			{
				SetValue("physcialUri", value);
			}
		}

		[JsonProperty(PropertyName = "partitionIndex")]
		public string PartitionIndex
		{
			get
			{
				return GetValue<string>("partitionIndex");
			}
			internal set
			{
				SetValue("partitionIndex", value);
			}
		}

		[JsonProperty(PropertyName = "partitionKeyRangeId")]
		public string PartitionKeyRangeId
		{
			get
			{
				return GetValue<string>("partitionKeyRangeId");
			}
			internal set
			{
				SetValue("partitionKeyRangeId", value);
			}
		}
	}
}
