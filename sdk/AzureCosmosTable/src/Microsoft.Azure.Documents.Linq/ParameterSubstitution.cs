using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Maintains a map from parameters to expressions.
	/// </summary>
	internal sealed class ParameterSubstitution
	{
		private Dictionary<ParameterExpression, Expression> substitutionTable;

		public const string InputParameterName = "root";

		public ParameterSubstitution()
		{
			substitutionTable = new Dictionary<ParameterExpression, Expression>();
		}

		public void AddSubstitution(ParameterExpression parameter, Expression with)
		{
			if (parameter == with)
			{
				throw new InvalidOperationException("Substitution with self attempted");
			}
			substitutionTable.Add(parameter, with);
		}

		public Expression Lookup(ParameterExpression parameter)
		{
			if (substitutionTable.ContainsKey(parameter))
			{
				return substitutionTable[parameter];
			}
			return null;
		}
	}
}
