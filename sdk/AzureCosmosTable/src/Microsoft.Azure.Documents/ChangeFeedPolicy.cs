using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the change feed policy for a collection in the Azure Cosmos DB service.
	/// </summary>
	/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />
	internal sealed class ChangeFeedPolicy : JsonSerializable, Microsoft.Azure.Documents.ICloneable
	{
		/// <summary>
		/// Gets or sets a value that indicates for how long operation logs have to be retained.
		/// </summary>
		/// <value>
		/// Value is in TimeSpan. Any seconds will be ceiled as 1 minute.
		/// </value>
		[JsonProperty(PropertyName = "retentionDuration")]
		public TimeSpan RetentionDuration
		{
			get
			{
				return TimeSpan.FromMinutes(GetValue<int>("retentionDuration"));
			}
			set
			{
				TimeSpan timeSpan = value;
				int num = (int)timeSpan.TotalMinutes + ((timeSpan.Seconds > 0) ? 1 : 0);
				SetValue("retentionDuration", num);
			}
		}

		/// <summary>
		/// Performs a deep copy of the operation log policy.
		/// </summary>
		/// <returns>
		/// A clone of the operation log policy.
		/// </returns>
		public object Clone()
		{
			return new ChangeFeedPolicy
			{
				RetentionDuration = RetentionDuration
			};
		}
	}
}
