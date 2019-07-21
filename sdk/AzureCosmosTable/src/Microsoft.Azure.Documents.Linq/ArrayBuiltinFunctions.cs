using Microsoft.Azure.Documents.Sql;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class ArrayBuiltinFunctions
	{
		private class ArrayConcatVisitor : SqlBuiltinFunctionVisitor
		{
			public ArrayConcatVisitor()
				: base("ARRAY_CONCAT", isStatic: true, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 2)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					SqlScalarExpression sqlScalarExpression2 = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[1], context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("ARRAY_CONCAT", sqlScalarExpression, sqlScalarExpression2);
				}
				return null;
			}
		}

		private class ArrayContainsVisitor : SqlBuiltinFunctionVisitor
		{
			public ArrayContainsVisitor()
				: base("ARRAY_CONTAINS", isStatic: true, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				Expression expression = null;
				Expression expression2 = null;
				if (methodCallExpression.Arguments.Count == 1)
				{
					expression = methodCallExpression.Object;
					expression2 = methodCallExpression.Arguments[0];
				}
				else if (methodCallExpression.Arguments.Count == 2)
				{
					expression = methodCallExpression.Arguments[0];
					expression2 = methodCallExpression.Arguments[1];
				}
				if (expression == null || expression2 == null)
				{
					return null;
				}
				if (expression.NodeType == ExpressionType.Constant)
				{
					return VisitIN(expression2, (ConstantExpression)expression, context);
				}
				SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(expression, context);
				SqlScalarExpression sqlScalarExpression2 = ExpressionToSql.VisitScalarExpression(expression2, context);
				return SqlFunctionCallScalarExpression.CreateBuiltin("ARRAY_CONTAINS", sqlScalarExpression, sqlScalarExpression2);
			}

			private SqlScalarExpression VisitIN(Expression expression, ConstantExpression constantExpressionList, TranslationContext context)
			{
				List<SqlScalarExpression> list = new List<SqlScalarExpression>();
				foreach (object item in (IEnumerable)constantExpressionList.Value)
				{
					list.Add(ExpressionToSql.VisitConstant(Expression.Constant(item)));
				}
				if (list.Count == 0)
				{
					return SqlLiteralScalarExpression.SqlFalseLiteralScalarExpression;
				}
				return SqlInScalarExpression.Create(ExpressionToSql.VisitNonSubqueryScalarExpression(expression, context), not: false, list);
			}
		}

		private class ArrayCountVisitor : SqlBuiltinFunctionVisitor
		{
			public ArrayCountVisitor()
				: base("ARRAY_LENGTH", isStatic: true, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					new List<SqlScalarExpression>();
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("ARRAY_LENGTH", sqlScalarExpression);
				}
				return null;
			}
		}

		private class ArrayGetItemVisitor : BuiltinFunctionVisitor
		{
			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Object != null && methodCallExpression.Arguments.Count == 1)
				{
					SqlScalarExpression memberExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Object, context);
					SqlScalarExpression indexExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					return SqlMemberIndexerScalarExpression.Create(memberExpression, indexExpression);
				}
				return null;
			}

			protected override SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				return null;
			}
		}

		private class ArrayToArrayVisitor : BuiltinFunctionVisitor
		{
			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					return ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
				}
				return null;
			}

			protected override SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				return null;
			}
		}

		private static Dictionary<string, BuiltinFunctionVisitor> ArrayBuiltinFunctionDefinitions
		{
			get;
			set;
		}

		static ArrayBuiltinFunctions()
		{
			ArrayBuiltinFunctionDefinitions = new Dictionary<string, BuiltinFunctionVisitor>();
			ArrayBuiltinFunctionDefinitions.Add("Concat", new ArrayConcatVisitor());
			ArrayBuiltinFunctionDefinitions.Add("Contains", new ArrayContainsVisitor());
			ArrayBuiltinFunctionDefinitions.Add("Count", new ArrayCountVisitor());
			ArrayBuiltinFunctionDefinitions.Add("get_Item", new ArrayGetItemVisitor());
			ArrayBuiltinFunctionDefinitions.Add("ToArray", new ArrayToArrayVisitor());
			ArrayBuiltinFunctionDefinitions.Add("ToList", new ArrayToArrayVisitor());
		}

		public static SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			BuiltinFunctionVisitor value = null;
			if (ArrayBuiltinFunctionDefinitions.TryGetValue(methodCallExpression.Method.Name, out value))
			{
				return value.Visit(methodCallExpression, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}
	}
}
