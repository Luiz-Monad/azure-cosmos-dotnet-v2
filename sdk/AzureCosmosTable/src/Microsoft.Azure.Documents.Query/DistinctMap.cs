using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// Base class for all types of DistinctMaps.
	/// An IDistinctMap is able to efficiently store a hash set of json values.
	/// This is done by taking the json value and storing a GUID like hash of that value in a hashset.
	/// By storing the hash we avoid storing the entire object in main memory.
	/// Only downside is that there is a possibility of a hash collision.
	/// However we store the hash as 192 bits, so the possibility of a collision is pretty low.
	/// You can run the birthday paradox math to figure out how low: https://en.wikipedia.org/wiki/Birthday_problem
	/// </summary>
	/// <summary>
	/// Partial wrapper
	/// </summary>
	/// <summary>
	/// Partial wrapper
	/// </summary>
	internal abstract class DistinctMap
	{
		/// <summary>
		/// Base class for DistinctHash.
		/// This class is able to take hashes with seeded values.
		/// </summary>
		private sealed class DistinctHash
		{
			/// <summary>
			/// The seeds to use for hashing different json types.
			/// </summary>
			public struct HashSeeds
			{
				/// <summary>
				/// Gets the seed used for the JSON root.
				/// </summary>
				public UInt192 Root
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON null values.
				/// </summary>
				public UInt192 Null
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON false values.
				/// </summary>
				public UInt192 False
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON true values.
				/// </summary>
				public UInt192 True
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON number values.
				/// </summary>
				public UInt192 Number
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON string values.
				/// </summary>
				public UInt192 String
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON array values.
				/// </summary>
				public UInt192 Array
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON object values.
				/// </summary>
				public UInt192 Object
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON array elements.
				/// </summary>
				public UInt192 ArrayIndex
				{
					get;
				}

				/// <summary>
				/// Gets the seed used for JSON property names.
				/// </summary>
				public UInt192 PropertyName
				{
					get;
				}

				/// <summary>
				/// Initializes a new instance of the HashSeeds struct.
				/// </summary>
				/// <param name="rootHashSeed">The seed used for the JSON root.</param>
				/// <param name="nullHashSeed">The seed used for JSON null values.</param>
				/// <param name="falseHashSeed">The seed used for JSON false values.</param>
				/// <param name="trueHashSeed">The seed used for JSON true values.</param>
				/// <param name="numberHashSeed">The seed used for JSON number values.</param>
				/// <param name="stringHashSeed">The seed used for JSON string values.</param>
				/// <param name="arrayHashSeed">The seed used for JSON array values.</param>
				/// <param name="objectHashSeed">The seed used for JSON object values.</param>
				/// <param name="arrayIndexHashSeed">The seed used for JSON array elements.</param>
				/// <param name="propertyNameHashSeed">The seed used for JSON property names.</param>
				public HashSeeds(UInt192 rootHashSeed, UInt192 nullHashSeed, UInt192 falseHashSeed, UInt192 trueHashSeed, UInt192 numberHashSeed, UInt192 stringHashSeed, UInt192 arrayHashSeed, UInt192 objectHashSeed, UInt192 arrayIndexHashSeed, UInt192 propertyNameHashSeed)
				{
					Root = rootHashSeed;
					Null = nullHashSeed;
					False = falseHashSeed;
					True = trueHashSeed;
					Number = numberHashSeed;
					String = stringHashSeed;
					Array = arrayHashSeed;
					Object = objectHashSeed;
					ArrayIndex = arrayIndexHashSeed;
					PropertyName = propertyNameHashSeed;
				}
			}

			/// <summary>
			/// Singleton for DistinctHash.
			/// </summary>
			/// <remarks>All the hashseeds have to be different.</remarks>
			public static readonly DistinctHash Value = new DistinctHash(new HashSeeds(UInt192.Create(15992047605174166762uL, 11514953832815889395uL, 4478289918784009099uL), UInt192.Create(12278193463599318509uL, 13575553817073638549uL, 12367631068517817386uL), UInt192.Create(1429697140260995089uL, 12058514231935786610uL, 15213120655323614500uL), UInt192.Create(4787996480607579680uL, 5989861620227925760uL, 6910469410427058089uL), UInt192.Create(13151337925362354973uL, 1067564635031500007uL, 6409491933319599190uL), UInt192.Create(7496206448276951651uL, 676088056534543826uL, 12486218591645107815uL), UInt192.Create(16779609907233277819uL, 7151985556769501107uL, 16729802325368678337uL), UInt192.Create(2859240458868302001uL, 12963811524562026943uL, 7949939114162109242uL), UInt192.Create(17548991362029881809uL, 6875091296931507438uL, 14055947870195278658uL), UInt192.Create(18405052666065471316uL, 14772279679115199733uL, 5168031347707020066uL)));

			/// <summary>
			/// Length of a UInt192 in bits
			/// </summary>
			private const int UInt192LengthInBits = 192;

			/// <summary>
			/// The number of bits in a byte.
			/// </summary>
			private const int BitsPerByte = 8;

			/// <summary>
			/// Length of a UInt192 in bytes.
			/// </summary>
			private const int UInt192LengthInBytes = 24;

			/// <summary>
			/// Gets the HashSeeds for this type.
			/// </summary>
			public HashSeeds HashSeedValues
			{
				get;
			}

			/// <summary>
			/// Initializes a new instance of the DistinctHash class.
			/// </summary>
			/// <param name="hashSeeds">The hash seeds to use.</param>
			private DistinctHash(HashSeeds hashSeeds)
			{
				HashSeedValues = hashSeeds;
			}

			/// <summary>
			/// Gets the hash given a value and a seed.
			/// </summary>
			/// <param name="value">The value to hash.</param>
			/// <param name="seed">The seed.</param>
			/// <returns>The hash.</returns>
			public UInt192 GetHash(UInt192 value, UInt192 seed)
			{
				return GetHash(UInt192.ToByteArray(value), seed);
			}

			/// <summary>
			/// Gets the hash of a byte array.
			/// </summary>
			/// <param name="bytes">The bytes.</param>
			/// <param name="seed">The seed.</param>
			/// <returns>The hash.</returns>
			public UInt192 GetHash(byte[] bytes, UInt192 seed)
			{
				UInt128 uInt = MurmurHash3.Hash128(bytes, bytes.Length, UInt128.Create(seed.GetLow(), seed.GetMid()));
				ulong high = MurmurHash3.Hash64(bytes, bytes.Length, seed.GetHigh());
				return UInt192.Create(uInt.GetLow(), uInt.GetHigh(), high);
			}

			/// <summary>
			/// Gets the hash of a JToken value.
			/// </summary>
			/// <param name="value">The JToken to hash.</param>
			/// <returns>The hash of the JToken.</returns>
			public UInt192 GetHashToken(JToken value)
			{
				return GetHashToken(value, HashSeedValues.Root);
			}

			/// <summary>
			/// Gets the hash of a JToken given a seed.
			/// </summary>
			/// <param name="value">The JToken to hash.</param>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of the JToken.</returns>
			private UInt192 GetHashToken(JToken value, UInt192 seed)
			{
				if (value == Undefined)
				{
					return seed;
				}
				JTokenType type = value.Type;
				switch (type)
				{
				case JTokenType.Object:
					return GetObjectHash((JObject)value, seed);
				case JTokenType.Array:
					return GetArrayHash((JArray)value, seed);
				case JTokenType.Integer:
				case JTokenType.Float:
					return GetNumberHash((double)value, seed);
				case JTokenType.String:
				case JTokenType.Date:
				case JTokenType.Guid:
				case JTokenType.Uri:
				case JTokenType.TimeSpan:
					return GetStringHash(value.ToString(), seed);
				case JTokenType.Boolean:
					return GetBooleanHash((bool)value, seed);
				case JTokenType.Null:
					return GetNullHash(seed);
				case JTokenType.None:
				case JTokenType.Undefined:
					return GetUndefinedHash(seed);
				default:
					throw new ArgumentException($"Unexpected JTokenType of: {type}");
				}
			}

			/// <summary>
			/// Gets the hash of a undefined JSON value.
			/// </summary>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a undefined JSON value.</returns>
			private UInt192 GetUndefinedHash(UInt192 seed)
			{
				return seed;
			}

			/// <summary>
			/// Gets the hash of a null JSON value.
			/// </summary>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a null JSON value given a seed.</returns>
			private UInt192 GetNullHash(UInt192 seed)
			{
				return GetHash(HashSeedValues.Null, seed);
			}

			/// <summary>
			/// Gets the hash of a boolean JSON value.
			/// </summary>
			/// <param name="boolean">The boolean to hash.</param>
			/// <param name="seed">The seed.</param>
			/// <returns>The hash of a boolean JSON value.</returns>
			private UInt192 GetBooleanHash(bool boolean, UInt192 seed)
			{
				return GetHash(boolean ? HashSeedValues.True : HashSeedValues.False, seed);
			}

			/// <summary>
			/// Gets the hash of a JSON number value.
			/// </summary>
			/// <param name="number">The number to hash.</param>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a JSON number value.</returns>
			private UInt192 GetNumberHash(double number, UInt192 seed)
			{
				UInt192 hash = GetHash(HashSeedValues.Number, seed);
				return GetHash(BitConverter.DoubleToInt64Bits(number), hash);
			}

			/// <summary>
			/// Gets the hash of a JSON string value.
			/// </summary>
			/// <param name="value">The value to hash.</param>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a JSON string value.</returns>
			private UInt192 GetStringHash(string value, UInt192 seed)
			{
				UInt192 hash = GetHash(HashSeedValues.String, seed);
				byte[] bytes = Encoding.UTF8.GetBytes(value);
				return GetHash(bytes, hash);
			}

			/// <summary>
			/// Gets the hash of a JSON array.
			/// </summary>
			/// <param name="array">The array to hash.</param>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a JSON array.</returns>
			private UInt192 GetArrayHash(JArray array, UInt192 seed)
			{
				UInt192 hash = GetHash(HashSeedValues.Array, seed);
				for (int i = 0; i < array.Count; i++)
				{
					JToken value = array[i];
					UInt192 seed2 = HashSeedValues.ArrayIndex + i;
					hash = GetHash(hash, GetHashToken(value, seed2));
				}
				return hash;
			}

			/// <summary>
			/// Gets the hash of a JSON object.
			/// </summary>
			/// <param name="jObject">The object to hash.</param>
			/// <param name="seed">The seed to use.</param>
			/// <returns>The hash of a JSON object.</returns>
			private UInt192 GetObjectHash(JObject jObject, UInt192 seed)
			{
				UInt192 hash = GetHash(HashSeedValues.Object, seed);
				UInt192 uInt = 0;
				foreach (KeyValuePair<string, JToken> item in jObject)
				{
					UInt192 hashToken = GetHashToken(item.Key, HashSeedValues.PropertyName);
					UInt192 hashToken2 = GetHashToken(item.Value, hashToken);
					uInt ^= hashToken2;
				}
				if (uInt > 0)
				{
					hash = GetHash(uInt, hash);
				}
				return hash;
			}
		}

		/// <summary>
		/// For distinct queries of the form:
		/// SELECT DISTINCT VALUE c.(blah) from c order by c.(blah)
		/// We can make an optimization, since the problem boils down to
		/// "How can you find all the distinct items in a sorted stream"
		/// Ex. "1, 1, 2, 2, 2, 3, 4, 4" -&gt; "1, 2, 3, 4"
		/// The solution is that you only need to remember the previous item of the stream:
		/// foreach item in stream:
		///     if item != previous item:
		///         yield item
		/// This class accomplishes that by storing the previous hash and assuming the items come in sorted order.
		/// </summary>
		private sealed class OrderedDistinctMap : DistinctMap
		{
			/// <summary>
			/// The hash of the last item that was added to this distinct map.
			/// </summary>
			private UInt192 lastHash;

			/// <summary>
			/// Initializes a new instance of the OrderedDistinctMap class.
			/// </summary>
			/// <param name="lastHash">The previous hash from the previous continuation.</param>
			public OrderedDistinctMap(UInt192 lastHash)
			{
				this.lastHash = lastHash;
			}

			/// <summary>
			/// Adds a JToken to this map if it hasn't already been added.
			/// </summary>
			/// <param name="jToken">The token to add.</param>
			/// <param name="hash">The hash of the token.</param>
			/// <returns>Whether or not the item was added to this Distinct Map.</returns>
			/// <remarks>This function assumes data is added in sorted order.</remarks>
			public override bool Add(JToken jToken, out UInt192? hash)
			{
				hash = GetHash(jToken);
				UInt192 value = lastHash;
				UInt192? right = hash;
				if (value != right)
				{
					lastHash = hash.Value;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Flags for all the simple json values, so that we don't need a separate hash for them.
		/// </summary>
		[Flags]
		private enum SimpleValues
		{
			/// <summary>
			/// None JSON Value.
			/// </summary>
			None = 0x0,
			/// <summary>
			/// Undefined JSON Value.
			/// </summary>
			Undefined = 0x1,
			/// <summary>
			/// Null JSON Value.
			/// </summary>
			Null = 0x2,
			/// <summary>
			/// False JSON Value.
			/// </summary>
			False = 0x4,
			/// <summary>
			/// True JSON Value.
			/// </summary>
			True = 0x8,
			/// <summary>
			/// Empty String.
			/// </summary>
			EmptyString = 0x10,
			/// <summary>
			/// Empty Array.
			/// </summary>
			EmptyArray = 0x20,
			/// <summary>
			/// Empty Object.
			/// </summary>
			EmptyObject = 0x40
		}

		/// <summary>
		/// For distinct queries we need to keep a running hash set of all the documents seen.
		/// You can read more about this in DistinctDocumentQueryExecutionComponent.cs.
		/// This class does that with the additional optimization that it doesn't store the whole JSON.
		/// Instead this class takes a GUID like hash and store that instead.
		/// </summary>
		private sealed class UnorderdDistinctMap : DistinctMap
		{
			/// <summary>
			/// Length of UInt192 (in bytes).
			/// </summary>
			private const int UInt192Length = 24;

			/// <summary>
			/// Length of UInt128 (in bytes).
			/// </summary>
			private const int UInt128Length = 16;

			/// <summary>
			/// Length of ulong (in bytes).
			/// </summary>
			private const int ULongLength = 8;

			/// <summary>
			/// Length of uint (in bytes).
			/// </summary>
			private const int UIntLength = 4;

			/// <summary>
			/// Buffer that gets reused to convert a .net string (utf-16) to a (utf-8) byte array.
			/// </summary>
			private readonly byte[] utf8Buffer = new byte[24];

			/// <summary>
			/// HashSet for all numbers seen.
			/// This takes less space than a 24 byte hash and has full fidelity.
			/// </summary>
			private readonly HashSet<double> numbers = new HashSet<double>();

			/// <summary>
			/// HashSet for all strings seen of length less than or equal to 4 stored as a uint.
			/// This takes less space than a 24 byte hash and has full fidelity.
			/// </summary>
			private readonly HashSet<uint> stringsLength4 = new HashSet<uint>();

			/// <summary>
			/// HashSet for all strings seen of length less than or equal to 8 stored as a ulong.
			/// This takes less space than a 24 byte hash and has full fidelity.
			/// </summary>
			private readonly HashSet<ulong> stringLength8 = new HashSet<ulong>();

			/// <summary>
			/// HashSet for all strings of length less than or equal to 16 stored as a UInt128.
			/// This takes less space than a 24 byte hash and has full fidelity.
			/// </summary>
			private readonly HashSet<UInt128> stringLength16 = new HashSet<UInt128>();

			/// <summary>
			/// HashSet for all strings seen of length less than or equal to 24 stored as a UInt192.
			/// This takes the same space as 24 byte hash and has full fidelity.
			/// </summary>
			private readonly HashSet<UInt192> stringLength24 = new HashSet<UInt192>();

			/// <summary>
			/// HashSet for all strings seen of length greater than 24 stored as a UInt192.
			/// This set only stores the hash, since we don't want to spend the space for storing large strings.
			/// </summary>
			private readonly HashSet<UInt192> stringLength24Plus = new HashSet<UInt192>();

			/// <summary>
			/// HashSet for all arrays seen.
			/// This set only stores the hash, since we don't want to spend the space for storing large arrays.
			/// </summary>
			private readonly HashSet<UInt192> arrays = new HashSet<UInt192>();

			/// <summary>
			/// HashSet for all object seen.
			/// This set only stores the hash, since we don't want to spend the space for storing large objects.
			/// </summary>
			private readonly HashSet<UInt192> objects = new HashSet<UInt192>();

			/// <summary>
			/// Stores all the simple values that we don't want to dedicate a hash set for.
			/// </summary>
			private SimpleValues simpleValues;

			/// <summary>
			/// Adds a JToken to this map if it hasn't already been added.
			/// </summary>
			/// <param name="jToken">The token to add.</param>
			/// <param name="hash">The hash of the token.</param>
			/// <returns>Whether or not the item was added to this Distinct Map.</returns>
			public override bool Add(JToken jToken, out UInt192? hash)
			{
				hash = null;
				if (jToken == Undefined)
				{
					return AddSimpleValue(SimpleValues.Undefined);
				}
				JTokenType type = jToken.Type;
				switch (type)
				{
				case JTokenType.Object:
					return AddObjectValue((JObject)jToken);
				case JTokenType.Array:
					return AddArrayValue((JArray)jToken);
				case JTokenType.Integer:
				case JTokenType.Float:
					return AddNumberValue((double)jToken);
				case JTokenType.String:
				case JTokenType.Date:
				case JTokenType.Guid:
				case JTokenType.Uri:
				case JTokenType.TimeSpan:
					return AddStringValue(jToken.ToString());
				case JTokenType.Boolean:
					return AddSimpleValue(((bool)jToken) ? SimpleValues.True : SimpleValues.False);
				case JTokenType.Null:
					return AddSimpleValue(SimpleValues.Null);
				default:
					throw new ArgumentException($"Unexpected JTokenType of: {type}");
				}
			}

			/// <summary>
			/// Adds a number value to the map.
			/// </summary>
			/// <param name="value">The value to add.</param>
			/// <returns>Whether or not the value was successfully added.</returns>
			private bool AddNumberValue(double value)
			{
				return numbers.Add(value);
			}

			/// <summary>
			/// Adds a simple value to the map.
			/// </summary>
			/// <param name="value">The simple value.</param>
			/// <returns>Whether or not the value was successfully added.</returns>
			private bool AddSimpleValue(SimpleValues value)
			{
				if ((simpleValues & value) == SimpleValues.None)
				{
					simpleValues |= value;
					return true;
				}
				return false;
			}

			/// <summary>
			/// Adds a string to the distinct map.
			/// </summary>
			/// <param name="value">The string to add.</param>
			/// <returns>Whether or not the value was successfully added.</returns>
			private bool AddStringValue(string value)
			{
				int byteCount = Encoding.UTF8.GetByteCount(value);
				if (byteCount <= 24)
				{
					Array.Clear(utf8Buffer, 0, utf8Buffer.Length);
					Encoding.UTF8.GetBytes(value, 0, byteCount, utf8Buffer, 0);
					if (byteCount == 0)
					{
						return AddSimpleValue(SimpleValues.EmptyString);
					}
					if (byteCount <= 4)
					{
						uint item = BitConverter.ToUInt32(utf8Buffer, 0);
						return stringsLength4.Add(item);
					}
					if (byteCount <= 8)
					{
						ulong item2 = BitConverter.ToUInt64(utf8Buffer, 0);
						return stringLength8.Add(item2);
					}
					if (byteCount <= 16)
					{
						UInt128 item3 = UInt128.FromByteArray(utf8Buffer);
						return stringLength16.Add(item3);
					}
					UInt192 item4 = UInt192.FromByteArray(utf8Buffer);
					return stringLength24.Add(item4);
				}
				UInt192 hash = GetHash(value);
				return stringLength24Plus.Add(hash);
			}

			/// <summary>
			/// Adds an array value to the distinct map.
			/// </summary>
			/// <param name="array">The array to add.</param>
			/// <returns>Whether or not the value was successfully added.</returns>
			private bool AddArrayValue(JArray array)
			{
				UInt192 hash = GetHash(array);
				return arrays.Add(hash);
			}

			/// <summary>
			/// Adds an object value to the distinct map.
			/// </summary>
			/// <param name="jObject">The object to add.</param>
			/// <returns>Whether or not the value was successfully added.</returns>
			private bool AddObjectValue(JObject jObject)
			{
				UInt192 hash = GetHash(jObject);
				return objects.Add(hash);
			}
		}

		private static readonly JToken Undefined = null;

		/// <summary>
		/// Creates an IDistinctMap based on the type.
		/// </summary>
		/// <param name="distinctQueryType">The type of distinct query.</param>
		/// <param name="previousHash">The hash of the previous value successfully inserted into this DistinctMap</param>
		/// <returns>The appropriate IDistinctMap.</returns>
		public static DistinctMap Create(DistinctQueryType distinctQueryType, UInt192? previousHash)
		{
			switch (distinctQueryType)
			{
			case DistinctQueryType.None:
				throw new ArgumentException("distinctQueryType can not be None. This part of code is not supposed to be reachable. Please contact support to resolve this issue.");
			case DistinctQueryType.Unordered:
				return new UnorderdDistinctMap();
			case DistinctQueryType.Ordered:
				return new OrderedDistinctMap(previousHash.GetValueOrDefault());
			default:
				throw new ArgumentException($"Unrecognized DistinctQueryType: {distinctQueryType}.");
			}
		}

		/// <summary>
		/// Adds a JToken to this DistinctMap.
		/// </summary>
		/// <param name="jToken">The token to add.</param>
		/// <param name="hash">The hash of the token.</param>
		/// <returns>Whether or not the token was successfully added.</returns>
		public abstract bool Add(JToken jToken, out UInt192? hash);

		/// <summary>
		/// Gets the hash of a JToken.
		/// </summary>
		/// <param name="jToken">The token to hash.</param>
		/// <returns>The hash of the JToken.</returns>
		protected static UInt192 GetHash(JToken jToken)
		{
			return DistinctHash.Value.GetHashToken(jToken);
		}
	}
}
