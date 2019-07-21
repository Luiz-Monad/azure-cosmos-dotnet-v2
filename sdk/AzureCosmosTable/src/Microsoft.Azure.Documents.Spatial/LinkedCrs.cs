using System;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Coordinate Reference System which is identified by link in the Azure Cosmos DB service. 
	/// </summary>
	public sealed class LinkedCrs : Crs, IEquatable<LinkedCrs>
	{
		/// <summary>
		/// Gets the link which identifies the Coordinate Reference System in the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// Link which identifies the Coordinate Reference System.
		/// </value>
		public string Href
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets optional string which hints at the format used to represent CRS parameters at the provided <see cref="P:Microsoft.Azure.Documents.Spatial.LinkedCrs.Href" /> in the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// Optional string which hints at the format used to represent CRS parameters at the provided <see cref="P:Microsoft.Azure.Documents.Spatial.LinkedCrs.Href" />.
		/// </value>
		public string HrefType
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> class in the Azure Cosmos DB service. 
		/// </summary>
		/// <param name="href">
		/// Link which identifies the Coordinate Reference System.
		/// </param>
		/// <param name="hrefType">
		/// Optional string which hints at the format used to represent CRS parameters at the provided <paramref name="href" />.
		/// </param>
		internal LinkedCrs(string href, string hrefType = null)
			: base(CrsType.Linked)
		{
			if (href == null)
			{
				throw new ArgumentNullException("href");
			}
			Href = href;
			HrefType = hrefType;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> in the Azure Cosmos DB service. 
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as LinkedCrs);
		}

		/// <summary>
		/// Serves as a hash function for <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> in the Azure Cosmos DB service. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />.
		/// </returns>
		public override int GetHashCode()
		{
			return (((Href != null) ? Href.GetHashCode() : 0) * 397) ^ ((HrefType != null) ? HrefType.GetHashCode() : 0);
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> is equal to <paramref name="other" /> in the Azure Cosmos DB service. 
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />.</param>
		/// <returns><c>true</c> if CRSs are equal. <c>false</c> otherwise.</returns>
		public bool Equals(LinkedCrs other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (string.Equals(Href, other.Href))
			{
				return string.Equals(HrefType, other.HrefType);
			}
			return false;
		}
	}
}
