using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Simplifies an Expression tree evaluating everything that can be evaluated 
	/// at the current time.  Could be more efficient by evaluating a complete constant subtree at once.
	/// </summary>
	internal static class ConstantFolding
	{
		public static bool IsConstant(Expression inputExpression)
		{
			if (inputExpression != null)
			{
				return inputExpression.NodeType == ExpressionType.Constant;
			}
			return true;
		}

		public static Expression Fold(Expression inputExpression)
		{
			if (inputExpression == null)
			{
				return inputExpression;
			}
			switch (inputExpression.NodeType)
			{
			case ExpressionType.ArrayLength:
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.Negate:
			case ExpressionType.UnaryPlus:
			case ExpressionType.NegateChecked:
			case ExpressionType.Not:
			case ExpressionType.Quote:
			case ExpressionType.TypeAs:
			case ExpressionType.Decrement:
			case ExpressionType.Increment:
			case ExpressionType.OnesComplement:
				return FoldUnary((UnaryExpression)inputExpression);
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
			case ExpressionType.And:
			case ExpressionType.AndAlso:
			case ExpressionType.ArrayIndex:
			case ExpressionType.Coalesce:
			case ExpressionType.Divide:
			case ExpressionType.Equal:
			case ExpressionType.ExclusiveOr:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
			case ExpressionType.LeftShift:
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
			case ExpressionType.Modulo:
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
			case ExpressionType.NotEqual:
			case ExpressionType.Or:
			case ExpressionType.OrElse:
			case ExpressionType.RightShift:
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
				return FoldBinary((BinaryExpression)inputExpression);
			case ExpressionType.TypeIs:
				return FoldTypeIs((TypeBinaryExpression)inputExpression);
			case ExpressionType.Conditional:
				return FoldConditional((ConditionalExpression)inputExpression);
			case ExpressionType.Constant:
				return inputExpression;
			case ExpressionType.Parameter:
				return FoldParameter((ParameterExpression)inputExpression);
			case ExpressionType.MemberAccess:
				return FoldMemberAccess((MemberExpression)inputExpression);
			case ExpressionType.Call:
				return FoldMethodCall((MethodCallExpression)inputExpression);
			case ExpressionType.Lambda:
				return FoldLambda((LambdaExpression)inputExpression);
			case ExpressionType.New:
				return FoldNew((NewExpression)inputExpression);
			case ExpressionType.NewArrayInit:
			case ExpressionType.NewArrayBounds:
				return FoldNewArray((NewArrayExpression)inputExpression);
			case ExpressionType.Invoke:
				return FoldInvocation((InvocationExpression)inputExpression);
			case ExpressionType.MemberInit:
				return FoldMemberInit((MemberInitExpression)inputExpression);
			case ExpressionType.ListInit:
				return FoldListInit((ListInitExpression)inputExpression);
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, "Unhandled expression type: '{0}'", inputExpression.NodeType));
			}
		}

		public static MemberBinding FoldBinding(MemberBinding inputExpression)
		{
			switch (inputExpression.BindingType)
			{
			case MemberBindingType.Assignment:
				return FoldMemberAssignment((MemberAssignment)inputExpression);
			case MemberBindingType.MemberBinding:
				return FoldMemberMemberBinding((MemberMemberBinding)inputExpression);
			case MemberBindingType.ListBinding:
				return FoldMemberListBinding((MemberListBinding)inputExpression);
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, "Unhandled binding type '{0}'", inputExpression.BindingType));
			}
		}

		public static ElementInit FoldElementInitializer(ElementInit inputExpression)
		{
			ReadOnlyCollection<Expression> readOnlyCollection = FoldExpressionList(inputExpression.Arguments);
			if (readOnlyCollection != inputExpression.Arguments)
			{
				return Expression.ElementInit(inputExpression.AddMethod, readOnlyCollection);
			}
			return inputExpression;
		}

		public static Expression FoldUnary(UnaryExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Operand);
			Expression expression2 = (expression == inputExpression.Operand) ? inputExpression : Expression.MakeUnary(inputExpression.NodeType, expression, inputExpression.Type, inputExpression.Method);
			if (IsConstant(expression))
			{
				expression2 = ExpressionSimplifier.EvaluateToExpression(expression2);
			}
			return expression2;
		}

		public static Expression FoldBinary(BinaryExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Left);
			Expression expression2 = Fold(inputExpression.Right);
			LambdaExpression lambdaExpression = FoldLambda(inputExpression.Conversion);
			Expression expression3 = (expression == inputExpression.Left && expression2 == inputExpression.Right && lambdaExpression == inputExpression.Conversion) ? inputExpression : ((inputExpression.NodeType != ExpressionType.Coalesce) ? Expression.MakeBinary(inputExpression.NodeType, expression, expression2, inputExpression.IsLiftedToNull, inputExpression.Method) : Expression.Coalesce(expression, expression2, lambdaExpression));
			if (IsConstant(expression) && inputExpression.NodeType == ExpressionType.Coalesce)
			{
				object obj = ExpressionSimplifier.Evaluate(expression);
				expression3 = ((obj != null) ? Expression.Constant(obj) : expression2);
			}
			else if (IsConstant(expression) && IsConstant(expression2))
			{
				expression3 = ExpressionSimplifier.EvaluateToExpression(expression3);
			}
			return expression3;
		}

		public static Expression FoldTypeIs(TypeBinaryExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Expression);
			Expression expression2 = (expression == inputExpression.Expression) ? inputExpression : Expression.TypeIs(expression, inputExpression.TypeOperand);
			if (IsConstant(expression))
			{
				expression2 = ExpressionSimplifier.EvaluateToExpression(expression2);
			}
			return expression2;
		}

		public static Expression FoldConstant(ConstantExpression inputExpression)
		{
			return inputExpression;
		}

		public static Expression FoldConditional(ConditionalExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Test);
			Expression expression2 = Fold(inputExpression.IfTrue);
			Expression expression3 = Fold(inputExpression.IfFalse);
			Expression result = (expression == inputExpression.Test && expression2 == inputExpression.IfTrue && expression3 == inputExpression.IfFalse) ? inputExpression : Expression.Condition(expression, expression2, expression3);
			if (IsConstant(expression))
			{
				result = ((!(bool)ExpressionSimplifier.Evaluate(expression)) ? expression3 : expression2);
			}
			return result;
		}

		public static Expression FoldParameter(ParameterExpression inputExpression)
		{
			return inputExpression;
		}

		public static Expression FoldMemberAccess(MemberExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Expression);
			Expression expression2 = (expression == inputExpression.Expression) ? inputExpression : Expression.MakeMemberAccess(expression, inputExpression.Member);
			if (IsConstant(expression))
			{
				expression2 = ExpressionSimplifier.EvaluateToExpression(expression2);
			}
			return expression2;
		}

		public static Expression FoldMethodCall(MethodCallExpression inputExpression)
		{
			Expression expression = Fold(inputExpression.Object);
			ReadOnlyCollection<Expression> readOnlyCollection = FoldExpressionList(inputExpression.Arguments);
			Expression expression2 = (expression == inputExpression.Object && readOnlyCollection == inputExpression.Arguments) ? inputExpression : Expression.Call(expression, inputExpression.Method, readOnlyCollection);
			if (!IsConstant(expression))
			{
				return expression2;
			}
			foreach (Expression item in readOnlyCollection)
			{
				if (!IsConstant(item))
				{
					return expression2;
				}
			}
			if (inputExpression.Method.IsStatic && CustomTypeExtensions.IsAssignableFrom(inputExpression.Method.DeclaringType, typeof(Queryable)) && inputExpression.Method.Name.Equals("Take"))
			{
				return expression2;
			}
			return ExpressionSimplifier.EvaluateToExpression(expression2);
		}

		public static ReadOnlyCollection<Expression> FoldExpressionList(ReadOnlyCollection<Expression> inputExpressionList)
		{
			List<Expression> list = null;
			for (int i = 0; i < inputExpressionList.Count; i++)
			{
				Expression expression = Fold(inputExpressionList[i]);
				if (list != null)
				{
					list.Add(expression);
				}
				else if (expression != inputExpressionList[i])
				{
					list = new List<Expression>(inputExpressionList.Count);
					for (int j = 0; j < i; j++)
					{
						list.Add(inputExpressionList[j]);
					}
					list.Add(expression);
				}
			}
			if (list != null)
			{
				return list.AsReadOnly();
			}
			return inputExpressionList;
		}

		public static MemberAssignment FoldMemberAssignment(MemberAssignment inputExpression)
		{
			Expression expression = Fold(inputExpression.Expression);
			if (expression != inputExpression.Expression)
			{
				return Expression.Bind(inputExpression.Member, expression);
			}
			return inputExpression;
		}

		public static MemberMemberBinding FoldMemberMemberBinding(MemberMemberBinding inputExpression)
		{
			IEnumerable<MemberBinding> enumerable = FoldBindingList(inputExpression.Bindings);
			if (enumerable != inputExpression.Bindings)
			{
				return Expression.MemberBind(inputExpression.Member, enumerable);
			}
			return inputExpression;
		}

		public static MemberListBinding FoldMemberListBinding(MemberListBinding inputExpression)
		{
			IEnumerable<ElementInit> enumerable = FoldElementInitializerList(inputExpression.Initializers);
			if (enumerable != inputExpression.Initializers)
			{
				return Expression.ListBind(inputExpression.Member, enumerable);
			}
			return inputExpression;
		}

		public static IList<MemberBinding> FoldBindingList(ReadOnlyCollection<MemberBinding> inputExpressionList)
		{
			List<MemberBinding> list = null;
			for (int i = 0; i < inputExpressionList.Count; i++)
			{
				MemberBinding memberBinding = FoldBinding(inputExpressionList[i]);
				if (list != null)
				{
					list.Add(memberBinding);
				}
				else if (memberBinding != inputExpressionList[i])
				{
					list = new List<MemberBinding>(inputExpressionList.Count);
					for (int j = 0; j < i; j++)
					{
						list.Add(inputExpressionList[j]);
					}
					list.Add(memberBinding);
				}
			}
			if (list != null)
			{
				return list;
			}
			return inputExpressionList;
		}

		public static IList<ElementInit> FoldElementInitializerList(ReadOnlyCollection<ElementInit> inputExpressionList)
		{
			List<ElementInit> list = null;
			for (int i = 0; i < inputExpressionList.Count; i++)
			{
				ElementInit elementInit = FoldElementInitializer(inputExpressionList[i]);
				if (list != null)
				{
					list.Add(elementInit);
				}
				else if (elementInit != inputExpressionList[i])
				{
					list = new List<ElementInit>(inputExpressionList.Count);
					for (int j = 0; j < i; j++)
					{
						list.Add(inputExpressionList[j]);
					}
					list.Add(elementInit);
				}
			}
			if (list != null)
			{
				return list;
			}
			return inputExpressionList;
		}

		public static LambdaExpression FoldLambda(LambdaExpression inputExpression)
		{
			if (inputExpression == null)
			{
				return null;
			}
			Expression expression = Fold(inputExpression.Body);
			if (expression != inputExpression.Body)
			{
				return Expression.Lambda(inputExpression.Type, expression, inputExpression.Parameters);
			}
			return inputExpression;
		}

		public static NewExpression FoldNew(NewExpression inputExpression)
		{
			IEnumerable<Expression> enumerable = FoldExpressionList(inputExpression.Arguments);
			if (enumerable != inputExpression.Arguments)
			{
				if (inputExpression.Members != null)
				{
					return Expression.New(inputExpression.Constructor, enumerable, inputExpression.Members);
				}
				return Expression.New(inputExpression.Constructor, enumerable);
			}
			return inputExpression;
		}

		public static Expression FoldMemberInit(MemberInitExpression inputExpression)
		{
			NewExpression newExpression = FoldNew(inputExpression.NewExpression);
			IEnumerable<MemberBinding> enumerable = FoldBindingList(inputExpression.Bindings);
			if (newExpression != inputExpression.NewExpression || enumerable != inputExpression.Bindings)
			{
				return Expression.MemberInit(newExpression, enumerable);
			}
			return inputExpression;
		}

		public static Expression FoldListInit(ListInitExpression inputExpression)
		{
			NewExpression newExpression = FoldNew(inputExpression.NewExpression);
			IEnumerable<ElementInit> enumerable = FoldElementInitializerList(inputExpression.Initializers);
			if (newExpression != inputExpression.NewExpression || enumerable != inputExpression.Initializers)
			{
				return Expression.ListInit(newExpression, enumerable);
			}
			return inputExpression;
		}

		public static Expression FoldNewArray(NewArrayExpression inputExpression)
		{
			IEnumerable<Expression> enumerable = FoldExpressionList(inputExpression.Expressions);
			if (enumerable != inputExpression.Expressions)
			{
				if (inputExpression.NodeType == ExpressionType.NewArrayInit)
				{
					return Expression.NewArrayInit(inputExpression.Type.GetElementType(), enumerable);
				}
				return Expression.NewArrayBounds(inputExpression.Type.GetElementType(), enumerable);
			}
			return inputExpression;
		}

		public static Expression FoldInvocation(InvocationExpression inputExpression)
		{
			IEnumerable<Expression> enumerable = FoldExpressionList(inputExpression.Arguments);
			Expression expression = Fold(inputExpression.Expression);
			Expression expression2 = (enumerable == inputExpression.Arguments && expression == inputExpression.Expression) ? inputExpression : Expression.Invoke(expression, enumerable);
			foreach (Expression item in enumerable)
			{
				if (!IsConstant(item))
				{
					return expression2;
				}
			}
			return ExpressionSimplifier.EvaluateToExpression(expression2);
		}
	}
}
