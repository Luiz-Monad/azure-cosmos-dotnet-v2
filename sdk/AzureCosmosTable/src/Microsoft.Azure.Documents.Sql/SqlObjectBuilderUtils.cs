using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents.Sql
{
	internal static class SqlObjectBuilderUtils
	{
		public static SqlMemberIndexerScalarExpression CreateSqlMemberIndexerScalarExpression(SqlScalarExpression first, SqlScalarExpression second, params SqlScalarExpression[] everythingElse)
		{
			List<SqlScalarExpression> list = new List<SqlScalarExpression>(2 + everythingElse.Length);
			list.Add(first);
			list.Add(second);
			list.AddRange(everythingElse);
			SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression = SqlMemberIndexerScalarExpression.Create(first, second);
			foreach (SqlScalarExpression item in list.Skip(2))
			{
				sqlMemberIndexerScalarExpression = SqlMemberIndexerScalarExpression.Create(sqlMemberIndexerScalarExpression, item);
			}
			return sqlMemberIndexerScalarExpression;
		}
	}
}
