using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the consistency policy of a database account of the Azure Cosmos DB service.
	/// </summary>
	public sealed class ConsistencyPolicy : JsonSerializable
	{
		private const ConsistencyLevel defaultDefaultConsistencyLevel = ConsistencyLevel.Session;

		internal const int DefaultMaxStalenessInterval = 5;

		internal const int DefaultMaxStalenessPrefix = 100;

		internal const int MaxStalenessIntervalInSecondsMinValue = 5;

		internal const int MaxStalenessIntervalInSecondsMaxValue = 86400;

		internal const int MaxStalenessPrefixMinValue = 10;

		internal const int MaxStalenessPrefixMaxValue = 1000000;

		/// <summary>
		/// Get or set the default consistency level in the Azure Cosmos DB service.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty(PropertyName = "defaultConsistencyLevel")]
		public ConsistencyLevel DefaultConsistencyLevel
		{
			get
			{
				return GetValue("defaultConsistencyLevel", ConsistencyLevel.Session);
			}
			set
			{
				SetValue("defaultConsistencyLevel", value.ToString());
			}
		}

		/// <summary>
		/// For bounded staleness consistency, the maximum allowed staleness
		/// in terms difference in sequence numbers (aka version) in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "maxStalenessPrefix")]
		public int MaxStalenessPrefix
		{
			get
			{
				return GetValue("maxStalenessPrefix", 100);
			}
			set
			{
				SetValue("maxStalenessPrefix", value);
			}
		}

		/// <summary>
		/// For bounded staleness consistency, the maximum allowed staleness
		/// in terms time interval in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "maxIntervalInSeconds")]
		public int MaxStalenessIntervalInSeconds
		{
			get
			{
				return GetValue("maxIntervalInSeconds", 5);
			}
			set
			{
				SetValue("maxIntervalInSeconds", value);
			}
		}

		internal void Validate()
		{
			Helpers.ValidateNonNegativeInteger("maxStalenessPrefix", MaxStalenessPrefix);
			Helpers.ValidateNonNegativeInteger("maxIntervalInSeconds", MaxStalenessIntervalInSeconds);
			if (DefaultConsistencyLevel == ConsistencyLevel.BoundedStaleness && (MaxStalenessIntervalInSeconds < 5 || MaxStalenessIntervalInSeconds > 86400))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidMaxStalenessInterval, 5, 86400));
			}
			if (DefaultConsistencyLevel == ConsistencyLevel.BoundedStaleness && (MaxStalenessPrefix < 10 || MaxStalenessPrefix > 1000000))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidMaxStalenessPrefix, 10, 1000000));
			}
		}
	}
}
