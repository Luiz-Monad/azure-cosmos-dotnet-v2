namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to make MessageHeaderAttribute mean nothing when compiling for .NET Standard 1.6
	/// so as to avoid adding #if/#endif around it in the StoreResponse class that uses them.
	/// </summary>
	internal class MessageHeaderAttribute : MessageContractMemberAttribute
	{
		public string Actor
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public bool MustUnderstand
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public bool Relay
		{
			get
			{
				return false;
			}
			set
			{
			}
		}
	}
}
