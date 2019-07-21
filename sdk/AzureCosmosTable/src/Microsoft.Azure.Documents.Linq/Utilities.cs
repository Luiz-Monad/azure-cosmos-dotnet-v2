using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class Utilities
	{
		/// <summary>
		/// Add quotation signs to a string.
		/// </summary>
		/// <param name="toQuote">String to quote.</param>
		/// <returns>A string properly quoted for embedding into SQL.</returns>
		public static string SqlQuoteString(string toQuote)
		{
			toQuote = toQuote.Replace("'", "\\'").Replace("\"", "\\\"");
			toQuote = "\"" + toQuote + "\"";
			return toQuote;
		}

		/// <summary>
		/// Get a lambda expression; may unpeel quotes.
		/// </summary> 
		/// <param name="expr">Expression to convert to a lambda.</param>
		/// <returns>The contained lambda expression, or an exception.</returns>
		public static LambdaExpression GetLambda(Expression expr)
		{
			while (expr.NodeType == ExpressionType.Quote)
			{
				expr = ((UnaryExpression)expr).Operand;
			}
			if (expr.NodeType != ExpressionType.Lambda)
			{
				throw new ArgumentException("Expected a lambda expression");
			}
			return expr as LambdaExpression;
		}

		/// <summary>
		/// Generate a new parameter and add it to the current scope.
		/// </summary>
		/// <param name="prefix">Prefix for the parameter name.</param>
		/// <param name="type">Parameter type.</param>
		/// <param name="inScope">Names to avoid.</param>
		/// <returns>The new parameter.</returns>
		public static ParameterExpression NewParameter(string prefix, Type type, HashSet<ParameterExpression> inScope)
		{
			int num = 0;
			ParameterExpression parameterExpression;
			while (true)
			{
				string name = prefix + num.ToString(CultureInfo.InvariantCulture);
				parameterExpression = Expression.Parameter(type, name);
				if (!inScope.Any((ParameterExpression p) => p.Name.Equals(name)))
				{
					break;
				}
				num++;
			}
			inScope.Add(parameterExpression);
			return parameterExpression;
		}
	}
}
