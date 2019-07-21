using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class TypeCheckFunctions
	{
		private static Dictionary<string, BuiltinFunctionVisitor> TypeCheckFunctionsDefinitions
		{
			get;
			set;
		}

		static TypeCheckFunctions()
		{
			TypeCheckFunctionsDefinitions = new Dictionary<string, BuiltinFunctionVisitor>();
			TypeCheckFunctionsDefinitions.Add("IsDefined", new SqlBuiltinFunctionVisitor("IS_DEFINED", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(object)
				}
			}));
			TypeCheckFunctionsDefinitions.Add("IsNull", new SqlBuiltinFunctionVisitor("IS_NULL", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(object)
				}
			}));
			TypeCheckFunctionsDefinitions.Add("IsPrimitive", new SqlBuiltinFunctionVisitor("IS_PRIMITIVE", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(object)
				}
			}));
		}

		public static SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			BuiltinFunctionVisitor value = null;
			if (TypeCheckFunctionsDefinitions.TryGetValue(methodCallExpression.Method.Name, out value))
			{
				return value.Visit(methodCallExpression, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}
	}
}
