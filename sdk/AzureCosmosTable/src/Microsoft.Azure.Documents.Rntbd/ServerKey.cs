using System;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal sealed class ServerKey
	{
		public string Server
		{
			get;
			private set;
		}

		public int Port
		{
			get;
			private set;
		}

		public ServerKey(Uri uri)
		{
			Server = uri.DnsSafeHost;
			Port = uri.Port;
		}

		public override string ToString()
		{
			return $"{Server}:{Port}";
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			return Equals(obj as ServerKey);
		}

		public bool Equals(ServerKey key)
		{
			if (key != null && Server.Equals(key.Server))
			{
				return Port == key.Port;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((0x22970BE1 ^ Server.GetHashCode()) * 302869537) ^ HashInt32(Port)) * 302869537;
		}

		private static int HashInt32(int key)
		{
			int num = 266758603;
			for (int i = 0; i < 4; i++)
			{
				num ^= (key & 0xFF);
				num *= 646844749;
				key >>= 8;
			}
			return num;
		}
	}
}
