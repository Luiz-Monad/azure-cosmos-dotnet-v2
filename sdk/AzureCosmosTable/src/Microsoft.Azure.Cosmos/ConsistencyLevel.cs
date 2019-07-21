namespace Microsoft.Azure.Cosmos
{
	public enum ConsistencyLevel
	{
		Strong,
		BoundedStaleness,
		Session,
		Eventual,
		ConsistentPrefix
	}
}
