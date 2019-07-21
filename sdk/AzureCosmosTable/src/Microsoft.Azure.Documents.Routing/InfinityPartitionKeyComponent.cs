using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class InfinityPartitionKeyComponent : IPartitionKeyComponent
	{
		public int CompareTo(IPartitionKeyComponent other)
		{
			if (!(other is InfinityPartitionKeyComponent))
			{
				throw new ArgumentException("other");
			}
			return 0;
		}

		public int GetTypeOrdinal()
		{
			return 255;
		}

		public IPartitionKeyComponent Truncate()
		{
			throw new InvalidOperationException();
		}

		public void WriteForHashing(BinaryWriter writer)
		{
			throw new InvalidOperationException();
		}

		public void WriteForHashingV2(BinaryWriter writer)
		{
			throw new InvalidOperationException();
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public void JsonEncode(JsonWriter writer)
		{
			throw new NotImplementedException();
		}

		public object ToObject()
		{
			throw new NotImplementedException();
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write(byte.MaxValue);
		}
	}
}
