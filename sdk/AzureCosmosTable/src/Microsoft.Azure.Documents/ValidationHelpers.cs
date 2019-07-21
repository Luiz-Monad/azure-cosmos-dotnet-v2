using System;

namespace Microsoft.Azure.Documents
{
	internal static class ValidationHelpers
	{
		public static bool ValidateConsistencyLevel(ConsistencyLevel backendConsistency, ConsistencyLevel desiredConsistency)
		{
			switch (backendConsistency)
			{
			case ConsistencyLevel.Strong:
				if (desiredConsistency != 0 && desiredConsistency != ConsistencyLevel.BoundedStaleness && desiredConsistency != ConsistencyLevel.Session && desiredConsistency != ConsistencyLevel.Eventual)
				{
					return desiredConsistency == ConsistencyLevel.ConsistentPrefix;
				}
				return true;
			case ConsistencyLevel.BoundedStaleness:
				if (desiredConsistency != ConsistencyLevel.BoundedStaleness && desiredConsistency != ConsistencyLevel.Session && desiredConsistency != ConsistencyLevel.Eventual)
				{
					return desiredConsistency == ConsistencyLevel.ConsistentPrefix;
				}
				return true;
			case ConsistencyLevel.Session:
			case ConsistencyLevel.Eventual:
			case ConsistencyLevel.ConsistentPrefix:
				if (desiredConsistency != ConsistencyLevel.Session && desiredConsistency != ConsistencyLevel.Eventual)
				{
					return desiredConsistency == ConsistencyLevel.ConsistentPrefix;
				}
				return true;
			default:
				throw new ArgumentException("backendConsistency");
			}
		}
	}
}
