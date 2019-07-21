using System.IO;

namespace Microsoft.Azure.Documents
{
	internal sealed class RntbdToken
	{
		private ushort identifier;

		private RntbdTokenTypes type;

		private bool isRequired;

		public bool isPresent;

		public RntbdTokenValue value;

		public RntbdToken(bool isRequired, RntbdTokenTypes type, ushort identifier)
		{
			this.isRequired = isRequired;
			isPresent = false;
			this.type = type;
			this.identifier = identifier;
			value = default(RntbdTokenValue);
		}

		public RntbdTokenTypes GetTokenType()
		{
			return type;
		}

		public ushort GetTokenIdentifier()
		{
			return identifier;
		}

		public bool IsRequired()
		{
			return isRequired;
		}

		public void SerializeToBinaryWriter(BinaryWriter writer, out int written)
		{
			if (!isPresent && isRequired)
			{
				throw new BadRequestException();
			}
			if (isPresent)
			{
				writer.Write(identifier);
				writer.Write((byte)type);
				switch (type)
				{
				case RntbdTokenTypes.Byte:
					writer.Write(value.valueByte);
					written = 4;
					break;
				case RntbdTokenTypes.UShort:
					writer.Write(value.valueUShort);
					written = 5;
					break;
				case RntbdTokenTypes.ULong:
					writer.Write(value.valueULong);
					written = 7;
					break;
				case RntbdTokenTypes.Long:
					writer.Write(value.valueLong);
					written = 7;
					break;
				case RntbdTokenTypes.ULongLong:
					writer.Write(value.valueULongLong);
					written = 11;
					break;
				case RntbdTokenTypes.LongLong:
					writer.Write(value.valueLongLong);
					written = 11;
					break;
				case RntbdTokenTypes.Float:
					writer.Write(value.valueFloat);
					written = 7;
					break;
				case RntbdTokenTypes.Double:
					writer.Write(value.valueDouble);
					written = 11;
					break;
				case RntbdTokenTypes.Guid:
				{
					byte[] array = value.valueGuid.ToByteArray();
					writer.Write(array);
					written = 3 + array.Length;
					break;
				}
				case RntbdTokenTypes.SmallString:
				case RntbdTokenTypes.SmallBytes:
					if (value.valueBytes.Length > 255)
					{
						throw new RequestEntityTooLargeException();
					}
					writer.Write((byte)value.valueBytes.Length);
					writer.Write(value.valueBytes);
					written = 4 + value.valueBytes.Length;
					break;
				case RntbdTokenTypes.String:
				case RntbdTokenTypes.Bytes:
					if (value.valueBytes.Length > 65535)
					{
						throw new RequestEntityTooLargeException();
					}
					writer.Write((ushort)value.valueBytes.Length);
					writer.Write(value.valueBytes);
					written = 5 + value.valueBytes.Length;
					break;
				case RntbdTokenTypes.ULongString:
				case RntbdTokenTypes.ULongBytes:
					writer.Write((uint)value.valueBytes.Length);
					writer.Write(value.valueBytes);
					written = 7 + value.valueBytes.Length;
					break;
				default:
					throw new BadRequestException();
				}
			}
			else
			{
				written = 0;
			}
		}
	}
}
