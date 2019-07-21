namespace Microsoft.Azure.Documents
{
	internal enum RntbdResponseStateEnum
	{
		NotStarted,
		Called,
		StartHeader,
		BufferingHeader,
		DoneBufferingHeader,
		BufferingMetadata,
		DoneBufferingMetadata,
		BufferingBodySize,
		DoneBufferingBodySize,
		BufferingBody,
		DoneBufferingBody,
		Done
	}
}
