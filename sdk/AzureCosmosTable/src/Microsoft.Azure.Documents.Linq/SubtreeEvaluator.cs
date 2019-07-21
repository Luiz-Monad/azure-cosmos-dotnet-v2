using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary> 
	/// Evaluates and replaces sub-trees when first candidate is reached (top-down) 
	/// </summary> 
	internal sealed class SubtreeEvaluator : ExpressionVisitor
	{
		private HashSet<Expression> candidates;

		public SubtreeEvaluator(HashSet<Expression> candidates)
		{
			this.candidates = candidates;
		}

		public Expression Evaluate(Expression expression)
		{
			return Visit(expression);
		}

		public override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}
			if (candidates.Contains(expression))
			{
				return EvaluateConstant(expression);
			}
			return base.Visit(expression);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return node;
		}

		private Expression EvaluateConstant(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Constant)
			{
				return expression;
			}
			return Expression.Constant(Expression.Lambda(expression).Compile().DynamicInvoke(null), expression.Type);
		}
	}
}
