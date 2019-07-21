using System;
using System.Net.Security;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to make MessageContractMemberAttribute mean nothing when compiling for .NET Standard 1.6
	/// so as to avoid adding #if/#endif around it in the StoreResponse class that uses them.
	/// </summary>
	internal abstract class MessageContractMemberAttribute : Attribute
	{
		public bool HasProtectionLevel => false;

		public string Name
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string Namespace
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public ProtectionLevel ProtectionLevel
		{
			get
			{
				return ProtectionLevel.None;
			}
			set
			{
			}
		}
	}
}
