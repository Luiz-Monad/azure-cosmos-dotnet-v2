using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a document attachment in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks>
	/// Each document may contain zero or more attachments containing data of arbitrary formats like images, binary or large text blobs. 
	/// The Attachment class represents the Azure Cosmos DB resource used to store information about the attachment like its location and 
	/// MIME content type. The payload itself ("Media") is referenced through the MediaLink property. The Attachment class is a DynamicObject 
	/// and can contain any custom metadata to be persisted. 
	///
	/// Attachments can be created as managed or unmanaged. If attachments are created as managed through Azure Cosmos DB, then it is assigned a system 
	/// generated mediaLink. Azure Cosmos DB then automatically performs garbage collection on the media when parent document is deleted.
	///
	/// You can reuse the mediaLink property to store an external location e.g., a file share or an Azure Blob Storage URI. 
	/// Azure Cosmos DB will not perform garbage collection on mediaLinks for external locations.
	/// </remarks>
	public class Attachment : Resource, IDynamicMetaObjectProvider
	{
		private class AttachmentDynamicMetaObject : DynamicMetaObject
		{
			private readonly Attachment attachment;

			public AttachmentDynamicMetaObject(Attachment attachment, Expression expression)
				: base(expression, BindingRestrictions.Empty, attachment)
			{
				this.attachment = attachment;
			}

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
			{
				if (IsResourceProperty(binder.Name))
				{
					return base.BindGetMember(binder);
				}
				string name = "GetProperty";
				Expression[] arguments = new Expression[2]
				{
					Expression.Constant(binder.Name),
					Expression.Constant(binder.ReturnType)
				};
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(Attachment).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic), arguments), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
			{
				if (IsResourceProperty(binder.Name))
				{
					return base.BindSetMember(binder, value);
				}
				string name = "SetProperty";
				BindingRestrictions typeRestriction = BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType);
				return new DynamicMetaObject(Expression.Call(arguments: new Expression[2]
				{
					Expression.Constant(binder.Name),
					Expression.Convert(value.Expression, typeof(object))
				}, instance: Expression.Convert(base.Expression, base.LimitType), method: typeof(Attachment).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)), typeRestriction);
			}

			public override DynamicMetaObject BindConvert(ConvertBinder binder)
			{
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(Attachment).GetMethod("AsType", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(binder.Type)), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			public override IEnumerable<string> GetDynamicMemberNames()
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, JToken> item in attachment.propertyBag)
				{
					if (!IsResourceSerializedProperty(item.Key))
					{
						list.Add(item.Key);
					}
				}
				return list;
			}

			internal static bool IsResourceSerializedProperty(string propertyName)
			{
				if (propertyName == "id" || propertyName == "_rid" || propertyName == "_etag" || propertyName == "_ts" || propertyName == "_self" || propertyName == "contentType" || propertyName == "media")
				{
					return true;
				}
				return false;
			}

			internal static bool IsResourceProperty(string propertyName)
			{
				if (propertyName == "Id" || propertyName == "ResourceId" || propertyName == "ETag" || propertyName == "Timestamp" || propertyName == "SelfLink" || propertyName == "MediaLink" || propertyName == "ContentType")
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets or sets the MIME content type of the attachment in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The MIME content type of the attachment.
		/// </value>
		/// <remarks>For example, set to "text/plain" for text files, "image/jpeg" for images.</remarks>
		[JsonProperty(PropertyName = "contentType")]
		public string ContentType
		{
			get
			{
				return GetValue<string>("contentType");
			}
			set
			{
				SetValue("contentType", value);
			}
		}

		/// <summary>
		/// Gets or sets the media link associated with the attachment content in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The media link associated with the attachment content.
		/// </value>
		/// <remarks>Azure Cosmos DB supports both managed and unmanaged attachments.</remarks>
		[JsonProperty(PropertyName = "media")]
		public string MediaLink
		{
			get
			{
				return GetValue<string>("media");
			}
			set
			{
				SetValue("media", value);
			}
		}

		internal static Attachment FromObject(object attachment, JsonSerializerSettings settings = null)
		{
			if (attachment != null)
			{
				if (typeof(Attachment).IsAssignableFrom(attachment.GetType()))
				{
					return (Attachment)attachment;
				}
				JObject propertyBag = JObject.FromObject(attachment);
				return new Attachment
				{
					propertyBag = propertyBag,
					SerializerSettings = settings
				};
			}
			return null;
		}

		private object GetProperty(string propertyName, Type returnType)
		{
			if (propertyBag != null)
			{
				JToken jToken = propertyBag[propertyName];
				if (jToken != null)
				{
					if (base.SerializerSettings != null)
					{
						return jToken.ToObject(returnType, JsonSerializer.Create(base.SerializerSettings));
					}
					return jToken.ToObject(returnType);
				}
			}
			throw new DocumentClientException(string.Format(CultureInfo.CurrentUICulture, RMResources.PropertyNotFound, propertyName), null, null);
		}

		private object SetProperty(string propertyName, object value)
		{
			if (value != null)
			{
				if (propertyBag == null)
				{
					propertyBag = new JObject();
				}
				propertyBag[propertyName] = JToken.FromObject(value);
			}
			else if (propertyBag != null)
			{
				propertyBag.Remove(propertyName);
			}
			return value;
		}

		private T AsType<T>()
		{
			if ((object)typeof(T) == typeof(Attachment) || (object)typeof(T) == typeof(object))
			{
				return (T)(object)this;
			}
			if (propertyBag == null)
			{
				return default(T);
			}
			if (base.SerializerSettings != null)
			{
				return propertyBag.ToObject<T>(JsonSerializer.Create(base.SerializerSettings));
			}
			return propertyBag.ToObject<T>();
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new AttachmentDynamicMetaObject(this, parameter);
		}
	}
}
