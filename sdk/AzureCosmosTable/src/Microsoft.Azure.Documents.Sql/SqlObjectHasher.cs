using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Sql
{
	internal sealed class SqlObjectHasher : SqlObjectVisitor<int>
	{
		private static class SqlBinaryScalarOperatorKindHashCodes
		{
			public const int Add = 977447154;

			public const int And = -539169937;

			public const int BitwiseAnd = 192594476;

			public const int BitwiseOr = -1494193777;

			public const int BitwiseXor = 140893802;

			public const int Coalesce = -461857726;

			public const int Divide = -1486745780;

			public const int Equal = -69389992;

			public const int GreaterThan = 1643533106;

			public const int GreaterThanOrEqual = 180538014;

			public const int LessThan = -1452081072;

			public const int LessThanOrEqual = -1068434012;

			public const int Modulo = -371220256;

			public const int Multiply = -178990484;

			public const int NotEqual = 65181046;

			public const int Or = -2095255335;

			public const int StringConcat = -525384764;

			public const int Subtract = 2070749634;
		}

		private static class SqlUnaryScalarOperatorKindHashCodes
		{
			public const int BitwiseNot = 1177827907;

			public const int Not = 1278008063;

			public const int Minus = -1942284846;

			public const int Plus = 251767493;
		}

		public static readonly SqlObjectHasher Singleton = new SqlObjectHasher(isStrict: true);

		private const int SqlAliasedCollectionExpressionHashCode = 1202039781;

		private const int SqlArrayCreateScalarExpressionHashCode = 1760950661;

		private const int SqlArrayIteratorCollectionExpressionHashCode = -468874086;

		private const int SqlArrayScalarExpressionHashCode = -1093553293;

		private const int SqlBetweenScalarExpressionHashCode = -943872277;

		private const int SqlBetweenScalarExpressionNotHashCode = -1283200473;

		private const int SqlBinaryScalarExpressionHashCode = 1667146665;

		private const int SqlBooleanLiteralHashCode = 739161617;

		private const int SqlBooleanLiteralTrueHashCode = 1545461565;

		private const int SqlBooleanLiteralFalseHashCode = -2072875075;

		private const int SqlCoalesceScalarExpressionHashCode = -1400659633;

		private const int SqlConditionalScalarExpressionHashCode = -421337832;

		private const int SqlExistsScalarExpressionHashCode = 1168675587;

		private const int SqlFromClauseHashCode = 52588336;

		private const int SqlFunctionCallScalarExpressionHashCode = 496783446;

		private const int SqlFunctionCallScalarExpressionUdfHashCode = 1547906315;

		private const int SqlGroupByClauseHashCode = 130396242;

		private const int SqlIdentifierHashCode = -1664307981;

		private const int SqlIdentifierPathExpressionHashCode = -1445813508;

		private const int SqlInputPathCollectionHashCode = -209963066;

		private const int SqlInScalarExpressionHashCode = 1439386783;

		private const int SqlInScalarExpressionNotHashCode = -1131398119;

		private const int SqlJoinCollectionExpressionHashCode = 1000382226;

		private const int SqlLimitSpecHashCode = 92601316;

		private const int SqlLiteralArrayCollectionHashCode = 1634639566;

		private const int SqlLiteralScalarExpressionHashCode = -158339101;

		private const int SqlMemberIndexerScalarExpressionHashCode = 1589675618;

		private const int SqlNullLiteralHashCode = -709456592;

		private const int SqlNumberLiteralHashCode = 159836309;

		private const int SqlNumberPathExpressionHashCode = 874210976;

		private const int SqlObjectCreateScalarExpressionHashCode = -131129165;

		private const int SqlObjectPropertyHashCode = 1218972715;

		private const int SqlOffsetLimitClauseHashCode = 150154755;

		private const int SqlOffsetSpecHashCode = 109062001;

		private const int SqlOrderbyClauseHashCode = 1361708336;

		private const int SqlOrderbyItemHashCode = 846566057;

		private const int SqlOrderbyItemAscendingHashCode = -1123129997;

		private const int SqlOrderbyItemDescendingHashCode = -703648622;

		private const int SqlProgramHashCode = -492711050;

		private const int SqlPropertyNameHashCode = 1262661966;

		private const int SqlPropertyRefScalarExpressionHashCode = -1586896865;

		private const int SqlQueryHashCode = 1968642960;

		private const int SqlSelectClauseHashCode = 19731870;

		private const int SqlSelectClauseDistinctHashCode = 1467616881;

		private const int SqlSelectItemHashCode = -611151157;

		private const int SqlSelectListSpecHashCode = -1704039197;

		private const int SqlSelectStarSpecHashCode = -1125875092;

		private const int SqlSelectValueSpecHashCode = 507077368;

		private const int SqlStringLiteralHashCode = -1542874155;

		private const int SqlStringPathExpressionHashCode = -1280625326;

		private const int SqlSubqueryCollectionHashCode = 1175697100;

		private const int SqlSubqueryScalarExpressionHashCode = -1327458193;

		private const int SqlTopSpecHashCode = -791376698;

		private const int SqlUnaryScalarExpressionHashCode = 723832597;

		private const int SqlUndefinedLiteralHashCode = 1290712518;

		private const int SqlWhereClauseHashCode = -516465563;

		private readonly bool isStrict;

		public SqlObjectHasher(bool isStrict)
		{
			this.isStrict = isStrict;
		}

		public override int Visit(SqlAliasedCollectionExpression sqlAliasedCollectionExpression)
		{
			int num = 1202039781;
			num = CombineHashes(num, sqlAliasedCollectionExpression.Collection.Accept(this));
			if (sqlAliasedCollectionExpression.Alias != null)
			{
				num = CombineHashes(num, sqlAliasedCollectionExpression.Alias.Accept(this));
			}
			return num;
		}

		public override int Visit(SqlArrayCreateScalarExpression sqlArrayCreateScalarExpression)
		{
			int num = 1760950661;
			for (int i = 0; i < sqlArrayCreateScalarExpression.Items.Count; i++)
			{
				num = CombineHashes(num, sqlArrayCreateScalarExpression.Items[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlArrayIteratorCollectionExpression sqlArrayIteratorCollectionExpression)
		{
			return CombineHashes(CombineHashes(-468874086L, sqlArrayIteratorCollectionExpression.Alias.Accept(this)), sqlArrayIteratorCollectionExpression.Collection.Accept(this));
		}

		public override int Visit(SqlArrayScalarExpression sqlArrayScalarExpression)
		{
			return CombineHashes(-1093553293L, sqlArrayScalarExpression.SqlQuery.Accept(this));
		}

		public override int Visit(SqlBetweenScalarExpression sqlBetweenScalarExpression)
		{
			int num = -943872277;
			num = CombineHashes(num, sqlBetweenScalarExpression.Expression.Accept(this));
			if (sqlBetweenScalarExpression.IsNot)
			{
				num = CombineHashes(num, -1283200473L);
			}
			num = CombineHashes(num, sqlBetweenScalarExpression.LeftExpression.Accept(this));
			return CombineHashes(num, sqlBetweenScalarExpression.RightExpression.Accept(this));
		}

		public override int Visit(SqlBinaryScalarExpression sqlBinaryScalarExpression)
		{
			return CombineHashes(CombineHashes(CombineHashes(1667146665L, sqlBinaryScalarExpression.LeftExpression.Accept(this)), SqlBinaryScalarOperatorKindGetHashCode(sqlBinaryScalarExpression.OperatorKind)), sqlBinaryScalarExpression.RightExpression.Accept(this));
		}

		public override int Visit(SqlBooleanLiteral sqlBooleanLiteral)
		{
			return CombineHashes(739161617L, sqlBooleanLiteral.Value ? 1545461565 : (-2072875075));
		}

		public override int Visit(SqlCoalesceScalarExpression sqlCoalesceScalarExpression)
		{
			return CombineHashes(CombineHashes(-1400659633L, sqlCoalesceScalarExpression.LeftExpression.Accept(this)), sqlCoalesceScalarExpression.RightExpression.Accept(this));
		}

		public override int Visit(SqlConditionalScalarExpression sqlConditionalScalarExpression)
		{
			return CombineHashes(CombineHashes(CombineHashes(-421337832L, sqlConditionalScalarExpression.ConditionExpression.Accept(this)), sqlConditionalScalarExpression.FirstExpression.Accept(this)), sqlConditionalScalarExpression.SecondExpression.Accept(this));
		}

		public override int Visit(SqlExistsScalarExpression sqlExistsScalarExpression)
		{
			return CombineHashes(1168675587L, sqlExistsScalarExpression.SqlQuery.Accept(this));
		}

		public override int Visit(SqlFromClause sqlFromClause)
		{
			return CombineHashes(52588336L, sqlFromClause.Expression.Accept(this));
		}

		public override int Visit(SqlFunctionCallScalarExpression sqlFunctionCallScalarExpression)
		{
			int num = 496783446;
			if (sqlFunctionCallScalarExpression.IsUdf)
			{
				num = CombineHashes(num, 1547906315L);
			}
			num = CombineHashes(num, sqlFunctionCallScalarExpression.Name.Accept(this));
			for (int i = 0; i < sqlFunctionCallScalarExpression.Arguments.Count; i++)
			{
				num = CombineHashes(num, sqlFunctionCallScalarExpression.Arguments[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlGroupByClause sqlGroupByClause)
		{
			int num = 130396242;
			for (int i = 0; i < sqlGroupByClause.Expressions.Count; i++)
			{
				num = CombineHashes(num, sqlGroupByClause.Expressions[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlIdentifier sqlIdentifier)
		{
			return CombineHashes(-1664307981L, sqlIdentifier.Value.GetHashCode());
		}

		public override int Visit(SqlIdentifierPathExpression sqlIdentifierPathExpression)
		{
			int num = -1445813508;
			if (sqlIdentifierPathExpression.ParentPath != null)
			{
				num = CombineHashes(num, sqlIdentifierPathExpression.ParentPath.Accept(this));
			}
			return CombineHashes(num, sqlIdentifierPathExpression.Value.Accept(this));
		}

		public override int Visit(SqlInputPathCollection sqlInputPathCollection)
		{
			int num = -209963066;
			num = CombineHashes(num, sqlInputPathCollection.Input.Accept(this));
			if (sqlInputPathCollection.RelativePath != null)
			{
				num = CombineHashes(num, sqlInputPathCollection.RelativePath.Accept(this));
			}
			return num;
		}

		public override int Visit(SqlInScalarExpression sqlInScalarExpression)
		{
			int num = 1439386783;
			num = CombineHashes(num, sqlInScalarExpression.Expression.Accept(this));
			if (sqlInScalarExpression.Not)
			{
				num = CombineHashes(num, -1131398119L);
			}
			for (int i = 0; i < sqlInScalarExpression.Items.Count; i++)
			{
				num = CombineHashes(num, sqlInScalarExpression.Items[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlLimitSpec sqlObject)
		{
			return CombineHashes(92601316L, sqlObject.Limit);
		}

		public override int Visit(SqlJoinCollectionExpression sqlJoinCollectionExpression)
		{
			return CombineHashes(CombineHashes(1000382226L, sqlJoinCollectionExpression.LeftExpression.Accept(this)), sqlJoinCollectionExpression.RightExpression.Accept(this));
		}

		public override int Visit(SqlLiteralArrayCollection sqlLiteralArrayCollection)
		{
			int num = 1634639566;
			for (int i = 0; i < sqlLiteralArrayCollection.Items.Count; i++)
			{
				num = CombineHashes(num, sqlLiteralArrayCollection.Items[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlLiteralScalarExpression sqlLiteralScalarExpression)
		{
			return CombineHashes(-158339101L, sqlLiteralScalarExpression.Literal.Accept(this));
		}

		public override int Visit(SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression)
		{
			return CombineHashes(CombineHashes(1589675618L, sqlMemberIndexerScalarExpression.MemberExpression.Accept(this)), sqlMemberIndexerScalarExpression.IndexExpression.Accept(this));
		}

		public override int Visit(SqlNullLiteral sqlNullLiteral)
		{
			return -709456592;
		}

		public override int Visit(SqlNumberLiteral sqlNumberLiteral)
		{
			return CombineHashes(159836309L, sqlNumberLiteral.Value.GetHashCode());
		}

		public override int Visit(SqlNumberPathExpression sqlNumberPathExpression)
		{
			int num = 874210976;
			if (sqlNumberPathExpression.ParentPath != null)
			{
				num = CombineHashes(num, sqlNumberPathExpression.ParentPath.Accept(this));
			}
			return CombineHashes(num, sqlNumberPathExpression.Value.Accept(this));
		}

		public override int Visit(SqlObjectCreateScalarExpression sqlObjectCreateScalarExpression)
		{
			int num = -131129165;
			foreach (SqlObjectProperty property in sqlObjectCreateScalarExpression.Properties)
			{
				num = ((!isStrict) ? (num + property.Accept(this)) : CombineHashes(num, property.Accept(this)));
			}
			return num;
		}

		public override int Visit(SqlObjectProperty sqlObjectProperty)
		{
			return CombineHashes(CombineHashes(1218972715L, sqlObjectProperty.Name.Accept(this)), sqlObjectProperty.Expression.Accept(this));
		}

		public override int Visit(SqlOffsetLimitClause sqlObject)
		{
			return CombineHashes(CombineHashes(150154755L, sqlObject.OffsetSpec.Accept(this)), sqlObject.LimitSpec.Accept(this));
		}

		public override int Visit(SqlOffsetSpec sqlObject)
		{
			return CombineHashes(109062001L, sqlObject.Offset);
		}

		public override int Visit(SqlOrderbyClause sqlOrderByClause)
		{
			int num = 1361708336;
			for (int i = 0; i < sqlOrderByClause.OrderbyItems.Count; i++)
			{
				num = CombineHashes(num, sqlOrderByClause.OrderbyItems[i].Accept(this));
			}
			return num;
		}

		public override int Visit(SqlOrderByItem sqlOrderByItem)
		{
			int num = 846566057;
			num = CombineHashes(num, sqlOrderByItem.Expression.Accept(this));
			if (sqlOrderByItem.IsDescending)
			{
				return CombineHashes(num, -703648622L);
			}
			return CombineHashes(num, -1123129997L);
		}

		public override int Visit(SqlProgram sqlProgram)
		{
			return CombineHashes(-492711050L, sqlProgram.Query.Accept(this));
		}

		public override int Visit(SqlPropertyName sqlPropertyName)
		{
			return CombineHashes(1262661966L, sqlPropertyName.Value.GetHashCode());
		}

		public override int Visit(SqlPropertyRefScalarExpression sqlPropertyRefScalarExpression)
		{
			int num = -1586896865;
			if (sqlPropertyRefScalarExpression.MemberExpression != null)
			{
				num = CombineHashes(num, sqlPropertyRefScalarExpression.MemberExpression.Accept(this));
			}
			return CombineHashes(num, sqlPropertyRefScalarExpression.PropertyIdentifier.Accept(this));
		}

		public override int Visit(SqlQuery sqlQuery)
		{
			int num = 1968642960;
			num = CombineHashes(num, sqlQuery.SelectClause.Accept(this));
			if (sqlQuery.FromClause != null)
			{
				num = CombineHashes(num, sqlQuery.FromClause.Accept(this));
			}
			if (sqlQuery.WhereClause != null)
			{
				num = CombineHashes(num, sqlQuery.WhereClause.Accept(this));
			}
			if (sqlQuery.GroupByClause != null)
			{
				num = CombineHashes(num, sqlQuery.GroupByClause.Accept(this));
			}
			if (sqlQuery.OrderbyClause != null)
			{
				num = CombineHashes(num, sqlQuery.OrderbyClause.Accept(this));
			}
			if (sqlQuery.OffsetLimitClause != null)
			{
				num = CombineHashes(num, sqlQuery.OffsetLimitClause.Accept(this));
			}
			return num;
		}

		public override int Visit(SqlSelectClause sqlSelectClause)
		{
			int num = 19731870;
			if (sqlSelectClause.HasDistinct)
			{
				num = CombineHashes(num, 1467616881L);
			}
			if (sqlSelectClause.TopSpec != null)
			{
				num = CombineHashes(num, sqlSelectClause.TopSpec.Accept(this));
			}
			return CombineHashes(num, sqlSelectClause.SelectSpec.Accept(this));
		}

		public override int Visit(SqlSelectItem sqlSelectItem)
		{
			int num = -611151157;
			num = CombineHashes(num, sqlSelectItem.Expression.Accept(this));
			if (sqlSelectItem.Alias != null)
			{
				num = CombineHashes(num, sqlSelectItem.Alias.Accept(this));
			}
			return num;
		}

		public override int Visit(SqlSelectListSpec sqlSelectListSpec)
		{
			int num = -1704039197;
			foreach (SqlSelectItem item in sqlSelectListSpec.Items)
			{
				num = ((!isStrict) ? (num + item.Accept(this)) : CombineHashes(num, item.Accept(this)));
			}
			return num;
		}

		public override int Visit(SqlSelectStarSpec sqlSelectStarSpec)
		{
			return -1125875092;
		}

		public override int Visit(SqlSelectValueSpec sqlSelectValueSpec)
		{
			return CombineHashes(507077368L, sqlSelectValueSpec.Expression.Accept(this));
		}

		public override int Visit(SqlStringLiteral sqlStringLiteral)
		{
			return CombineHashes(-1542874155L, sqlStringLiteral.Value.GetHashCode());
		}

		public override int Visit(SqlStringPathExpression sqlStringPathExpression)
		{
			int num = -1280625326;
			if (sqlStringPathExpression.ParentPath != null)
			{
				num = CombineHashes(num, sqlStringPathExpression.ParentPath.Accept(this));
			}
			return CombineHashes(num, sqlStringPathExpression.Value.Accept(this));
		}

		public override int Visit(SqlSubqueryCollection sqlSubqueryCollection)
		{
			return CombineHashes(1175697100L, sqlSubqueryCollection.Query.Accept(this));
		}

		public override int Visit(SqlSubqueryScalarExpression sqlSubqueryScalarExpression)
		{
			return CombineHashes(-1327458193L, sqlSubqueryScalarExpression.Query.Accept(this));
		}

		public override int Visit(SqlTopSpec sqlTopSpec)
		{
			return CombineHashes(-791376698L, sqlTopSpec.Count.GetHashCode());
		}

		public override int Visit(SqlUnaryScalarExpression sqlUnaryScalarExpression)
		{
			return CombineHashes(CombineHashes(723832597L, SqlUnaryScalarOperatorKindGetHashCode(sqlUnaryScalarExpression.OperatorKind)), sqlUnaryScalarExpression.Expression.Accept(this));
		}

		public override int Visit(SqlUndefinedLiteral sqlUndefinedLiteral)
		{
			return 1290712518;
		}

		public override int Visit(SqlWhereClause sqlWhereClause)
		{
			return CombineHashes(-516465563L, sqlWhereClause.FilterExpression.Accept(this));
		}

		private static int SqlUnaryScalarOperatorKindGetHashCode(SqlUnaryScalarOperatorKind kind)
		{
			switch (kind)
			{
			case SqlUnaryScalarOperatorKind.BitwiseNot:
				return 1177827907;
			case SqlUnaryScalarOperatorKind.Not:
				return 1278008063;
			case SqlUnaryScalarOperatorKind.Minus:
				return -1942284846;
			case SqlUnaryScalarOperatorKind.Plus:
				return 251767493;
			default:
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported operator {0}", kind));
			}
		}

		private static int SqlBinaryScalarOperatorKindGetHashCode(SqlBinaryScalarOperatorKind kind)
		{
			switch (kind)
			{
			case SqlBinaryScalarOperatorKind.Add:
				return 977447154;
			case SqlBinaryScalarOperatorKind.And:
				return -539169937;
			case SqlBinaryScalarOperatorKind.BitwiseAnd:
				return 192594476;
			case SqlBinaryScalarOperatorKind.BitwiseOr:
				return -1494193777;
			case SqlBinaryScalarOperatorKind.BitwiseXor:
				return 140893802;
			case SqlBinaryScalarOperatorKind.Coalesce:
				return -461857726;
			case SqlBinaryScalarOperatorKind.Divide:
				return -1486745780;
			case SqlBinaryScalarOperatorKind.Equal:
				return -69389992;
			case SqlBinaryScalarOperatorKind.GreaterThan:
				return 1643533106;
			case SqlBinaryScalarOperatorKind.GreaterThanOrEqual:
				return 180538014;
			case SqlBinaryScalarOperatorKind.LessThan:
				return -1452081072;
			case SqlBinaryScalarOperatorKind.LessThanOrEqual:
				return -1068434012;
			case SqlBinaryScalarOperatorKind.Modulo:
				return -371220256;
			case SqlBinaryScalarOperatorKind.Multiply:
				return -178990484;
			case SqlBinaryScalarOperatorKind.NotEqual:
				return 65181046;
			case SqlBinaryScalarOperatorKind.Or:
				return -2095255335;
			case SqlBinaryScalarOperatorKind.StringConcat:
				return -525384764;
			case SqlBinaryScalarOperatorKind.Subtract:
				return 2070749634;
			default:
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Unsupported operator {0}", kind));
			}
		}

		/// <summary>
		/// Combines Two Hashes in an antisymmetric way (stolen from boost).
		/// </summary>
		/// <param name="lhs">The first hash</param>
		/// <param name="rhs">The second hash</param>
		/// <returns>The combined hash.</returns>
		private static int CombineHashes(long lhs, long rhs)
		{
			lhs ^= rhs + 2654435769u + (lhs << 6) + (lhs >> 2);
			return (int)lhs;
		}

		public override int Visit(SqlConversionScalarExpression sqlConversionScalarExpression)
		{
			throw new NotImplementedException("This DOM element is being removed.");
		}

		public override int Visit(SqlGeoNearCallScalarExpression sqlGeoNearCallScalarExpression)
		{
			throw new NotImplementedException("This DOM element is being removed.");
		}

		public override int Visit(SqlObjectLiteral sqlObjectLiteral)
		{
			throw new NotImplementedException("This DOM element is being removed.");
		}
	}
}
