namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ServerProperties
	{
		public string Agent
		{
			get;
			private set;
		}

		public string Version
		{
			get;
			private set;
		}

		public ServerProperties(string agent, string version)
		{
			Agent = agent;
			Version = version;
		}
	}
}
