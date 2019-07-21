using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlObjectObfuscator : SqlObjectVisitor<SqlObject>
	{
		private static readonly HashSet<string> ExemptedString = new HashSet<string>
		{
			"GeometryCollection",
			"LineString",
			"MultiLineString",
			"MultiPoint",
			"MultiPolygon",
			"Point",
			"Polygon",
			"_attachments",
			"_etag",
			"_rid",
			"_self",
			"_ts",
			"coordinates",
			"id",
			"name",
			"type"
		};

		private int numberSequenceNumber;

		private int stringSequenceNumber;

		private int identifierSequenceNumber;

		private int fieldNameSequenceNumber;

		private readonly Dictionary<string, string> obfuscatedStrings = new Dictionary<string, string>();

		private readonly Dictionary<Number64, Number64> obfuscatedNumbers = new Dictionary<Number64, Number64>();

		public override SqlObject Visit(SqlAliasedCollectionExpression sqlAliasedCollectionExpression)
		{
			return SqlAliasedCollectionExpression.Create(sqlAliasedCollectionExpression.Collection.Accept(this) as SqlCollection, sqlAliasedCollectionExpression.Alias.Accept(this) as SqlIdentifier);
		}

		public override SqlObject Visit(SqlArrayCreateScalarExpression sqlArrayCreateScalarExpression)
		{
			List<SqlScalarExpression> list = new List<SqlScalarExpression>();
			foreach (SqlScalarExpression item in sqlArrayCreateScalarExpression.Items)
			{
				list.Add(item.Accept(this) as SqlScalarExpression);
			}
			return SqlArrayCreateScalarExpression.Create(list);
		}

		public override SqlObject Visit(SqlArrayIteratorCollectionExpression sqlArrayIteratorCollectionExpression)
		{
			return SqlArrayIteratorCollectionExpression.Create(sqlArrayIteratorCollectionExpression.Alias.Accept(this) as SqlIdentifier, sqlArrayIteratorCollectionExpression.Collection.Accept(this) as SqlCollection);
		}

		public override SqlObject Visit(SqlArrayScalarExpression sqlArrayScalarExpression)
		{
			return SqlArrayScalarExpression.Create(sqlArrayScalarExpression.SqlQuery.Accept(this) as SqlQuery);
		}

		public override SqlObject Visit(SqlBetweenScalarExpression sqlBetweenScalarExpression)
		{
			return SqlBetweenScalarExpression.Create(sqlBetweenScalarExpression.Expression.Accept(this) as SqlScalarExpression, sqlBetweenScalarExpression.LeftExpression.Accept(this) as SqlScalarExpression, sqlBetweenScalarExpression.RightExpression.Accept(this) as SqlScalarExpression, sqlBetweenScalarExpression.IsNot);
		}

		public override SqlObject Visit(SqlBinaryScalarExpression sqlBinaryScalarExpression)
		{
			return SqlBinaryScalarExpression.Create(sqlBinaryScalarExpression.OperatorKind, sqlBinaryScalarExpression.LeftExpression.Accept(this) as SqlScalarExpression, sqlBinaryScalarExpression.RightExpression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlBooleanLiteral sqlBooleanLiteral)
		{
			return sqlBooleanLiteral;
		}

		public override SqlObject Visit(SqlCoalesceScalarExpression sqlCoalesceScalarExpression)
		{
			return SqlCoalesceScalarExpression.Create(sqlCoalesceScalarExpression.LeftExpression.Accept(this) as SqlScalarExpression, sqlCoalesceScalarExpression.RightExpression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlConditionalScalarExpression sqlConditionalScalarExpression)
		{
			return SqlConditionalScalarExpression.Create(sqlConditionalScalarExpression.ConditionExpression.Accept(this) as SqlScalarExpression, sqlConditionalScalarExpression.FirstExpression.Accept(this) as SqlScalarExpression, sqlConditionalScalarExpression.SecondExpression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlExistsScalarExpression sqlExistsScalarExpression)
		{
			return SqlExistsScalarExpression.Create(sqlExistsScalarExpression.SqlQuery.Accept(this) as SqlQuery);
		}

		public override SqlObject Visit(SqlFromClause sqlFromClause)
		{
			return SqlFromClause.Create(sqlFromClause.Expression.Accept(this) as SqlCollectionExpression);
		}

		public override SqlObject Visit(SqlFunctionCallScalarExpression sqlFunctionCallScalarExpression)
		{
			SqlScalarExpression[] array = new SqlScalarExpression[sqlFunctionCallScalarExpression.Arguments.Count];
			for (int i = 0; i < sqlFunctionCallScalarExpression.Arguments.Count; i++)
			{
				array[i] = (sqlFunctionCallScalarExpression.Arguments[i].Accept(this) as SqlScalarExpression);
			}
			return SqlFunctionCallScalarExpression.Create(sqlFunctionCallScalarExpression.Name, sqlFunctionCallScalarExpression.IsUdf, array);
		}

		public override SqlObject Visit(SqlGroupByClause sqlGroupByClause)
		{
			SqlScalarExpression[] array = new SqlScalarExpression[sqlGroupByClause.Expressions.Count];
			for (int i = 0; i < sqlGroupByClause.Expressions.Count; i++)
			{
				array[i] = (sqlGroupByClause.Expressions[i].Accept(this) as SqlScalarExpression);
			}
			return SqlGroupByClause.Create(array);
		}

		public override SqlObject Visit(SqlIdentifier sqlIdentifier)
		{
			return SqlIdentifier.Create(GetObfuscatedString(sqlIdentifier.Value, "ident", ref identifierSequenceNumber));
		}

		public override SqlObject Visit(SqlIdentifierPathExpression sqlIdentifierPathExpression)
		{
			return SqlIdentifierPathExpression.Create(sqlIdentifierPathExpression.ParentPath?.Accept(this) as SqlPathExpression, sqlIdentifierPathExpression.Value.Accept(this) as SqlIdentifier);
		}

		public override SqlObject Visit(SqlInputPathCollection sqlInputPathCollection)
		{
			return SqlInputPathCollection.Create(sqlInputPathCollection.Input.Accept(this) as SqlIdentifier, sqlInputPathCollection.RelativePath?.Accept(this) as SqlPathExpression);
		}

		public override SqlObject Visit(SqlInScalarExpression sqlInScalarExpression)
		{
			SqlScalarExpression[] array = new SqlScalarExpression[sqlInScalarExpression.Items.Count];
			for (int i = 0; i < sqlInScalarExpression.Items.Count; i++)
			{
				array[i] = (sqlInScalarExpression.Items[i].Accept(this) as SqlScalarExpression);
			}
			return SqlInScalarExpression.Create(sqlInScalarExpression.Expression.Accept(this) as SqlScalarExpression, sqlInScalarExpression.Not, array);
		}

		public override SqlObject Visit(SqlJoinCollectionExpression sqlJoinCollectionExpression)
		{
			return SqlJoinCollectionExpression.Create(sqlJoinCollectionExpression.LeftExpression.Accept(this) as SqlCollectionExpression, sqlJoinCollectionExpression.RightExpression.Accept(this) as SqlCollectionExpression);
		}

		public override SqlObject Visit(SqlLimitSpec sqlObject)
		{
			return SqlLimitSpec.Create(0L);
		}

		public override SqlObject Visit(SqlLiteralArrayCollection sqlLiteralArrayCollection)
		{
			SqlScalarExpression[] array = new SqlScalarExpression[sqlLiteralArrayCollection.Items.Count];
			for (int i = 0; i < sqlLiteralArrayCollection.Items.Count; i++)
			{
				array[i] = (sqlLiteralArrayCollection.Items[i].Accept(this) as SqlScalarExpression);
			}
			return SqlLiteralArrayCollection.Create(array);
		}

		public override SqlObject Visit(SqlLiteralScalarExpression sqlLiteralScalarExpression)
		{
			return SqlLiteralScalarExpression.Create(sqlLiteralScalarExpression.Literal.Accept(this) as SqlLiteral);
		}

		public override SqlObject Visit(SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression)
		{
			return SqlMemberIndexerScalarExpression.Create(sqlMemberIndexerScalarExpression.MemberExpression.Accept(this) as SqlScalarExpression, sqlMemberIndexerScalarExpression.IndexExpression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlNullLiteral sqlNullLiteral)
		{
			return sqlNullLiteral;
		}

		public override SqlObject Visit(SqlNumberLiteral sqlNumberLiteral)
		{
			return SqlNumberLiteral.Create(Number64.ToDouble(GetObfuscatedNumber(sqlNumberLiteral.Value)));
		}

		public override SqlObject Visit(SqlNumberPathExpression sqlNumberPathExpression)
		{
			return SqlNumberPathExpression.Create(sqlNumberPathExpression.ParentPath?.Accept(this) as SqlPathExpression, sqlNumberPathExpression.Value.Accept(this) as SqlNumberLiteral);
		}

		public override SqlObject Visit(SqlObjectCreateScalarExpression sqlObjectCreateScalarExpression)
		{
			List<SqlObjectProperty> list = new List<SqlObjectProperty>();
			foreach (SqlObjectProperty property in sqlObjectCreateScalarExpression.Properties)
			{
				list.Add(property.Accept(this) as SqlObjectProperty);
			}
			return SqlObjectCreateScalarExpression.Create(list);
		}

		public override SqlObject Visit(SqlObjectProperty sqlObjectProperty)
		{
			return SqlObjectProperty.Create(sqlObjectProperty.Name.Accept(this) as SqlPropertyName, sqlObjectProperty.Expression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlOffsetLimitClause sqlObject)
		{
			return SqlOffsetLimitClause.Create(sqlObject.OffsetSpec.Accept(this) as SqlOffsetSpec, sqlObject.LimitSpec.Accept(this) as SqlLimitSpec);
		}

		public override SqlObject Visit(SqlOffsetSpec sqlObject)
		{
			return SqlOffsetSpec.Create(0L);
		}

		public override SqlObject Visit(SqlOrderbyClause sqlOrderByClause)
		{
			SqlOrderByItem[] array = new SqlOrderByItem[sqlOrderByClause.OrderbyItems.Count];
			for (int i = 0; i < sqlOrderByClause.OrderbyItems.Count; i++)
			{
				array[i] = (sqlOrderByClause.OrderbyItems[i].Accept(this) as SqlOrderByItem);
			}
			return SqlOrderbyClause.Create(array);
		}

		public override SqlObject Visit(SqlOrderByItem sqlOrderByItem)
		{
			return SqlOrderByItem.Create(sqlOrderByItem.Expression.Accept(this) as SqlScalarExpression, sqlOrderByItem.IsDescending);
		}

		public override SqlObject Visit(SqlProgram sqlProgram)
		{
			return SqlProgram.Create(sqlProgram.Query.Accept(this) as SqlQuery);
		}

		public override SqlObject Visit(SqlPropertyName sqlPropertyName)
		{
			return SqlPropertyName.Create(GetObfuscatedString(sqlPropertyName.Value, "p", ref fieldNameSequenceNumber));
		}

		public override SqlObject Visit(SqlPropertyRefScalarExpression sqlPropertyRefScalarExpression)
		{
			return SqlPropertyRefScalarExpression.Create(sqlPropertyRefScalarExpression.MemberExpression?.Accept(this) as SqlScalarExpression, sqlPropertyRefScalarExpression.PropertyIdentifier.Accept(this) as SqlIdentifier);
		}

		public override SqlObject Visit(SqlQuery sqlQuery)
		{
			return SqlQuery.Create(sqlQuery.SelectClause.Accept(this) as SqlSelectClause, sqlQuery.FromClause?.Accept(this) as SqlFromClause, sqlQuery.WhereClause?.Accept(this) as SqlWhereClause, sqlQuery.GroupByClause?.Accept(this) as SqlGroupByClause, sqlQuery.OrderbyClause?.Accept(this) as SqlOrderbyClause, sqlQuery.OffsetLimitClause?.Accept(this) as SqlOffsetLimitClause);
		}

		public override SqlObject Visit(SqlSelectClause sqlSelectClause)
		{
			return SqlSelectClause.Create(sqlSelectClause.SelectSpec.Accept(this) as SqlSelectSpec, sqlSelectClause.TopSpec?.Accept(this) as SqlTopSpec, sqlSelectClause.HasDistinct);
		}

		public override SqlObject Visit(SqlSelectItem sqlSelectItem)
		{
			return SqlSelectItem.Create(sqlSelectItem.Expression.Accept(this) as SqlScalarExpression, sqlSelectItem.Alias?.Accept(this) as SqlIdentifier);
		}

		public override SqlObject Visit(SqlSelectListSpec sqlSelectListSpec)
		{
			List<SqlSelectItem> list = new List<SqlSelectItem>();
			foreach (SqlSelectItem item in sqlSelectListSpec.Items)
			{
				list.Add(item.Accept(this) as SqlSelectItem);
			}
			return SqlSelectListSpec.Create(list);
		}

		public override SqlObject Visit(SqlSelectStarSpec sqlSelectStarSpec)
		{
			return sqlSelectStarSpec;
		}

		public override SqlObject Visit(SqlSelectValueSpec sqlSelectValueSpec)
		{
			return SqlSelectValueSpec.Create(sqlSelectValueSpec.Expression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlStringLiteral sqlStringLiteral)
		{
			return SqlStringLiteral.Create(GetObfuscatedString(sqlStringLiteral.Value, "str", ref stringSequenceNumber));
		}

		public override SqlObject Visit(SqlStringPathExpression sqlStringPathExpression)
		{
			return SqlStringPathExpression.Create(sqlStringPathExpression.ParentPath?.Accept(this) as SqlPathExpression, sqlStringPathExpression.Value.Accept(this) as SqlStringLiteral);
		}

		public override SqlObject Visit(SqlSubqueryCollection sqlSubqueryCollection)
		{
			return SqlSubqueryCollection.Create(sqlSubqueryCollection.Query.Accept(this) as SqlQuery);
		}

		public override SqlObject Visit(SqlSubqueryScalarExpression sqlSubqueryScalarExpression)
		{
			return SqlSubqueryScalarExpression.Create(sqlSubqueryScalarExpression.Query.Accept(this) as SqlQuery);
		}

		public override SqlObject Visit(SqlTopSpec sqlTopSpec)
		{
			return SqlTopSpec.Create(0L);
		}

		public override SqlObject Visit(SqlUnaryScalarExpression sqlUnaryScalarExpression)
		{
			return SqlUnaryScalarExpression.Create(sqlUnaryScalarExpression.OperatorKind, sqlUnaryScalarExpression.Expression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlUndefinedLiteral sqlUndefinedLiteral)
		{
			return sqlUndefinedLiteral;
		}

		public override SqlObject Visit(SqlWhereClause sqlWhereClause)
		{
			return SqlWhereClause.Create(sqlWhereClause.FilterExpression.Accept(this) as SqlScalarExpression);
		}

		public override SqlObject Visit(SqlConversionScalarExpression sqlConversionScalarExpression)
		{
			throw new NotImplementedException("This is not part of the actual grammar");
		}

		public override SqlObject Visit(SqlGeoNearCallScalarExpression sqlGeoNearCallScalarExpression)
		{
			throw new NotImplementedException("This is not part of the actual grammar");
		}

		public override SqlObject Visit(SqlObjectLiteral sqlObjectLiteral)
		{
			throw new NotImplementedException("This is not part of the actual grammar");
		}

		private Number64 GetObfuscatedNumber(Number64 value)
		{
			Number64 value2;
			if (value.IsInfinity || (value.IsInteger && Number64.ToLong(value) == long.MinValue) || (value.IsInteger && Math.Abs(Number64.ToLong(value)) < 100) || (value.IsDouble && Math.Abs(Number64.ToDouble(value)) < 100.0 && (double)(long)Number64.ToDouble(value) == Number64.ToDouble(value)) || (value.IsDouble && Math.Abs(Number64.ToDouble(value)) <= double.Epsilon))
			{
				value2 = value;
			}
			else if (!obfuscatedNumbers.TryGetValue(value, out value2))
			{
				double value3 = Number64.ToDouble(value);
				int num = ++numberSequenceNumber;
				double y = Math.Floor(Math.Log10(Math.Abs(value3)));
				double num2 = Math.Pow(10.0, y) * (double)num / 10000.0;
				value2 = Math.Round(value3, 2) + num2;
				obfuscatedNumbers.Add(value, value2);
			}
			return value2;
		}

		private string GetObfuscatedString(string value, string prefix, ref int sequence)
		{
			string value2;
			if (value.Length <= 1)
			{
				value2 = value;
			}
			else if (ExemptedString.Contains(value))
			{
				value2 = value;
			}
			else if (!obfuscatedStrings.TryGetValue(value, out value2))
			{
				int num = ++sequence;
				value2 = ((value.Length < 10) ? $"{prefix}{sequence}" : $"{prefix}{sequence}__{value.Length}");
				obfuscatedStrings.Add(value, value2);
			}
			return value2;
		}
	}
}
