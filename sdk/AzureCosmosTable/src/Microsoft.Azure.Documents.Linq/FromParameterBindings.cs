using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Bindings for a set of parameters used in a FROM expression.
	/// Each parameter is bound to a collection.
	/// </summary>
	internal sealed class FromParameterBindings
	{
		/// <summary>
		/// Binding for a single parameter.
		/// </summary>
		public sealed class Binding
		{
			/// <summary>
			/// Parameter defined by FROM clause
			/// </summary>
			public ParameterExpression Parameter;

			/// <summary>
			/// How parameter is defined (may be null).  
			/// </summary>
			public SqlCollection ParameterDefinition;

			/// <summary>
			/// If true this corresponds to the clause `Parameter IN ParameterDefinition'
			/// else this corresponds to the clause `ParameterDefinition Parameter'
			/// </summary>
			public bool IsInCollection;

			/// <summary>
			/// True if a binding should be an input paramter for the next transformation. 
			/// E.g. in Select(f -&gt; f.Children).Select(), if the lambda's translation is
			/// a subquery SELECT VALUE ARRAY() with alias v0 then v0 should be the input of the second Select.
			/// </summary>
			public bool IsInputParameter;

			public Binding(ParameterExpression parameter, SqlCollection collection, bool isInCollection, bool isInputParameter = true)
			{
				ParameterDefinition = collection;
				Parameter = parameter;
				IsInCollection = isInCollection;
				IsInputParameter = isInputParameter;
				if (isInCollection && collection == null)
				{
					throw new ArgumentNullException(string.Format("{0} cannot be null for in-collection parameter.", "collection"));
				}
			}
		}

		/// <summary>
		/// The list of parameter definitions.  This will generate a FROM clause of the shape:
		/// FROM ParameterDefinitions[0] JOIN ParameterDefinitions[1] ... ParameterDefinitions[n]
		/// </summary>
		private List<Binding> ParameterDefinitions;

		/// <summary>
		/// Create empty parameter bindings.
		/// </summary>
		public FromParameterBindings()
		{
			ParameterDefinitions = new List<Binding>();
		}

		/// <summary>
		/// Sets the parameter which iterates over the outer collection.
		/// </summary> 
		/// <param name="parameterName">Hint for name.</param>
		/// <param name="parameterType">Parameter type.</param>
		/// <param name="inScope">List of parameter names currently in scope.</param>
		/// <returns>The name of the parameter which iterates over the outer collection.  
		/// If the name is already set it will return the existing name.</returns>
		public ParameterExpression SetInputParameter(Type parameterType, string parameterName, HashSet<ParameterExpression> inScope)
		{
			if (ParameterDefinitions.Count > 0)
			{
				throw new InvalidOperationException("First parameter already set");
			}
			ParameterExpression parameterExpression = Expression.Parameter(parameterType, parameterName);
			inScope.Add(parameterExpression);
			Binding item = new Binding(parameterExpression, null, isInCollection: false);
			ParameterDefinitions.Add(item);
			return parameterExpression;
		}

		public void Add(Binding binding)
		{
			ParameterDefinitions.Add(binding);
		}

		public IEnumerable<Binding> GetBindings()
		{
			return ParameterDefinitions;
		}

		/// <summary>
		/// Get the input parameter.
		/// </summary>
		/// <returns>The input parameter.</returns>
		public ParameterExpression GetInputParameter()
		{
			int num = ParameterDefinitions.Count - 1;
			while (num > 0 && !ParameterDefinitions[num].IsInputParameter)
			{
				num--;
			}
			if (num < 0)
			{
				return null;
			}
			return ParameterDefinitions[num].Parameter;
		}
	}
}
