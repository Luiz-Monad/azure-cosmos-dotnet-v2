using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Query that is being constructed.
	/// </summary>
	internal sealed class QueryUnderConstruction
	{
		private readonly Func<string, ParameterExpression> aliasCreatorFunc;

		public const string DefaultSubqueryRoot = "r";

		private SqlSelectClause selectClause;

		private SqlWhereClause whereClause;

		private SqlOrderbyClause orderByClause;

		private SqlTopSpec topSpec;

		private SqlOffsetSpec offsetSpec;

		private SqlLimitSpec limitSpec;

		private Lazy<ParameterExpression> alias;

		/// <summary>
		/// Input subquery.
		/// </summary>
		private QueryUnderConstruction inputQuery;

		/// <summary>
		/// Binding for the FROM parameters.
		/// </summary>
		public FromParameterBindings fromParameters
		{
			get;
			set;
		}

		/// <summary>
		/// The parameter expression to be used as this query's alias.
		/// </summary>
		public ParameterExpression Alias => alias.Value;

		public QueryUnderConstruction(Func<string, ParameterExpression> aliasCreatorFunc)
			: this(aliasCreatorFunc, null)
		{
		}

		public QueryUnderConstruction(Func<string, ParameterExpression> aliasCreatorFunc, QueryUnderConstruction inputQuery)
		{
			fromParameters = new FromParameterBindings();
			this.aliasCreatorFunc = aliasCreatorFunc;
			this.inputQuery = inputQuery;
			alias = new Lazy<ParameterExpression>(() => aliasCreatorFunc("r"));
		}

		public void Bind(ParameterExpression parameter, SqlCollection collection)
		{
			AddBinding(new FromParameterBindings.Binding(parameter, collection, isInCollection: true));
		}

		public void AddBinding(FromParameterBindings.Binding binding)
		{
			fromParameters.Add(binding);
		}

		public ParameterExpression GetInputParameterInContext(bool isInNewQuery)
		{
			if (!isInNewQuery)
			{
				return fromParameters.GetInputParameter();
			}
			return Alias;
		}

		/// <summary>
		/// Create a FROM clause from a set of FROM parameter bindings.
		/// </summary>
		/// <returns>The created FROM clause.</returns>
		private SqlFromClause CreateFrom(SqlCollectionExpression inputCollectionExpression)
		{
			bool flag = true;
			foreach (FromParameterBindings.Binding binding in fromParameters.GetBindings())
			{
				if (flag)
				{
					flag = false;
					if (inputCollectionExpression != null)
					{
						continue;
					}
				}
				ParameterExpression parameter = binding.Parameter;
				SqlCollection parameterDefinition = binding.ParameterDefinition;
				SqlIdentifier sqlIdentifier = SqlIdentifier.Create(parameter.Name);
				SqlCollectionExpression sqlCollectionExpression;
				if (!binding.IsInCollection)
				{
					SqlCollection collection = parameterDefinition ?? SqlInputPathCollection.Create(sqlIdentifier, null);
					SqlIdentifier sqlIdentifier2 = (parameterDefinition == null) ? null : sqlIdentifier;
					sqlCollectionExpression = SqlAliasedCollectionExpression.Create(collection, sqlIdentifier2);
				}
				else
				{
					sqlCollectionExpression = SqlArrayIteratorCollectionExpression.Create(sqlIdentifier, parameterDefinition);
				}
				inputCollectionExpression = ((inputCollectionExpression == null) ? sqlCollectionExpression : SqlJoinCollectionExpression.Create(inputCollectionExpression, sqlCollectionExpression));
			}
			return SqlFromClause.Create(inputCollectionExpression);
		}

		private SqlFromClause CreateSubqueryFromClause()
		{
			SqlSubqueryCollection collection = SqlSubqueryCollection.Create(inputQuery.GetSqlQuery());
			SqlIdentifier sqlIdentifier = SqlIdentifier.Create(inputQuery.Alias.Name);
			SqlAliasedCollectionExpression inputCollectionExpression = SqlAliasedCollectionExpression.Create(collection, sqlIdentifier);
			return CreateFrom(inputCollectionExpression);
		}

		/// <summary>
		/// Convert the entire query to a SQL Query.
		/// </summary>
		/// <returns>The corresponding SQL Query.</returns>
		public SqlQuery GetSqlQuery()
		{
			SqlFromClause fromClause = (inputQuery == null) ? CreateFrom(null) : CreateSubqueryFromClause();
			SqlSelectClause sqlSelectClause = selectClause;
			if (sqlSelectClause == null)
			{
				string name = fromParameters.GetInputParameter().Name;
				SqlScalarExpression expression = SqlPropertyRefScalarExpression.Create(null, SqlIdentifier.Create(name));
				sqlSelectClause = (selectClause = SqlSelectClause.Create(SqlSelectValueSpec.Create(expression)));
			}
			sqlSelectClause = SqlSelectClause.Create(sqlSelectClause.SelectSpec, sqlSelectClause.TopSpec ?? topSpec, sqlSelectClause.HasDistinct);
			SqlOffsetLimitClause sqlOffsetLimitClause = (offsetSpec != null) ? SqlOffsetLimitClause.Create(offsetSpec, limitSpec ?? SqlLimitSpec.Create(2147483647L)) : (sqlOffsetLimitClause = null);
			return SqlQuery.Create(sqlSelectClause, fromClause, whereClause, null, orderByClause, sqlOffsetLimitClause);
		}

		/// <summary>
		/// Create a new QueryUnderConstruction node that take the current query as its input
		/// </summary>
		/// <param name="inScope">The current context's parameters scope</param>
		/// <returns>The new query node</returns>
		public QueryUnderConstruction PackageQuery(HashSet<ParameterExpression> inScope)
		{
			QueryUnderConstruction queryUnderConstruction = new QueryUnderConstruction(aliasCreatorFunc);
			queryUnderConstruction.fromParameters.SetInputParameter(typeof(object), Alias.Name, inScope);
			queryUnderConstruction.inputQuery = this;
			return queryUnderConstruction;
		}

		/// <summary>
		/// Find and flatten the prefix set of queries into a single query by substituting their expressions.
		/// </summary>
		/// <returns>The query that has been flatten</returns>
		public QueryUnderConstruction FlattenAsPossible()
		{
			QueryUnderConstruction queryUnderConstruction = null;
			QueryUnderConstruction queryUnderConstruction2 = null;
			bool flag = false;
			bool flag2 = false;
			for (QueryUnderConstruction queryUnderConstruction3 = this; queryUnderConstruction3 != null; queryUnderConstruction3 = queryUnderConstruction3.inputQuery)
			{
				foreach (FromParameterBindings.Binding binding in queryUnderConstruction3.fromParameters.GetBindings())
				{
					if (binding.ParameterDefinition != null && binding.ParameterDefinition is SqlSubqueryCollection)
					{
						queryUnderConstruction2 = this;
						break;
					}
				}
				if (queryUnderConstruction3.inputQuery != null && queryUnderConstruction3.fromParameters.GetBindings().First().Parameter.Name == queryUnderConstruction3.inputQuery.Alias.Name && queryUnderConstruction3.fromParameters.GetBindings().Any((FromParameterBindings.Binding b) => b.ParameterDefinition != null))
				{
					queryUnderConstruction2 = this;
					break;
				}
				if (queryUnderConstruction2 != null)
				{
					break;
				}
				if (((queryUnderConstruction3.topSpec != null || queryUnderConstruction3.offsetSpec != null || queryUnderConstruction3.limitSpec != null) & flag2) || ((queryUnderConstruction3.selectClause != null && queryUnderConstruction3.selectClause.HasDistinct) & flag))
				{
					queryUnderConstruction.inputQuery = queryUnderConstruction3.FlattenAsPossible();
					queryUnderConstruction2 = this;
					break;
				}
				flag = (flag || (queryUnderConstruction3.selectClause != null && !queryUnderConstruction3.selectClause.HasDistinct));
				flag2 |= (queryUnderConstruction3.whereClause != null || queryUnderConstruction3.orderByClause != null || queryUnderConstruction3.topSpec != null || queryUnderConstruction3.offsetSpec != null || queryUnderConstruction3.fromParameters.GetBindings().Any((FromParameterBindings.Binding b) => b.ParameterDefinition != null) || (queryUnderConstruction3.selectClause != null && (queryUnderConstruction3.selectClause.HasDistinct || HasSelectAggregate())));
				queryUnderConstruction = queryUnderConstruction3;
			}
			if (queryUnderConstruction2 == null)
			{
				queryUnderConstruction2 = Flatten();
			}
			return queryUnderConstruction2;
		}

		/// <summary>
		/// Flatten subqueries into a single query by substituting their expressions in the current query.
		/// </summary>
		/// <returns>A flattened query.</returns>
		private QueryUnderConstruction Flatten()
		{
			if (inputQuery == null)
			{
				if (selectClause == null)
				{
					string name = fromParameters.GetInputParameter().Name;
					SqlScalarExpression expression = SqlPropertyRefScalarExpression.Create(null, SqlIdentifier.Create(name));
					selectClause = SqlSelectClause.Create(SqlSelectValueSpec.Create(expression));
				}
				else
				{
					selectClause = SqlSelectClause.Create(selectClause.SelectSpec, topSpec, selectClause.HasDistinct);
				}
				return this;
			}
			QueryUnderConstruction queryUnderConstruction = inputQuery.Flatten();
			SqlSelectClause sqlSelectClause = queryUnderConstruction.selectClause;
			SqlWhereClause first = queryUnderConstruction.whereClause;
			string value = null;
			HashSet<string> hashSet = new HashSet<string>();
			foreach (FromParameterBindings.Binding binding in inputQuery.fromParameters.GetBindings())
			{
				hashSet.Add(binding.Parameter.Name);
			}
			foreach (FromParameterBindings.Binding binding2 in fromParameters.GetBindings())
			{
				if (binding2.ParameterDefinition == null || hashSet.Contains(binding2.Parameter.Name))
				{
					value = binding2.Parameter.Name;
				}
			}
			SqlIdentifier inputParam = SqlIdentifier.Create(value);
			SqlSelectClause sqlSelectClause2 = Substitute(sqlSelectClause, sqlSelectClause.TopSpec ?? topSpec, inputParam, selectClause);
			SqlWhereClause second = Substitute(sqlSelectClause.SelectSpec, inputParam, whereClause);
			SqlOrderbyClause sqlOrderbyClause = Substitute(sqlSelectClause.SelectSpec, inputParam, orderByClause);
			SqlWhereClause sqlWhereClause = CombineWithConjunction(first, second);
			CombineInputParameters(queryUnderConstruction.fromParameters, fromParameters);
			SqlOffsetSpec sqlOffsetSpec;
			SqlLimitSpec sqlLimitSpec;
			if (queryUnderConstruction.offsetSpec != null)
			{
				sqlOffsetSpec = queryUnderConstruction.offsetSpec;
				sqlLimitSpec = queryUnderConstruction.limitSpec;
			}
			else
			{
				sqlOffsetSpec = offsetSpec;
				sqlLimitSpec = limitSpec;
			}
			return new QueryUnderConstruction(aliasCreatorFunc)
			{
				selectClause = sqlSelectClause2,
				whereClause = sqlWhereClause,
				inputQuery = null,
				fromParameters = queryUnderConstruction.fromParameters,
				orderByClause = (sqlOrderbyClause ?? inputQuery.orderByClause),
				offsetSpec = sqlOffsetSpec,
				limitSpec = sqlLimitSpec,
				alias = new Lazy<ParameterExpression>(() => Alias)
			};
		}

		private SqlSelectClause Substitute(SqlSelectClause inputSelectClause, SqlTopSpec topSpec, SqlIdentifier inputParam, SqlSelectClause selectClause)
		{
			SqlSelectSpec selectSpec = inputSelectClause.SelectSpec;
			if (selectClause == null)
			{
				if (selectSpec == null)
				{
					return null;
				}
				return SqlSelectClause.Create(selectSpec, topSpec, inputSelectClause.HasDistinct);
			}
			if (selectSpec is SqlSelectStarSpec)
			{
				return SqlSelectClause.Create(selectSpec, topSpec, inputSelectClause.HasDistinct);
			}
			SqlSelectValueSpec sqlSelectValueSpec = selectSpec as SqlSelectValueSpec;
			if (sqlSelectValueSpec != null)
			{
				SqlSelectSpec selectSpec2 = selectClause.SelectSpec;
				if (selectSpec2 is SqlSelectStarSpec)
				{
					return SqlSelectClause.Create(selectSpec, topSpec, selectClause.HasDistinct || inputSelectClause.HasDistinct);
				}
				SqlSelectValueSpec sqlSelectValueSpec2 = selectSpec2 as SqlSelectValueSpec;
				if (sqlSelectValueSpec2 != null)
				{
					return SqlSelectClause.Create(SqlSelectValueSpec.Create(SqlExpressionManipulation.Substitute(sqlSelectValueSpec.Expression, inputParam, sqlSelectValueSpec2.Expression)), topSpec, selectClause.HasDistinct || inputSelectClause.HasDistinct);
				}
				throw new DocumentQueryException("Unexpected SQL select clause type: " + selectSpec2.Kind);
			}
			throw new DocumentQueryException("Unexpected SQL select clause type: " + selectSpec.Kind);
		}

		private SqlWhereClause Substitute(SqlSelectSpec spec, SqlIdentifier inputParam, SqlWhereClause whereClause)
		{
			if (whereClause == null)
			{
				return null;
			}
			if (spec is SqlSelectStarSpec)
			{
				return whereClause;
			}
			SqlSelectValueSpec sqlSelectValueSpec = spec as SqlSelectValueSpec;
			if (sqlSelectValueSpec != null)
			{
				SqlScalarExpression expression = sqlSelectValueSpec.Expression;
				SqlScalarExpression filterExpression = whereClause.FilterExpression;
				return SqlWhereClause.Create(SqlExpressionManipulation.Substitute(expression, inputParam, filterExpression));
			}
			throw new DocumentQueryException("Unexpected SQL select clause type: " + spec.Kind);
		}

		private SqlOrderbyClause Substitute(SqlSelectSpec spec, SqlIdentifier inputParam, SqlOrderbyClause orderByClause)
		{
			if (orderByClause == null)
			{
				return null;
			}
			if (spec is SqlSelectStarSpec)
			{
				return orderByClause;
			}
			SqlSelectValueSpec sqlSelectValueSpec = spec as SqlSelectValueSpec;
			if (sqlSelectValueSpec != null)
			{
				SqlScalarExpression expression = sqlSelectValueSpec.Expression;
				SqlOrderByItem[] array = new SqlOrderByItem[orderByClause.OrderbyItems.Count];
				for (int i = 0; i < array.Length; i++)
				{
					SqlScalarExpression expression2 = SqlExpressionManipulation.Substitute(expression, inputParam, orderByClause.OrderbyItems[i].Expression);
					array[i] = SqlOrderByItem.Create(expression2, orderByClause.OrderbyItems[i].IsDescending);
				}
				return SqlOrderbyClause.Create(array);
			}
			throw new DocumentQueryException("Unexpected SQL select clause type: " + spec.Kind);
		}

		/// <summary>
		/// Determine if the current method call should create a new QueryUnderConstruction node or not.
		/// </summary>
		/// <param name="methodName">The current method name</param>
		/// <param name="argumentCount">The method's parameter count</param>
		/// <returns>True if the current method should be in a new query node</returns>
		public bool ShouldBeOnNewQuery(string methodName, int argumentCount)
		{
			bool result = false;
			switch (methodName)
			{
			case "Select":
				result = (selectClause != null);
				break;
			case "Min":
			case "Max":
			case "Sum":
			case "Average":
				result = (selectClause != null || offsetSpec != null || topSpec != null);
				break;
			case "Count":
				result = ((argumentCount == 2 && ShouldBeOnNewQuery("Where", 2)) || ShouldBeOnNewQuery("Sum", 1));
				break;
			case "Where":
			case "Any":
			case "OrderBy":
			case "OrderByDescending":
			case "Distinct":
				result = (topSpec != null || offsetSpec != null || (selectClause != null && !selectClause.HasDistinct));
				break;
			case "Skip":
				result = (topSpec != null || limitSpec != null);
				break;
			case "SelectMany":
				result = (topSpec != null || offsetSpec != null || selectClause != null);
				break;
			}
			return result;
		}

		/// <summary>
		/// Add a Select clause to a query, without creating a new subquery
		/// </summary>
		/// <param name="select">The Select clause to add</param>
		/// <returns>A new query containing a select clause.</returns>
		public QueryUnderConstruction AddSelectClause(SqlSelectClause select)
		{
			if ((selectClause == null || !selectClause.HasDistinct || !selectClause.HasDistinct) && selectClause != null)
			{
				throw new DocumentQueryException("Internal error: attempting to overwrite SELECT clause");
			}
			selectClause = select;
			return this;
		}

		/// <summary>
		/// Add a Select clause to a query; may need to create a new subquery.
		/// </summary>
		/// <param name="select">Select clause to add.</param>
		/// <param name="context">The translation context.</param>
		/// <returns>A new query containing a select clause.</returns>
		public QueryUnderConstruction AddSelectClause(SqlSelectClause select, TranslationContext context)
		{
			QueryUnderConstruction queryUnderConstruction = context.PackageCurrentQueryIfNeccessary();
			if ((queryUnderConstruction.selectClause == null || !queryUnderConstruction.selectClause.HasDistinct || !selectClause.HasDistinct) && queryUnderConstruction.selectClause != null)
			{
				throw new DocumentQueryException("Internal error: attempting to overwrite SELECT clause");
			}
			queryUnderConstruction.selectClause = select;
			foreach (FromParameterBindings.Binding item in context.CurrentSubqueryBinding.TakeBindings())
			{
				queryUnderConstruction.AddBinding(item);
			}
			return queryUnderConstruction;
		}

		public QueryUnderConstruction AddOrderByClause(SqlOrderbyClause orderBy, TranslationContext context)
		{
			QueryUnderConstruction queryUnderConstruction = context.PackageCurrentQueryIfNeccessary();
			queryUnderConstruction.orderByClause = orderBy;
			foreach (FromParameterBindings.Binding item in context.CurrentSubqueryBinding.TakeBindings())
			{
				queryUnderConstruction.AddBinding(item);
			}
			return queryUnderConstruction;
		}

		public QueryUnderConstruction AddOffsetSpec(SqlOffsetSpec offsetSpec, TranslationContext context)
		{
			QueryUnderConstruction queryUnderConstruction = context.PackageCurrentQueryIfNeccessary();
			if (queryUnderConstruction.offsetSpec != null)
			{
				queryUnderConstruction.offsetSpec = SqlOffsetSpec.Create(queryUnderConstruction.offsetSpec.Offset + offsetSpec.Offset);
			}
			else
			{
				queryUnderConstruction.offsetSpec = offsetSpec;
			}
			return queryUnderConstruction;
		}

		public QueryUnderConstruction AddLimitSpec(SqlLimitSpec limitSpec, TranslationContext context)
		{
			if (this.limitSpec != null)
			{
				this.limitSpec = ((this.limitSpec.Limit < limitSpec.Limit) ? this.limitSpec : limitSpec);
			}
			else
			{
				this.limitSpec = limitSpec;
			}
			return this;
		}

		public QueryUnderConstruction AddTopSpec(SqlTopSpec topSpec)
		{
			if (this.topSpec != null)
			{
				this.topSpec = ((this.topSpec.Count < topSpec.Count) ? this.topSpec : topSpec);
			}
			else
			{
				this.topSpec = topSpec;
			}
			return this;
		}

		private static SqlWhereClause CombineWithConjunction(SqlWhereClause first, SqlWhereClause second)
		{
			if (first == null)
			{
				return second;
			}
			if (second == null)
			{
				return first;
			}
			SqlScalarExpression filterExpression = first.FilterExpression;
			SqlScalarExpression filterExpression2 = second.FilterExpression;
			return SqlWhereClause.Create(SqlBinaryScalarExpression.Create(SqlBinaryScalarOperatorKind.And, filterExpression, filterExpression2));
		}

		private static FromParameterBindings CombineInputParameters(FromParameterBindings inputQueryParams, FromParameterBindings currentQueryParams)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (FromParameterBindings.Binding binding in inputQueryParams.GetBindings())
			{
				hashSet.Add(binding.Parameter.Name);
			}
			foreach (FromParameterBindings.Binding binding2 in currentQueryParams.GetBindings())
			{
				if (binding2.ParameterDefinition != null && !hashSet.Contains(binding2.Parameter.Name))
				{
					inputQueryParams.Add(binding2);
					hashSet.Add(binding2.Parameter.Name);
				}
			}
			return inputQueryParams;
		}

		/// <summary>
		/// Add a Where clause to a query; may need to create a new query.
		/// </summary>
		/// <param name="whereClause">Clause to add.</param>
		/// <param name="context">The translation context.</param>
		/// <returns>A new query containing the specified Where clause.</returns>
		public QueryUnderConstruction AddWhereClause(SqlWhereClause whereClause, TranslationContext context)
		{
			QueryUnderConstruction queryUnderConstruction = context.PackageCurrentQueryIfNeccessary();
			whereClause = CombineWithConjunction(queryUnderConstruction.whereClause, whereClause);
			queryUnderConstruction.whereClause = whereClause;
			foreach (FromParameterBindings.Binding item in context.CurrentSubqueryBinding.TakeBindings())
			{
				queryUnderConstruction.AddBinding(item);
			}
			return queryUnderConstruction;
		}

		/// <summary>
		/// Separate out the query branch, which makes up a subquery and is built on top of the parent query chain.
		/// E.g. Let the query chain at some point in time be q0 - q1 - q2. When a subquery is recognized, its expression is visited.
		/// Assume that adds 2 queries to the chain to q0 - q1 - q2 - q3 - q4. Invoking q4.GetSubquery(q2) would return q3 - q4
		/// after it's isolated from the rest of the chain.
		/// </summary>
		/// <param name="queryBeforeVisit">The last query in the chain before the collection expression is visited</param>
		/// <returns>The subquery that has been decoupled from the parent query chain</returns>
		public QueryUnderConstruction GetSubquery(QueryUnderConstruction queryBeforeVisit)
		{
			QueryUnderConstruction queryUnderConstruction = null;
			for (QueryUnderConstruction queryUnderConstruction2 = this; queryUnderConstruction2 != queryBeforeVisit; queryUnderConstruction2 = queryUnderConstruction2.inputQuery)
			{
				queryUnderConstruction = queryUnderConstruction2;
			}
			queryUnderConstruction.inputQuery = null;
			return this;
		}

		public bool HasOffsetSpec()
		{
			return offsetSpec != null;
		}

		/// <summary>
		/// Check whether the current SELECT clause has an aggregate function
		/// </summary>
		/// <returns>true if the selectClause has an aggregate function call</returns>
		private bool HasSelectAggregate()
		{
			string text = ((selectClause?.SelectSpec as SqlSelectValueSpec)?.Expression as SqlFunctionCallScalarExpression)?.Name.Value;
			switch (text)
			{
			default:
				return text == "SUM";
			case "MAX":
			case "MIN":
			case "AVG":
			case "COUNT":
				return true;
			case null:
				return false;
			}
		}

		/// <summary>
		/// Debugging string.
		/// </summary>
		/// <returns>Query representation as a string (not legal SQL).</returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (inputQuery != null)
			{
				stringBuilder.Append(inputQuery);
			}
			if (whereClause != null)
			{
				stringBuilder.Append("->");
				stringBuilder.Append(whereClause);
			}
			if (selectClause != null)
			{
				stringBuilder.Append("->");
				stringBuilder.Append(selectClause);
			}
			return stringBuilder.ToString();
		}
	}
}
