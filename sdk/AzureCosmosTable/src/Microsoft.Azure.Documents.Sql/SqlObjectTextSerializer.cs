using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlObjectTextSerializer : SqlObjectVisitor
	{
		private const bool MongoDoesNotUseBaselineFiles = true;

		private static readonly string Tab = "    ";

		private readonly StringWriter writer;

		private readonly bool prettyPrint;

		private int indentLevel;

		public SqlObjectTextSerializer(bool prettyPrint)
		{
			writer = new StringWriter(CultureInfo.InvariantCulture);
			this.prettyPrint = prettyPrint;
		}

		public override void Visit(SqlAliasedCollectionExpression sqlAliasedCollectionExpression)
		{
			sqlAliasedCollectionExpression.Collection.Accept(this);
			if (sqlAliasedCollectionExpression.Alias != null)
			{
				writer.Write(" AS ");
				sqlAliasedCollectionExpression.Alias.Accept(this);
			}
		}

		public override void Visit(SqlArrayCreateScalarExpression sqlArrayCreateScalarExpression)
		{
			switch (sqlArrayCreateScalarExpression.Items.Count())
			{
			case 0:
				writer.Write("[]");
				return;
			case 1:
				writer.Write("[");
				sqlArrayCreateScalarExpression.Items[0].Accept(this);
				writer.Write("]");
				return;
			}
			WriteStartContext("[");
			for (int i = 0; i < sqlArrayCreateScalarExpression.Items.Count; i++)
			{
				if (i > 0)
				{
					WriteDelimiter(",");
				}
				sqlArrayCreateScalarExpression.Items[i].Accept(this);
			}
			WriteEndContext("]");
		}

		public override void Visit(SqlArrayIteratorCollectionExpression sqlArrayIteratorCollectionExpression)
		{
			sqlArrayIteratorCollectionExpression.Alias.Accept(this);
			writer.Write(" IN ");
			sqlArrayIteratorCollectionExpression.Collection.Accept(this);
		}

		public override void Visit(SqlArrayScalarExpression sqlArrayScalarExpression)
		{
			writer.Write("ARRAY");
			WriteStartContext("(");
			sqlArrayScalarExpression.SqlQuery.Accept(this);
			WriteEndContext(")");
		}

		public override void Visit(SqlBetweenScalarExpression sqlBetweenScalarExpression)
		{
			writer.Write("(");
			sqlBetweenScalarExpression.Expression.Accept(this);
			if (sqlBetweenScalarExpression.IsNot)
			{
				writer.Write(" NOT");
			}
			writer.Write(" BETWEEN ");
			sqlBetweenScalarExpression.LeftExpression.Accept(this);
			writer.Write(" AND ");
			sqlBetweenScalarExpression.RightExpression.Accept(this);
			writer.Write(")");
		}

		public override void Visit(SqlBinaryScalarExpression sqlBinaryScalarExpression)
		{
			writer.Write("(");
			sqlBinaryScalarExpression.LeftExpression.Accept(this);
			writer.Write(" ");
			writer.Write(SqlBinaryScalarOperatorKindToString(sqlBinaryScalarExpression.OperatorKind));
			writer.Write(" ");
			sqlBinaryScalarExpression.RightExpression.Accept(this);
			writer.Write(")");
		}

		public override void Visit(SqlBooleanLiteral sqlBooleanLiteral)
		{
			writer.Write(sqlBooleanLiteral.Value ? "true" : "false");
		}

		public override void Visit(SqlCoalesceScalarExpression sqlCoalesceScalarExpression)
		{
			writer.Write("(");
			sqlCoalesceScalarExpression.LeftExpression.Accept(this);
			writer.Write(" ?? ");
			sqlCoalesceScalarExpression.RightExpression.Accept(this);
			writer.Write(")");
		}

		public override void Visit(SqlConditionalScalarExpression sqlConditionalScalarExpression)
		{
			writer.Write('(');
			sqlConditionalScalarExpression.ConditionExpression.Accept(this);
			writer.Write(" ? ");
			sqlConditionalScalarExpression.FirstExpression.Accept(this);
			writer.Write(" : ");
			sqlConditionalScalarExpression.SecondExpression.Accept(this);
			writer.Write(')');
		}

		public override void Visit(SqlConversionScalarExpression sqlConversionScalarExpression)
		{
			sqlConversionScalarExpression.expression.Accept(this);
		}

		public override void Visit(SqlExistsScalarExpression sqlExistsScalarExpression)
		{
			writer.Write("EXISTS");
			WriteStartContext("(");
			sqlExistsScalarExpression.SqlQuery.Accept(this);
			WriteEndContext(")");
		}

		public override void Visit(SqlFromClause sqlFromClause)
		{
			writer.Write("FROM ");
			sqlFromClause.Expression.Accept(this);
		}

		public override void Visit(SqlFunctionCallScalarExpression sqlFunctionCallScalarExpression)
		{
			if (sqlFunctionCallScalarExpression.IsUdf)
			{
				writer.Write("udf.");
			}
			sqlFunctionCallScalarExpression.Name.Accept(this);
			switch (sqlFunctionCallScalarExpression.Arguments.Count())
			{
			case 0:
				writer.Write("()");
				return;
			case 1:
				writer.Write("(");
				sqlFunctionCallScalarExpression.Arguments[0].Accept(this);
				writer.Write(")");
				return;
			}
			WriteStartContext("(");
			for (int i = 0; i < sqlFunctionCallScalarExpression.Arguments.Count; i++)
			{
				if (i > 0)
				{
					WriteDelimiter(",");
				}
				sqlFunctionCallScalarExpression.Arguments[i].Accept(this);
			}
			WriteEndContext(")");
		}

		public override void Visit(SqlGeoNearCallScalarExpression sqlGeoNearCallScalarExpression)
		{
			writer.Write("(");
			writer.Write("_ST_DISTANCE");
			writer.Write("(");
			sqlGeoNearCallScalarExpression.PropertyRef.Accept(this);
			writer.Write(",");
			sqlGeoNearCallScalarExpression.Geometry.Accept(this);
			writer.Write(")");
			writer.Write(" BETWEEN ");
			if (!sqlGeoNearCallScalarExpression.NumberOfPoints.HasValue)
			{
				writer.Write(sqlGeoNearCallScalarExpression.MinimumDistance);
				writer.Write(" AND ");
				writer.Write(sqlGeoNearCallScalarExpression.MaximumDistance);
			}
			else
			{
				writer.Write("@nearMinimumDistance");
				writer.Write(" AND ");
				writer.Write("@nearMaximumDistance");
			}
			writer.Write(")");
		}

		public override void Visit(SqlGroupByClause sqlGroupByClause)
		{
			writer.Write("GROUP BY ");
			sqlGroupByClause.Expressions[0].Accept(this);
			for (int i = 1; i < sqlGroupByClause.Expressions.Count; i++)
			{
				writer.Write(", ");
				sqlGroupByClause.Expressions[i].Accept(this);
			}
		}

		public override void Visit(SqlIdentifier sqlIdentifier)
		{
			writer.Write(sqlIdentifier.Value);
		}

		public override void Visit(SqlIdentifierPathExpression sqlIdentifierPathExpression)
		{
			if (sqlIdentifierPathExpression.ParentPath != null)
			{
				sqlIdentifierPathExpression.ParentPath.Accept(this);
				writer.Write(".");
			}
			sqlIdentifierPathExpression.Value.Accept(this);
		}

		public override void Visit(SqlInputPathCollection sqlInputPathCollection)
		{
			sqlInputPathCollection.Input.Accept(this);
			if (sqlInputPathCollection.RelativePath != null)
			{
				sqlInputPathCollection.RelativePath.Accept(this);
			}
		}

		public override void Visit(SqlInScalarExpression sqlInScalarExpression)
		{
			writer.Write("(");
			sqlInScalarExpression.Expression.Accept(this);
			if (sqlInScalarExpression.Not)
			{
				writer.Write(" NOT");
			}
			writer.Write(" IN ");
			switch (sqlInScalarExpression.Items.Count())
			{
			case 0:
				writer.Write("()");
				break;
			case 1:
				writer.Write("(");
				sqlInScalarExpression.Items[0].Accept(this);
				writer.Write(")");
				break;
			default:
				WriteStartContext("(");
				for (int i = 0; i < sqlInScalarExpression.Items.Count; i++)
				{
					if (i > 0)
					{
						WriteDelimiter(",");
					}
					sqlInScalarExpression.Items[i].Accept(this);
				}
				WriteEndContext(")");
				break;
			}
			writer.Write(")");
		}

		public override void Visit(SqlJoinCollectionExpression sqlJoinCollectionExpression)
		{
			sqlJoinCollectionExpression.LeftExpression.Accept(this);
			WriteNewline();
			WriteTab();
			writer.Write(" JOIN ");
			sqlJoinCollectionExpression.RightExpression.Accept(this);
		}

		public override void Visit(SqlLimitSpec sqlObject)
		{
			writer.Write("LIMIT ");
			writer.Write(sqlObject.Limit);
		}

		public override void Visit(SqlLiteralArrayCollection sqlLiteralArrayCollection)
		{
			writer.Write("[");
			for (int i = 0; i < sqlLiteralArrayCollection.Items.Count; i++)
			{
				if (i > 0)
				{
					writer.Write(", ");
				}
				sqlLiteralArrayCollection.Items[i].Accept(this);
			}
			writer.Write("]");
		}

		public override void Visit(SqlLiteralScalarExpression sqlLiteralScalarExpression)
		{
			sqlLiteralScalarExpression.Literal.Accept(this);
		}

		public override void Visit(SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression)
		{
			sqlMemberIndexerScalarExpression.MemberExpression.Accept(this);
			writer.Write("[");
			sqlMemberIndexerScalarExpression.IndexExpression.Accept(this);
			writer.Write("]");
		}

		public override void Visit(SqlNullLiteral sqlNullLiteral)
		{
			writer.Write("null");
		}

		public override void Visit(SqlNumberLiteral sqlNumberLiteral)
		{
			if (sqlNumberLiteral.Value.IsDouble)
			{
				string text = sqlNumberLiteral.Value.ToString(CultureInfo.InvariantCulture);
				double result = 0.0;
				if (!sqlNumberLiteral.Value.IsNaN && !sqlNumberLiteral.Value.IsInfinity && (!double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result) || !Number64.ToDouble(sqlNumberLiteral.Value).Equals(result)))
				{
					text = sqlNumberLiteral.Value.ToString("G17", CultureInfo.InvariantCulture);
				}
				writer.Write(text);
			}
			else
			{
				writer.Write(sqlNumberLiteral.Value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public override void Visit(SqlNumberPathExpression sqlNumberPathExpression)
		{
			if (sqlNumberPathExpression.ParentPath != null)
			{
				sqlNumberPathExpression.ParentPath.Accept(this);
			}
			writer.Write("[");
			sqlNumberPathExpression.Value.Accept(this);
			writer.Write("]");
		}

		public override void Visit(SqlObjectCreateScalarExpression sqlObjectCreateScalarExpression)
		{
			switch (sqlObjectCreateScalarExpression.Properties.Count())
			{
			case 0:
				writer.Write("{}");
				break;
			case 1:
				writer.Write("{");
				sqlObjectCreateScalarExpression.Properties.First().Accept(this);
				writer.Write("}");
				break;
			default:
			{
				WriteStartContext("{");
				bool flag = false;
				foreach (SqlObjectProperty property in sqlObjectCreateScalarExpression.Properties)
				{
					if (flag)
					{
						WriteDelimiter(",");
					}
					property.Accept(this);
					flag = true;
				}
				WriteEndContext("}");
				break;
			}
			}
		}

		public override void Visit(SqlObjectLiteral sqlObjectLiteral)
		{
			if (sqlObjectLiteral.isValueSerialized)
			{
				writer.Write(sqlObjectLiteral.Value);
			}
			else
			{
				writer.Write(JsonConvert.SerializeObject(sqlObjectLiteral.Value));
			}
		}

		public override void Visit(SqlObjectProperty sqlObjectProperty)
		{
			sqlObjectProperty.Name.Accept(this);
			writer.Write(": ");
			sqlObjectProperty.Expression.Accept(this);
		}

		public override void Visit(SqlOffsetLimitClause sqlObject)
		{
			sqlObject.OffsetSpec.Accept(this);
			writer.Write(" ");
			sqlObject.LimitSpec.Accept(this);
		}

		public override void Visit(SqlOffsetSpec sqlObject)
		{
			writer.Write("OFFSET ");
			writer.Write(sqlObject.Offset);
		}

		public override void Visit(SqlOrderbyClause sqlOrderByClause)
		{
			writer.Write("ORDER BY ");
			sqlOrderByClause.OrderbyItems[0].Accept(this);
			for (int i = 1; i < sqlOrderByClause.OrderbyItems.Count; i++)
			{
				writer.Write(", ");
				sqlOrderByClause.OrderbyItems[i].Accept(this);
			}
		}

		public override void Visit(SqlOrderByItem sqlOrderByItem)
		{
			sqlOrderByItem.Expression.Accept(this);
			if (sqlOrderByItem.IsDescending)
			{
				writer.Write(" DESC");
			}
			else
			{
				writer.Write(" ASC");
			}
		}

		public override void Visit(SqlProgram sqlProgram)
		{
			sqlProgram.Query.Accept(this);
		}

		public override void Visit(SqlPropertyName sqlPropertyName)
		{
			writer.Write('"');
			writer.Write(sqlPropertyName.Value);
			writer.Write('"');
		}

		public override void Visit(SqlPropertyRefScalarExpression sqlPropertyRefScalarExpression)
		{
			if (sqlPropertyRefScalarExpression.MemberExpression != null)
			{
				sqlPropertyRefScalarExpression.MemberExpression.Accept(this);
				writer.Write(".");
			}
			sqlPropertyRefScalarExpression.PropertyIdentifier.Accept(this);
		}

		public override void Visit(SqlQuery sqlQuery)
		{
			sqlQuery.SelectClause.Accept(this);
			if (sqlQuery.FromClause != null)
			{
				WriteDelimiter("");
				sqlQuery.FromClause.Accept(this);
			}
			if (sqlQuery.WhereClause != null)
			{
				WriteDelimiter("");
				sqlQuery.WhereClause.Accept(this);
			}
			if (sqlQuery.GroupByClause != null)
			{
				sqlQuery.GroupByClause.Accept(this);
				writer.Write(" ");
			}
			if (sqlQuery.OrderbyClause != null)
			{
				WriteDelimiter("");
				sqlQuery.OrderbyClause.Accept(this);
			}
			if (sqlQuery.OffsetLimitClause != null)
			{
				WriteDelimiter("");
				sqlQuery.OffsetLimitClause.Accept(this);
			}
			writer.Write(" ");
		}

		public override void Visit(SqlSelectClause sqlSelectClause)
		{
			writer.Write("SELECT ");
			if (sqlSelectClause.HasDistinct)
			{
				writer.Write("DISTINCT ");
			}
			if (sqlSelectClause.TopSpec != null)
			{
				sqlSelectClause.TopSpec.Accept(this);
				writer.Write(" ");
			}
			sqlSelectClause.SelectSpec.Accept(this);
		}

		public override void Visit(SqlSelectItem sqlSelectItem)
		{
			sqlSelectItem.Expression.Accept(this);
			if (sqlSelectItem.Alias != null)
			{
				writer.Write(" AS ");
				sqlSelectItem.Alias.Accept(this);
			}
		}

		public override void Visit(SqlSelectListSpec sqlSelectListSpec)
		{
			switch (sqlSelectListSpec.Items.Count())
			{
			case 0:
				throw new ArgumentException(string.Format("Expected {0} to have atleast 1 item.", "sqlSelectListSpec"));
			case 1:
				sqlSelectListSpec.Items[0].Accept(this);
				return;
			}
			bool flag = false;
			indentLevel++;
			WriteNewline();
			WriteTab();
			foreach (SqlSelectItem item in sqlSelectListSpec.Items)
			{
				if (flag)
				{
					WriteDelimiter(",");
				}
				item.Accept(this);
				flag = true;
			}
			indentLevel--;
		}

		public override void Visit(SqlSelectStarSpec sqlSelectStarSpec)
		{
			writer.Write("*");
		}

		public override void Visit(SqlSelectValueSpec sqlSelectValueSpec)
		{
			writer.Write("VALUE ");
			sqlSelectValueSpec.Expression.Accept(this);
		}

		public override void Visit(SqlStringLiteral sqlStringLiteral)
		{
			writer.Write("\"");
			writer.Write(GetEscapedString(sqlStringLiteral.Value));
			writer.Write("\"");
		}

		public override void Visit(SqlStringPathExpression sqlStringPathExpression)
		{
			if (sqlStringPathExpression.ParentPath != null)
			{
				sqlStringPathExpression.ParentPath.Accept(this);
			}
			writer.Write("[");
			sqlStringPathExpression.Value.Accept(this);
			writer.Write("]");
		}

		public override void Visit(SqlSubqueryCollection sqlSubqueryCollection)
		{
			WriteStartContext("(");
			sqlSubqueryCollection.Query.Accept(this);
			WriteEndContext(")");
		}

		public override void Visit(SqlSubqueryScalarExpression sqlSubqueryScalarExpression)
		{
			WriteStartContext("(");
			sqlSubqueryScalarExpression.Query.Accept(this);
			WriteEndContext(")");
		}

		public override void Visit(SqlTopSpec sqlTopSpec)
		{
			writer.Write("TOP ");
			writer.Write(sqlTopSpec.Count);
		}

		public override void Visit(SqlUnaryScalarExpression sqlUnaryScalarExpression)
		{
			writer.Write("(");
			writer.Write(SqlUnaryScalarOperatorKindToString(sqlUnaryScalarExpression.OperatorKind));
			writer.Write(" ");
			sqlUnaryScalarExpression.Expression.Accept(this);
			writer.Write(")");
		}

		public override void Visit(SqlUndefinedLiteral sqlUndefinedLiteral)
		{
			writer.Write("undefined");
		}

		public override void Visit(SqlWhereClause sqlWhereClause)
		{
			writer.Write("WHERE ");
			sqlWhereClause.FilterExpression.Accept(this);
		}

		public override string ToString()
		{
			return writer.ToString();
		}

		private void WriteStartContext(string startCharacter)
		{
			indentLevel++;
			writer.Write(startCharacter);
			WriteNewline();
			WriteTab();
		}

		private void WriteDelimiter(string delimiter)
		{
			writer.Write(delimiter);
			writer.Write(' ');
			WriteNewline();
			WriteTab();
		}

		private void WriteEndContext(string endCharacter)
		{
			indentLevel--;
			WriteNewline();
			WriteTab();
			writer.Write(endCharacter);
		}

		private void WriteNewline()
		{
			if (prettyPrint)
			{
				writer.WriteLine();
			}
		}

		private void WriteTab()
		{
			if (prettyPrint)
			{
				for (int i = 0; i < indentLevel; i++)
				{
					writer.Write(Tab);
				}
			}
		}

		private static string GetEscapedString(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.All((char c) => !IsEscapedCharacter(c)))
			{
				return value;
			}
			StringBuilder stringBuilder = new StringBuilder(value.Length);
			foreach (char c2 in value)
			{
				switch (c2)
				{
				case '"':
					stringBuilder.Append("\\\"");
					continue;
				case '\\':
					stringBuilder.Append("\\\\");
					continue;
				case '\b':
					stringBuilder.Append("\\b");
					continue;
				case '\f':
					stringBuilder.Append("\\f");
					continue;
				case '\n':
					stringBuilder.Append("\\n");
					continue;
				case '\r':
					stringBuilder.Append("\\r");
					continue;
				case '\t':
					stringBuilder.Append("\\t");
					continue;
				}
				switch (CharUnicodeInfo.GetUnicodeCategory(c2))
				{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherNumber:
				case UnicodeCategory.SpaceSeparator:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DashPunctuation:
				case UnicodeCategory.OpenPunctuation:
				case UnicodeCategory.ClosePunctuation:
				case UnicodeCategory.InitialQuotePunctuation:
				case UnicodeCategory.FinalQuotePunctuation:
				case UnicodeCategory.OtherPunctuation:
				case UnicodeCategory.MathSymbol:
				case UnicodeCategory.CurrencySymbol:
				case UnicodeCategory.ModifierSymbol:
				case UnicodeCategory.OtherSymbol:
					stringBuilder.Append(c2);
					break;
				default:
					stringBuilder.AppendFormat("\\u{0:x4}", (int)c2);
					break;
				}
			}
			return stringBuilder.ToString();
		}

		private static bool IsEscapedCharacter(char c)
		{
			switch (c)
			{
			case '\b':
			case '\t':
			case '\n':
			case '\f':
			case '\r':
			case '"':
			case '\\':
				return true;
			default:
				switch (CharUnicodeInfo.GetUnicodeCategory(c))
				{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherNumber:
				case UnicodeCategory.SpaceSeparator:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DashPunctuation:
				case UnicodeCategory.OpenPunctuation:
				case UnicodeCategory.ClosePunctuation:
				case UnicodeCategory.InitialQuotePunctuation:
				case UnicodeCategory.FinalQuotePunctuation:
				case UnicodeCategory.OtherPunctuation:
				case UnicodeCategory.MathSymbol:
				case UnicodeCategory.CurrencySymbol:
				case UnicodeCategory.ModifierSymbol:
				case UnicodeCategory.OtherSymbol:
					return false;
				default:
					return true;
				}
			}
		}

		private static string SqlUnaryScalarOperatorKindToString(SqlUnaryScalarOperatorKind kind)
		{
			switch (kind)
			{
			case SqlUnaryScalarOperatorKind.BitwiseNot:
				return "~";
			case SqlUnaryScalarOperatorKind.Not:
				return "NOT";
			case SqlUnaryScalarOperatorKind.Minus:
				return "-";
			case SqlUnaryScalarOperatorKind.Plus:
				return "+";
			default:
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported operator {0}", kind));
			}
		}

		private static string SqlBinaryScalarOperatorKindToString(SqlBinaryScalarOperatorKind kind)
		{
			switch (kind)
			{
			case SqlBinaryScalarOperatorKind.Add:
				return "+";
			case SqlBinaryScalarOperatorKind.And:
				return "AND";
			case SqlBinaryScalarOperatorKind.BitwiseAnd:
				return "&";
			case SqlBinaryScalarOperatorKind.BitwiseOr:
				return "|";
			case SqlBinaryScalarOperatorKind.BitwiseXor:
				return "^";
			case SqlBinaryScalarOperatorKind.Coalesce:
				return "??";
			case SqlBinaryScalarOperatorKind.Divide:
				return "/";
			case SqlBinaryScalarOperatorKind.Equal:
				return "=";
			case SqlBinaryScalarOperatorKind.GreaterThan:
				return ">";
			case SqlBinaryScalarOperatorKind.GreaterThanOrEqual:
				return ">=";
			case SqlBinaryScalarOperatorKind.LessThan:
				return "<";
			case SqlBinaryScalarOperatorKind.LessThanOrEqual:
				return "<=";
			case SqlBinaryScalarOperatorKind.Modulo:
				return "%";
			case SqlBinaryScalarOperatorKind.Multiply:
				return "*";
			case SqlBinaryScalarOperatorKind.NotEqual:
				return "!=";
			case SqlBinaryScalarOperatorKind.Or:
				return "OR";
			case SqlBinaryScalarOperatorKind.StringConcat:
				return "||";
			case SqlBinaryScalarOperatorKind.Subtract:
				return "-";
			default:
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported operator {0}", kind));
			}
		}
	}
}
