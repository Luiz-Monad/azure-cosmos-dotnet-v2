namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This enum specifies the types of geo links across regions, 
	/// with strong links having the lowest latency and best reliability, 
	/// and weak links having the highest latency and worst reliability.
	/// </summary>
	internal enum GeoLinkTypes
	{
		Strong,
		Medium,
		Weak
	}
}
