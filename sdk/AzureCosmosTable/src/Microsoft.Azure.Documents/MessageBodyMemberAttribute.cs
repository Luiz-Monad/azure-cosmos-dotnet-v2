namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to make MessageBodyMemberAttribute mean nothing when compiling for .NET Standard 1.6
	/// so as to avoid adding #if/#endif around it in the StoreResponse class that uses them.
	/// </summary>
	internal class MessageBodyMemberAttribute : MessageContractMemberAttribute
	{
		public int Order
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}
	}
}
