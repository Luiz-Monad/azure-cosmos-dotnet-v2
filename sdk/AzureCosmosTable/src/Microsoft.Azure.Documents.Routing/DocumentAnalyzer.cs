using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Microsoft.Azure.Documents.Routing
{
	internal class DocumentAnalyzer
	{
		/// <summary>
		/// Extracts effective <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" /> from deserialized document.
		/// </summary>
		/// <remarks>
		/// This code doesn't do any validation, as it assumes that IndexingPolicy is valid, as it is coming from the backend.
		/// Expected format is "/prop1/prop2/?". No array expressions are expected.
		/// </remarks>
		/// <param name="document">Deserialized document to extract partition key value from.</param>
		/// <param name="partitionKeyDefinition">Information about partition key.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" />.</returns>
		public static PartitionKeyInternal ExtractPartitionKeyValue(Document document, PartitionKeyDefinition partitionKeyDefinition)
		{
			if (partitionKeyDefinition == null || partitionKeyDefinition.Paths.Count == 0)
			{
				return PartitionKeyInternal.Empty;
			}
			if (CustomTypeExtensions.IsSubclassOf(document.GetType(), typeof(Document)))
			{
				return ExtractPartitionKeyValue(document, partitionKeyDefinition, (Document doc) => JToken.FromObject(doc));
			}
			return PartitionKeyInternal.FromObjectArray(partitionKeyDefinition.Paths.Select(delegate(string path)
			{
				string[] pathParts = PathParser.GetPathParts(path);
				return document.GetValueByPath(pathParts, (object)Undefined.Value);
			}).ToArray(), strict: false);
		}

		/// <summary>
		/// Extracts effective <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" /> from serialized document.
		/// </summary>
		/// <remarks>
		/// This code doesn't do any validation, as it assumes that IndexingPolicy is valid, as it is coming from the backend.
		/// Expected format is "/prop1/prop2/?". No array expressions are expected.
		/// </remarks>
		/// <param name="documentString">Serialized document to extract partition key value from.</param>
		/// <param name="partitionKeyDefinition">Information about partition key.</param>
		/// <returns>Instance of <see cref="T:Microsoft.Azure.Documents.Routing.PartitionKeyInternal" />.</returns>
		public static PartitionKeyInternal ExtractPartitionKeyValue(string documentString, PartitionKeyDefinition partitionKeyDefinition)
		{
			if (partitionKeyDefinition == null || partitionKeyDefinition.Paths.Count == 0)
			{
				return PartitionKeyInternal.Empty;
			}
			return ExtractPartitionKeyValue(documentString, partitionKeyDefinition, (string docString) => JToken.Parse(docString));
		}

		internal static PartitionKeyInternal ExtractPartitionKeyValue<T>(T data, PartitionKeyDefinition partitionKeyDefinition, Func<T, JToken> convertToJToken)
		{
			return PartitionKeyInternal.FromObjectArray(partitionKeyDefinition.Paths.Select(delegate(string path)
			{
				string[] pathParts = PathParser.GetPathParts(path);
				JToken jToken = convertToJToken(data);
				string[] array = pathParts;
				foreach (string key in array)
				{
					if (jToken == null)
					{
						break;
					}
					jToken = jToken[key];
				}
				if (jToken == null)
				{
					return Undefined.Value;
				}
				return jToken.ToObject<object>();
			}).ToArray(), strict: false);
		}
	}
}
