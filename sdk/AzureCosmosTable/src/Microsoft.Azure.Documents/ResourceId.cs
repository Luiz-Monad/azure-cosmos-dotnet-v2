using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class ResourceId : IEquatable<ResourceId>
	{
		private enum CollectionChildResourceType : byte
		{
			Document = 0,
			StoredProcedure = 8,
			Trigger = 7,
			UserDefinedFunction = 6,
			Conflict = 4,
			PartitionKeyRange = 5,
			Schema = 9
		}

		private enum ExtendedDatabaseChildResourceType
		{
			UserDefinedType = 1
		}

		private const int OfferIdLength = 3;

		public static readonly ushort Length = 20;

		public static readonly ushort MaxPathFragment = 8;

		public static readonly ResourceId Empty = new ResourceId();

		public uint Offer
		{
			get;
			private set;
		}

		public ResourceId OfferId => new ResourceId
		{
			Offer = Offer
		};

		public uint Database
		{
			get;
			private set;
		}

		public ResourceId DatabaseId => new ResourceId
		{
			Database = Database
		};

		public bool IsDatabaseId
		{
			get
			{
				if (Database != 0)
				{
					if (DocumentCollection == 0 && User == 0)
					{
						return UserDefinedType == 0;
					}
					return false;
				}
				return false;
			}
		}

		public bool IsDocumentCollectionId
		{
			get
			{
				if (Database != 0 && DocumentCollection != 0)
				{
					if (Document == 0L && StoredProcedure == 0L && Trigger == 0L)
					{
						return UserDefinedFunction == 0;
					}
					return false;
				}
				return false;
			}
		}

		public uint DocumentCollection
		{
			get;
			private set;
		}

		public ResourceId DocumentCollectionId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection
		};

		/// <summary>
		/// Unique (across all databases) Id for the DocumentCollection.
		/// First 4 bytes are DatabaseId and next 4 bytes are CollectionId.
		/// </summary>
		public ulong UniqueDocumentCollectionId => ((ulong)Database << 32) | DocumentCollection;

		public ulong StoredProcedure
		{
			get;
			private set;
		}

		public ResourceId StoredProcedureId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			StoredProcedure = StoredProcedure
		};

		public ulong Trigger
		{
			get;
			private set;
		}

		public ResourceId TriggerId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			Trigger = Trigger
		};

		public ulong UserDefinedFunction
		{
			get;
			private set;
		}

		public ResourceId UserDefinedFunctionId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			UserDefinedFunction = UserDefinedFunction
		};

		public ulong Conflict
		{
			get;
			private set;
		}

		public ResourceId ConflictId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			Conflict = Conflict
		};

		public ulong Document
		{
			get;
			private set;
		}

		public ResourceId DocumentId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			Document = Document
		};

		public ulong PartitionKeyRange
		{
			get;
			private set;
		}

		public ResourceId PartitionKeyRangeId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			PartitionKeyRange = PartitionKeyRange
		};

		public uint User
		{
			get;
			private set;
		}

		public ResourceId UserId => new ResourceId
		{
			Database = Database,
			User = User
		};

		public uint UserDefinedType
		{
			get;
			private set;
		}

		public ResourceId UserDefinedTypeId => new ResourceId
		{
			Database = Database,
			UserDefinedType = UserDefinedType
		};

		public ulong Permission
		{
			get;
			private set;
		}

		public ResourceId PermissionId => new ResourceId
		{
			Database = Database,
			User = User,
			Permission = Permission
		};

		public uint Attachment
		{
			get;
			private set;
		}

		public ResourceId AttachmentId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			Document = Document,
			Attachment = Attachment
		};

		public ulong Schema
		{
			get;
			private set;
		}

		public ResourceId SchemaId => new ResourceId
		{
			Database = Database,
			DocumentCollection = DocumentCollection,
			Schema = Schema
		};

		public byte[] Value
		{
			get
			{
				int num = 0;
				if (Offer != 0)
				{
					num += 3;
				}
				else if (Database != 0)
				{
					num += 4;
				}
				if (DocumentCollection != 0 || User != 0 || UserDefinedType != 0)
				{
					num += 4;
				}
				if (Document != 0 || Permission != 0 || StoredProcedure != 0 || Trigger != 0 || UserDefinedFunction != 0 || Conflict != 0 || PartitionKeyRange != 0 || Schema != 0 || UserDefinedType != 0)
				{
					num += 8;
				}
				if (Attachment != 0)
				{
					num += 4;
				}
				byte[] array = new byte[num];
				if (Offer != 0)
				{
					BlockCopy(BitConverter.GetBytes(Offer), 0, array, 0, 3);
				}
				else if (Database != 0)
				{
					BlockCopy(BitConverter.GetBytes(Database), 0, array, 0, 4);
				}
				if (DocumentCollection != 0)
				{
					BlockCopy(BitConverter.GetBytes(DocumentCollection), 0, array, 4, 4);
				}
				else if (User != 0)
				{
					BlockCopy(BitConverter.GetBytes(User), 0, array, 4, 4);
				}
				if (StoredProcedure != 0)
				{
					BlockCopy(BitConverter.GetBytes(StoredProcedure), 0, array, 8, 8);
				}
				else if (Trigger != 0)
				{
					BlockCopy(BitConverter.GetBytes(Trigger), 0, array, 8, 8);
				}
				else if (UserDefinedFunction != 0)
				{
					BlockCopy(BitConverter.GetBytes(UserDefinedFunction), 0, array, 8, 8);
				}
				else if (Conflict != 0)
				{
					BlockCopy(BitConverter.GetBytes(Conflict), 0, array, 8, 8);
				}
				else if (Document != 0)
				{
					BlockCopy(BitConverter.GetBytes(Document), 0, array, 8, 8);
				}
				else if (PartitionKeyRange != 0)
				{
					BlockCopy(BitConverter.GetBytes(PartitionKeyRange), 0, array, 8, 8);
				}
				else if (Permission != 0)
				{
					BlockCopy(BitConverter.GetBytes(Permission), 0, array, 8, 8);
				}
				else if (Schema != 0)
				{
					BlockCopy(BitConverter.GetBytes(Schema), 0, array, 8, 8);
				}
				else if (UserDefinedType != 0)
				{
					BlockCopy(BitConverter.GetBytes(UserDefinedType), 0, array, 8, 4);
					BlockCopy(BitConverter.GetBytes(1u), 0, array, 12, 4);
				}
				if (Attachment != 0)
				{
					BlockCopy(BitConverter.GetBytes(Attachment), 0, array, 16, 4);
				}
				return array;
			}
		}

		private ResourceId()
		{
			Offer = 0u;
			Database = 0u;
			DocumentCollection = 0u;
			StoredProcedure = 0uL;
			Trigger = 0uL;
			UserDefinedFunction = 0uL;
			Document = 0uL;
			PartitionKeyRange = 0uL;
			User = 0u;
			Permission = 0uL;
			Attachment = 0u;
			Schema = 0uL;
			UserDefinedType = 0u;
		}

		public static ResourceId Parse(string id)
		{
			ResourceId rid = null;
			if (!TryParse(id, out rid))
			{
				throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResourceID, id));
			}
			return rid;
		}

		public static byte[] Parse(ResourceType eResourceType, string id)
		{
			if (HasNonHierarchicalResourceId(eResourceType))
			{
				return Encoding.UTF8.GetBytes(id);
			}
			return Parse(id).Value;
		}

		public static ResourceId NewDatabaseId(uint dbid)
		{
			return new ResourceId
			{
				Database = dbid
			};
		}

		public static ResourceId NewDocumentCollectionId(string databaseId, uint collectionId)
		{
			return NewDocumentCollectionId(Parse(databaseId).Database, collectionId);
		}

		public static ResourceId NewDocumentCollectionId(uint databaseId, uint collectionId)
		{
			return new ResourceId
			{
				Database = databaseId,
				DocumentCollection = collectionId
			};
		}

		public static ResourceId NewUserId(string databaseId, uint userId)
		{
			ResourceId resourceId = Parse(databaseId);
			return new ResourceId
			{
				Database = resourceId.Database,
				User = userId
			};
		}

		public static ResourceId NewPermissionId(string userId, ulong permissionId)
		{
			ResourceId resourceId = Parse(userId);
			return new ResourceId
			{
				Database = resourceId.Database,
				User = resourceId.User,
				Permission = permissionId
			};
		}

		public static ResourceId NewDocumentId(string collectionId, uint documentId)
		{
			ResourceId resourceId = Parse(collectionId);
			return new ResourceId
			{
				Database = resourceId.Database,
				DocumentCollection = resourceId.DocumentCollection,
				Document = documentId
			};
		}

		public static ResourceId NewAttachmentId(string documentId, uint attachmentId)
		{
			ResourceId resourceId = Parse(documentId);
			return new ResourceId
			{
				Database = resourceId.Database,
				DocumentCollection = resourceId.DocumentCollection,
				Document = resourceId.Document,
				Attachment = attachmentId
			};
		}

		public static ResourceId NewPartitionKeyRangeId(string collectionId, ulong partitionKeyRangeId)
		{
			ResourceId resourceId = Parse(collectionId);
			return new ResourceId
			{
				Database = resourceId.Database,
				DocumentCollection = resourceId.DocumentCollection,
				PartitionKeyRange = partitionKeyRangeId
			};
		}

		public static bool TryParse(string id, out ResourceId rid)
		{
			rid = null;
			try
			{
				if (string.IsNullOrEmpty(id))
				{
					return false;
				}
				if (id.Length % 4 != 0)
				{
					return false;
				}
				byte[] buffer = null;
				if (!Verify(id, out buffer))
				{
					return false;
				}
				if (buffer.Length % 4 != 0 && buffer.Length != 3)
				{
					return false;
				}
				rid = new ResourceId();
				if (buffer.Length == 3)
				{
					rid.Offer = 0u;
					for (int i = 0; i < 3; i++)
					{
						rid.Offer |= (uint)(buffer[i] << i * 8);
					}
					return true;
				}
				if (buffer.Length >= 4)
				{
					rid.Database = BitConverter.ToUInt32(buffer, 0);
				}
				if (buffer.Length >= 8)
				{
					byte[] array = new byte[4];
					BlockCopy(buffer, 4, array, 0, 4);
					if (((array[0] & 0x80) > 0) ? true : false)
					{
						rid.DocumentCollection = BitConverter.ToUInt32(array, 0);
						if (buffer.Length >= 16)
						{
							byte[] array2 = new byte[8];
							BlockCopy(buffer, 8, array2, 0, 8);
							ulong num = BitConverter.ToUInt64(buffer, 8);
							if (array2[7] >> 4 == 0)
							{
								rid.Document = num;
								if (buffer.Length == 20)
								{
									rid.Attachment = BitConverter.ToUInt32(buffer, 16);
								}
							}
							else if (array2[7] >> 4 == 8)
							{
								rid.StoredProcedure = num;
							}
							else if (array2[7] >> 4 == 7)
							{
								rid.Trigger = num;
							}
							else if (array2[7] >> 4 == 6)
							{
								rid.UserDefinedFunction = num;
							}
							else if (array2[7] >> 4 == 4)
							{
								rid.Conflict = num;
							}
							else if (array2[7] >> 4 == 5)
							{
								rid.PartitionKeyRange = num;
							}
							else
							{
								if (array2[7] >> 4 != 9)
								{
									return false;
								}
								rid.Schema = num;
							}
						}
						else if (buffer.Length != 8)
						{
							return false;
						}
					}
					else
					{
						rid.User = BitConverter.ToUInt32(array, 0);
						if (buffer.Length == 16)
						{
							if (rid.User != 0)
							{
								rid.Permission = BitConverter.ToUInt64(buffer, 8);
							}
							else
							{
								uint userDefinedType = BitConverter.ToUInt32(buffer, 8);
								if (BitConverter.ToUInt32(buffer, 12) != 1)
								{
									return false;
								}
								rid.UserDefinedType = userDefinedType;
							}
						}
						else if (buffer.Length != 8)
						{
							return false;
						}
					}
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool Verify(string id, out byte[] buffer)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentNullException("id");
			}
			buffer = null;
			try
			{
				buffer = FromBase64String(id);
			}
			catch (FormatException)
			{
			}
			if (buffer == null || buffer.Length > Length)
			{
				buffer = null;
				return false;
			}
			return true;
		}

		public static bool Verify(string id)
		{
			byte[] buffer = null;
			return Verify(id, out buffer);
		}

		public override string ToString()
		{
			return ToBase64String(Value);
		}

		public bool Equals(ResourceId other)
		{
			if (other == null)
			{
				return false;
			}
			return Value.SequenceEqual(other.Value);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj is ResourceId)
			{
				return Equals((ResourceId)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}

		public static byte[] FromBase64String(string s)
		{
			return Convert.FromBase64String(s.Replace('-', '/'));
		}

		public static string ToBase64String(byte[] buffer)
		{
			return ToBase64String(buffer, 0, buffer.Length);
		}

		public static string ToBase64String(byte[] buffer, int offset, int length)
		{
			return Convert.ToBase64String(buffer, offset, length).Replace('/', '-');
		}

		private static ResourceId NewDocumentId(uint dbId, uint collId)
		{
			ResourceId obj = new ResourceId
			{
				Database = dbId,
				DocumentCollection = collId
			};
			byte[] value = Guid.NewGuid().ToByteArray();
			obj.Document = BitConverter.ToUInt64(value, 0);
			return obj;
		}

		private static ResourceId NewDocumentCollectionId(uint dbId)
		{
			ResourceId obj = new ResourceId
			{
				Database = dbId
			};
			byte[] array = new byte[4];
			byte[] array2 = Guid.NewGuid().ToByteArray();
			array2[0] |= 128;
			BlockCopy(array2, 0, array, 0, 4);
			obj.DocumentCollection = BitConverter.ToUInt32(array, 0);
			obj.Document = 0uL;
			obj.User = 0u;
			obj.Permission = 0uL;
			return obj;
		}

		private static ResourceId NewDatabaseId()
		{
			ResourceId resourceId = new ResourceId();
			byte[] value = Guid.NewGuid().ToByteArray();
			resourceId.Database = BitConverter.ToUInt32(value, 0);
			resourceId.DocumentCollection = 0u;
			resourceId.Document = 0uL;
			resourceId.User = 0u;
			resourceId.Permission = 0uL;
			return resourceId;
		}

		public static void BlockCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
		{
			int num = srcOffset + count;
			for (int i = srcOffset; i < num; i++)
			{
				dst[dstOffset++] = src[i];
			}
		}

		private static bool HasNonHierarchicalResourceId(ResourceType eResourceType)
		{
			if (eResourceType != ResourceType.MasterPartition && eResourceType != ResourceType.ServerPartition)
			{
				return eResourceType == ResourceType.RidRange;
			}
			return true;
		}
	}
}
