using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// This is the conflicting resource resulting from a concurrent async operation in the Azure Cosmos DB service.
	/// </summary> 
	/// <remarks>
	/// On rare occasions, during an async operation (insert, replace and delete), a version conflict may occur on a resource.
	/// The conflicting resource is persisted as a Conflict resource.  
	/// Inspecting Conflict resources will allow you to determine which operations and resources resulted in conflicts.
	/// </remarks>
	public class Conflict : Resource
	{
		/// <summary>
		/// Gets the resource ID for the conflict in the Azure Cosmos DB service.
		/// </summary>
		public string SourceResourceId
		{
			get
			{
				return GetValue<string>("resourceId");
			}
			internal set
			{
				SetValue("resourceId", value);
			}
		}

		internal long ConflictLSN
		{
			get
			{
				return GetValue<long>("conflict_lsn");
			}
			set
			{
				SetValue("conflict_lsn", value);
			}
		}

		/// <summary>
		/// Gets the operation that resulted in the conflict in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// One of the values of the <see cref="P:Microsoft.Azure.Documents.Conflict.OperationKind" /> enumeration.
		/// </value>
		public OperationKind OperationKind
		{
			get
			{
				string value = GetValue<string>("operationType");
				if (string.Equals("create", value, StringComparison.OrdinalIgnoreCase))
				{
					return OperationKind.Create;
				}
				if (string.Equals("replace", value, StringComparison.OrdinalIgnoreCase) || string.Equals("patch", value, StringComparison.OrdinalIgnoreCase))
				{
					return OperationKind.Replace;
				}
				if (string.Equals("delete", value, StringComparison.OrdinalIgnoreCase))
				{
					return OperationKind.Delete;
				}
				return OperationKind.Invalid;
			}
			internal set
			{
				string text = "";
				switch (value)
				{
				case OperationKind.Create:
					text = "create";
					break;
				case OperationKind.Replace:
					text = "replace";
					break;
				case OperationKind.Delete:
					text = "delete";
					break;
				default:
					throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported operation kind {0}", value.ToString()));
				}
				SetValue("operationType", text);
			}
		}

		/// <summary>
		/// Gets the type of the conflicting resource in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The type of the resource.
		/// </value>
		public Type ResourceType
		{
			get
			{
				string value = GetValue<string>("resourceType");
				if (string.Equals("document", value, StringComparison.OrdinalIgnoreCase))
				{
					return typeof(Document);
				}
				if (string.Equals("storedProcedure", value, StringComparison.OrdinalIgnoreCase))
				{
					return typeof(StoredProcedure);
				}
				if (string.Equals("trigger", value, StringComparison.OrdinalIgnoreCase))
				{
					return typeof(Trigger);
				}
				if (string.Equals("userDefinedFunction", value, StringComparison.OrdinalIgnoreCase))
				{
					return typeof(UserDefinedFunction);
				}
				if (string.Equals("attachment", value, StringComparison.OrdinalIgnoreCase))
				{
					return typeof(Attachment);
				}
				return null;
			}
			internal set
			{
				string text = null;
				if ((object)value == typeof(Document))
				{
					text = "document";
				}
				else if ((object)value == typeof(StoredProcedure))
				{
					text = "storedProcedure";
				}
				else if ((object)value == typeof(Trigger))
				{
					text = "trigger";
				}
				else if ((object)value == typeof(UserDefinedFunction))
				{
					text = "userDefinedFunction";
				}
				else
				{
					if ((object)value != typeof(Attachment))
					{
						throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported resource type {0}", value.ToString()));
					}
					text = "attachment";
				}
				SetValue("resourceType", text);
			}
		}

		/// <summary>
		/// Gets the conflicting resource in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="T">The returned type of conflicting resource.</typeparam>
		/// <returns>The conflicting resource.</returns>
		public T GetResource<T>() where T : Resource, new()
		{
			if ((object)typeof(T) != ResourceType)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResourceType, typeof(T).Name, ResourceType.Name));
			}
			string value = GetValue<string>("content");
			if (!string.IsNullOrEmpty(value))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (StreamWriter streamWriter = new StreamWriter(memoryStream))
					{
						streamWriter.Write(value);
						streamWriter.Flush();
						memoryStream.Position = 0L;
						return JsonSerializable.LoadFrom<T>(memoryStream);
					}
				}
			}
			return null;
		}

		internal void SetResource<T>(T baseResource) where T : Resource, new()
		{
			if ((object)typeof(T) != ResourceType)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResourceType, typeof(T).Name, ResourceType.Name));
			}
			StringBuilder stringBuilder = new StringBuilder();
			baseResource.SaveTo(stringBuilder);
			string value = stringBuilder.ToString();
			if (!string.IsNullOrEmpty(value))
			{
				SetValue("content", value);
			}
			Id = baseResource.Id;
			ResourceId = baseResource.ResourceId;
		}
	}
}
