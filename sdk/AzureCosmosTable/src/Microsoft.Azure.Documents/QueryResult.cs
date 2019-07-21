using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Documents
{
	internal sealed class QueryResult : IDynamicMetaObjectProvider
	{
		private class DocumentDynamicMetaObject : DynamicMetaObject
		{
			private readonly QueryResult queryResult;

			public DocumentDynamicMetaObject(QueryResult queryResult, Expression expression)
				: base(expression, BindingRestrictions.Empty, queryResult)
			{
				this.queryResult = queryResult;
			}

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
			{
				string name = "GetProperty";
				Expression[] arguments = new Expression[2]
				{
					Expression.Constant(binder.Name),
					Expression.Constant(binder.ReturnType)
				};
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(QueryResult).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic), arguments), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
			{
				string name = "SetProperty";
				BindingRestrictions typeRestriction = BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType);
				return new DynamicMetaObject(Expression.Call(arguments: new Expression[2]
				{
					Expression.Constant(binder.Name),
					Expression.Convert(value.Expression, typeof(object))
				}, instance: Expression.Convert(base.Expression, base.LimitType), method: typeof(QueryResult).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)), typeRestriction);
			}

			public override DynamicMetaObject BindConvert(ConvertBinder binder)
			{
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), typeof(QueryResult).GetMethod("AsType", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(binder.Type)), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			public override IEnumerable<string> GetDynamicMemberNames()
			{
				return queryResult.GetDynamicMemberNames();
			}
		}

		private readonly JContainer jObject;

		private readonly string ownerFullName;

		private JsonSerializer jsonSerializer;

		/// <summary>
		/// Gets the raw payload of this object.
		/// To avoid double deserializations.
		/// </summary>
		public JContainer Payload => jObject;

		public QueryResult(JContainer jObject, string ownerFullName, JsonSerializer jsonSerializer)
		{
			this.jObject = jObject;
			this.ownerFullName = ownerFullName;
			this.jsonSerializer = jsonSerializer;
		}

		public QueryResult(JContainer jObject, string ownerFullName, JsonSerializerSettings serializerSettings = null)
			: this(jObject, ownerFullName, (serializerSettings != null) ? JsonSerializer.Create(serializerSettings) : JsonSerializer.Create())
		{
		}

		public override string ToString()
		{
			using (StringWriter stringWriter = new StringWriter())
			{
				jsonSerializer.Serialize(stringWriter, jObject);
				return stringWriter.ToString();
			}
		}

		private IEnumerable<string> GetDynamicMemberNames()
		{
			List<string> list = new List<string>();
			JObject jObject = this.jObject as JObject;
			if (jObject != null)
			{
				foreach (KeyValuePair<string, JToken> item in jObject)
				{
					list.Add(item.Key);
				}
			}
			return list.ToList();
		}

		private object Convert(Type type)
		{
			if ((object)type == typeof(object))
			{
				return this;
			}
			object obj;
			if ((object)type == typeof(Database))
			{
				obj = new Database
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(DocumentCollection))
			{
				obj = new DocumentCollection
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(User))
			{
				obj = new User
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(UserDefinedType))
			{
				obj = new UserDefinedType
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Permission))
			{
				obj = new Permission
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Attachment))
			{
				obj = new Attachment
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Document))
			{
				obj = new Document
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Conflict))
			{
				obj = new Conflict
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Trigger))
			{
				obj = new Trigger
				{
					propertyBag = (jObject as JObject)
				};
			}
			else if ((object)type == typeof(Offer))
			{
				obj = OfferTypeResolver.ResponseOfferTypeResolver.Resolve(jObject as JObject);
			}
			else if (typeof(Document).IsAssignableFrom(type))
			{
				obj = (Resource)((jsonSerializer == null) ? jObject.ToObject(type) : jObject.ToObject(type, jsonSerializer));
				((Document)obj).propertyBag = (jObject as JObject);
			}
			else if (!typeof(Attachment).IsAssignableFrom(type))
			{
				obj = (((object)type != typeof(Schema)) ? ((jsonSerializer == null) ? jObject.ToObject(type) : jObject.ToObject(type, jsonSerializer)) : new Schema
				{
					propertyBag = (jObject as JObject)
				});
			}
			else
			{
				obj = (Resource)jObject.ToObject(type);
				((Attachment)obj).propertyBag = (jObject as JObject);
			}
			Resource resource = obj as Resource;
			if (resource != null)
			{
				resource.AltLink = PathsHelper.GeneratePathForNameBased(type, ownerFullName, resource.Id);
			}
			return obj;
		}

		private object GetProperty(string propertyName, Type returnType)
		{
			JToken jToken = jObject[propertyName];
			if (jToken != null)
			{
				return jToken.ToObject(returnType);
			}
			throw new DocumentClientException(string.Format(CultureInfo.CurrentUICulture, RMResources.PropertyNotFound, propertyName), null, null);
		}

		private object SetProperty(string propertyName, object value)
		{
			if (value != null)
			{
				jObject[propertyName] = JToken.FromObject(value);
				return true;
			}
			return value;
		}

		private T AsType<T>()
		{
			return (T)Convert(typeof(T));
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new DocumentDynamicMetaObject(this, parameter);
		}
	}
}
