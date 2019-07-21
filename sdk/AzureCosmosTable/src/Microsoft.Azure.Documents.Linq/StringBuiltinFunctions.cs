using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class StringBuiltinFunctions
	{
		private class StringVisitConcat : SqlBuiltinFunctionVisitor
		{
			public StringVisitConcat()
				: base("CONCAT", isStatic: true, new List<Type[]>
				{
					new Type[2]
					{
						typeof(string),
						typeof(string)
					},
					new Type[3]
					{
						typeof(string),
						typeof(string),
						typeof(string)
					},
					new Type[4]
					{
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string)
					}
				})
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0] is NewArrayExpression)
				{
					ReadOnlyCollection<Expression> expressions = ((NewArrayExpression)methodCallExpression.Arguments[0]).Expressions;
					List<SqlScalarExpression> list = new List<SqlScalarExpression>();
					foreach (Expression item in expressions)
					{
						list.Add(ExpressionToSql.VisitScalarExpression(item, context));
					}
					return SqlFunctionCallScalarExpression.CreateBuiltin("CONCAT", list);
				}
				return null;
			}
		}

		private class StringVisitContains : SqlBuiltinFunctionVisitor
		{
			public StringVisitContains()
				: base("CONTAINS", isStatic: false, new List<Type[]>
				{
					new Type[1]
					{
						typeof(string)
					}
				})
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 2)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					SqlScalarExpression sqlScalarExpression2 = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[1], context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("CONTAINS", sqlScalarExpression, sqlScalarExpression2);
				}
				return null;
			}
		}

		private class StringVisitCount : SqlBuiltinFunctionVisitor
		{
			public StringVisitCount()
				: base("LENGTH", isStatic: true, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("LENGTH", sqlScalarExpression);
				}
				return null;
			}
		}

		private class StringVisitTrimStart : SqlBuiltinFunctionVisitor
		{
			public StringVisitTrimStart()
				: base("LTRIM", isStatic: false, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant && (object)methodCallExpression.Arguments[0].Type == typeof(char[]) && ((char[])((ConstantExpression)methodCallExpression.Arguments[0]).Value).Length == 0)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Object, context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("LTRIM", sqlScalarExpression);
				}
				return null;
			}
		}

		private class StringVisitReverse : SqlBuiltinFunctionVisitor
		{
			public StringVisitReverse()
				: base("REVERSE", isStatic: true, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitNonSubqueryScalarExpression(methodCallExpression.Arguments[0], context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("REVERSE", sqlScalarExpression);
				}
				return null;
			}
		}

		private class StringVisitTrimEnd : SqlBuiltinFunctionVisitor
		{
			public StringVisitTrimEnd()
				: base("RTRIM", isStatic: false, null)
			{
			}

			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0].NodeType == ExpressionType.Constant && (object)methodCallExpression.Arguments[0].Type == typeof(char[]) && ((char[])((ConstantExpression)methodCallExpression.Arguments[0]).Value).Length == 0)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Object, context);
					return SqlFunctionCallScalarExpression.CreateBuiltin("RTRIM", sqlScalarExpression);
				}
				return null;
			}
		}

		private class StringGetCharsVisitor : BuiltinFunctionVisitor
		{
			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					SqlScalarExpression sqlScalarExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Object, context);
					SqlScalarExpression sqlScalarExpression2 = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					SqlScalarExpression[] arguments = new SqlScalarExpression[3]
					{
						sqlScalarExpression,
						sqlScalarExpression2,
						ExpressionToSql.VisitScalarExpression(Expression.Constant(1), context)
					};
					return SqlFunctionCallScalarExpression.CreateBuiltin("SUBSTRING", arguments);
				}
				return null;
			}

			protected override SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				return null;
			}
		}

		private class StringEqualsVisitor : BuiltinFunctionVisitor
		{
			protected override SqlScalarExpression VisitImplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				if (methodCallExpression.Arguments.Count == 1)
				{
					SqlScalarExpression leftExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Object, context);
					SqlScalarExpression rightExpression = ExpressionToSql.VisitScalarExpression(methodCallExpression.Arguments[0], context);
					return SqlBinaryScalarExpression.Create(SqlBinaryScalarOperatorKind.Equal, leftExpression, rightExpression);
				}
				return null;
			}

			protected override SqlScalarExpression VisitExplicit(MethodCallExpression methodCallExpression, TranslationContext context)
			{
				return null;
			}
		}

		private static Dictionary<string, BuiltinFunctionVisitor> StringBuiltinFunctionDefinitions
		{
			get;
			set;
		}

		static StringBuiltinFunctions()
		{
			StringBuiltinFunctionDefinitions = new Dictionary<string, BuiltinFunctionVisitor>();
			StringBuiltinFunctionDefinitions.Add("Concat", new StringVisitConcat());
			StringBuiltinFunctionDefinitions.Add("Contains", new StringVisitContains());
			StringBuiltinFunctionDefinitions.Add("EndsWith", new SqlBuiltinFunctionVisitor("ENDSWITH", isStatic: false, new List<Type[]>
			{
				new Type[1]
				{
					typeof(string)
				}
			}));
			StringBuiltinFunctionDefinitions.Add("IndexOf", new SqlBuiltinFunctionVisitor("INDEX_OF", isStatic: false, new List<Type[]>
			{
				new Type[1]
				{
					typeof(char)
				},
				new Type[1]
				{
					typeof(string)
				},
				new Type[2]
				{
					typeof(char),
					typeof(int)
				},
				new Type[2]
				{
					typeof(string),
					typeof(int)
				}
			}));
			StringBuiltinFunctionDefinitions.Add("Count", new StringVisitCount());
			StringBuiltinFunctionDefinitions.Add("ToLower", new SqlBuiltinFunctionVisitor("LOWER", isStatic: false, new List<Type[]>
			{
				new Type[0]
			}));
			StringBuiltinFunctionDefinitions.Add("TrimStart", new StringVisitTrimStart());
			StringBuiltinFunctionDefinitions.Add("Replace", new SqlBuiltinFunctionVisitor("REPLACE", isStatic: false, new List<Type[]>
			{
				new Type[2]
				{
					typeof(char),
					typeof(char)
				},
				new Type[2]
				{
					typeof(string),
					typeof(string)
				}
			}));
			StringBuiltinFunctionDefinitions.Add("Reverse", new StringVisitReverse());
			StringBuiltinFunctionDefinitions.Add("TrimEnd", new StringVisitTrimEnd());
			StringBuiltinFunctionDefinitions.Add("StartsWith", new SqlBuiltinFunctionVisitor("STARTSWITH", isStatic: false, new List<Type[]>
			{
				new Type[1]
				{
					typeof(string)
				}
			}));
			StringBuiltinFunctionDefinitions.Add("Substring", new SqlBuiltinFunctionVisitor("SUBSTRING", isStatic: false, new List<Type[]>
			{
				new Type[2]
				{
					typeof(int),
					typeof(int)
				}
			}));
			StringBuiltinFunctionDefinitions.Add("ToUpper", new SqlBuiltinFunctionVisitor("UPPER", isStatic: false, new List<Type[]>
			{
				new Type[0]
			}));
			StringBuiltinFunctionDefinitions.Add("get_Chars", new StringGetCharsVisitor());
			StringBuiltinFunctionDefinitions.Add("Equals", new StringEqualsVisitor());
		}

		public static SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			BuiltinFunctionVisitor value = null;
			if (StringBuiltinFunctionDefinitions.TryGetValue(methodCallExpression.Method.Name, out value))
			{
				return value.Visit(methodCallExpression, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}
	}
}
