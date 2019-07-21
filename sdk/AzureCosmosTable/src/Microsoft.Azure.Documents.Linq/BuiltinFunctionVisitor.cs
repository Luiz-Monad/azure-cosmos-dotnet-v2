using Microsoft.Azure.Documents.Spatial;
using Microsoft.Azure.Documents.Sql;
using Microsoft.Azure.Documents.SystemFunctions;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal abstract class BuiltinFunctionVisitor
	{
		public SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			SqlScalarExpression sqlScalarExpression = VisitExplicit(methodCallExpression, context);
			if (sqlScalarExpression != null)
			{
				return sqlScalarExpression;
			}
			sqlScalarExpression = VisitImplicit(methodCallExpression, context);
			if (sqlScalarExpression != null)
			{
				return sqlScalarExpression;
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}

		public static SqlScalarExpression VisitBuiltinFunctionCall(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			Type type;
			if (methodCallExpression.Method.IsStatic && methodCallExpression.Method.IsExtensionMethod())
			{
				if (methodCallExpression.Arguments.Count < 1)
				{
					throw new ArgumentException();
				}
				type = methodCallExpression.Arguments[0].Type;
				if ((object)methodCallExpression.Method.DeclaringType.GeUnderlyingSystemType() == typeof(TypeCheckFunctionsExtensions))
				{
					return TypeCheckFunctions.Visit(methodCallExpression, context);
				}
			}
			else
			{
				type = methodCallExpression.Method.DeclaringType;
			}
			if ((object)type == typeof(Math))
			{
				return MathBuiltinFunctions.Visit(methodCallExpression, context);
			}
			if ((object)type == typeof(string))
			{
				return StringBuiltinFunctions.Visit(methodCallExpression, context);
			}
			if (type.IsEnumerable())
			{
				return ArrayBuiltinFunctions.Visit(methodCallExpression, context);
			}
			if (typeof(Geometry).IsAssignableFrom(type) || (object)methodCallExpression.Method.DeclaringType == typeof(GeometryOperationExtensions))
			{
				return SpatialBuiltinFunctions.Visit(methodCallExpression, context);
			}
			if (methodCallExpression.Method.Name == "ToString" && methodCallExpression.Arguments.Count == 0 && methodCallExpression.Object != null)
			{
				return ExpressionToSql.VisitNonSubqueryScalarExpression(methodCallExpression.Object, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}

		protected abstract SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context);

		protected abstract SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context);
	}
}
