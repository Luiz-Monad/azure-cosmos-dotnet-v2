using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class TypeSystem
	{
		public static Type GetElementType(Type type)
		{
			return GetElementType(type, new HashSet<Type>());
		}

		public static string GetMemberName(this MemberInfo memberInfo)
		{
			JsonPropertyAttribute customAttribute = memberInfo.GetCustomAttribute<JsonPropertyAttribute>(inherit: true);
			if (customAttribute != null && !string.IsNullOrEmpty(customAttribute.PropertyName))
			{
				return customAttribute.PropertyName;
			}
			if (memberInfo.DeclaringType.GetCustomAttribute<DataContractAttribute>(inherit: true) != null)
			{
				DataMemberAttribute customAttribute2 = memberInfo.GetCustomAttribute<DataMemberAttribute>(inherit: true);
				if (customAttribute2 != null && !string.IsNullOrEmpty(customAttribute2.Name))
				{
					return customAttribute2.Name;
				}
			}
			return memberInfo.Name;
		}

		private static Type GetElementType(Type type, HashSet<Type> visitedSet)
		{
			if (visitedSet.Contains(type))
			{
				return null;
			}
			visitedSet.Add(type);
			if (type.IsArray)
			{
				return type.GetElementType();
			}
			Type type2 = null;
			if (type.IsInterface() && type.IsGenericType() && (object)type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				type2 = GetMoreSpecificType(type2, CustomTypeExtensions.GetGenericArguments(type)[0]);
			}
			Type[] interfaces = CustomTypeExtensions.GetInterfaces(type);
			foreach (Type type3 in interfaces)
			{
				type2 = GetMoreSpecificType(type2, GetElementType(type3, visitedSet));
			}
			if ((object)type.GetBaseType() != null && (object)type.GetBaseType() != typeof(object))
			{
				type2 = GetMoreSpecificType(type2, GetElementType(type.GetBaseType(), visitedSet));
			}
			return type2;
		}

		private static Type GetMoreSpecificType(Type left, Type right)
		{
			if ((object)left != null && (object)right != null)
			{
				if (CustomTypeExtensions.IsAssignableFrom(right, left))
				{
					return left;
				}
				if (CustomTypeExtensions.IsAssignableFrom(left, right))
				{
					return right;
				}
				return left;
			}
			return left ?? right;
		}

		/// <summary>
		/// True if type is anonymous.
		/// </summary>
		/// <param name="type">Type to check.</param>
		/// <returns>Trye if the type is anonymous.</returns>
		public static bool IsAnonymousType(this Type type)
		{
			bool num = CustomTypeExtensions.GetCustomAttributes(type, typeof(CompilerGeneratedAttribute), inherit: false).Any();
			bool flag = type.FullName.Contains("AnonymousType");
			return num & flag;
		}

		public static bool IsEnumerable(this Type type)
		{
			if ((object)type == typeof(Enumerable))
			{
				return true;
			}
			if (type.IsGenericType() && (object)type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				return true;
			}
			return (object)CustomTypeExtensions.GetInterfaces(type).Where(delegate(Type interfaceType)
			{
				if (interfaceType.IsGenericType())
				{
					return (object)interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
				}
				return false;
			}).FirstOrDefault() != null;
		}

		public static bool IsExtensionMethod(this MethodInfo methodInfo)
		{
			return methodInfo.GetCustomAttribute(typeof(ExtensionAttribute)) != null;
		}

		public static bool IsNullable(this Type type)
		{
			if (type.IsGenericType())
			{
				return (object)type.GetGenericTypeDefinition() == typeof(Nullable<>);
			}
			return false;
		}

		public static Type NullableUnderlyingType(this Type type)
		{
			if (type.IsNullable())
			{
				return CustomTypeExtensions.GetGenericArguments(type)[0];
			}
			return type;
		}
	}
}
