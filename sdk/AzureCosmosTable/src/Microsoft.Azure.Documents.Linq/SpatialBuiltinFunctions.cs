using Microsoft.Azure.Documents.Spatial;
using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class SpatialBuiltinFunctions
	{
		private static Dictionary<string, BuiltinFunctionVisitor> SpatialBuiltinFunctionDefinitions
		{
			get;
			set;
		}

		static SpatialBuiltinFunctions()
		{
			SpatialBuiltinFunctionDefinitions = new Dictionary<string, BuiltinFunctionVisitor>();
			SpatialBuiltinFunctionDefinitions.Add("Distance", new SqlBuiltinFunctionVisitor("ST_Distance", isStatic: true, new List<Type[]>
			{
				new Type[2]
				{
					typeof(Geometry),
					typeof(Geometry)
				}
			}));
			SpatialBuiltinFunctionDefinitions.Add("Within", new SqlBuiltinFunctionVisitor("ST_Within", isStatic: true, new List<Type[]>
			{
				new Type[2]
				{
					typeof(Geometry),
					typeof(Geometry)
				}
			}));
			SpatialBuiltinFunctionDefinitions.Add("IsValidDetailed", new SqlBuiltinFunctionVisitor("ST_IsValidDetailed", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(Geometry)
				}
			}));
			SpatialBuiltinFunctionDefinitions.Add("IsValid", new SqlBuiltinFunctionVisitor("ST_IsValid", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(Geometry)
				}
			}));
			SpatialBuiltinFunctionDefinitions.Add("Intersects", new SqlBuiltinFunctionVisitor("ST_Intersects", isStatic: true, new List<Type[]>
			{
				new Type[2]
				{
					typeof(Geometry),
					typeof(Geometry)
				}
			}));
		}

		public static SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			BuiltinFunctionVisitor value = null;
			if (SpatialBuiltinFunctionDefinitions.TryGetValue(methodCallExpression.Method.Name, out value))
			{
				return value.Visit(methodCallExpression, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}
	}
}
