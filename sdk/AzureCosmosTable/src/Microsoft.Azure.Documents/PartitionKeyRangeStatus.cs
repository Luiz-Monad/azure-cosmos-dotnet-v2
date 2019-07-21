using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Documents
{
	[JsonConverter(typeof(StringEnumConverter))]
	internal enum PartitionKeyRangeStatus
	{
		Invalid,
		[EnumMember(Value = "online")]
		Online,
		[EnumMember(Value = "splitting")]
		Splitting,
		[EnumMember(Value = "offline")]
		Offline,
		[EnumMember(Value = "split")]
		Split
	}
}
