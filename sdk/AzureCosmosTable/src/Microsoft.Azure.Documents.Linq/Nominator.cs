using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary> 
	/// Performs bottom-up analysis to determine which nodes can possibly 
	/// be part of an evaluated sub-tree. 
	/// </summary>
	internal static class Nominator
	{
		private sealed class NominatorVisitor : ExpressionVisitor
		{
			private readonly Func<Expression, bool> fnCanBeEvaluated;

			private HashSet<Expression> candidates;

			private bool canBeEvaluated;

			public NominatorVisitor(Func<Expression, bool> fnCanBeEvaluated)
			{
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			public HashSet<Expression> Nominate(Expression expression)
			{
				candidates = new HashSet<Expression>();
				Visit(expression);
				return candidates;
			}

			public override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					bool flag = canBeEvaluated;
					canBeEvaluated = true;
					base.Visit(expression);
					if (canBeEvaluated)
					{
						canBeEvaluated = fnCanBeEvaluated(expression);
						if (canBeEvaluated)
						{
							candidates.Add(expression);
						}
					}
					canBeEvaluated = (canBeEvaluated && flag);
				}
				return expression;
			}
		}

		public static HashSet<Expression> Nominate(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return new NominatorVisitor(fnCanBeEvaluated).Nominate(expression);
		}
	}
}
