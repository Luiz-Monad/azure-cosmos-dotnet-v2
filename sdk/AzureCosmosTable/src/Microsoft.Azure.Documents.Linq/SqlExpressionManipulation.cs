using Microsoft.Azure.Documents.Sql;
using System;
using System.Linq;

namespace Microsoft.Azure.Documents.Linq
{
	internal static class SqlExpressionManipulation
	{
		public static SqlScalarExpression Substitute(SqlScalarExpression replacement, SqlIdentifier toReplace, SqlScalarExpression into)
		{
			if (into == null)
			{
				return null;
			}
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			switch (into.Kind)
			{
			case SqlObjectKind.ArrayCreateScalarExpression:
			{
				SqlArrayCreateScalarExpression sqlArrayCreateScalarExpression = into as SqlArrayCreateScalarExpression;
				if (sqlArrayCreateScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a SqlArrayCreateScalarExpression, got a " + into.GetType());
				}
				SqlScalarExpression[] array2 = new SqlScalarExpression[sqlArrayCreateScalarExpression.Items.Count];
				for (int j = 0; j < array2.Length; j++)
				{
					SqlScalarExpression into2 = sqlArrayCreateScalarExpression.Items[j];
					SqlScalarExpression sqlScalarExpression = array2[j] = Substitute(replacement, toReplace, into2);
				}
				return SqlArrayCreateScalarExpression.Create(array2);
			}
			case SqlObjectKind.BinaryScalarExpression:
			{
				SqlBinaryScalarExpression sqlBinaryScalarExpression = into as SqlBinaryScalarExpression;
				if (sqlBinaryScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a BinaryScalarExpression, got a " + into.GetType());
				}
				SqlScalarExpression leftExpression = Substitute(replacement, toReplace, sqlBinaryScalarExpression.LeftExpression);
				SqlScalarExpression rightExpression = Substitute(replacement, toReplace, sqlBinaryScalarExpression.RightExpression);
				return SqlBinaryScalarExpression.Create(sqlBinaryScalarExpression.OperatorKind, leftExpression, rightExpression);
			}
			case SqlObjectKind.UnaryScalarExpression:
			{
				SqlUnaryScalarExpression sqlUnaryScalarExpression = into as SqlUnaryScalarExpression;
				if (sqlUnaryScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a SqlUnaryScalarExpression, got a " + into.GetType());
				}
				SqlScalarExpression expression2 = Substitute(replacement, toReplace, sqlUnaryScalarExpression.Expression);
				return SqlUnaryScalarExpression.Create(sqlUnaryScalarExpression.OperatorKind, expression2);
			}
			case SqlObjectKind.LiteralScalarExpression:
				return into;
			case SqlObjectKind.FunctionCallScalarExpression:
			{
				SqlFunctionCallScalarExpression sqlFunctionCallScalarExpression = into as SqlFunctionCallScalarExpression;
				if (sqlFunctionCallScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a SqlFunctionCallScalarExpression, got a " + into.GetType());
				}
				SqlScalarExpression[] array3 = new SqlScalarExpression[sqlFunctionCallScalarExpression.Arguments.Count];
				for (int k = 0; k < array3.Length; k++)
				{
					SqlScalarExpression into3 = sqlFunctionCallScalarExpression.Arguments[k];
					SqlScalarExpression sqlScalarExpression2 = array3[k] = Substitute(replacement, toReplace, into3);
				}
				return SqlFunctionCallScalarExpression.Create(sqlFunctionCallScalarExpression.Name, sqlFunctionCallScalarExpression.IsUdf, array3);
			}
			case SqlObjectKind.ObjectCreateScalarExpression:
			{
				SqlObjectCreateScalarExpression obj = into as SqlObjectCreateScalarExpression;
				if (obj == null)
				{
					throw new DocumentQueryException("Expected a SqlObjectCreateScalarExpression, got a " + into.GetType());
				}
				return SqlObjectCreateScalarExpression.Create(from prop in obj.Properties
				select SqlObjectProperty.Create(prop.Name, Substitute(replacement, toReplace, prop.Expression)));
			}
			case SqlObjectKind.MemberIndexerScalarExpression:
			{
				SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression = into as SqlMemberIndexerScalarExpression;
				if (sqlMemberIndexerScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a SqlMemberIndexerScalarExpression, got a " + into.GetType());
				}
				SqlScalarExpression memberExpression = Substitute(replacement, toReplace, sqlMemberIndexerScalarExpression.MemberExpression);
				SqlScalarExpression indexExpression = Substitute(replacement, toReplace, sqlMemberIndexerScalarExpression.IndexExpression);
				return SqlMemberIndexerScalarExpression.Create(memberExpression, indexExpression);
			}
			case SqlObjectKind.PropertyRefScalarExpression:
			{
				SqlPropertyRefScalarExpression sqlPropertyRefScalarExpression = into as SqlPropertyRefScalarExpression;
				if (sqlPropertyRefScalarExpression == null)
				{
					throw new DocumentQueryException("Expected a SqlPropertyRefScalarExpression, got a " + into.GetType());
				}
				if (sqlPropertyRefScalarExpression.MemberExpression == null)
				{
					if (sqlPropertyRefScalarExpression.PropertyIdentifier.Value == toReplace.Value)
					{
						return replacement;
					}
					return sqlPropertyRefScalarExpression;
				}
				return SqlPropertyRefScalarExpression.Create(Substitute(replacement, toReplace, sqlPropertyRefScalarExpression.MemberExpression), sqlPropertyRefScalarExpression.PropertyIdentifier);
			}
			case SqlObjectKind.ConditionalScalarExpression:
			{
				SqlConditionalScalarExpression sqlConditionalScalarExpression = (SqlConditionalScalarExpression)into;
				if (sqlConditionalScalarExpression == null)
				{
					throw new ArgumentException();
				}
				SqlScalarExpression condition = Substitute(replacement, toReplace, sqlConditionalScalarExpression.ConditionExpression);
				SqlScalarExpression first = Substitute(replacement, toReplace, sqlConditionalScalarExpression.FirstExpression);
				SqlScalarExpression second = Substitute(replacement, toReplace, sqlConditionalScalarExpression.SecondExpression);
				return SqlConditionalScalarExpression.Create(condition, first, second);
			}
			case SqlObjectKind.InScalarExpression:
			{
				SqlInScalarExpression sqlInScalarExpression = (SqlInScalarExpression)into;
				if (sqlInScalarExpression == null)
				{
					throw new ArgumentException();
				}
				SqlScalarExpression expression = Substitute(replacement, toReplace, sqlInScalarExpression.Expression);
				SqlScalarExpression[] array = new SqlScalarExpression[sqlInScalarExpression.Items.Count];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = Substitute(replacement, toReplace, sqlInScalarExpression.Items[i]);
				}
				return SqlInScalarExpression.Create(expression, sqlInScalarExpression.Not, array);
			}
			default:
				throw new ArgumentOutOfRangeException("Unexpected Sql Scalar expression kind " + into.Kind);
			}
		}
	}
}
