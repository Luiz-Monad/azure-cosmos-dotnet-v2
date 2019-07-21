using Microsoft.Azure.Documents.Spatial;
using Microsoft.Azure.Documents.Sql;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Core Linq to DocDBSQL translator.
	/// </summary>
	internal static class ExpressionToSql
	{
		public static class LinqMethods
		{
			public const string Any = "Any";

			public const string Average = "Average";

			public const string Count = "Count";

			public const string Max = "Max";

			public const string Min = "Min";

			public const string OrderBy = "OrderBy";

			public const string OrderByDescending = "OrderByDescending";

			public const string Select = "Select";

			public const string SelectMany = "SelectMany";

			public const string Sum = "Sum";

			public const string Skip = "Skip";

			public const string Take = "Take";

			public const string Distinct = "Distinct";

			public const string Where = "Where";
		}

		private static string SqlRoot = "root";

		private static string DefaultParameterName = "v";

		private static bool usePropertyRef = false;

		private static SqlIdentifier RootIdentifier = SqlIdentifier.Create(SqlRoot);

		/// <summary>
		/// Toplevel entry point.
		/// </summary>
		/// <param name="inputExpression">An Expression representing a Query on a IDocumentQuery object.</param>
		/// <returns>The corresponding SQL query.</returns>
		public static SqlQuery TranslateQuery(Expression inputExpression)
		{
			TranslationContext translationContext = new TranslationContext();
			Translate(inputExpression, translationContext);
			return translationContext.currentQuery.FlattenAsPossible().GetSqlQuery();
		}

		/// <summary>
		/// Translate an expression into a query.
		/// Query is constructed as a side-effect in context.currentQuery.
		/// </summary>
		/// <param name="inputExpression">Expression to translate.</param>
		/// <param name="context">Context for translation.</param>
		public static Collection Translate(Expression inputExpression, TranslationContext context)
		{
			if (inputExpression == null)
			{
				throw new ArgumentNullException("inputExpression");
			}
			Collection result;
			switch (inputExpression.NodeType)
			{
			case ExpressionType.Call:
			{
				MethodCallExpression methodCallExpression = (MethodCallExpression)inputExpression;
				bool num = context.PeekMethod() == null && methodCallExpression.Method.Name.Equals("Any");
				result = VisitMethodCall(methodCallExpression, context);
				if (num)
				{
					result = ConvertToScalarAnyCollection(context);
				}
				break;
			}
			case ExpressionType.Constant:
				result = TranslateInput((ConstantExpression)inputExpression, context);
				break;
			case ExpressionType.MemberAccess:
				result = VisitMemberAccessCollectionExpression(inputExpression, context, GetBindingParameterName(context));
				break;
			case ExpressionType.Parameter:
				result = ConvertToCollection(VisitNonSubqueryScalarExpression(inputExpression, context));
				break;
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
			}
			return result;
		}

		private static Collection TranslateInput(ConstantExpression inputExpression, TranslationContext context)
		{
			if (!typeof(IDocumentQuery).IsAssignableFrom(inputExpression.Type))
			{
				throw new DocumentQueryException(ClientResources.InputIsNotIDocumentQuery);
			}
			if (!(inputExpression.Value is IDocumentQuery))
			{
				throw new DocumentQueryException(ClientResources.InputIsNotIDocumentQuery);
			}
			context.currentQuery = new QueryUnderConstruction(context.GetGenFreshParameterFunc());
			Type elementType = TypeSystem.GetElementType(inputExpression.Type);
			context.SetInputParameter(elementType, "root");
			return new Collection(SqlRoot);
		}

		/// <summary>
		/// Get a paramter name to be binded to the a collection from the next lambda.
		/// It's merely for readability purpose. If that is not possible, use a default 
		/// parameter name.
		/// </summary>
		/// <param name="context">The translation context</param>
		/// <returns>A parameter name</returns>
		private static string GetBindingParameterName(TranslationContext context)
		{
			MethodCallExpression methodCallExpression = context.PeekMethod();
			string text = null;
			if (methodCallExpression.Arguments.Count > 1)
			{
				LambdaExpression lambdaExpression = methodCallExpression.Arguments[1] as LambdaExpression;
				if (lambdaExpression != null && lambdaExpression.Parameters.Count > 0)
				{
					text = lambdaExpression.Parameters[0].Name;
				}
			}
			if (text == null)
			{
				text = DefaultParameterName;
			}
			return text;
		}

		/// <summary>
		/// Visitor which produces a SqlScalarExpression.
		/// </summary>
		/// <param name="inputExpression">Expression to visit.</param>
		/// <param name="context">Context information.</param>
		/// <returns>The translation as a ScalarExpression.</returns>
		internal static SqlScalarExpression VisitNonSubqueryScalarExpression(Expression inputExpression, TranslationContext context)
		{
			if (inputExpression == null)
			{
				return null;
			}
			switch (inputExpression.NodeType)
			{
			case ExpressionType.ArrayLength:
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
			case ExpressionType.Not:
			case ExpressionType.Quote:
			case ExpressionType.TypeAs:
				return VisitUnary((UnaryExpression)inputExpression, context);
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
				return VisitBinary((BinaryExpression)inputExpression, context);
			case ExpressionType.TypeIs:
				return VisitTypeIs((TypeBinaryExpression)inputExpression, context);
			case ExpressionType.Conditional:
				return VisitConditional((ConditionalExpression)inputExpression, context);
			case ExpressionType.Constant:
				return VisitConstant((ConstantExpression)inputExpression);
			case ExpressionType.Parameter:
				return VisitParameter((ParameterExpression)inputExpression, context);
			case ExpressionType.MemberAccess:
				return VisitMemberAccess((MemberExpression)inputExpression, context);
			case ExpressionType.New:
				return VisitNew((NewExpression)inputExpression, context);
			case ExpressionType.NewArrayInit:
			case ExpressionType.NewArrayBounds:
				return VisitNewArray((NewArrayExpression)inputExpression, context);
			case ExpressionType.Invoke:
				return VisitInvocation((InvocationExpression)inputExpression, context);
			case ExpressionType.MemberInit:
				return VisitMemberInit((MemberInitExpression)inputExpression, context);
			case ExpressionType.ListInit:
				return VisitListInit((ListInitExpression)inputExpression, context);
			case ExpressionType.Call:
				return VisitMethodCallScalar((MethodCallExpression)inputExpression, context);
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
			}
		}

		private static SqlScalarExpression VisitMethodCallScalar(MethodCallExpression methodCallExpression, TranslationContext context)
		{
			if (methodCallExpression.Method.Equals(typeof(UserDefinedFunctionProvider).GetMethod("Invoke")))
			{
				string value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value as string;
				if (string.IsNullOrEmpty(value))
				{
					throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.UdfNameIsNullOrEmpty));
				}
				SqlIdentifier name = SqlIdentifier.Create(value);
				List<SqlScalarExpression> list = new List<SqlScalarExpression>();
				if (methodCallExpression.Arguments.Count == 2)
				{
					if (methodCallExpression.Arguments[1] is NewArrayExpression)
					{
						foreach (Expression expression in ((NewArrayExpression)methodCallExpression.Arguments[1]).Expressions)
						{
							list.Add(VisitScalarExpression(expression, context));
						}
					}
					else if (methodCallExpression.Arguments[1].NodeType == ExpressionType.Constant && (object)methodCallExpression.Arguments[1].Type == typeof(object[]))
					{
						object[] array = (object[])((ConstantExpression)methodCallExpression.Arguments[1]).Value;
						foreach (object value2 in array)
						{
							list.Add(VisitConstant(Expression.Constant(value2)));
						}
					}
					else
					{
						list.Add(VisitScalarExpression(methodCallExpression.Arguments[1], context));
					}
				}
				return SqlFunctionCallScalarExpression.Create(name, isUdf: true, list.ToArray());
			}
			return BuiltinFunctionVisitor.VisitBuiltinFunctionCall(methodCallExpression, context);
		}

		private static SqlObjectProperty VisitBinding(MemberBinding binding, TranslationContext context)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
				return VisitMemberAssignment((MemberAssignment)binding, context);
			case MemberBindingType.MemberBinding:
				return VisitMemberMemberBinding((MemberMemberBinding)binding, context);
			default:
				return VisitMemberListBinding((MemberListBinding)binding, context);
			}
		}

		private static SqlUnaryScalarOperatorKind GetUnaryOperatorKind(ExpressionType type)
		{
			switch (type)
			{
			case ExpressionType.UnaryPlus:
				return SqlUnaryScalarOperatorKind.Plus;
			case ExpressionType.Negate:
				return SqlUnaryScalarOperatorKind.Minus;
			case ExpressionType.OnesComplement:
				return SqlUnaryScalarOperatorKind.BitwiseNot;
			case ExpressionType.Not:
				return SqlUnaryScalarOperatorKind.Not;
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.UnaryOperatorNotSupported, type));
			}
		}

		private static SqlScalarExpression VisitUnary(UnaryExpression inputExpression, TranslationContext context)
		{
			SqlScalarExpression sqlScalarExpression = VisitScalarExpression(inputExpression.Operand, context);
			if (sqlScalarExpression is SqlInScalarExpression && inputExpression.NodeType == ExpressionType.Not)
			{
				SqlInScalarExpression sqlInScalarExpression = (SqlInScalarExpression)sqlScalarExpression;
				return SqlInScalarExpression.Create(sqlInScalarExpression.Expression, not: true, sqlInScalarExpression.Items);
			}
			if (inputExpression.NodeType == ExpressionType.Quote)
			{
				return sqlScalarExpression;
			}
			if (inputExpression.NodeType == ExpressionType.Convert)
			{
				return sqlScalarExpression;
			}
			return SqlUnaryScalarExpression.Create(GetUnaryOperatorKind(inputExpression.NodeType), sqlScalarExpression);
		}

		private static SqlBinaryScalarOperatorKind GetBinaryOperatorKind(ExpressionType expressionType, Type resultType)
		{
			switch (expressionType)
			{
			case ExpressionType.Add:
				if ((object)resultType == typeof(string))
				{
					return SqlBinaryScalarOperatorKind.StringConcat;
				}
				return SqlBinaryScalarOperatorKind.Add;
			case ExpressionType.AndAlso:
				return SqlBinaryScalarOperatorKind.And;
			case ExpressionType.And:
				return SqlBinaryScalarOperatorKind.BitwiseAnd;
			case ExpressionType.Or:
				return SqlBinaryScalarOperatorKind.BitwiseOr;
			case ExpressionType.ExclusiveOr:
				return SqlBinaryScalarOperatorKind.BitwiseXor;
			case ExpressionType.Divide:
				return SqlBinaryScalarOperatorKind.Divide;
			case ExpressionType.Equal:
				return SqlBinaryScalarOperatorKind.Equal;
			case ExpressionType.GreaterThan:
				return SqlBinaryScalarOperatorKind.GreaterThan;
			case ExpressionType.GreaterThanOrEqual:
				return SqlBinaryScalarOperatorKind.GreaterThanOrEqual;
			case ExpressionType.LessThan:
				return SqlBinaryScalarOperatorKind.LessThan;
			case ExpressionType.LessThanOrEqual:
				return SqlBinaryScalarOperatorKind.LessThanOrEqual;
			case ExpressionType.Modulo:
				return SqlBinaryScalarOperatorKind.Modulo;
			case ExpressionType.Multiply:
				return SqlBinaryScalarOperatorKind.Multiply;
			case ExpressionType.NotEqual:
				return SqlBinaryScalarOperatorKind.NotEqual;
			case ExpressionType.OrElse:
				return SqlBinaryScalarOperatorKind.Or;
			case ExpressionType.Subtract:
				return SqlBinaryScalarOperatorKind.Subtract;
			case ExpressionType.Coalesce:
				return SqlBinaryScalarOperatorKind.Coalesce;
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.BinaryOperatorNotSupported, expressionType));
			}
		}

		private static SqlScalarExpression VisitBinary(BinaryExpression inputExpression, TranslationContext context)
		{
			MethodCallExpression methodCallExpression = null;
			ConstantExpression constantExpression = null;
			bool reverseNodeType = false;
			if (inputExpression.Left.NodeType == ExpressionType.Call && inputExpression.Right.NodeType == ExpressionType.Constant)
			{
				methodCallExpression = (MethodCallExpression)inputExpression.Left;
				constantExpression = (ConstantExpression)inputExpression.Right;
			}
			else if (inputExpression.Right.NodeType == ExpressionType.Call && inputExpression.Left.NodeType == ExpressionType.Constant)
			{
				methodCallExpression = (MethodCallExpression)inputExpression.Right;
				constantExpression = (ConstantExpression)inputExpression.Left;
				reverseNodeType = true;
			}
			if (methodCallExpression != null && constantExpression != null && TryMatchStringCompareTo(methodCallExpression, constantExpression, inputExpression.NodeType))
			{
				return VisitStringCompareTo(methodCallExpression, constantExpression, inputExpression.NodeType, reverseNodeType, context);
			}
			SqlScalarExpression sqlScalarExpression = VisitScalarExpression(inputExpression.Left, context);
			SqlScalarExpression sqlScalarExpression2 = VisitScalarExpression(inputExpression.Right, context);
			if (inputExpression.NodeType == ExpressionType.ArrayIndex)
			{
				return SqlMemberIndexerScalarExpression.Create(sqlScalarExpression, sqlScalarExpression2);
			}
			SqlBinaryScalarOperatorKind binaryOperatorKind = GetBinaryOperatorKind(inputExpression.NodeType, inputExpression.Type);
			if (sqlScalarExpression.Kind == SqlObjectKind.MemberIndexerScalarExpression && sqlScalarExpression2.Kind == SqlObjectKind.LiteralScalarExpression)
			{
				sqlScalarExpression2 = ApplyCustomConverters(inputExpression.Left, sqlScalarExpression2 as SqlLiteralScalarExpression);
			}
			else if (sqlScalarExpression2.Kind == SqlObjectKind.MemberIndexerScalarExpression && sqlScalarExpression.Kind == SqlObjectKind.LiteralScalarExpression)
			{
				sqlScalarExpression = ApplyCustomConverters(inputExpression.Right, sqlScalarExpression as SqlLiteralScalarExpression);
			}
			return SqlBinaryScalarExpression.Create(binaryOperatorKind, sqlScalarExpression, sqlScalarExpression2);
		}

		private static SqlScalarExpression ApplyCustomConverters(Expression left, SqlLiteralScalarExpression right)
		{
			MemberExpression memberExpression = (!(left is UnaryExpression)) ? (left as MemberExpression) : (((UnaryExpression)left).Operand as MemberExpression);
			if (memberExpression != null)
			{
				Type type = memberExpression.Type;
				if (type.IsNullable())
				{
					type = type.NullableUnderlyingType();
				}
				object customAttributeData = (from ca in memberExpression.Member.CustomAttributes
				where (object)ca.AttributeType == typeof(JsonConverterAttribute)
				select ca).FirstOrDefault();
				CustomAttributeData customAttributeData2 = (from ca in type.GetsCustomAttributes()
				where (object)ca.AttributeType == typeof(JsonConverterAttribute)
				select ca).FirstOrDefault();
				if (customAttributeData == null)
				{
					customAttributeData = customAttributeData2;
				}
				CustomAttributeData customAttributeData3 = (CustomAttributeData)customAttributeData;
				if (customAttributeData3 != null)
				{
					Type type2 = (Type)customAttributeData3.ConstructorArguments[0].Value;
					object obj = null;
					if (type.IsEnum())
					{
						Number64 value = ((SqlNumberLiteral)right.Literal).Value;
						obj = ((!value.IsDouble) ? Enum.ToObject(type, (object)Number64.ToLong(value)) : Enum.ToObject(type, Number64.ToDouble(value)));
					}
					else if ((object)type == typeof(DateTime))
					{
						obj = ((SqlObjectLiteral)right.Literal).Value;
					}
					if (obj != null)
					{
						string value2 = ((object)CustomTypeExtensions.GetConstructor(type2, Type.EmptyTypes) == null) ? JsonConvert.SerializeObject(obj) : JsonConvert.SerializeObject(obj, (JsonConverter)Activator.CreateInstance(type2));
						return SqlLiteralScalarExpression.Create(SqlObjectLiteral.Create(value2, isValueSerialized: true));
					}
				}
			}
			return right;
		}

		private static bool TryMatchStringCompareTo(MethodCallExpression left, ConstantExpression right, ExpressionType compareOperator)
		{
			if (left.Method.Equals(typeof(string).GetMethod("CompareTo", new Type[1]
			{
				typeof(string)
			})) && left.Arguments.Count == 1)
			{
				switch (compareOperator)
				{
				default:
					throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.StringCompareToInvalidOperator));
				case ExpressionType.Equal:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
					if (((object)right.Type != typeof(int) || (int)right.Value != 0) && ((object)right.Type != typeof(int?) || !((int?)right.Value).HasValue || ((int?)right.Value).Value != 0))
					{
						throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.StringCompareToInvalidConstant));
					}
					return true;
				}
			}
			return false;
		}

		private static SqlScalarExpression VisitStringCompareTo(MethodCallExpression left, ConstantExpression right, ExpressionType compareOperator, bool reverseNodeType, TranslationContext context)
		{
			if (reverseNodeType)
			{
				switch (compareOperator)
				{
				case ExpressionType.GreaterThan:
					compareOperator = ExpressionType.LessThan;
					break;
				case ExpressionType.GreaterThanOrEqual:
					compareOperator = ExpressionType.LessThanOrEqual;
					break;
				case ExpressionType.LessThan:
					compareOperator = ExpressionType.GreaterThan;
					break;
				case ExpressionType.LessThanOrEqual:
					compareOperator = ExpressionType.GreaterThanOrEqual;
					break;
				default:
					throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.StringCompareToInvalidOperator));
				case ExpressionType.Equal:
					break;
				}
			}
			SqlBinaryScalarOperatorKind binaryOperatorKind = GetBinaryOperatorKind(compareOperator, null);
			SqlScalarExpression leftExpression = VisitNonSubqueryScalarExpression(left.Object, context);
			SqlScalarExpression rightExpression = VisitNonSubqueryScalarExpression(left.Arguments[0], context);
			return SqlBinaryScalarExpression.Create(binaryOperatorKind, leftExpression, rightExpression);
		}

		private static SqlScalarExpression VisitTypeIs(TypeBinaryExpression inputExpression, TranslationContext context)
		{
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
		}

		public static SqlScalarExpression VisitConstant(ConstantExpression inputExpression)
		{
			if (inputExpression.Value == null)
			{
				return SqlLiteralScalarExpression.SqlNullLiteralScalarExpression;
			}
			if (inputExpression.Type.IsNullable())
			{
				return VisitConstant(Expression.Constant(inputExpression.Value, Nullable.GetUnderlyingType(inputExpression.Type)));
			}
			Type type = inputExpression.Value.GetType();
			if (type.IsValueType())
			{
				if ((object)type == typeof(bool))
				{
					return SqlLiteralScalarExpression.Create(SqlBooleanLiteral.Create((bool)inputExpression.Value));
				}
				if ((object)type == typeof(byte))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((byte)inputExpression.Value));
				}
				if ((object)type == typeof(sbyte))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((sbyte)inputExpression.Value));
				}
				if ((object)type == typeof(char))
				{
					return SqlLiteralScalarExpression.Create(SqlStringLiteral.Create(inputExpression.Value.ToString()));
				}
				if ((object)type == typeof(decimal))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((decimal)inputExpression.Value));
				}
				if ((object)type == typeof(double))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((double)inputExpression.Value));
				}
				if ((object)type == typeof(float))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((float)inputExpression.Value));
				}
				if ((object)type == typeof(int))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((int)inputExpression.Value));
				}
				if ((object)type == typeof(uint))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((uint)inputExpression.Value));
				}
				if ((object)type == typeof(long))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((long)inputExpression.Value));
				}
				if ((object)type == typeof(ulong))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((decimal)(ulong)inputExpression.Value));
				}
				if ((object)type == typeof(short))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((short)inputExpression.Value));
				}
				if ((object)type == typeof(ushort))
				{
					return SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create((ushort)inputExpression.Value));
				}
				if ((object)type == typeof(Guid))
				{
					return SqlLiteralScalarExpression.Create(SqlStringLiteral.Create(inputExpression.Value.ToString()));
				}
			}
			if ((object)type == typeof(string))
			{
				return SqlLiteralScalarExpression.Create(SqlStringLiteral.Create((string)inputExpression.Value));
			}
			if (typeof(Geometry).IsAssignableFrom(type))
			{
				return GeometrySqlExpressionFactory.Construct(inputExpression);
			}
			if (inputExpression.Value is IEnumerable)
			{
				List<SqlScalarExpression> list = new List<SqlScalarExpression>();
				foreach (object item in (IEnumerable)inputExpression.Value)
				{
					list.Add(VisitConstant(Expression.Constant(item)));
				}
				return SqlArrayCreateScalarExpression.Create(list.ToArray());
			}
			return SqlLiteralScalarExpression.Create(SqlObjectLiteral.Create(inputExpression.Value, isValueSerialized: false));
		}

		private static SqlScalarExpression VisitConditional(ConditionalExpression inputExpression, TranslationContext context)
		{
			SqlScalarExpression condition = VisitScalarExpression(inputExpression.Test, context);
			SqlScalarExpression first = VisitScalarExpression(inputExpression.IfTrue, context);
			SqlScalarExpression second = VisitScalarExpression(inputExpression.IfFalse, context);
			return SqlConditionalScalarExpression.Create(condition, first, second);
		}

		private static SqlScalarExpression VisitParameter(ParameterExpression inputExpression, TranslationContext context)
		{
			Expression expression = context.LookupSubstitution(inputExpression);
			if (expression != null)
			{
				return VisitNonSubqueryScalarExpression(expression, context);
			}
			SqlIdentifier propertyIdentifier = SqlIdentifier.Create(inputExpression.Name);
			return SqlPropertyRefScalarExpression.Create(null, propertyIdentifier);
		}

		private static SqlScalarExpression VisitMemberAccess(MemberExpression inputExpression, TranslationContext context)
		{
			SqlScalarExpression sqlScalarExpression = VisitScalarExpression(inputExpression.Expression, context);
			string memberName = inputExpression.Member.GetMemberName();
			if (inputExpression.Expression.Type.IsNullable())
			{
				if (memberName == "Value")
				{
					return sqlScalarExpression;
				}
				if (memberName == "HasValue")
				{
					return SqlFunctionCallScalarExpression.CreateBuiltin("IS_DEFINED", sqlScalarExpression);
				}
			}
			if (usePropertyRef)
			{
				SqlIdentifier propertyIdentifier = SqlIdentifier.Create(memberName);
				return SqlPropertyRefScalarExpression.Create(sqlScalarExpression, propertyIdentifier);
			}
			SqlScalarExpression indexExpression = SqlLiteralScalarExpression.Create(SqlStringLiteral.Create(memberName));
			return SqlMemberIndexerScalarExpression.Create(sqlScalarExpression, indexExpression);
		}

		private static SqlScalarExpression[] VisitExpressionList(ReadOnlyCollection<Expression> inputExpressionList, TranslationContext context)
		{
			SqlScalarExpression[] array = new SqlScalarExpression[inputExpressionList.Count];
			for (int i = 0; i < inputExpressionList.Count; i++)
			{
				SqlScalarExpression sqlScalarExpression = array[i] = VisitScalarExpression(inputExpressionList[i], context);
			}
			return array;
		}

		private static SqlObjectProperty VisitMemberAssignment(MemberAssignment inputExpression, TranslationContext context)
		{
			SqlScalarExpression expression = VisitScalarExpression(inputExpression.Expression, context);
			return SqlObjectProperty.Create(SqlPropertyName.Create(inputExpression.Member.GetMemberName()), expression);
		}

		private static SqlObjectProperty VisitMemberMemberBinding(MemberMemberBinding inputExpression, TranslationContext context)
		{
			throw new DocumentQueryException(ClientResources.MemberBindingNotSupported);
		}

		private static SqlObjectProperty VisitMemberListBinding(MemberListBinding inputExpression, TranslationContext context)
		{
			throw new DocumentQueryException(ClientResources.MemberBindingNotSupported);
		}

		private static SqlObjectProperty[] VisitBindingList(ReadOnlyCollection<MemberBinding> inputExpressionList, TranslationContext context)
		{
			SqlObjectProperty[] array = new SqlObjectProperty[inputExpressionList.Count];
			for (int i = 0; i < inputExpressionList.Count; i++)
			{
				SqlObjectProperty sqlObjectProperty = array[i] = VisitBinding(inputExpressionList[i], context);
			}
			return array;
		}

		private static SqlObjectProperty[] CreateInitializers(ReadOnlyCollection<Expression> arguments, ReadOnlyCollection<MemberInfo> members, TranslationContext context)
		{
			if (arguments.Count != members.Count)
			{
				throw new InvalidOperationException("Expected same number of arguments as members");
			}
			SqlObjectProperty[] array = new SqlObjectProperty[arguments.Count];
			for (int i = 0; i < arguments.Count; i++)
			{
				Expression expression = arguments[i];
				MemberInfo memberInfo = members[i];
				SqlScalarExpression expression2 = VisitScalarExpression(expression, context);
				SqlObjectProperty sqlObjectProperty = array[i] = SqlObjectProperty.Create(SqlPropertyName.Create(memberInfo.GetMemberName()), expression2);
			}
			return array;
		}

		private static SqlScalarExpression VisitNew(NewExpression inputExpression, TranslationContext context)
		{
			if (typeof(Geometry).IsAssignableFrom(inputExpression.Type))
			{
				return GeometrySqlExpressionFactory.Construct(inputExpression);
			}
			if (inputExpression.Arguments.Count > 0)
			{
				if (inputExpression.Members == null)
				{
					throw new DocumentQueryException(ClientResources.ConstructorInvocationNotSupported);
				}
				return SqlObjectCreateScalarExpression.Create(CreateInitializers(inputExpression.Arguments, inputExpression.Members, context));
			}
			return null;
		}

		private static SqlScalarExpression VisitMemberInit(MemberInitExpression inputExpression, TranslationContext context)
		{
			VisitNew(inputExpression.NewExpression, context);
			return SqlObjectCreateScalarExpression.Create(VisitBindingList(inputExpression.Bindings, context));
		}

		private static SqlScalarExpression VisitListInit(ListInitExpression inputExpression, TranslationContext context)
		{
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
		}

		private static SqlScalarExpression VisitNewArray(NewArrayExpression inputExpression, TranslationContext context)
		{
			SqlScalarExpression[] items = VisitExpressionList(inputExpression.Expressions, context);
			if (inputExpression.NodeType == ExpressionType.NewArrayInit)
			{
				return SqlArrayCreateScalarExpression.Create(items);
			}
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
		}

		private static SqlScalarExpression VisitInvocation(InvocationExpression inputExpression, TranslationContext context)
		{
			throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, inputExpression.NodeType));
		}

		private static Collection ConvertToCollection(SqlScalarExpression scalar)
		{
			if (usePropertyRef)
			{
				SqlPropertyRefScalarExpression obj = scalar as SqlPropertyRefScalarExpression;
				if (obj == null)
				{
					throw new DocumentQueryException(ClientResources.PathExpressionsOnly);
				}
				return new Collection(ConvertPropertyRefToPath(obj));
			}
			SqlMemberIndexerScalarExpression sqlMemberIndexerScalarExpression = scalar as SqlMemberIndexerScalarExpression;
			if (sqlMemberIndexerScalarExpression == null)
			{
				SqlPropertyRefScalarExpression obj2 = scalar as SqlPropertyRefScalarExpression;
				if (obj2 == null)
				{
					throw new DocumentQueryException(ClientResources.PathExpressionsOnly);
				}
				return new Collection(ConvertPropertyRefToPath(obj2));
			}
			return new Collection(ConvertMemberIndexerToPath(sqlMemberIndexerScalarExpression));
		}

		/// <summary>
		/// Convert the context's current query to a scalar Any collection
		/// by wrapping it as following: SELECT VALUE COUNT(v0) &gt; 0 FROM (current query) AS v0.
		/// This is used in cases where LINQ expression ends with Any() which is a boolean scalar.
		/// Normally Any would translate to SELECT VALUE EXISTS() subquery. However that wouldn't work
		/// for these cases because it would result in a boolean value for each row instead of 
		/// one single "aggregated" boolean value.
		/// </summary>
		/// <param name="context">The translation context</param>
		/// <returns>The scalar Any collection</returns>
		private static Collection ConvertToScalarAnyCollection(TranslationContext context)
		{
			SqlCollection collection = SqlSubqueryCollection.Create(context.currentQuery.FlattenAsPossible().GetSqlQuery());
			ParameterExpression parameterExpression = context.GenFreshParameter(typeof(object), DefaultParameterName);
			FromParameterBindings.Binding binding = new FromParameterBindings.Binding(parameterExpression, collection, isInCollection: false);
			context.currentQuery = new QueryUnderConstruction(context.GetGenFreshParameterFunc());
			context.currentQuery.AddBinding(binding);
			SqlSelectClause select = SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlBinaryScalarExpression.Create(SqlBinaryScalarOperatorKind.GreaterThan, SqlFunctionCallScalarExpression.CreateBuiltin("COUNT", SqlPropertyRefScalarExpression.Create(null, SqlIdentifier.Create(parameterExpression.Name))), SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create(0L)))));
			context.currentQuery.AddSelectClause(select);
			return new Collection("Any");
		}

		private static SqlScalarExpression VisitNonSubqueryScalarExpression(Expression expression, ReadOnlyCollection<ParameterExpression> parameters, TranslationContext context)
		{
			foreach (ParameterExpression parameter in parameters)
			{
				context.PushParameter(parameter, context.CurrentSubqueryBinding.ShouldBeOnNewQuery);
			}
			SqlScalarExpression result = VisitNonSubqueryScalarExpression(expression, context);
			foreach (ParameterExpression parameter2 in parameters)
			{
				ParameterExpression parameterExpression = parameter2;
				context.PopParameter();
			}
			return result;
		}

		private static SqlScalarExpression VisitNonSubqueryScalarLambda(LambdaExpression lambdaExpression, TranslationContext context)
		{
			ReadOnlyCollection<ParameterExpression> parameters = lambdaExpression.Parameters;
			if (parameters.Count != 1)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, lambdaExpression.Body, 1, parameters.Count));
			}
			return VisitNonSubqueryScalarExpression(lambdaExpression.Body, parameters, context);
		}

		private static Collection VisitCollectionExpression(Expression expression, ReadOnlyCollection<ParameterExpression> parameters, TranslationContext context)
		{
			foreach (ParameterExpression parameter in parameters)
			{
				context.PushParameter(parameter, context.CurrentSubqueryBinding.ShouldBeOnNewQuery);
			}
			Collection result = VisitCollectionExpression(expression, context, (parameters.Count > 0) ? parameters.First().Name : DefaultParameterName);
			foreach (ParameterExpression parameter2 in parameters)
			{
				ParameterExpression parameterExpression = parameter2;
				context.PopParameter();
			}
			return result;
		}

		private static Collection VisitCollectionExpression(Expression expression, TranslationContext context, string parameterName)
		{
			switch (expression.NodeType)
			{
			case ExpressionType.Call:
				return Translate(expression, context);
			case ExpressionType.MemberAccess:
				return VisitMemberAccessCollectionExpression(expression, context, parameterName);
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.ExpressionTypeIsNotSupported, expression.NodeType));
			}
		}

		/// <summary>
		/// Visit a lambda which is supposed to return a collection.
		/// </summary>
		/// <param name="lambdaExpression">LambdaExpression with a result which is a collection.</param>
		/// <param name="context">The translation context.</param>
		/// <returns>The collection computed by the lambda.</returns>
		private static Collection VisitCollectionLambda(LambdaExpression lambdaExpression, TranslationContext context)
		{
			ReadOnlyCollection<ParameterExpression> parameters = lambdaExpression.Parameters;
			if (parameters.Count != 1)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, lambdaExpression.Body, 1, parameters.Count));
			}
			return VisitCollectionExpression(lambdaExpression.Body, lambdaExpression.Parameters, context);
		}

		/// <summary>
		/// Visit an expression, usually a MemberAccess, then trigger parameter binding for that expression.
		/// </summary>
		/// <param name="inputExpression">The input expression</param>
		/// <param name="context">The current translation context</param>
		/// <param name="parameterName">Parameter name is merely for readability</param>
		/// <returns></returns>
		private static Collection VisitMemberAccessCollectionExpression(Expression inputExpression, TranslationContext context, string parameterName)
		{
			SqlScalarExpression scalar = VisitNonSubqueryScalarExpression(inputExpression, context);
			Type type = inputExpression.Type;
			Collection collection = ConvertToCollection(scalar);
			context.PushCollection(collection);
			ParameterExpression parameterExpression = context.GenFreshParameter(type, parameterName);
			context.PushParameter(parameterExpression, context.CurrentSubqueryBinding.ShouldBeOnNewQuery);
			context.PopParameter();
			context.PopCollection();
			return new Collection(parameterExpression.Name);
		}

		/// <summary>
		/// Visit a method call, construct the corresponding query in context.currentQuery.
		/// At ExpressionToSql point only LINQ method calls are allowed.
		/// These methods are static extension methods of IQueryable or IEnumerable.
		/// </summary>
		/// <param name="inputExpression">Method to translate.</param>
		/// <param name="context">Query translation context.</param>
		private static Collection VisitMethodCall(MethodCallExpression inputExpression, TranslationContext context)
		{
			context.PushMethod(inputExpression);
			Type declaringType = inputExpression.Method.DeclaringType;
			if (((object)declaringType != typeof(Queryable) && (object)declaringType != typeof(Enumerable)) || !inputExpression.Method.IsStatic)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.OnlyLINQMethodsAreSupported, inputExpression.Method.Name));
			}
			TypeSystem.GetElementType(inputExpression.Method.ReturnType);
			if (inputExpression.Object != null)
			{
				throw new DocumentQueryException(ClientResources.ExpectedMethodCallsMethods);
			}
			Expression expression = inputExpression.Arguments[0];
			TypeSystem.GetElementType(expression.Type);
			Collection collection = Translate(expression, context);
			context.PushCollection(collection);
			Collection result = new Collection(inputExpression.Method.Name);
			bool shouldBeOnNewQuery = context.currentQuery.ShouldBeOnNewQuery(inputExpression.Method.Name, inputExpression.Arguments.Count);
			context.PushSubqueryBinding(shouldBeOnNewQuery);
			switch (inputExpression.Method.Name)
			{
			case "Select":
			{
				SqlSelectClause select7 = VisitSelect(inputExpression.Arguments, context);
				context.currentQuery = context.currentQuery.AddSelectClause(select7, context);
				break;
			}
			case "Where":
			{
				SqlWhereClause whereClause2 = VisitWhere(inputExpression.Arguments, context);
				context.currentQuery = context.currentQuery.AddWhereClause(whereClause2, context);
				break;
			}
			case "SelectMany":
				context.currentQuery = context.PackageCurrentQueryIfNeccessary();
				result = VisitSelectMany(inputExpression.Arguments, context);
				break;
			case "OrderBy":
			{
				SqlOrderbyClause orderBy2 = VisitOrderBy(inputExpression.Arguments, isDescending: false, context);
				context.currentQuery = context.currentQuery.AddOrderByClause(orderBy2, context);
				break;
			}
			case "OrderByDescending":
			{
				SqlOrderbyClause orderBy = VisitOrderBy(inputExpression.Arguments, isDescending: true, context);
				context.currentQuery = context.currentQuery.AddOrderByClause(orderBy, context);
				break;
			}
			case "Skip":
			{
				SqlOffsetSpec offsetSpec = VisitSkip(inputExpression.Arguments, context);
				context.currentQuery = context.currentQuery.AddOffsetSpec(offsetSpec, context);
				break;
			}
			case "Take":
				if (context.currentQuery.HasOffsetSpec())
				{
					SqlLimitSpec limitSpec = VisitTakeLimit(inputExpression.Arguments, context);
					context.currentQuery = context.currentQuery.AddLimitSpec(limitSpec, context);
				}
				else
				{
					SqlTopSpec topSpec = VisitTakeTop(inputExpression.Arguments, context);
					context.currentQuery = context.currentQuery.AddTopSpec(topSpec);
				}
				break;
			case "Distinct":
			{
				SqlSelectClause select6 = VisitDistinct(inputExpression.Arguments, context);
				context.currentQuery = context.currentQuery.AddSelectClause(select6, context);
				break;
			}
			case "Max":
			{
				SqlSelectClause select5 = VisitAggregateFunction(inputExpression.Arguments, context, "MAX");
				context.currentQuery = context.currentQuery.AddSelectClause(select5, context);
				break;
			}
			case "Min":
			{
				SqlSelectClause select4 = VisitAggregateFunction(inputExpression.Arguments, context, "MIN");
				context.currentQuery = context.currentQuery.AddSelectClause(select4, context);
				break;
			}
			case "Average":
			{
				SqlSelectClause select3 = VisitAggregateFunction(inputExpression.Arguments, context, "AVG");
				context.currentQuery = context.currentQuery.AddSelectClause(select3, context);
				break;
			}
			case "Count":
			{
				SqlSelectClause select2 = VisitCount(inputExpression.Arguments, context);
				context.currentQuery = context.currentQuery.AddSelectClause(select2, context);
				break;
			}
			case "Sum":
			{
				SqlSelectClause select = VisitAggregateFunction(inputExpression.Arguments, context, "SUM");
				context.currentQuery = context.currentQuery.AddSelectClause(select, context);
				break;
			}
			case "Any":
				result = new Collection("");
				if (inputExpression.Arguments.Count == 2)
				{
					SqlWhereClause whereClause = VisitWhere(inputExpression.Arguments, context);
					context.currentQuery = context.currentQuery.AddWhereClause(whereClause, context);
				}
				break;
			default:
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.MethodNotSupported, inputExpression.Method.Name));
			}
			context.PopSubqueryBinding();
			context.PopCollection();
			context.PopMethod();
			return result;
		}

		/// <summary>
		/// Determine if an expression should be translated to a subquery.
		/// This only applies to expression that is inside a lamda.
		/// </summary>
		/// <param name="expression">The input expression</param>
		/// <param name="expressionObjKind">The expression object kind of the expression</param>
		/// <param name="isMinMaxAvgMethod">True if the method is either Min, Max, or Avg</param>
		/// <returns>True if subquery is needed, otherwise false</returns>
		private static bool IsSubqueryScalarExpression(Expression expression, out SqlObjectKind? expressionObjKind, out bool isMinMaxAvgMethod)
		{
			MethodCallExpression methodCallExpression = expression as MethodCallExpression;
			if (methodCallExpression == null)
			{
				expressionObjKind = null;
				isMinMaxAvgMethod = false;
				return false;
			}
			string name = methodCallExpression.Method.Name;
			isMinMaxAvgMethod = false;
			bool result;
			switch (name)
			{
			case "Min":
			case "Max":
			case "Average":
				isMinMaxAvgMethod = true;
				result = true;
				expressionObjKind = SqlObjectKind.SubqueryScalarExpression;
				break;
			case "Sum":
				result = true;
				expressionObjKind = SqlObjectKind.SubqueryScalarExpression;
				break;
			case "Count":
			{
				SqlObjectKind? expressionObjKind2;
				bool isMinMaxAvgMethod2;
				if (methodCallExpression.Arguments.Count > 1)
				{
					result = true;
					expressionObjKind = SqlObjectKind.SubqueryScalarExpression;
				}
				else if (IsSubqueryScalarExpression(methodCallExpression.Arguments[0] as MethodCallExpression, out expressionObjKind2, out isMinMaxAvgMethod2))
				{
					result = true;
					expressionObjKind = SqlObjectKind.SubqueryScalarExpression;
				}
				else
				{
					result = false;
					expressionObjKind = null;
				}
				break;
			}
			case "Any":
				result = true;
				expressionObjKind = SqlObjectKind.ExistsScalarExpression;
				break;
			case "Select":
			case "SelectMany":
			case "Where":
			case "OrderBy":
			case "OrderByDescending":
			case "Skip":
			case "Take":
			case "Distinct":
				result = true;
				expressionObjKind = SqlObjectKind.ArrayScalarExpression;
				break;
			default:
				result = false;
				expressionObjKind = null;
				break;
			}
			return result;
		}

		/// <summary>
		/// Visit an lambda expression which is in side a lambda and translate it to a scalar expression or a subquery scalar expression.
		/// See the other overload of this method for more details.
		/// </summary>
		/// <param name="lambda">The input lambda expression</param>
		/// <param name="context">The translation context</param>
		/// <returns>A scalar expression representing the input expression</returns>
		private static SqlScalarExpression VisitScalarExpression(LambdaExpression lambda, TranslationContext context)
		{
			return VisitScalarExpression(lambda.Body, lambda.Parameters, context);
		}

		/// <summary>
		/// Visit an lambda expression which is in side a lambda and translate it to a scalar expression or a collection scalar expression.
		/// If it is a collection scalar expression, e.g. should be translated to subquery such as SELECT VALUE ARRAY, SELECT VALUE EXISTS, 
		/// SELECT VALUE [aggregate], the subquery will be aliased to a new binding for the FROM clause. E.g. consider 
		/// Select(family =&gt; family.Children.Select(child =&gt; child.Grade)). Since the inner Select corresponds to a subquery, this method would 
		/// create a new binding of v0 to the subquery SELECT VALUE ARRAY(), and the inner expression will be just SELECT v0.
		/// </summary>
		/// <param name="expression">The input expression</param>
		/// <param name="context">The translation context</param>
		/// <returns>A scalar expression representing the input expression</returns>
		internal static SqlScalarExpression VisitScalarExpression(Expression expression, TranslationContext context)
		{
			return VisitScalarExpression(expression, new ReadOnlyCollection<ParameterExpression>(new ParameterExpression[0]), context);
		}

		/// <summary>
		/// Visit an lambda expression which is in side a lambda and translate it to a scalar expression or a collection scalar expression.
		/// See the other overload of this method for more details.
		/// </summary>
		private static SqlScalarExpression VisitScalarExpression(Expression expression, ReadOnlyCollection<ParameterExpression> parameters, TranslationContext context)
		{
			if (!IsSubqueryScalarExpression(expression, out SqlObjectKind? expressionObjKind, out bool isMinMaxAvgMethod))
			{
				return VisitNonSubqueryScalarExpression(expression, parameters, context);
			}
			SqlQuery query = CreateSubquery(expression, parameters, context);
			ParameterExpression parameterExpression = context.GenFreshParameter(typeof(object), DefaultParameterName);
			SqlCollection collection = CreateSubquerySqlCollection(query, context, isMinMaxAvgMethod ? SqlObjectKind.ArrayScalarExpression : expressionObjKind.Value);
			FromParameterBindings.Binding item = new FromParameterBindings.Binding(parameterExpression, collection, isInCollection: false, context.IsInMainBranchSelect());
			context.CurrentSubqueryBinding.NewBindings.Add(item);
			if (isMinMaxAvgMethod)
			{
				return SqlMemberIndexerScalarExpression.Create(SqlPropertyRefScalarExpression.Create(null, SqlIdentifier.Create(parameterExpression.Name)), SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create(0L)));
			}
			return SqlPropertyRefScalarExpression.Create(null, SqlIdentifier.Create(parameterExpression.Name));
		}

		/// <summary>
		/// Create a subquery SQL collection object for a SQL query
		/// </summary>
		/// <param name="query">The SQL query object</param>
		/// <param name="context">The translation context</param>
		/// <param name="subqueryType">The subquery type</param>
		/// <returns></returns>
		private static SqlCollection CreateSubquerySqlCollection(SqlQuery query, TranslationContext context, SqlObjectKind subqueryType)
		{
			switch (subqueryType)
			{
			case SqlObjectKind.ArrayScalarExpression:
				query = SqlQuery.Create(SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlArrayScalarExpression.Create(query))), null, null, null, null, null);
				break;
			case SqlObjectKind.ExistsScalarExpression:
				query = SqlQuery.Create(SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlExistsScalarExpression.Create(query))), null, null, null, null, null);
				break;
			default:
				throw new DocumentQueryException($"Unsupported subquery type {subqueryType}");
			case SqlObjectKind.SubqueryScalarExpression:
				break;
			}
			return SqlSubqueryCollection.Create(query);
		}

		/// <summary>
		/// Create a subquery from a subquery scalar expression.
		/// By visiting the collection expression, this builds a new QueryUnderConstruction on top of the current one
		/// and then translate it to a SQL query while keeping the current QueryUnderConstruction in tact.
		/// </summary>
		/// <param name="expression">The subquery scalar expression</param>
		/// <param name="parameters">The list of parameters of the expression</param>
		/// <param name="context">The translation context</param>
		/// <returns>A query corresponding to the collection expression</returns>
		/// <remarks>The QueryUnderConstruction remains unchanged after this.</remarks>
		private static SqlQuery CreateSubquery(Expression expression, ReadOnlyCollection<ParameterExpression> parameters, TranslationContext context)
		{
			bool shouldBeOnNewQuery = context.CurrentSubqueryBinding.ShouldBeOnNewQuery;
			QueryUnderConstruction currentQuery = context.currentQuery;
			QueryUnderConstruction queryUnderConstruction = new QueryUnderConstruction(context.GetGenFreshParameterFunc(), context.currentQuery);
			queryUnderConstruction.fromParameters.SetInputParameter(typeof(object), context.currentQuery.GetInputParameterInContext(shouldBeOnNewQuery).Name, context.InScope);
			context.currentQuery = queryUnderConstruction;
			if (shouldBeOnNewQuery)
			{
				context.CurrentSubqueryBinding.ShouldBeOnNewQuery = false;
			}
			VisitCollectionExpression(expression, parameters, context);
			QueryUnderConstruction subquery = context.currentQuery.GetSubquery(currentQuery);
			context.CurrentSubqueryBinding.ShouldBeOnNewQuery = shouldBeOnNewQuery;
			context.currentQuery = currentQuery;
			return subquery.FlattenAsPossible().GetSqlQuery();
		}

		private static SqlWhereClause VisitWhere(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Where", 2, arguments.Count));
			}
			return SqlWhereClause.Create(VisitScalarExpression(Utilities.GetLambda(arguments[1]), context));
		}

		private static SqlSelectClause VisitSelect(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Select", 2, arguments.Count));
			}
			return SqlSelectClause.Create(SqlSelectValueSpec.Create(VisitScalarExpression(Utilities.GetLambda(arguments[1]), context)));
		}

		private static Collection VisitSelectMany(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "SelectMany", 2, arguments.Count));
			}
			LambdaExpression lambda = Utilities.GetLambda(arguments[1]);
			bool flag = false;
			for (MethodCallExpression methodCallExpression = lambda.Body as MethodCallExpression; methodCallExpression != null; methodCallExpression = (methodCallExpression.Arguments[0] as MethodCallExpression))
			{
				string name = methodCallExpression.Method.Name;
				flag |= (name.Equals("Distinct") || name.Equals("Take") || name.Equals("OrderBy") || name.Equals("OrderByDescending"));
			}
			Collection result;
			if (!flag)
			{
				result = VisitCollectionLambda(lambda, context);
			}
			else
			{
				result = new Collection("");
				SqlCollection collection = SqlSubqueryCollection.Create(CreateSubquery(lambda.Body, lambda.Parameters, context));
				FromParameterBindings.Binding binding = new FromParameterBindings.Binding(context.GenFreshParameter(typeof(object), DefaultParameterName), collection, isInCollection: false);
				context.currentQuery.fromParameters.Add(binding);
			}
			return result;
		}

		private static SqlOrderbyClause VisitOrderBy(ReadOnlyCollection<Expression> arguments, bool isDescending, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "OrderBy", 2, arguments.Count));
			}
			SqlOrderByItem sqlOrderByItem = SqlOrderByItem.Create(VisitScalarExpression(Utilities.GetLambda(arguments[1]), context), isDescending);
			return SqlOrderbyClause.Create(sqlOrderByItem);
		}

		private static bool TryGetTopSkipTakeLiteral(Expression expression, TranslationContext context, out SqlNumberLiteral literal)
		{
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			literal = null;
			SqlLiteralScalarExpression sqlLiteralScalarExpression = VisitScalarExpression(expression, context) as SqlLiteralScalarExpression;
			if (sqlLiteralScalarExpression != null && sqlLiteralScalarExpression.Literal.Kind == SqlObjectKind.NumberLiteral)
			{
				ParameterExpression parameter = context.GenFreshParameter(typeof(object), DefaultParameterName);
				context.PushParameter(parameter, context.CurrentSubqueryBinding.ShouldBeOnNewQuery);
				context.PopParameter();
				literal = (SqlNumberLiteral)sqlLiteralScalarExpression.Literal;
			}
			if (literal != null)
			{
				return literal.Value >= 0L;
			}
			return false;
		}

		private static SqlOffsetSpec VisitSkip(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Skip", 2, arguments.Count));
			}
			if (TryGetTopSkipTakeLiteral(arguments[1], context, out SqlNumberLiteral literal))
			{
				return SqlOffsetSpec.Create(Number64.ToLong(literal.Value));
			}
			throw new ArgumentException(ClientResources.InvalidSkipValue);
		}

		private static SqlLimitSpec VisitTakeLimit(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Take", 2, arguments.Count));
			}
			if (TryGetTopSkipTakeLiteral(arguments[1], context, out SqlNumberLiteral literal))
			{
				return SqlLimitSpec.Create(Number64.ToLong(literal.Value));
			}
			throw new ArgumentException(ClientResources.InvalidTakeValue);
		}

		private static SqlTopSpec VisitTakeTop(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			if (arguments.Count != 2)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Take", 2, arguments.Count));
			}
			if (TryGetTopSkipTakeLiteral(arguments[1], context, out SqlNumberLiteral literal))
			{
				return SqlTopSpec.Create(Number64.ToLong(literal.Value));
			}
			throw new ArgumentException(ClientResources.InvalidTakeValue);
		}

		private static SqlSelectClause VisitAggregateFunction(ReadOnlyCollection<Expression> arguments, TranslationContext context, string aggregateFunctionName)
		{
			SqlScalarExpression sqlScalarExpression;
			if (arguments.Count == 1)
			{
				ParameterExpression parameterExpression = context.GenFreshParameter(typeof(object), DefaultParameterName);
				context.PushParameter(parameterExpression, context.CurrentSubqueryBinding.ShouldBeOnNewQuery);
				sqlScalarExpression = VisitParameter(parameterExpression, context);
				context.PopParameter();
			}
			else
			{
				if (arguments.Count != 2)
				{
					throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, aggregateFunctionName, 2, arguments.Count));
				}
				sqlScalarExpression = VisitScalarExpression(Utilities.GetLambda(arguments[1]), context);
			}
			return SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlFunctionCallScalarExpression.CreateBuiltin(aggregateFunctionName, sqlScalarExpression)));
		}

		private static SqlSelectClause VisitDistinct(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			string arg = "Distinct";
			if (arguments.Count != 1)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, arg, 1, arguments.Count));
			}
			ParameterExpression parameterExpression = context.GenFreshParameter(typeof(object), DefaultParameterName);
			return SqlSelectClause.Create(SqlSelectValueSpec.Create(VisitNonSubqueryScalarLambda(Expression.Lambda(parameterExpression, parameterExpression), context)), null, hasDistinct: true);
		}

		private static SqlSelectClause VisitCount(ReadOnlyCollection<Expression> arguments, TranslationContext context)
		{
			SqlScalarExpression sqlScalarExpression = SqlLiteralScalarExpression.Create(SqlNumberLiteral.Create(1L));
			if (arguments.Count == 2)
			{
				SqlWhereClause whereClause = VisitWhere(arguments, context);
				context.currentQuery = context.currentQuery.AddWhereClause(whereClause, context);
			}
			else if (arguments.Count != 1)
			{
				throw new DocumentQueryException(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidArgumentsCount, "Count", 2, arguments.Count));
			}
			return SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlFunctionCallScalarExpression.CreateBuiltin("COUNT", sqlScalarExpression)));
		}

		/// <summary>
		/// Property references that refer to array-valued properties are converted to collection references.
		/// </summary>
		/// <param name="propRef">Property reference object.</param>
		/// <returns>An inputPathCollection which contains the same property path as the propRef.</returns>
		private static SqlInputPathCollection ConvertPropertyRefToPath(SqlPropertyRefScalarExpression propRef)
		{
			List<SqlIdentifier> list = new List<SqlIdentifier>();
			while (true)
			{
				list.Add(propRef.PropertyIdentifier);
				SqlScalarExpression memberExpression = propRef.MemberExpression;
				if (memberExpression == null)
				{
					break;
				}
				if (memberExpression is SqlPropertyRefScalarExpression)
				{
					propRef = (memberExpression as SqlPropertyRefScalarExpression);
					continue;
				}
				throw new DocumentQueryException(ClientResources.NotSupported);
			}
			if (list.Count == 0)
			{
				throw new DocumentQueryException(ClientResources.NotSupported);
			}
			SqlPathExpression sqlPathExpression = null;
			for (int num = list.Count - 2; num >= 0; num--)
			{
				SqlIdentifier value = list[num];
				sqlPathExpression = SqlIdentifierPathExpression.Create(sqlPathExpression, value);
			}
			return SqlInputPathCollection.Create(list[list.Count - 1], sqlPathExpression);
		}

		private static SqlInputPathCollection ConvertMemberIndexerToPath(SqlMemberIndexerScalarExpression memberIndexerExpression)
		{
			List<SqlStringLiteral> list = new List<SqlStringLiteral>();
			while (true)
			{
				list.Add((SqlStringLiteral)((SqlLiteralScalarExpression)memberIndexerExpression.IndexExpression).Literal);
				SqlScalarExpression memberExpression = memberIndexerExpression.MemberExpression;
				if (memberExpression == null)
				{
					break;
				}
				if (memberExpression is SqlPropertyRefScalarExpression)
				{
					list.Add(SqlStringLiteral.Create(((SqlPropertyRefScalarExpression)memberExpression).PropertyIdentifier.Value));
					break;
				}
				if (memberExpression is SqlMemberIndexerScalarExpression)
				{
					memberIndexerExpression = (memberExpression as SqlMemberIndexerScalarExpression);
					continue;
				}
				throw new DocumentQueryException(ClientResources.NotSupported);
			}
			if (list.Count == 0)
			{
				throw new ArgumentException("memberIndexerExpression");
			}
			SqlPathExpression sqlPathExpression = null;
			for (int num = list.Count - 2; num >= 0; num--)
			{
				sqlPathExpression = SqlStringPathExpression.Create(sqlPathExpression, list[num]);
			}
			return SqlInputPathCollection.Create(SqlIdentifier.Create(list[list.Count - 1].Value), sqlPathExpression);
		}
	}
}
