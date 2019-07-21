using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class UndefinedPartitionKeyComponent : IPartitionKeyComponent
	{
		public static readonly UndefinedPartitionKeyComponent Value = new UndefinedPartitionKeyComponent();

		internal UndefinedPartitionKeyComponent()
		{
		}

		public int CompareTo(IPartitionKeyComponent other)
		{
			if (!(other is UndefinedPartitionKeyComponent))
			{
				throw new ArgumentException("other");
			}
			return 0;
		}

		public int GetTypeOrdinal()
		{
			return 0;
		}

		public double GetHashValue()
		{
			return 0.0;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public void JsonEncode(JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WriteEndObject();
		}

		public object ToObject()
		{
			return Undefined.Value;
		}

		public IPartitionKeyComponent Truncate()
		{
			return this;
		}

		public void WriteForHashing(BinaryWriter writer)
		{
			writer.Write((byte)0);
		}

		public void WriteForHashingV2(BinaryWriter writer)
		{
			writer.Write((byte)0);
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)0);
		}
	}
}
