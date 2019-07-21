using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Offer resolver based on input.
	/// </summary>
	internal sealed class OfferTypeResolver : ITypeResolver<Offer>
	{
		public static readonly ITypeResolver<Offer> RequestOfferTypeResolver = new OfferTypeResolver(isResponse: false);

		public static readonly ITypeResolver<Offer> ResponseOfferTypeResolver = new OfferTypeResolver(isResponse: true);

		private readonly bool isResponse;

		/// <summary>
		/// Constructor with a flag indicating whether this is invoked in response or request path.
		/// </summary>
		/// <param name="isResponse">True if invoked in response path</param>
		private OfferTypeResolver(bool isResponse)
		{
			this.isResponse = isResponse;
		}

		/// <summary>
		/// Returns a reference of an object in Offer's hierarchy based on a property bag.
		/// </summary>
		/// <param name="propertyBag">Property bag used to deserialize Offer object</param>
		/// <returns>Object of type Offer or OfferV2</returns>
		Offer ITypeResolver<Offer>.Resolve(JObject propertyBag)
		{
			Offer offer;
			if (propertyBag != null)
			{
				offer = new Offer();
				offer.propertyBag = propertyBag;
				string text = offer.OfferVersion ?? string.Empty;
				if (!(text == "V1") && (text == null || text.Length != 0))
				{
					if (text == "V2")
					{
						offer = new OfferV2();
						offer.propertyBag = propertyBag;
					}
					else
					{
						DefaultTrace.TraceCritical("Unexpected offer version {0}", offer.OfferVersion);
						if (!isResponse)
						{
							throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.UnsupportedOfferVersion, offer.OfferVersion));
						}
					}
				}
			}
			else
			{
				offer = null;
			}
			return offer;
		}
	}
}
