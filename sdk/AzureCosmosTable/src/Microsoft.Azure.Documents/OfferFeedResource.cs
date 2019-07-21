using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	internal sealed class OfferFeedResource : Resource, IEnumerable<Offer>, IEnumerable
	{
		private static string CollectionName => typeof(Offer).Name + "s";

		public int Count => InnerCollection.Count;

		internal Collection<Offer> InnerCollection
		{
			get
			{
				Collection<Offer> collection = GetObjectCollection(CollectionName, typeof(Offer), base.AltLink, OfferTypeResolver.ResponseOfferTypeResolver);
				if (collection == null)
				{
					collection = new Collection<Offer>();
					SetObjectCollection(CollectionName, collection);
				}
				return collection;
			}
			set
			{
				SetObjectCollection(CollectionName, value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return InnerCollection.GetEnumerator();
		}

		IEnumerator<Offer> IEnumerable<Offer>.GetEnumerator()
		{
			return InnerCollection.GetEnumerator();
		}

		internal override void OnSave()
		{
			SetValue("_count", InnerCollection.Count);
		}
	}
}
