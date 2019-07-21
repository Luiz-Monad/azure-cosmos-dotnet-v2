namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Replication policy.
	/// </summary>
	public sealed class ReplicationPolicy : JsonSerializable
	{
		private const int DefaultMaxReplicaSetSize = 4;

		private const int DefaultMinReplicaSetSize = 3;

		private const bool DefaultAsyncReplication = false;

		/// <summary>
		/// Maximum number of replicas for the partition.
		/// </summary>
		public int MaxReplicaSetSize
		{
			get
			{
				return GetValue("maxReplicasetSize", 4);
			}
			set
			{
				SetValue("maxReplicasetSize", value);
			}
		}

		/// <summary>
		/// Minimum number of replicas to ensure availability
		/// of the partition.
		/// </summary>
		public int MinReplicaSetSize
		{
			get
			{
				return GetValue("minReplicaSetSize", 3);
			}
			set
			{
				SetValue("minReplicaSetSize", value);
			}
		}

		/// <summary>
		/// Whether or not async replication is enabled.
		/// </summary>
		public bool AsyncReplication
		{
			get
			{
				return GetValue("asyncReplication", defaultValue: false);
			}
			set
			{
				SetValue("asyncReplication", value);
			}
		}

		internal void Validate()
		{
			Helpers.ValidateNonNegativeInteger("minReplicaSetSize", MinReplicaSetSize);
			Helpers.ValidateNonNegativeInteger("minReplicaSetSize", MaxReplicaSetSize);
		}
	}
}
