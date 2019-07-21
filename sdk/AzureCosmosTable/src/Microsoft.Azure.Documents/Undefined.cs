using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Instance of the class can be supplied as part of Partition Key Value to denote a value which is absent in the Azure Cosmos DB document.
	/// </summary>
	public sealed class Undefined : IEquatable<Undefined>
	{
		/// <summary>
		/// <see cref="T:Microsoft.Azure.Documents.Undefined" /> singleton to help reuse the object.
		/// </summary>
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Undefined is infact immutable")]
		public static readonly Undefined Value = new Undefined();

		private Undefined()
		{
		}

		/// <summary>
		/// Determines whether <paramref name="other" /> is <see cref="T:Microsoft.Azure.Documents.Undefined" /> from the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="other">The object to compare with the current object. </param>
		public bool Equals(Undefined other)
		{
			return other != null;
		}

		/// <summary>
		/// Determines whether other is <see cref="T:Microsoft.Azure.Documents.Undefined" /> from the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="other">The object to compare with the current object. </param>
		public override bool Equals(object other)
		{
			return Equals(other as Undefined);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Undefined" /> Azure Cosmos DB database type.
		/// </summary>
		/// <returns>
		/// A hash code value.
		/// </returns>
		public override int GetHashCode()
		{
			return 0;
		}
	}
}
