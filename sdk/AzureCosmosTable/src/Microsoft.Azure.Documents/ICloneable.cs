using System.Runtime.InteropServices;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is a hack to add ICloneable interface as part of our namespace since it doesn't exist in .NET Standard 1.6
	/// That way we will avoid adding #if/#endif around the classes that currently implement it. Any .NET Core 1.0 app
	/// will not be using this type anyways.
	/// </summary>
	[ComVisible(true)]
	internal interface ICloneable
	{
		object Clone();
	}
}
