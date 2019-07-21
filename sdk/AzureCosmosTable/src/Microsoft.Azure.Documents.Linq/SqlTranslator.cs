using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Wrapper class for translating LINQ to DocDB SQL.
	/// </summary>
	internal static class SqlTranslator
	{
		/// <summary>
		/// This function exists for testing only.
		/// </summary>
		/// <param name="inputExpression">Expression to translate.</param>
		/// <returns>A string describing the expression translation.</returns>
		internal static string TranslateExpression(Expression inputExpression)
		{
			TranslationContext context = new TranslationContext();
			inputExpression = ConstantEvaluator.PartialEval(inputExpression);
			return ExpressionToSql.VisitNonSubqueryScalarExpression(inputExpression, context).ToString();
		}

		internal static string TranslateExpressionOld(Expression inputExpression)
		{
			TranslationContext context = new TranslationContext();
			inputExpression = ConstantFolding.Fold(inputExpression);
			return ExpressionToSql.VisitNonSubqueryScalarExpression(inputExpression, context).ToString();
		}

		internal static SqlQuerySpec TranslateQuery(Expression inputExpression)
		{
			inputExpression = ConstantEvaluator.PartialEval(inputExpression);
			return new SqlQuerySpec(ExpressionToSql.TranslateQuery(inputExpression).ToString());
		}
	}
}
