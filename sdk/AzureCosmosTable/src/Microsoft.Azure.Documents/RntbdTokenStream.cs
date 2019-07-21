using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Azure.Documents
{
	internal abstract class RntbdTokenStream
	{
		internal RntbdToken[] tokens;

		private Dictionary<ushort, RntbdToken> tokenMap;

		protected void SetTokens(RntbdToken[] t)
		{
			tokens = t;
			tokenMap = t.ToDictionary((RntbdToken token) => token.GetTokenIdentifier(), (RntbdToken token) => token);
		}

		public int CalculateLength()
		{
			int num = 0;
			RntbdToken[] array = tokens;
			foreach (RntbdToken rntbdToken in array)
			{
				if (rntbdToken.isPresent)
				{
					num++;
					num += 2;
					switch (rntbdToken.GetTokenType())
					{
					case RntbdTokenTypes.Byte:
						num++;
						break;
					case RntbdTokenTypes.UShort:
						num += 2;
						break;
					case RntbdTokenTypes.ULong:
					case RntbdTokenTypes.Long:
						num += 4;
						break;
					case RntbdTokenTypes.ULongLong:
					case RntbdTokenTypes.LongLong:
						num += 8;
						break;
					case RntbdTokenTypes.Float:
						num += 4;
						break;
					case RntbdTokenTypes.Double:
						num += 8;
						break;
					case RntbdTokenTypes.Guid:
						num += 12;
						break;
					case RntbdTokenTypes.SmallString:
					case RntbdTokenTypes.SmallBytes:
						num++;
						num += rntbdToken.value.valueBytes.Length;
						break;
					case RntbdTokenTypes.String:
					case RntbdTokenTypes.Bytes:
						num += 2;
						num += rntbdToken.value.valueBytes.Length;
						break;
					case RntbdTokenTypes.ULongString:
					case RntbdTokenTypes.ULongBytes:
						num += 4;
						num += rntbdToken.value.valueBytes.Length;
						break;
					default:
						throw new BadRequestException();
					}
				}
			}
			return num;
		}

		public void SerializeToBinaryWriter(BinaryWriter writer, out int tokensLength)
		{
			tokensLength = 0;
			RntbdToken[] array = tokens;
			foreach (RntbdToken obj in array)
			{
				int written = 0;
				obj.SerializeToBinaryWriter(writer, out written);
				tokensLength += written;
			}
		}

		public void ParseFrom(BinaryReader reader)
		{
			while (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				ushort num = reader.ReadUInt16();
				RntbdTokenTypes type = (RntbdTokenTypes)reader.ReadByte();
				if (!tokenMap.TryGetValue(num, out RntbdToken value))
				{
					value = new RntbdToken(isRequired: false, type, num);
				}
				if (value.isPresent)
				{
					DefaultTrace.TraceError("Duplicate token with identifier {0} type {1} found in RNTBD token stream", value.GetTokenIdentifier(), value.GetTokenType());
					throw new InternalServerErrorException(RMResources.InternalServerError, GetValidationFailureHeader());
				}
				switch (value.GetTokenType())
				{
				case RntbdTokenTypes.Byte:
					value.value.valueByte = reader.ReadByte();
					break;
				case RntbdTokenTypes.UShort:
					value.value.valueUShort = reader.ReadUInt16();
					break;
				case RntbdTokenTypes.ULong:
					value.value.valueULong = reader.ReadUInt32();
					break;
				case RntbdTokenTypes.Long:
					value.value.valueLong = reader.ReadInt32();
					break;
				case RntbdTokenTypes.ULongLong:
					value.value.valueULongLong = reader.ReadUInt64();
					break;
				case RntbdTokenTypes.LongLong:
					value.value.valueLongLong = reader.ReadInt64();
					break;
				case RntbdTokenTypes.Float:
					value.value.valueFloat = reader.ReadSingle();
					break;
				case RntbdTokenTypes.Double:
					value.value.valueDouble = reader.ReadDouble();
					break;
				case RntbdTokenTypes.Guid:
					value.value.valueGuid = new Guid(reader.ReadBytes(16));
					break;
				case RntbdTokenTypes.SmallString:
				case RntbdTokenTypes.SmallBytes:
				{
					byte count3 = reader.ReadByte();
					value.value.valueBytes = reader.ReadBytes(count3);
					break;
				}
				case RntbdTokenTypes.String:
				case RntbdTokenTypes.Bytes:
				{
					ushort count2 = reader.ReadUInt16();
					value.value.valueBytes = reader.ReadBytes(count2);
					break;
				}
				case RntbdTokenTypes.ULongString:
				case RntbdTokenTypes.ULongBytes:
				{
					uint count = reader.ReadUInt32();
					value.value.valueBytes = reader.ReadBytes((int)count);
					break;
				}
				default:
					DefaultTrace.TraceError("Unrecognized token type {0} with identifier {1} found in RNTBD token stream", value.GetTokenType(), value.GetTokenIdentifier());
					throw new InternalServerErrorException(RMResources.InternalServerError, GetValidationFailureHeader());
				}
				value.isPresent = true;
			}
			RntbdToken[] array = tokens;
			int num2 = 0;
			RntbdToken rntbdToken;
			while (true)
			{
				if (num2 < array.Length)
				{
					rntbdToken = array[num2];
					if (!rntbdToken.isPresent && rntbdToken.IsRequired())
					{
						break;
					}
					num2++;
					continue;
				}
				return;
			}
			DefaultTrace.TraceError("Required token with identifier {0} not found in RNTBD token stream", rntbdToken.GetTokenIdentifier());
			throw new InternalServerErrorException(RMResources.InternalServerError, GetValidationFailureHeader());
		}

		private INameValueCollection GetValidationFailureHeader()
		{
			return new StringKeyValueCollection
			{
				{
					"x-ms-request-validation-failure",
					"1"
				}
			};
		}
	}
}
