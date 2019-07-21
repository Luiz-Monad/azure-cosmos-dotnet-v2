namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Enumeration offer auto scale modes that could be specified with auto scale settings.
	/// </summary>
	internal enum AutoScaleMode
	{
		Invalid = -1,
		None,
		Predictive,
		Reactive
	}
}
