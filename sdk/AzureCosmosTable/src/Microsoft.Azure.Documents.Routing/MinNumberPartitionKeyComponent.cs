using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class MinNumberPartitionKeyComponent : IPartitionKeyComponent
	{
		public static readonly MinNumberPartitionKeyComponent Value = new MinNumberPartitionKeyComponent();

		private MinNumberPartitionKeyComponent()
		{
		}

		public int CompareTo(IPartitionKeyComponent other)
		{
			if (!(other is MinNumberPartitionKeyComponent))
			{
				throw new ArgumentException("other");
			}
			return 0;
		}

		public int GetTypeOrdinal()
		{
			return 4;
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
			return MinNumber.Value;
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)4);
		}
	}
}
