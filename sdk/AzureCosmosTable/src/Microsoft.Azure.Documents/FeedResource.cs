using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.Documents
{
	internal sealed class FeedResource<T> : Resource, IEnumerable<T>, IEnumerable where T : JsonSerializable, new()
	{
		private static string collectionName;

		private static string CollectionName
		{
			get
			{
				if (collectionName == null)
				{
					if (typeof(Document).IsAssignableFrom(typeof(T)))
					{
						collectionName = "Documents";
					}
					else if (typeof(Attachment).IsAssignableFrom(typeof(T)))
					{
						collectionName = "Attachments";
					}
					else
					{
						collectionName = typeof(T).Name + "s";
					}
				}
				return collectionName;
			}
		}

		public int Count => InnerCollection.Count;

		internal Collection<T> InnerCollection
		{
			get
			{
				Collection<T> collection = GetObjectCollection<T>(CollectionName, typeof(T), base.AltLink);
				if (collection == null)
				{
					collection = new Collection<T>();
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

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return InnerCollection.GetEnumerator();
		}

		internal override void OnSave()
		{
			SetValue("_count", InnerCollection.Count);
		}
	}
}
