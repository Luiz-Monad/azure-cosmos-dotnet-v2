using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class MathBuiltinFunctions
	{
		private static Dictionary<string, BuiltinFunctionVisitor> MathBuiltinFunctionDefinitions
		{
			get;
			set;
		}

		static MathBuiltinFunctions()
		{
			MathBuiltinFunctionDefinitions = new Dictionary<string, BuiltinFunctionVisitor>();
			MathBuiltinFunctionDefinitions.Add("Abs", new SqlBuiltinFunctionVisitor("ABS", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				},
				new Type[1]
				{
					typeof(float)
				},
				new Type[1]
				{
					typeof(int)
				},
				new Type[1]
				{
					typeof(long)
				},
				new Type[1]
				{
					typeof(sbyte)
				},
				new Type[1]
				{
					typeof(short)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Acos", new SqlBuiltinFunctionVisitor("ACOS", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Asin", new SqlBuiltinFunctionVisitor("ASIN", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Atan", new SqlBuiltinFunctionVisitor("ATAN", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Atan2", new SqlBuiltinFunctionVisitor("ATN2", isStatic: true, new List<Type[]>
			{
				new Type[2]
				{
					typeof(double),
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Ceiling", new SqlBuiltinFunctionVisitor("CEILING", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Cos", new SqlBuiltinFunctionVisitor("COS", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Exp", new SqlBuiltinFunctionVisitor("EXP", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Floor", new SqlBuiltinFunctionVisitor("FLOOR", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Log", new SqlBuiltinFunctionVisitor("LOG", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				},
				new Type[2]
				{
					typeof(double),
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Log10", new SqlBuiltinFunctionVisitor("LOG10", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Pow", new SqlBuiltinFunctionVisitor("POWER", isStatic: true, new List<Type[]>
			{
				new Type[2]
				{
					typeof(double),
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Round", new SqlBuiltinFunctionVisitor("ROUND", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Sign", new SqlBuiltinFunctionVisitor("SIGN", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				},
				new Type[1]
				{
					typeof(float)
				},
				new Type[1]
				{
					typeof(int)
				},
				new Type[1]
				{
					typeof(long)
				},
				new Type[1]
				{
					typeof(sbyte)
				},
				new Type[1]
				{
					typeof(short)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Sin", new SqlBuiltinFunctionVisitor("SIN", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Sqrt", new SqlBuiltinFunctionVisitor("SQRT", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Tan", new SqlBuiltinFunctionVisitor("TAN", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(double)
				}
			}));
			MathBuiltinFunctionDefinitions.Add("Truncate", new SqlBuiltinFunctionVisitor("TRUNC", isStatic: true, new List<Type[]>
			{
				new Type[1]
				{
					typeof(decimal)
				},
				new Type[1]
				{
					typeof(double)
				}
			}));
		}

		public static SqlScalarExpression Visit(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			BuiltinFunctionVisitor value = null;
			if (MathBuiltinFunctionDefinitions.TryGetValue(methodCallExpression.Method.Name, out value))
			{
				return value.Visit(methodCallExpression, context);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, methodCallExpression.Method.Name));
		}
	}
}
