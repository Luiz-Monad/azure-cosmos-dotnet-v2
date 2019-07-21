using System;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary> 
	/// Represents an exception from the Azure Cosmos DB service queries.
	/// </summary>
	[Serializable]
	public sealed class DocumentQueryException : DocumentClientException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Linq.DocumentQueryException" /> class in the Azure Cosmos DB service.</summary>
		/// <param name="message">The exception message.</param>
		public DocumentQueryException(string message)
			: base(message, null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Linq.DocumentQueryException" /> class in the Azure Cosmos DB service.</summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The inner exception.</param>
		public DocumentQueryException(string message, Exception innerException)
			: base(message, innerException, null)
		{
		}
	}
}
