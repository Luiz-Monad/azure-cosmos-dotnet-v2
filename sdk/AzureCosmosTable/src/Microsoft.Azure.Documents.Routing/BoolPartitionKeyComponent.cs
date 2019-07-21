using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class BoolPartitionKeyComponent : IPartitionKeyComponent
	{
		private readonly bool value;

		public BoolPartitionKeyComponent(bool value)
		{
			this.value = value;
		}

		public int CompareTo(IPartitionKeyComponent other)
		{
			BoolPartitionKeyComponent boolPartitionKeyComponent = other as BoolPartitionKeyComponent;
			if (boolPartitionKeyComponent == null)
			{
				throw new ArgumentException("other");
			}
			return Math.Sign((value ? 1 : 0) - (boolPartitionKeyComponent.value ? 1 : 0));
		}

		public int GetTypeOrdinal()
		{
			if (!value)
			{
				return 2;
			}
			return 3;
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public void JsonEncode(JsonWriter writer)
		{
			writer.WriteValue(value);
		}

		public object ToObject()
		{
			return value;
		}

		public IPartitionKeyComponent Truncate()
		{
			return this;
		}

		public void WriteForHashing(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)(value ? 3 : 2));
		}

		public void WriteForHashingV2(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)(value ? 3 : 2));
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)(value ? 3 : 2));
		}
	}
}
