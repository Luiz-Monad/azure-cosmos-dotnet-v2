using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal class SqlBuiltinFunctionVisitor : BuiltinFunctionVisitor
	{
		public string SqlName
		{
			get;
			private set;
		}

		public bool IsStatic
		{
			get;
			private set;
		}

		public List<Type[]> ArgumentLists
		{
			get;
			private set;
		}

		public SqlBuiltinFunctionVisitor(string sqlName, bool isStatic, List<Type[]> argumentLists)
		{
			SqlName = sqlName;
			IsStatic = isStatic;
			ArgumentLists = argumentLists;
		}

		protected override SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			if (ArgumentLists != null)
			{
				foreach (Type[] argumentList in ArgumentLists)
				{
					if (MatchArgumentLists(methodCallExpression.Arguments, argumentList))
					{
						return VisitBuiltinFunction(methodCallExpression, context);
					}
				}
			}
			return null;
		}

		protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			return null;
		}

		private bool MatchArgumentLists(ReadOnlyCollection<Expression> methodCallArguments, Type[] expectedArguments)
		{
			if (methodCallArguments.Count != expectedArguments.Length)
			{
				return false;
			}
			for (int i = 0; i < expectedArguments.Length; i++)
			{
				if ((object)methodCallArguments[i].Type != expectedArguments[i] && !CustomTypeExtensions.IsAssignableFrom(expectedArguments[i], methodCallArguments[i].Type))
				{
					return false;
				}
			}
			return true;
		}

		private SqlScalarExpression VisitBuiltinFunction(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			List<SqlScalarExpression> list = new List<SqlScalarExpression>();
			if (methodCallExpression.Object != null)
			{
				list.Add(ExpressionToSql.VisitNonSubqueryScalarExpression(methodCallExpression.Object, context));
			}
			foreach (Expression argument in methodCallExpression.Arguments)
			{
				list.Add(ExpressionToSql.VisitNonSubqueryScalarExpression(argument, context));
			}
			return SqlFunctionCallScalarExpression.CreateBuiltin(SqlName, list.ToArray());
		}
	}
}
