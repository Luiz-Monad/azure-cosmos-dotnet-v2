using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	internal sealed class ReadPolicy : JsonSerializable
	{
		private const int DefaultPrimaryReadCoefficient = 0;

		private const int DefaultSecondaryReadCoefficient = 1;

		/// <summary>
		/// Relative weight of primary to serve read requests. Higher the value, it is preferred to issue reads to primary.
		/// Direct connectivity client can use this value to dynamically decide where to send reads to effectively use the service.
		/// </summary>
		[JsonProperty(PropertyName = "primaryReadCoefficient")]
		public int PrimaryReadCoefficient
		{
			get
			{
				return GetValue("primaryReadCoefficient", 0);
			}
			set
			{
				SetValue("primaryReadCoefficient", value);
			}
		}

		/// <summary>
		/// Relative weight of secondary to serve read requests. Higher the value, it is preferred to issue reads to secondary.
		/// Direct connectivity client can use this value to dynamically decide where to send reads to effectively use the service.
		/// </summary>
		[JsonProperty(PropertyName = "secondaryReadCoefficient")]
		public int SecondaryReadCoefficient
		{
			get
			{
				return GetValue("secondaryReadCoefficient", 1);
			}
			set
			{
				SetValue("secondaryReadCoefficient", value);
			}
		}
	}
}
