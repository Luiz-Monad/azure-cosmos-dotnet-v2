namespace Microsoft.Azure.Documents
{
	internal class TransportExceptionCounters
	{
		internal virtual void IncrementDecryptionFailures()
		{
		}

		internal virtual void IncrementEphemeralPortExhaustion()
		{
		}
	}
}
