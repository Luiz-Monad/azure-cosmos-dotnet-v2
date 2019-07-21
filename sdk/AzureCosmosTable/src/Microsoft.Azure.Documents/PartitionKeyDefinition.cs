using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	/// <summary> 
	/// Specifies a partition key definition for a particular path in the Azure Cosmos DB service.
	/// </summary>
	public sealed class PartitionKeyDefinition : JsonSerializable
	{
		private Collection<string> paths;

		private PartitionKind? kind;

		/// <summary>
		/// Gets or sets the paths to be partitioned in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The path to be partitioned.
		/// </value>
		[JsonProperty(PropertyName = "paths")]
		public Collection<string> Paths
		{
			get
			{
				if (paths == null)
				{
					paths = (GetValue<Collection<string>>("paths") ?? new Collection<string>());
				}
				return paths;
			}
			set
			{
				paths = value;
				SetValue("paths", value);
			}
		}

		/// <summary>
		/// Gets or sets the kind of partitioning to be applied in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.PartitionKind" /> enumeration.
		/// </value>
		[JsonProperty(PropertyName = "kind")]
		[JsonConverter(typeof(StringEnumConverter))]
		internal PartitionKind Kind
		{
			get
			{
				if (!kind.HasValue)
				{
					kind = GetValue("kind", PartitionKind.Hash);
				}
				return kind.Value;
			}
			set
			{
				kind = null;
				SetValue("kind", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets version of the partitioning scheme to be applied on the partition key
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="T:Microsoft.Azure.Documents.PartitionKeyDefinitionVersion" /> enumeration. 
		/// </value>
		[JsonProperty(PropertyName = "version", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public PartitionKeyDefinitionVersion? Version
		{
			get
			{
				return (PartitionKeyDefinitionVersion?)GetValue<int?>("version");
			}
			set
			{
				SetValue("version", (int?)value);
			}
		}

		/// <summary>
		/// Gets whether the partition key definition in the collection is system inserted key
		/// in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// </value>
		[JsonProperty(PropertyName = "systemKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
		internal bool? IsSystemKey
		{
			get
			{
				return GetValue<bool?>("systemKey");
			}
			set
			{
				SetValue("systemKey", value);
			}
		}

		internal override void OnSave()
		{
			if (paths != null)
			{
				SetValue("paths", paths);
			}
			if (kind.HasValue)
			{
				SetValue("kind", kind.ToString());
			}
		}

		internal static bool AreEquivalent(PartitionKeyDefinition pkd1, PartitionKeyDefinition pkd2)
		{
			if (pkd1.Kind != pkd2.Kind)
			{
				return false;
			}
			if (pkd1.Version != pkd2.Version)
			{
				return false;
			}
			if (!(from i in pkd1.Paths
			orderby i
			select i).SequenceEqual(from i in pkd2.Paths
			orderby i
			select i))
			{
				return false;
			}
			if (pkd1.IsSystemKey != pkd2.IsSystemKey)
			{
				return false;
			}
			return true;
		}
	}
}
