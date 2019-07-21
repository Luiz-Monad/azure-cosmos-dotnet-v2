using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Documents
{
	internal static class ResourceTypeExtensions
	{
		private static Dictionary<int, string> resourceTypeNames;

		static ResourceTypeExtensions()
		{
			resourceTypeNames = new Dictionary<int, string>();
			foreach (ResourceType value in Enum.GetValues(typeof(ResourceType)))
			{
				resourceTypeNames[(int)value] = value.ToString();
			}
		}

		public static string ToResourceTypeString(this ResourceType type)
		{
			return resourceTypeNames[(int)type];
		}

		/// <summary>
		/// Resources for which this method returns true, are spread between multiple
		/// partitions.
		/// </summary>
		public static bool IsPartitioned(this ResourceType type)
		{
			if (type != ResourceType.Document && type != ResourceType.Attachment)
			{
				return type == ResourceType.Conflict;
			}
			return true;
		}

		public static bool IsCollectionChild(this ResourceType type)
		{
			if (type != ResourceType.Document && type != ResourceType.Attachment && type != ResourceType.Conflict && type != ResourceType.Schema)
			{
				return type.IsScript();
			}
			return true;
		}

		public static bool IsScript(this ResourceType type)
		{
			if (type != ResourceType.UserDefinedFunction && type != ResourceType.Trigger)
			{
				return type == ResourceType.StoredProcedure;
			}
			return true;
		}
	}
}
