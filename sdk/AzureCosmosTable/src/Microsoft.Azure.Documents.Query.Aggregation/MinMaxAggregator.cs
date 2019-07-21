using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Documents.Query.Aggregation
{
	/// <summary>
	/// Concrete implementation of IAggregator that can take the global min/max from the local min/max of multiple partitions and continuations.
	/// Let min/max_i,j be the min/max from the ith continuation in the jth partition, 
	/// then the min/max for the entire query is MIN/MAX(min/max_i,j for all i and j).
	/// </summary>
	internal sealed class MinMaxAggregator : IAggregator
	{
		/// <summary>
		/// Whether or not the aggregation is a min or a max.
		/// </summary>
		private readonly bool isMinAggregation;

		/// <summary>
		/// The global max of all items seen.
		/// </summary>
		private object globalMinMax;

		public MinMaxAggregator(bool isMinAggregation)
		{
			this.isMinAggregation = isMinAggregation;
			if (this.isMinAggregation)
			{
				globalMinMax = ItemComparer.MaxValue;
			}
			else
			{
				globalMinMax = ItemComparer.MinValue;
			}
		}

		public void Aggregate(object item)
		{
			if (globalMinMax == Undefined.Value)
			{
				return;
			}
			if (item == Undefined.Value)
			{
				globalMinMax = Undefined.Value;
				return;
			}
			JObject jObject = item as JObject;
			if (jObject != null)
			{
				JToken jToken = jObject["count"];
				if (jToken != null)
				{
					if (jToken.ToObject<long>() == 0L)
					{
						return;
					}
					JToken jToken2 = jObject["min"];
					JToken jToken3 = jObject["max"];
					item = ((jToken2 != null) ? jToken2.ToObject<object>() : ((jToken3 == null) ? Undefined.Value : jToken3.ToObject<object>()));
				}
			}
			if (!ItemComparer.IsMinOrMax(globalMinMax) && (!ItemTypeHelper.IsPrimitive(item) || !ItemTypeHelper.IsPrimitive(globalMinMax)))
			{
				globalMinMax = Undefined.Value;
			}
			else if (isMinAggregation)
			{
				if (ItemComparer.Instance.Compare(item, globalMinMax) < 0)
				{
					globalMinMax = item;
				}
			}
			else if (ItemComparer.Instance.Compare(item, globalMinMax) > 0)
			{
				globalMinMax = item;
			}
		}

		public object GetResult()
		{
			if (globalMinMax == ItemComparer.MinValue || globalMinMax == ItemComparer.MaxValue)
			{
				return Undefined.Value;
			}
			return globalMinMax;
		}
	}
}
