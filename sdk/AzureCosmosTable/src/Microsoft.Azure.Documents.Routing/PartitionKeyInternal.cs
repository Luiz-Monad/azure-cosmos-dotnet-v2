using Microsoft.Azure.Documents.SharedFiles.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// Schema-less Partition Key value.
	/// </summary>
	[JsonConverter(typeof(PartitionKeyInternalJsonConverter))]
	[SuppressMessage("", "AvoidMultiLineComments", Justification = "Multi line business logic")]
	internal sealed class PartitionKeyInternal : IComparable<PartitionKeyInternal>, IEquatable<PartitionKeyInternal>, ICloneable
	{
		internal static class HexConvert
		{
			private static readonly ushort[] LookupTable = CreateLookupTable();

			private static ushort[] CreateLookupTable()
			{
				ushort[] array = new ushort[256];
				for (int i = 0; i < 256; i++)
				{
					string text = i.ToString("X2", CultureInfo.InvariantCulture);
					array[i] = (ushort)(text[0] + ((uint)text[1] << 8));
				}
				return array;
			}

			public static string ToHex(byte[] bytes, int start, int length)
			{
				char[] array = new char[length * 2];
				for (int i = 0; i < length; i++)
				{
					ushort num = LookupTable[bytes[i + start]];
					array[2 * i] = (char)(num & 0xFF);
					array[2 * i + 1] = (char)(num >> 8);
				}
				return new string(array);
			}
		}

		private readonly IReadOnlyList<IPartitionKeyComponent> components;

		private static readonly PartitionKeyInternal NonePartitionKey = new PartitionKeyInternal();

		private static readonly PartitionKeyInternal EmptyPartitionKey = new PartitionKeyInternal(new IPartitionKeyComponent[0]);

		private static readonly PartitionKeyInternal InfinityPartitionKey = new PartitionKeyInternal(new InfinityPartitionKeyComponent[1]
		{
			new InfinityPartitionKeyComponent()
		});

		private static readonly PartitionKeyInternal UndefinedPartitionKey = new PartitionKeyInternal(new UndefinedPartitionKeyComponent[1]
		{
			new UndefinedPartitionKeyComponent()
		});

		private const int MaxPartitionKeyBinarySize = 336;

		private static readonly Int128 MaxHashV2Value = new Int128(new byte[16]
		{
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			255,
			63
		});

		public static readonly string MinimumInclusiveEffectivePartitionKey = ToHexEncodedBinaryString(new IPartitionKeyComponent[0]);

		public static readonly string MaximumExclusiveEffectivePartitionKey = ToHexEncodedBinaryString(new InfinityPartitionKeyComponent[1]
		{
			new InfinityPartitionKeyComponent()
		});

		public static PartitionKeyInternal InclusiveMinimum => EmptyPartitionKey;

		public static PartitionKeyInternal ExclusiveMaximum => InfinityPartitionKey;

		public static PartitionKeyInternal Empty => EmptyPartitionKey;

		public static PartitionKeyInternal None => NonePartitionKey;

		public static PartitionKeyInternal Undefined => UndefinedPartitionKey;

		public IReadOnlyList<IPartitionKeyComponent> Components => components;

		private PartitionKeyInternal()
		{
			components = null;
		}

		public PartitionKeyInternal(IReadOnlyList<IPartitionKeyComponent> values)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			components = values;
		}

		/// <summary>
		/// Constructs instance of <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" /> from enumerable of objects.
		/// </summary>
		/// <param name="values">Partition key component values.</param>
		/// <param name="strict">If this is false, unsupported component values will be repliaced with 'Undefined'. If this is true, exception will be thrown.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" />.</returns>
		public static PartitionKeyInternal FromObjectArray(IEnumerable<object> values, bool strict)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			List<IPartitionKeyComponent> list = new List<IPartitionKeyComponent>();
			foreach (object value in values)
			{
				if (value == null)
				{
					list.Add(NullPartitionKeyComponent.Value);
				}
				else if (value is Undefined)
				{
					list.Add(UndefinedPartitionKeyComponent.Value);
				}
				else if (value is bool)
				{
					list.Add(new BoolPartitionKeyComponent((bool)value));
				}
				else if (value is string)
				{
					list.Add(new StringPartitionKeyComponent((string)value));
				}
				else if (IsNumeric(value))
				{
					list.Add(new NumberPartitionKeyComponent(Convert.ToDouble(value, CultureInfo.InvariantCulture)));
				}
				else if (value is MinNumber)
				{
					list.Add(MinNumberPartitionKeyComponent.Value);
				}
				else if (value is MaxNumber)
				{
					list.Add(MaxNumberPartitionKeyComponent.Value);
				}
				else if (value is MinString)
				{
					list.Add(MinStringPartitionKeyComponent.Value);
				}
				else if (value is MaxString)
				{
					list.Add(MaxStringPartitionKeyComponent.Value);
				}
				else
				{
					if (strict)
					{
						throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, RMResources.UnsupportedPartitionKeyComponentValue, value));
					}
					list.Add(UndefinedPartitionKeyComponent.Value);
				}
			}
			return new PartitionKeyInternal(list);
		}

		public object[] ToObjectArray()
		{
			return (from component in Components
			select component.ToObject()).ToArray();
		}

		public static PartitionKeyInternal FromJsonString(string partitionKey)
		{
			if (string.IsNullOrWhiteSpace(partitionKey))
			{
				throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, RMResources.UnableToDeserializePartitionKeyValue, partitionKey));
			}
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				DateParseHandling = DateParseHandling.None
			};
			return JsonConvert.DeserializeObject<PartitionKeyInternal>(partitionKey, settings);
		}

		public string ToJsonString()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
				Formatting = Formatting.None
			};
			return JsonConvert.SerializeObject(this, settings);
		}

		public bool Contains(PartitionKeyInternal nestedPartitionKey)
		{
			if (Components.Count > nestedPartitionKey.Components.Count)
			{
				return false;
			}
			for (int i = 0; i < Components.Count; i++)
			{
				if (Components[i].CompareTo(nestedPartitionKey.Components[i]) != 0)
				{
					return false;
				}
			}
			return true;
		}

		public static PartitionKeyInternal Max(PartitionKeyInternal key1, PartitionKeyInternal key2)
		{
			if (key1 == null)
			{
				return key2;
			}
			if (key2 == null)
			{
				return key1;
			}
			if (key1.CompareTo(key2) < 0)
			{
				return key2;
			}
			return key1;
		}

		public static PartitionKeyInternal Min(PartitionKeyInternal key1, PartitionKeyInternal key2)
		{
			if (key1 == null)
			{
				return key2;
			}
			if (key2 == null)
			{
				return key1;
			}
			if (key1.CompareTo(key2) > 0)
			{
				return key2;
			}
			return key1;
		}

		public static string GetMinInclusiveEffectivePartitionKey(int partitionIndex, int partitionCount, PartitionKeyDefinition partitionKeyDefinition)
		{
			if (partitionKeyDefinition.Paths.Count > 0 && partitionKeyDefinition.Kind != 0)
			{
				throw new NotImplementedException("Cannot figure out range boundaries");
			}
			if (partitionCount <= 0)
			{
				throw new ArgumentException("Invalid partition count", "partitionCount");
			}
			if (partitionIndex < 0 || partitionIndex >= partitionCount)
			{
				throw new ArgumentException("Invalid partition index", "partitionIndex");
			}
			if (partitionIndex == 0)
			{
				return MinimumInclusiveEffectivePartitionKey;
			}
			switch (partitionKeyDefinition.Version ?? PartitionKeyDefinitionVersion.V1)
			{
			case PartitionKeyDefinitionVersion.V2:
			{
				byte[] bytes = (MaxHashV2Value / partitionCount * partitionIndex).Bytes;
				Array.Reverse((Array)bytes);
				return HexConvert.ToHex(bytes, 0, bytes.Length);
			}
			case PartitionKeyDefinitionVersion.V1:
				return ToHexEncodedBinaryString(new IPartitionKeyComponent[1]
				{
					new NumberPartitionKeyComponent(4294967295L / (long)partitionCount * partitionIndex)
				});
			default:
				throw new InternalServerErrorException("Unexpected PartitionKeyDefinitionVersion");
			}
		}

		public static string GetMaxExclusiveEffectivePartitionKey(int partitionIndex, int partitionCount, PartitionKeyDefinition partitionKeyDefinition)
		{
			if (partitionKeyDefinition.Paths.Count > 0 && partitionKeyDefinition.Kind != 0)
			{
				throw new NotImplementedException("Cannot figure out range boundaries");
			}
			if (partitionCount <= 0)
			{
				throw new ArgumentException("Invalid partition count", "partitionCount");
			}
			if (partitionIndex < 0 || partitionIndex >= partitionCount)
			{
				throw new ArgumentException("Invalid partition index", "partitionIndex");
			}
			if (partitionIndex == partitionCount - 1)
			{
				return MaximumExclusiveEffectivePartitionKey;
			}
			switch (partitionKeyDefinition.Version ?? PartitionKeyDefinitionVersion.V1)
			{
			case PartitionKeyDefinitionVersion.V2:
			{
				byte[] bytes = (MaxHashV2Value / partitionCount * (partitionIndex + 1)).Bytes;
				Array.Reverse((Array)bytes);
				return HexConvert.ToHex(bytes, 0, bytes.Length);
			}
			case PartitionKeyDefinitionVersion.V1:
				return ToHexEncodedBinaryString(new IPartitionKeyComponent[1]
				{
					new NumberPartitionKeyComponent(4294967295L / (long)partitionCount * (partitionIndex + 1))
				});
			default:
				throw new InternalServerErrorException("Unexpected PartitionKeyDefinitionVersion");
			}
		}

		public int CompareTo(PartitionKeyInternal other)
		{
			if (other == null)
			{
				throw new ArgumentNullException("other");
			}
			if (other.components == null || components == null)
			{
				IReadOnlyList<IPartitionKeyComponent> readOnlyList = components;
				int num = (readOnlyList != null) ? readOnlyList.Count : 0;
				IReadOnlyList<IPartitionKeyComponent> readOnlyList2 = other.components;
				return Math.Sign(num - ((readOnlyList2 != null) ? readOnlyList2.Count : 0));
			}
			for (int i = 0; i < Math.Min(Components.Count, other.Components.Count); i++)
			{
				int typeOrdinal = Components[i].GetTypeOrdinal();
				int typeOrdinal2 = other.Components[i].GetTypeOrdinal();
				if (typeOrdinal != typeOrdinal2)
				{
					return Math.Sign(typeOrdinal - typeOrdinal2);
				}
				int num2 = Components[i].CompareTo(other.Components[i]);
				if (num2 != 0)
				{
					return Math.Sign(num2);
				}
			}
			return Math.Sign(Components.Count - other.Components.Count);
		}

		public bool Equals(PartitionKeyInternal other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return CompareTo(other) == 0;
		}

		public override bool Equals(object other)
		{
			return Equals(other as PartitionKeyInternal);
		}

		public override int GetHashCode()
		{
			return Components.Aggregate(0, (int current, IPartitionKeyComponent value) => (current * 397) ^ value.GetHashCode());
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}

		public object Clone()
		{
			return new PartitionKeyInternal(Components);
		}

		private static string ToHexEncodedBinaryString(IReadOnlyList<IPartitionKeyComponent> components)
		{
			byte[] array = new byte[336];
			using (MemoryStream memoryStream = new MemoryStream(array))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					for (int i = 0; i < components.Count; i++)
					{
						components[i].WriteForBinaryEncoding(binaryWriter);
					}
					return HexConvert.ToHex(array, 0, (int)memoryStream.Position);
				}
			}
		}

		/// <summary>
		/// Constructs a PartitionKeyInternal from hex-encoded byte string. This is only for testing/debugging. Please do not use in actual product code.
		/// </summary>
		[Obsolete]
		internal static PartitionKeyInternal FromHexEncodedBinaryString(string hexEncodedBinaryString)
		{
			List<IPartitionKeyComponent> list = new List<IPartitionKeyComponent>();
			byte[] array = HexStringToByteArray(hexEncodedBinaryString);
			int offset = 0;
			while (offset < array.Length)
			{
				switch ((PartitionKeyComponentType)Enum.Parse(typeof(PartitionKeyComponentType), array[offset++].ToString(CultureInfo.InvariantCulture)))
				{
				case PartitionKeyComponentType.Undefined:
					list.Add(UndefinedPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.Null:
					list.Add(NullPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.False:
					list.Add(new BoolPartitionKeyComponent(value: false));
					break;
				case PartitionKeyComponentType.True:
					list.Add(new BoolPartitionKeyComponent(value: true));
					break;
				case PartitionKeyComponentType.MinNumber:
					list.Add(MinNumberPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.MaxNumber:
					list.Add(MaxNumberPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.MinString:
					list.Add(MinStringPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.MaxString:
					list.Add(MaxStringPartitionKeyComponent.Value);
					break;
				case PartitionKeyComponentType.Infinity:
					list.Add(new InfinityPartitionKeyComponent());
					break;
				case PartitionKeyComponentType.Number:
					list.Add(NumberPartitionKeyComponent.FromHexEncodedBinaryString(array, ref offset));
					break;
				case PartitionKeyComponentType.String:
					list.Add(StringPartitionKeyComponent.FromHexEncodedBinaryString(array, ref offset));
					break;
				}
			}
			return new PartitionKeyInternal(list);
		}

		/// <summary>
		/// Produces effective value. Azure Cosmos DB has global index on effective partition key values.
		///
		/// Effective value is produced by applying is range or hash encoding to all the component values, based
		/// on partition key definition.
		///
		/// String components are hashed and converted to number components.
		/// Number components are hashed and remain number component.
		/// bool, null, undefined remain unhashed, because indexing policy doesn't specify index type for these types.
		/// </summary>
		public string GetEffectivePartitionKeyString(PartitionKeyDefinition partitionKeyDefinition, bool strict = true)
		{
			if (components == null)
			{
				throw new ArgumentException(RMResources.TooFewPartitionKeyComponents);
			}
			if (Equals(EmptyPartitionKey))
			{
				return MinimumInclusiveEffectivePartitionKey;
			}
			if (Equals(InfinityPartitionKey))
			{
				return MaximumExclusiveEffectivePartitionKey;
			}
			if (Components.Count < partitionKeyDefinition.Paths.Count)
			{
				throw new ArgumentException(RMResources.TooFewPartitionKeyComponents);
			}
			if (Components.Count > partitionKeyDefinition.Paths.Count && strict)
			{
				throw new ArgumentException(RMResources.TooManyPartitionKeyComponents);
			}
			if (partitionKeyDefinition.Kind == PartitionKind.Hash)
			{
				switch (partitionKeyDefinition.Version ?? PartitionKeyDefinitionVersion.V1)
				{
				case PartitionKeyDefinitionVersion.V1:
					return GetEffectivePartitionKeyForHashPartitioning();
				case PartitionKeyDefinitionVersion.V2:
					return GetEffectivePartitionKeyForHashPartitioningV2();
				default:
					throw new InternalServerErrorException("Unexpected PartitionKeyDefinitionVersion");
				}
			}
			return ToHexEncodedBinaryString(Components);
		}

		private string GetEffectivePartitionKeyForHashPartitioning()
		{
			IPartitionKeyComponent[] array = Components.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Components[i].Truncate();
			}
			double value;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					for (int j = 0; j < array.Length; j++)
					{
						array[j].WriteForHashing(binaryWriter);
					}
					value = MurmurHash3.Hash32(CustomTypeExtensions.GetBuffer(memoryStream), memoryStream.Length);
				}
			}
			IPartitionKeyComponent[] array2 = new IPartitionKeyComponent[Components.Count + 1];
			array2[0] = new NumberPartitionKeyComponent(value);
			for (int k = 0; k < array.Length; k++)
			{
				array2[k + 1] = array[k];
			}
			return ToHexEncodedBinaryString(array2);
		}

		private string GetEffectivePartitionKeyForHashPartitioningV2()
		{
			byte[] array = null;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					for (int i = 0; i < Components.Count; i++)
					{
						Components[i].WriteForHashingV2(binaryWriter);
					}
					array = UInt128.ToByteArray(MurmurHash3.Hash128(CustomTypeExtensions.GetBuffer(memoryStream), (int)memoryStream.Length, UInt128.MinValue));
					Array.Reverse((Array)array);
					array[0] &= 63;
				}
			}
			return HexConvert.ToHex(array, 0, array.Length);
		}

		private static bool IsNumeric(object value)
		{
			if (!(value is sbyte) && !(value is byte) && !(value is short) && !(value is ushort) && !(value is int) && !(value is uint) && !(value is long) && !(value is ulong) && !(value is float) && !(value is double))
			{
				return value is decimal;
			}
			return true;
		}

		private static byte[] HexStringToByteArray(string hex)
		{
			int length = hex.Length;
			if (length % 2 != 0)
			{
				throw new ArgumentException("Hex string should be even length", "hex");
			}
			byte[] array = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
			{
				array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return array;
		}

		public static string GetMiddleRangeEffectivePartitionKey(string minInclusive, string maxExclusive, PartitionKeyDefinition partitionKeyDefinition)
		{
			if (partitionKeyDefinition.Kind != 0)
			{
				throw new InvalidOperationException("Can determine middle of range only for hash partitioning.");
			}
			switch (partitionKeyDefinition.Version ?? PartitionKeyDefinitionVersion.V1)
			{
			case PartitionKeyDefinitionVersion.V2:
			{
				Int128 @int = 0;
				if (!minInclusive.Equals(MinimumInclusiveEffectivePartitionKey, StringComparison.Ordinal))
				{
					byte[] array = HexStringToByteArray(minInclusive);
					Array.Reverse((Array)array);
					@int = new Int128(array);
				}
				Int128 left = MaxHashV2Value;
				if (!maxExclusive.Equals(MaximumExclusiveEffectivePartitionKey, StringComparison.Ordinal))
				{
					byte[] array2 = HexStringToByteArray(maxExclusive);
					Array.Reverse((Array)array2);
					left = new Int128(array2);
				}
				byte[] bytes = (@int + (left - @int) / 2).Bytes;
				Array.Reverse((Array)bytes);
				return HexConvert.ToHex(bytes, 0, bytes.Length);
			}
			case PartitionKeyDefinitionVersion.V1:
			{
        		#pragma warning disable 612, 618
				long num = 0L;
				long num2 = 4294967295L;
				if (!minInclusive.Equals(MinimumInclusiveEffectivePartitionKey, StringComparison.Ordinal))
				{
					num = (long)((NumberPartitionKeyComponent)FromHexEncodedBinaryString(minInclusive).Components[0]).Value;
				}
				if (!maxExclusive.Equals(MaximumExclusiveEffectivePartitionKey, StringComparison.Ordinal))
				{
					num2 = (long)((NumberPartitionKeyComponent)FromHexEncodedBinaryString(maxExclusive).Components[0]).Value;
				}
				return ToHexEncodedBinaryString(new NumberPartitionKeyComponent[1]
				{
					new NumberPartitionKeyComponent((num + num2) / 2)
				});
        		#pragma warning restore 612, 618
			}
			default:
				throw new InternalServerErrorException("Unexpected PartitionKeyDefinitionVersion");
			}
		}
	}
}
