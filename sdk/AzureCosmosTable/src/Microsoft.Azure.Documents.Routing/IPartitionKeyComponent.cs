using Newtonsoft.Json;
using System.IO;

namespace Microsoft.Azure.Documents.Routing
{
	internal interface IPartitionKeyComponent
	{
		int CompareTo(IPartitionKeyComponent other);

		int GetTypeOrdinal();

		void JsonEncode(JsonWriter writer);

		object ToObject();

		void WriteForHashing(BinaryWriter binaryWriter);

		void WriteForHashingV2(BinaryWriter binaryWriter);

		void WriteForBinaryEncoding(BinaryWriter binaryWriter);

		IPartitionKeyComponent Truncate();
	}
}
