using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to make Serializable attribute mean nothing when compiling for .NET Standard 1.6
	/// so as to avoid adding #if/#endif around it in the entire codebase.
	/// </summary>
	internal class SerializableAttribute : Attribute
	{
	}
}
