using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	internal static class DefaultJsonSerializationSettings
	{
		public static readonly JsonSerializerSettings Value = new JsonSerializerSettings();
	}
}
