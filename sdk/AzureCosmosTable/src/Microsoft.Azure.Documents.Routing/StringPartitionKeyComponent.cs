using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class StringPartitionKeyComponent : IPartitionKeyComponent
	{
		public const int MaxStringChars = 100;

		public const int MaxStringBytesToAppend = 100;

		private readonly string value;

		private readonly byte[] utf8Value;

		public StringPartitionKeyComponent(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.value = value;
			utf8Value = Encoding.UTF8.GetBytes(value);
		}

		public void JsonEncode(JsonWriter writer)
		{
			writer.WriteValue(value);
		}

		public object ToObject()
		{
			return value;
		}

		public int CompareTo(IPartitionKeyComponent other)
		{
			StringPartitionKeyComponent stringPartitionKeyComponent = other as StringPartitionKeyComponent;
			if (stringPartitionKeyComponent == null)
			{
				throw new ArgumentException("other");
			}
			return string.CompareOrdinal(value, stringPartitionKeyComponent.value);
		}

		public int GetTypeOrdinal()
		{
			return 8;
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public IPartitionKeyComponent Truncate()
		{
			if (value.Length > 100)
			{
				return new StringPartitionKeyComponent(value.Substring(0, 100));
			}
			return this;
		}

		public void WriteForHashing(BinaryWriter writer)
		{
			writer.Write((byte)8);
			writer.Write(utf8Value);
			writer.Write((byte)0);
		}

		public void WriteForHashingV2(BinaryWriter writer)
		{
			writer.Write((byte)8);
			writer.Write(utf8Value);
			writer.Write(byte.MaxValue);
		}

		public void WriteForBinaryEncoding(BinaryWriter binaryWriter)
		{
			binaryWriter.Write((byte)8);
			bool flag = utf8Value.Length <= 100;
			for (int i = 0; i < (flag ? utf8Value.Length : 101); i++)
			{
				byte b = utf8Value[i];
				if (b < byte.MaxValue)
				{
					b = (byte)(b + 1);
				}
				binaryWriter.Write(b);
			}
			if (flag)
			{
				binaryWriter.Write((byte)0);
			}
		}

		/// <summary>
		/// Constructs a StringPartitionKeyComponent from byte string. This is only for testing/debugging. Please do not use in actual product code.
		/// </summary>
		public static IPartitionKeyComponent FromHexEncodedBinaryString(byte[] byteString, ref int offset)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i <= 100; i++)
			{
				byte b = byteString[offset++];
				if (b == 0)
				{
					break;
				}
				char c = (char)(b - 1);
				stringBuilder.Append(c);
			}
			return new StringPartitionKeyComponent(stringBuilder.ToString());
		}
	}
}
