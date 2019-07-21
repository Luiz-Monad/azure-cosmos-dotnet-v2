using System;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Coordinate Reference System which is identified by name in the Azure Cosmos DB service.
	/// </summary>
	public sealed class NamedCrs : Crs, IEquatable<NamedCrs>
	{
		/// <summary>
		/// Gets a name identifying a coordinate reference system in the Azure Cosmos DB service. For example "urn:ogc:def:crs:OGC:1.3:CRS84".
		/// </summary>
		/// <value>
		/// Name identifying a coordinate reference system. For example "urn:ogc:def:crs:OGC:1.3:CRS84".
		/// </value>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.NamedCrs" /> class in the Azure Cosmos DB service. 
		/// </summary>
		/// <param name="name">
		/// Name identifying a coordinate reference system.
		/// </param>
		internal NamedCrs(string name)
			: base(CrsType.Named)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			Name = name;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.NamedCrs" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.NamedCrs" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as NamedCrs);
		}

		/// <summary>
		/// Serves as a hash function for the name identifying a coordinate reference system in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.NamedCrs" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		/// <summary>
		/// Determines if this CRS is equal to <paramref name="other" /> CRS in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other">CRS to compare to this CRS.</param>
		/// <returns><c>true</c> if CRSs are equal. <c>false</c> otherwise.</returns>
		public bool Equals(NamedCrs other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return string.Equals(Name, other.Name);
		}
	}
}
