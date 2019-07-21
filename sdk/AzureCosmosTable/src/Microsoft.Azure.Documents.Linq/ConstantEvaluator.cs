using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class ConstantEvaluator
	{
		/// <summary> 
		/// Performs evaluation and replacement of independent sub-trees 
		/// </summary> 
		/// <param name="expression">The root of the expression tree.</param>
		/// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return new SubtreeEvaluator(Nominator.Nominate(expression, fnCanBeEvaluated)).Evaluate(expression);
		}

		/// <summary> 
		/// Performs evaluation and replacement of independent sub-trees 
		/// </summary> 
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
		public static Expression PartialEval(Expression expression)
		{
			return PartialEval(expression, CanBeEvaluated);
		}

		private static bool CanBeEvaluated(Expression expression)
		{
			ConstantExpression constantExpression = expression as ConstantExpression;
			if (constantExpression != null && constantExpression.Value is IQueryable)
			{
				return false;
			}
			MethodCallExpression methodCallExpression = expression as MethodCallExpression;
			if (methodCallExpression != null)
			{
				Type declaringType = methodCallExpression.Method.DeclaringType;
				if ((object)declaringType == typeof(Enumerable) || (object)declaringType == typeof(Queryable) || (object)declaringType == typeof(UserDefinedFunctionProvider))
				{
					return false;
				}
			}
			if (expression.NodeType == ExpressionType.Constant && (object)expression.Type == typeof(object))
			{
				return true;
			}
			if (expression.NodeType == ExpressionType.Parameter || expression.NodeType == ExpressionType.Lambda)
			{
				return false;
			}
			return true;
		}
	}
}
