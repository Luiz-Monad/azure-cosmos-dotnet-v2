using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a document in the Azure Cosmos DB service.
	/// </summary>
	/// <remarks> 
	/// A document is a structured JSON document. There is no set schema for the JSON documents, and a document may contain any 
	/// number of custom properties as well as an optional list of attachments. Document is an application resource and can be
	/// authorized using the master key or resource keys.
	/// </remarks>
	public class Document : Resource, IDynamicMetaObjectProvider
	{
		private class DocumentDynamicMetaObject : DynamicMetaObject
		{
			private readonly Document document;

			public DocumentDynamicMetaObject(Document document, Expression expression)
				: base(expression, BindingRestrictions.Empty, document)
			{
				this.document = document;
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
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(Document).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic), arguments), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
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
				}, instance: Expression.Convert(base.Expression, base.LimitType), method: typeof(Document).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)), typeRestriction);
			}

			public override DynamicMetaObject BindConvert(ConvertBinder binder)
			{
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(Document).GetMethod("AsType", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(binder.Type)), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			public override IEnumerable<string> GetDynamicMemberNames()
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, JToken> item in document.propertyBag)
				{
					if (!IsResourceSerializedProperty(item.Key))
					{
						list.Add(item.Key);
					}
				}
				return list.ToList();
			}

			internal static bool IsResourceSerializedProperty(string propertyName)
			{
				if (propertyName == "id" || propertyName == "_rid" || propertyName == "_etag" || propertyName == "_ts" || propertyName == "_self" || propertyName == "_attachments" || propertyName == "ttl")
				{
					return true;
				}
				return false;
			}

			internal static bool IsResourceProperty(string propertyName)
			{
				if (propertyName == "Id" || propertyName == "ResourceId" || propertyName == "ETag" || propertyName == "Timestamp" || propertyName == "SelfLink" || propertyName == "AttachmentsLink" || propertyName == "TimeToLive")
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the self-link corresponding to attachments of the document from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The self-link corresponding to attachments of the document.
		/// </value>
		/// <remarks>
		/// Every document can have between zero and many attachments. The attachments link contains a feed of attachments that belong to 
		/// the document.
		/// </remarks>
		public string AttachmentsLink => base.SelfLink.TrimEnd(new char[1]
		{
			'/'
		}) + "/" + GetValue<string>("_attachments");

		/// <summary>
		/// Gets or sets the time to live in seconds of the document in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// It is an optional property. 
		/// A valid value must be either a nonzero positive integer, '-1', or <c>null</c>.
		/// By default, TimeToLive is set to null meaning the document inherits the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" />.
		/// The unit of measurement is seconds. The maximum allowed value is 2147483647.
		/// When the value is '-1', it means never expire regardless of the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> value.
		/// </value>
		/// <remarks>
		/// <para>
		/// The final time-to-live policy of a document is evaluated after consulting the collection's
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" />.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> is <c>null</c>, the document inherits the collection's
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" />.
		/// If the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> is a nonzero positive integer,
		/// then the document will inherit that value as its time-to-live in seconds, and will be expired
		/// after the default time-to-live in seconds since its last write time. The expired documents will be deleted in background.
		/// Otherwise, the document will never expire.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> is '-1', the document will never expire regardless of the collection's
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> value.
		/// </para>
		/// <para>
		/// When the <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> is a nonzero positive integer, need to check the collection's
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" />.
		/// If the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> is <c>null</c>, which means the time-to-live
		/// has been turned off on the collection, and the document's <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> should be disregarded and the document
		/// will never expire.
		/// Otherwise, the document's <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" /> will be honored. The document will be expired
		/// after the default time-to-live in seconds since its last write time. The expired documents will be deleted in background.
		/// </para>
		/// <para>
		/// The table below shows an example of the matrix to evaluate the final time-to-live policy given a collection's
		/// <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> and a document's <see cref="P:Microsoft.Azure.Documents.Document.TimeToLive" />.
		/// </para>
		/// <list type="table">
		/// <listheader>
		/// <term>Collection</term>
		/// <description>Matrix</description>
		/// </listheader>
		/// <item>
		/// <term>DefaultTimeToLive = null</term>
		/// <description>
		/// <list type="table">
		/// <listheader>
		/// <term>Document</term>
		/// <description>Result</description>
		/// </listheader>
		/// <item>
		/// <term>TimeToLive = null</term>
		/// <description>TTL is disabled. The document will never expire (default).</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = -1</term>
		/// <description>TTL is disabled. The document will never expire.</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = 2000</term>
		/// <description>TTL is disabled. The document will never expire.</description>
		/// </item>
		/// </list>
		/// </description>
		/// </item>
		/// <item>
		/// <term>DefaultTimeToLive = -1</term>
		/// <description>
		/// <list type="table">
		/// <listheader>
		/// <term>Document</term>
		/// <description>Result</description>
		/// </listheader>
		/// <item>
		/// <term>TimeToLive = null</term>
		/// <description>TTL is enabled. The document will never expire (default).</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = -1</term>
		/// <description>TTL is enabled. The document will never expire.</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = 2000</term>
		/// <description>TTL is enabled. The document will expire after 2000 seconds.</description>
		/// </item>
		/// </list>
		/// </description>
		/// </item>
		/// <item>
		/// <term>DefaultTimeToLive = 1000</term>
		/// <description>
		/// <list type="table">
		/// <listheader>
		/// <term>Document</term>
		/// <description>Result</description>
		/// </listheader>
		/// <item>
		/// <term>TimeToLive = null</term>
		/// <description>TTL is enabled. The document will expire after 1000 seconds (default).</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = -1</term>
		/// <description>TTL is enabled. The document will never expire.</description>
		/// </item>
		/// <item>
		/// <term>TimeToLive = 2000</term>
		/// <description>TTL is enabled. The document will expire after 2000 seconds.</description>
		/// </item>
		/// </list>
		/// </description>
		/// </item>
		/// </list>
		/// </remarks>
		/// <example>
		/// The example below removes 'ttl' from document content.
		/// The document will inherit the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" /> as its time-to-live value.
		/// <code language="c#">
		/// <![CDATA[
		///     document.TimeToLive = null;
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// The example below ensures that the document should never expire regardless.
		/// <code language="c#">
		/// <![CDATA[
		///     document.TimeToLive = -1;
		/// ]]>
		/// </code>
		/// </example>
		/// <example>
		/// The example below sets the time-to-live in seconds on a document.
		/// The document will expire after 1000 seconds since its last write time when the collection's <see cref="P:Microsoft.Azure.Documents.DocumentCollection.DefaultTimeToLive" />
		/// is not <c>null</c>.
		/// <code language="c#">
		/// <![CDATA[
		///     document.TimeToLive = 1000;
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.DocumentCollection" />     
		[JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
		public int? TimeToLive
		{
			get
			{
				return GetValue<int?>("ttl");
			}
			set
			{
				SetValue("ttl", value);
			}
		}

		internal static Document FromObject(object document, JsonSerializerSettings settings = null)
		{
			if (document != null)
			{
				if (typeof(Document).IsAssignableFrom(document.GetType()))
				{
					return (Document)document;
				}
				JObject propertyBag = (settings == null) ? JObject.FromObject(document) : JObject.FromObject(document, JsonSerializer.Create(settings));
				return new Document
				{
					SerializerSettings = settings,
					propertyBag = propertyBag
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
			if ((object)typeof(T) == typeof(Document) || (object)typeof(T) == typeof(object))
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
			return new DocumentDynamicMetaObject(this, parameter);
		}
	}
}
