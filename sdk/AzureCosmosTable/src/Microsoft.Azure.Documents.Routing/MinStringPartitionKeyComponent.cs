using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class MinStringPartitionKeyComponent : IPartitionKeyComponent
	{
		public static readonly MinStringPartitionKeyComponent Value = new MinStringPartitionKeyComponent();

		private MinStringPartitionKeyComponent()
		{
		}

		public int CompareTo(IPartitionKeyComponent other)
		{
			if (!(other is MinStringPartitionKeyComponent))
			{
				throw new ArgumentException("other");
			}
			return 0;
		}

		public int GetTypeOrdinal()
		{
			return 7;
		}

		public override int GetHashCode()
		{
			return 0;
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

		public void JsonEncode(JsonWriter writer)
		{
			PartitionKeyInternalJsonConverter.JsonEncode(this, writer);
		}

		public object ToObject()
		{
			return MinString.Value;
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)7);
		}
	}
}
