using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the Standard pricing offer for a resource in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// Currently, offers are only bound to the collection resource.
	/// </remarks>
	public sealed class OfferV2 : Offer
	{
		/// <summary>
		/// Gets or sets the OfferContent for the resource offer in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "content", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public OfferContentV2 Content
		{
			get
			{
				return GetObject<OfferContentV2>("content");
			}
			internal set
			{
				SetObject("content", value);
			}
		}

		/// <summary>
		/// Initializes a Resource offer with the Standard pricing tier for the Azure Cosmos DB service.
		/// </summary>
		internal OfferV2()
		{
			base.OfferType = string.Empty;
			base.OfferVersion = "V2";
		}

		/// <summary>
		/// Initializes a Resource offer with the Standard pricing tier for the Azure Cosmos DB service.
		/// </summary>
		public OfferV2(int offerThroughput)
			: this()
		{
			Content = new OfferContentV2(offerThroughput);
		}

		/// <summary>
		/// Initializes a Resource offer with the Standard pricing tier for the Azure Cosmos DB service.
		/// </summary>
		public OfferV2(int offerThroughput, bool? offerEnableRUPerMinuteThroughput)
			: this()
		{
			Content = new OfferContentV2(offerThroughput, offerEnableRUPerMinuteThroughput);
		}

		/// <summary>
		/// Initializes a Resource offer with the Standard pricing tier, from a reference Offer object for the Azure Cosmos DB service.
		/// </summary>
		public OfferV2(Offer offer, int offerThroughput)
			: base(offer)
		{
			base.OfferType = string.Empty;
			base.OfferVersion = "V2";
			OfferContentV2 content = null;
			if (offer is OfferV2)
			{
				content = ((OfferV2)offer).Content;
			}
			Content = new OfferContentV2(content, offerThroughput, null);
		}

		/// <summary>
		/// Initializes a Resource offer with the Standard pricing tier, from a reference Offer object for the Azure Cosmos DB service.
		/// </summary>
		public OfferV2(Offer offer, int offerThroughput, bool? offerEnableRUPerMinuteThroughput)
			: base(offer)
		{
			base.OfferType = string.Empty;
			base.OfferVersion = "V2";
			OfferContentV2 content = null;
			if (offer is OfferV2)
			{
				content = ((OfferV2)offer).Content;
			}
			Content = new OfferContentV2(content, offerThroughput, offerEnableRUPerMinuteThroughput);
		}

		/// <summary>
		/// Validates the property, by calling it, in case of any errors exception is thrown
		/// </summary>
		internal override void Validate()
		{
			base.Validate();
			Content?.Validate();
		}

		/// <summary>
		/// Compares the offer object with the current offer
		/// </summary>
		/// <param name="offer"></param>
		/// <returns>Boolean representing the equality result</returns>
		public bool Equals(OfferV2 offer)
		{
			if (offer == null)
			{
				return false;
			}
			if (!Equals((Offer)offer))
			{
				return false;
			}
			if (Content == null && offer.Content == null)
			{
				return true;
			}
			if (Content != null && offer.Content != null)
			{
				if (Content.OfferThroughput == offer.Content.OfferThroughput)
				{
					return Content.OfferIsRUPerMinuteThroughputEnabled == offer.Content.OfferIsRUPerMinuteThroughputEnabled;
				}
				return false;
			}
			return false;
		}
	}
}
