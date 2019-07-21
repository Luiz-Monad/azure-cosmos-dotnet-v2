using System;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This replaces the use of ConfigurationErrorsException with Exception. This exception is thrown only
	/// in one internal method and is not caught within our code, so it'ss safe to use Exception here.
	/// </summary>
	internal class ConfigurationErrorsException : Exception
	{
		public ConfigurationErrorsException(string message)
			: base(message)
		{
		}
	}
}
