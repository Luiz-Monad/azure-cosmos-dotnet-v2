using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Class for composing multiple query metrics in a dictionary interface.
	/// </summary>
	internal sealed class PartitionedQueryMetrics : IReadOnlyDictionary<string, QueryMetrics>, IEnumerable<KeyValuePair<string, QueryMetrics>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, QueryMetrics>>
	{
		/// <summary>
		/// The backing store.
		/// </summary>
		private readonly Dictionary<string, QueryMetrics> partitionedQueryMetrics;

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count => partitionedQueryMetrics.Count;

		/// <summary>
		/// Gets the keys.
		/// </summary>
		public IEnumerable<string> Keys => partitionedQueryMetrics.Keys;

		/// <summary>
		/// Gets the values.
		/// </summary>
		public IEnumerable<QueryMetrics> Values => partitionedQueryMetrics.Values;

		/// <summary>
		/// Gets the QueryMetrics corresponding to the key.
		/// </summary>
		/// <param name="key">The partition id.</param>
		/// <returns>The QueryMetrics corresponding to the key.</returns>
		public QueryMetrics this[string key]
		{
			get
			{
				return partitionedQueryMetrics[key];
			}
		}

		/// <summary>
		/// Initializes a new instance of the PartitionedQueryMetrics class.
		/// </summary>
		/// <param name="other">The other dictionary of query metrics to create from.</param>
		public PartitionedQueryMetrics(IReadOnlyDictionary<string, QueryMetrics> other)
			: this()
		{
			foreach (KeyValuePair<string, QueryMetrics> item in other)
			{
				partitionedQueryMetrics[item.Key] = item.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the PartitionedQueryMetrics class.
		/// </summary>
		public PartitionedQueryMetrics()
		{
			partitionedQueryMetrics = new Dictionary<string, QueryMetrics>();
		}

		/// <summary>
		/// Aggregates an IEnumerable of partitioned query metrics together.
		/// </summary>
		/// <param name="partitionedQueryMetricsList">The partitioned query metrics to add together.</param>
		/// <returns>The summed up partitioned query metrics.</returns>
		public static PartitionedQueryMetrics CreateFromIEnumerable(IEnumerable<PartitionedQueryMetrics> partitionedQueryMetricsList)
		{
			return new PartitionedQueryMetrics(partitionedQueryMetricsList.SelectMany((PartitionedQueryMetrics partitionedQueryMetrics) => partitionedQueryMetrics).ToLookup((KeyValuePair<string, QueryMetrics> pair) => pair.Key, (KeyValuePair<string, QueryMetrics> pair) => pair.Value).ToDictionary((IGrouping<string, QueryMetrics> group) => group.Key, (IGrouping<string, QueryMetrics> group) => QueryMetrics.CreateFromIEnumerable(group)));
		}

		/// <summary>
		/// Overloaded for adding two partitioned query metrics together.
		/// </summary>
		/// <param name="partitionedQueryMetrics1">The first partitioned query metrics.</param>
		/// <param name="partitionedQueryMetrics2">The second partitioned query metrics.</param>
		/// <returns>
		/// Sums up two partitioned query metrics taking the union of the keys.
		/// If there is an intersection then the intersection is summed up as defined by QueryMetrics.
		/// The union minus the intersection is left as is.
		/// </returns>
		public static PartitionedQueryMetrics operator +(PartitionedQueryMetrics partitionedQueryMetrics1, PartitionedQueryMetrics partitionedQueryMetrics2)
		{
			return partitionedQueryMetrics1.Add(partitionedQueryMetrics2);
		}

		/// <summary>
		/// Adds partitioned query metrics together.
		/// </summary>
		/// <param name="partitionedQueryMetricsList">The partitioned query metrics to add.</param>
		/// <returns>The summed up partitioned query metrics.</returns>
		public PartitionedQueryMetrics Add(params PartitionedQueryMetrics[] partitionedQueryMetricsList)
		{
			List<PartitionedQueryMetrics> list = new List<PartitionedQueryMetrics>(partitionedQueryMetricsList.Length + 1);
			list.Add(this);
			list.AddRange(partitionedQueryMetricsList);
			return CreateFromIEnumerable(list);
		}

		/// <summary>
		/// Gets the string version.
		/// </summary>
		/// <returns>The string version.</returns>
		public override string ToString()
		{
			return ToTextString();
		}

		/// <summary>
		/// Checks to see if this contains a key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>Whether or not this partitioned query metrics contains a key.</returns>
		public bool ContainsKey(string key)
		{
			return partitionedQueryMetrics.ContainsKey(key);
		}

		/// <summary>
		/// Gets an enumerator.
		/// </summary>
		/// <returns>An enumerator.</returns>
		public IEnumerator<KeyValuePair<string, QueryMetrics>> GetEnumerator()
		{
			return partitionedQueryMetrics.GetEnumerator();
		}

		/// <summary>
		/// Tries to get a value corresponding to a key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// <returns>true if the key was found, else false.</returns>
		public bool TryGetValue(string key, out QueryMetrics value)
		{
			return partitionedQueryMetrics.TryGetValue(key, out value);
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Gets the text string of this object.
		/// </summary>
		/// <returns>The text string of this object.</returns>
		private string ToTextString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, QueryMetrics> item in from kvp in partitionedQueryMetrics
			orderby kvp.Key
			select kvp)
			{
				stringBuilder.AppendFormat("Partition {0}", item.Key);
				stringBuilder.AppendLine();
				stringBuilder.Append(item.Value);
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}
	}
}
