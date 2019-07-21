namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// CorrelationManager is not yet available in .NET Standard 1.6 and will be available in .NET Standard 2.0
	/// Got the source code from corefx repo and exposing it here from the Trace class.
	/// </summary>
	internal sealed class Trace
	{
		private static CorrelationManager s_correlationManager;

		public static CorrelationManager CorrelationManager
		{
			get
			{
				if (s_correlationManager == null)
				{
					s_correlationManager = new CorrelationManager();
				}
				return s_correlationManager;
			}
		}
	}
}
