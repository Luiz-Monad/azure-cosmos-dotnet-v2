using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	internal sealed class ResourceIdResponse : Resource
	{
		[JsonProperty(PropertyName = "resourceId")]
		public string NewResourceId
		{
			get
			{
				return GetValue<string>("resourceId");
			}
			internal set
			{
				SetValue("resourceId", value);
			}
		}

		[JsonProperty(PropertyName = "partitionIndex")]
		public int PartitionIndex
		{
			get
			{
				return GetValue<int>("partitionIndex");
			}
			internal set
			{
				SetValue("partitionIndex", value);
			}
		}

		[JsonProperty(PropertyName = "serviceIndex")]
		public int ServiceIndex
		{
			get
			{
				return GetValue<int>("serviceIndex");
			}
			internal set
			{
				SetValue("serviceIndex", value);
			}
		}
	}
}
