using System;

namespace Microsoft.Azure.Documents.Partitioning
{
	[Obsolete("Support for classes used with IPartitionResolver is now obsolete.")]
	internal static class PartitionResolverUtils
	{
		public static object ExtractPartitionKeyFromDocument(object document, string propertyName)
		{
			try
			{
				if (document is Document)
				{
					return ((Document)document).GetPropertyValue<object>(propertyName);
				}
				return CustomTypeExtensions.GetProperty(document.GetType(), propertyName).GetValue(document, null);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException(ClientResources.PartitionPropertyNotFound, innerException);
			}
		}
	}
}
