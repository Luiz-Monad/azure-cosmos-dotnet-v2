using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents content properties tied to the Standard pricing tier for the Azure Cosmos DB service.
	/// </summary>
	public sealed class OfferContentV2 : JsonSerializable
	{
		/// <summary>
		/// Represents customizable throughput chosen by user for his collection in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "offerThroughput", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public int OfferThroughput
		{
			get
			{
				return GetValue<int>("offerThroughput");
			}
			private set
			{
				SetValue("offerThroughput", value);
			}
		}

		/// <summary>
		/// Represents Request Units(RU)/Minute throughput is enabled/disabled for collection in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "offerIsRUPerMinuteThroughputEnabled", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool? OfferIsRUPerMinuteThroughputEnabled
		{
			get
			{
				return GetValue<bool?>("offerIsRUPerMinuteThroughputEnabled");
			}
			private set
			{
				SetValue("offerIsRUPerMinuteThroughputEnabled", value);
			}
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <remarks>
		/// The <see cref="T:Microsoft.Azure.Documents.OfferContentV2" /> class 
		/// represents content properties tied to the Standard pricing tier for the Azure Cosmos DB service.
		/// </remarks>
		public OfferContentV2()
			: this(0)
		{
		}

		/// <summary>
		/// Constructor accepting offer throughput.
		/// </summary>
		/// <remarks>
		/// The <see cref="T:Microsoft.Azure.Documents.OfferContentV2" /> class 
		/// represents content properties tied to the Standard pricing tier for the Azure Cosmos DB service.
		/// </remarks>
		public OfferContentV2(int offerThroughput)
		{
			OfferThroughput = offerThroughput;
			OfferIsRUPerMinuteThroughputEnabled = null;
		}

		/// <summary>
		/// Constructor accepting offer throughput, Request Units(RU)/Minute throughput is enabled or disabled
		/// and auto scale is enabled or disabled.
		/// </summary>
		/// <remarks>
		/// The <see cref="T:Microsoft.Azure.Documents.OfferContentV2" /> class 
		/// represents content properties tied to the Standard pricing tier for the Azure Cosmos DB service.
		/// </remarks>
		public OfferContentV2(int offerThroughput, bool? offerEnableRUPerMinuteThroughput)
		{
			OfferThroughput = offerThroughput;
			OfferIsRUPerMinuteThroughputEnabled = offerEnableRUPerMinuteThroughput;
		}

		/// <summary>
		/// internal constructor that takes offer throughput, RUPM is enabled/disabled and a reference offer content
		/// </summary>
		internal OfferContentV2(OfferContentV2 content, int offerThroughput, bool? offerEnableRUPerMinuteThroughput)
		{
			OfferThroughput = offerThroughput;
			OfferIsRUPerMinuteThroughputEnabled = offerEnableRUPerMinuteThroughput;
		}

		/// <summary>
		/// Validates the property, by calling it, in case of any errors exception is thrown
		/// </summary>
		internal void Validate()
		{
			GetValue<int>("offerThroughput");
			GetValue<bool?>("offerIsRUPerMinuteThroughputEnabled");
		}
	}
}
