using System;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Unspecified CRS. If a geometry has this CRS, no CRS can be assumed for it according to GeoJSON spec.
	/// </summary>
	internal class UnspecifiedCrs : Crs, IEquatable<UnspecifiedCrs>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.UnspecifiedCrs" /> class.
		/// </summary>
		public UnspecifiedCrs()
			: base(CrsType.Unspecified)
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as UnspecifiedCrs);
		}

		/// <summary>
		/// Serves as a hash function for <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />.
		/// </returns>
		public override int GetHashCode()
		{
			return 0;
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.LinkedCrs" />.</param>
		/// <returns><c>true</c> if CRSs are equal. <c>false</c> otherwise.</returns>
		public bool Equals(UnspecifiedCrs other)
		{
			if (other == null)
			{
				return false;
			}
			return true;
		}
	}
}
