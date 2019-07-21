using Microsoft.Azure.Documents.Sql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Used by the Expression tree visitor.
	/// </summary>
	internal sealed class TranslationContext
	{
		public class SubqueryBinding
		{
			public static SubqueryBinding EmptySubqueryBinding = new SubqueryBinding(shouldBeOnNewQuery: false);

			/// <summary>
			/// Indicates if the current query should be on a new QueryUnderConstruction
			/// </summary>
			public bool ShouldBeOnNewQuery
			{
				get;
				set;
			}

			/// <summary>
			/// Indicates the new bindings that are introduced when visiting the subquery
			/// </summary>
			public List<FromParameterBindings.Binding> NewBindings
			{
				get;
				private set;
			}

			public SubqueryBinding(bool shouldBeOnNewQuery)
			{
				ShouldBeOnNewQuery = shouldBeOnNewQuery;
				NewBindings = new List<FromParameterBindings.Binding>();
			}

			/// <summary>
			/// Consume all the bindings
			/// </summary>
			/// <returns>All the current bindings</returns>
			/// <remarks>The binding list is reset after this operation.</remarks>
			public List<FromParameterBindings.Binding> TakeBindings()
			{
				List<FromParameterBindings.Binding> newBindings = NewBindings;
				NewBindings = new List<FromParameterBindings.Binding>();
				return newBindings;
			}
		}

		/// <summary>
		/// Set of parameters in scope at any point; used to generate fresh parameter names if necessary.
		/// </summary>
		public HashSet<ParameterExpression> InScope;

		/// <summary>
		/// Query that is being assembled.
		/// </summary>
		public QueryUnderConstruction currentQuery;

		/// <summary>
		/// If the FROM clause uses a parameter name, it will be substituted for the parameter used in 
		/// the lambda expressions for the WHERE and SELECT clauses.
		/// </summary>
		private ParameterSubstitution substitutions;

		/// <summary>
		/// We are currently visiting these methods.
		/// </summary>
		private List<MethodCallExpression> methodStack;

		/// <summary>
		/// Stack of parameters from lambdas currently in scope.
		/// </summary>
		private List<ParameterExpression> lambdaParametersStack;

		/// <summary>
		/// Stack of collection-valued inputs.
		/// </summary>
		private List<Collection> collectionStack;

		/// <summary>
		/// The stack of subquery binding information.
		/// </summary>
		private Stack<SubqueryBinding> subqueryBindingStack;

		public SubqueryBinding CurrentSubqueryBinding
		{
			get
			{
				if (subqueryBindingStack.Count == 0)
				{
					throw new InvalidOperationException("Unexpected empty subquery binding stack.");
				}
				return subqueryBindingStack.Peek();
			}
		}

		public TranslationContext()
		{
			InScope = new HashSet<ParameterExpression>();
			substitutions = new ParameterSubstitution();
			methodStack = new List<MethodCallExpression>();
			lambdaParametersStack = new List<ParameterExpression>();
			collectionStack = new List<Collection>();
			currentQuery = new QueryUnderConstruction(GetGenFreshParameterFunc());
			subqueryBindingStack = new Stack<SubqueryBinding>();
		}

		public Expression LookupSubstitution(ParameterExpression parameter)
		{
			return substitutions.Lookup(parameter);
		}

		public ParameterExpression GenFreshParameter(Type parameterType, string baseParameterName)
		{
			return Utilities.NewParameter(baseParameterName, parameterType, InScope);
		}

		public Func<string, ParameterExpression> GetGenFreshParameterFunc()
		{
			return (string paramName) => GenFreshParameter(typeof(object), paramName);
		}

		/// <summary>
		/// Called when visiting a lambda with one parameter.
		/// Binds this parameter with the last collection visited.
		/// </summary>
		/// <param name="parameter">New parameter.</param>
		/// <param name="shouldBeOnNewQuery">Indicate if the parameter should be in a new QueryUnderConstruction clause</param>
		public void PushParameter(ParameterExpression parameter, bool shouldBeOnNewQuery)
		{
			lambdaParametersStack.Add(parameter);
			Collection collection = collectionStack[collectionStack.Count - 1];
			if (collection.isOuter)
			{
				ParameterExpression inputParameterInContext = currentQuery.GetInputParameterInContext(shouldBeOnNewQuery);
				substitutions.AddSubstitution(parameter, inputParameterInContext);
			}
			else
			{
				currentQuery.Bind(parameter, collection.inner);
			}
		}

		/// <summary>
		/// Remove a parameter from the stack.
		/// </summary>
		public void PopParameter()
		{
			ParameterExpression parameterExpression = lambdaParametersStack[lambdaParametersStack.Count - 1];
			lambdaParametersStack.RemoveAt(lambdaParametersStack.Count - 1);
		}

		/// <summary>
		/// Called when visiting a new MethodCall.
		/// </summary>
		/// <param name="method">Method that is being visited.</param>
		public void PushMethod(MethodCallExpression method)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			methodStack.Add(method);
		}

		/// <summary>
		/// Called when finished visiting a MethodCall.
		/// </summary>
		public void PopMethod()
		{
			methodStack.RemoveAt(methodStack.Count - 1);
		}

		/// <summary>
		/// Return the top method in the method stack
		/// This is used only to determine the parameter name that the user provides in the lamda expression
		/// for readability purpose.
		/// </summary>
		/// <returns></returns>
		public MethodCallExpression PeekMethod()
		{
			if (methodStack.Count <= 0)
			{
				return null;
			}
			return methodStack[methodStack.Count - 1];
		}

		/// <summary>
		/// Called when visiting a LINQ Method call with the input collection of the method.
		/// </summary>
		/// <param name="collection">Collection that is the input to a LINQ method.</param>
		public void PushCollection(Collection collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			collectionStack.Add(collection);
		}

		public void PopCollection()
		{
			collectionStack.RemoveAt(collectionStack.Count - 1);
		}

		/// <summary>
		/// Sets the parameter used to scan the input.
		/// </summary>
		/// <param name="name">Suggested name for the input parameter.</param>
		/// <param name="type">Type of the input parameter.</param>
		public ParameterExpression SetInputParameter(Type type, string name)
		{
			return currentQuery.fromParameters.SetInputParameter(type, name, InScope);
		}

		/// <summary>
		/// Sets the parameter used by the this.fromClause if it is not already set.
		/// </summary>
		/// <param name="parameter">Parameter to set for the FROM clause.</param>
		/// <param name="collection">Collection to bind parameter to.</param>
		public void SetFromParameter(ParameterExpression parameter, SqlCollection collection)
		{
			FromParameterBindings.Binding binding = new FromParameterBindings.Binding(parameter, collection, isInCollection: true);
			currentQuery.fromParameters.Add(binding);
		}

		/// <summary>
		/// Gets whether the context is currently in a Select method at top level or not.
		/// Used to determine if a paramter should be an input parameter.
		/// </summary>
		/// <returns></returns>
		public bool IsInMainBranchSelect()
		{
			if (methodStack.Count == 0)
			{
				return false;
			}
			bool flag = true;
			string text = methodStack[0].ToString();
			for (int i = 1; i < methodStack.Count; i++)
			{
				string text2 = methodStack[i].ToString();
				if (!text.StartsWith(text2, StringComparison.Ordinal))
				{
					flag = false;
					break;
				}
				text = text2;
			}
			string name = methodStack[methodStack.Count - 1].Method.Name;
			if (flag)
			{
				if (!name.Equals("Select"))
				{
					return name.Equals("SelectMany");
				}
				return true;
			}
			return false;
		}

		public void PushSubqueryBinding(bool shouldBeOnNewQuery)
		{
			subqueryBindingStack.Push(new SubqueryBinding(shouldBeOnNewQuery));
		}

		public SubqueryBinding PopSubqueryBinding()
		{
			if (subqueryBindingStack.Count == 0)
			{
				throw new InvalidOperationException("Unexpected empty subquery binding stack.");
			}
			return subqueryBindingStack.Pop();
		}

		/// <summary>
		/// Create a new QueryUnderConstruction node if indicated as neccesary by the subquery binding 
		/// </summary>
		/// <returns>The current QueryUnderConstruction after the package query call if necessary</returns>
		public QueryUnderConstruction PackageCurrentQueryIfNeccessary()
		{
			if (CurrentSubqueryBinding.ShouldBeOnNewQuery)
			{
				currentQuery = currentQuery.PackageQuery(InScope);
				CurrentSubqueryBinding.ShouldBeOnNewQuery = false;
			}
			return currentQuery;
		}
	}
}
