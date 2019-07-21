using Newtonsoft.Json;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents the offer for a resource (collection) in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// Currently, offers are only bound to the collection resource.
	/// </remarks>
	public class Offer : Resource
	{
		/// <summary>
		/// Gets or sets the version of this offer resource in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "offerVersion", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string OfferVersion
		{
			get
			{
				return GetValue<string>("offerVersion");
			}
			internal set
			{
				SetValue("offerVersion", value);
			}
		}

		/// <summary>
		/// Gets or sets the self-link of a resource to which the resource offer applies to in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "resource")]
		public string ResourceLink
		{
			get
			{
				return GetValue<string>("resource");
			}
			internal set
			{
				SetValue("resource", value);
			}
		}

		/// <summary>
		/// Gets or sets the OfferType for the resource offer in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "offerType", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string OfferType
		{
			get
			{
				return GetValue<string>("offerType");
			}
			set
			{
				SetValue("offerType", value);
			}
		}

		/// <summary>
		/// Gets or sets the Id of the resource on which the Offer applies to in the Azure Cosmos DB service.
		/// </summary>
		[JsonProperty(PropertyName = "offerResourceId")]
		internal string OfferResourceId
		{
			get
			{
				return GetValue<string>("offerResourceId");
			}
			set
			{
				SetValue("offerResourceId", value);
			}
		}

		/// <summary>
		/// Initializes a Resource offer for the Azure Cosmos DB service.
		/// </summary>
		public Offer()
		{
			OfferVersion = "V1";
		}

		/// <summary>
		/// Initializes a Resource offer from another offer object for the Azure Cosmos DB service.
		/// </summary>
		public Offer(Offer offer)
			: base(offer)
		{
			OfferVersion = "V1";
			ResourceLink = offer.ResourceLink;
			OfferType = offer.OfferType;
			OfferResourceId = offer.OfferResourceId;
		}

		/// <summary>
		/// Validates the property, by calling it, in case of any errors exception is thrown
		/// </summary>
		internal override void Validate()
		{
			base.Validate();
			GetValue<string>("offerVersion");
			GetValue<string>("resource");
			GetValue<string>("offerType");
			GetValue<string>("offerResourceId");
		}

		/// <summary>
		/// Compares the offer object with the current offer
		/// </summary>
		/// <param name="offer"></param>
		/// <returns>Boolean representing the equality result</returns>
		public bool Equals(Offer offer)
		{
			if (!OfferVersion.Equals(offer.OfferVersion) || !OfferResourceId.Equals(offer.OfferResourceId))
			{
				return false;
			}
			if (OfferVersion.Equals("V1") && !OfferType.Equals(offer.OfferType))
			{
				return false;
			}
			return true;
		}
	}
}
