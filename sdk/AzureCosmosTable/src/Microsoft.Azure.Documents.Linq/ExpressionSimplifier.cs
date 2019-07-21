using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal abstract class ExpressionSimplifier
	{
		private static ConcurrentDictionary<Type, ExpressionSimplifier> cached = new ConcurrentDictionary<Type, ExpressionSimplifier>();

		public abstract object EvalBoxed(Expression expr);

		public static object Evaluate(Expression expr)
		{
			ExpressionSimplifier expressionSimplifier;
			if (cached.ContainsKey(expr.Type))
			{
				expressionSimplifier = cached[expr.Type];
			}
			else
			{
				expressionSimplifier = (ExpressionSimplifier)Activator.CreateInstance(typeof(ExpressionSimplifier<>).MakeGenericType(expr.Type));
				cached.TryAdd(expr.Type, expressionSimplifier);
			}
			return expressionSimplifier.EvalBoxed(expr);
		}

		public static Expression EvaluateToExpression(Expression expr)
		{
			return Expression.Constant(Evaluate(expr), expr.Type);
		}
	}
	internal sealed class ExpressionSimplifier<T> : ExpressionSimplifier
	{
		public override object EvalBoxed(Expression expr)
		{
			return Eval(expr);
		}

		public T Eval(Expression expr)
		{
			return Expression.Lambda<Func<T>>(expr, Array.Empty<ParameterExpression>()).Compile()();
		}
	}
}
