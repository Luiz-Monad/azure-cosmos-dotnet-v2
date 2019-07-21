using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class DocumentQueryEvaluator
	{
		private const string SQLMethod = "AsSQL";

		public static SqlQuerySpec Evaluate(Expression expression)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.Constant:
				return HandleEmptyQuery((ConstantExpression)expression);
			case ExpressionType.Call:
				return HandleMethodCallExpression((MethodCallExpression)expression);
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, expression.ToString()));
			}
		}

		public static bool IsTransformExpression(Expression expression)
		{
			MethodCallExpression methodCallExpression = expression as MethodCallExpression;
			if (methodCallExpression != null && (object)methodCallExpression.Method.DeclaringType == typeof(DocumentQueryable))
			{
				return methodCallExpression.Method.Name == "AsSQL";
			}
			return false;
		}

		/// <summary>
		/// This is to handle the case, where user just executes code like this.
		/// foreach(Database db in client.CreateDatabaseQuery()) {}        
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		private static SqlQuerySpec HandleEmptyQuery(ConstantExpression expression)
		{
			if (expression.Value == null)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, expression.ToString()));
			}
			Type type = expression.Value.GetType();
			if (!type.IsGenericType() || (object)type.GetGenericTypeDefinition() != typeof(DocumentQuery<bool>).GetGenericTypeDefinition())
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, expression.ToString()));
			}
			return null;
		}

		private static SqlQuerySpec HandleMethodCallExpression(MethodCallExpression expression)
		{
			if (IsTransformExpression(expression))
			{
				if (string.Compare(expression.Method.Name, "AsSQL", StringComparison.Ordinal) == 0)
				{
					return HandleAsSqlTransformExpression(expression);
				}
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, expression.ToString()));
			}
			return SqlTranslator.TranslateQuery(expression);
		}

		/// <summary>
		/// foreach(string record in client.CreateDocumentQuery().Navigate("Raw JQuery"))
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		private static SqlQuerySpec HandleAsSqlTransformExpression(MethodCallExpression expression)
		{
			Expression expression2 = expression.Arguments[1];
			if (expression2.NodeType == ExpressionType.Lambda)
			{
				return GetSqlQuerySpec(((LambdaExpression)expression2).Compile().DynamicInvoke(null));
			}
			if (expression2.NodeType == ExpressionType.Constant)
			{
				return GetSqlQuerySpec(((ConstantExpression)expression2).Value);
			}
			return GetSqlQuerySpec(Expression.Lambda(expression2).Compile().DynamicInvoke(null));
		}

		private static SqlQuerySpec GetSqlQuerySpec(object value)
		{
			if (value == null)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, value));
			}
			if ((object)value.GetType() == typeof(SqlQuerySpec))
			{
				return (SqlQuerySpec)value;
			}
			if ((object)value.GetType() == typeof(string))
			{
				return new SqlQuerySpec((string)value);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentUICulture, ClientResources.BadQuery_InvalidExpression, value));
		}
	}
}
