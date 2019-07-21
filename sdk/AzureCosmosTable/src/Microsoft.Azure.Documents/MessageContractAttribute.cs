using System;
using System.Net.Security;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to make MessageContractAttribute mean nothing when compiling for .NET Standard 1.6
	/// so as to avoid adding #if/#endif around it in the StoreResponse class that uses them.
	/// </summary>
	internal sealed class MessageContractAttribute : Attribute
	{
		public bool HasProtectionLevel => false;

		public bool IsWrapped
		{
			get
			{
				return false;
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

		public string WrapperName
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string WrapperNamespace
		{
			get
			{
				return null;
			}
			set
			{
			}
		}
	}
}
