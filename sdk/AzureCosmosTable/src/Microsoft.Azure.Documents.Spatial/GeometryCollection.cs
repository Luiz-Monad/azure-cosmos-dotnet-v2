using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Represents a geometry consisting of other geometries.
	/// </summary>
	internal sealed class GeometryCollection : Geometry, IEquatable<GeometryCollection>
	{
		/// <summary>
		/// Gets child geometries.
		/// </summary>
		/// <value>
		/// Child geometries.
		/// </value>
		[JsonProperty("geometries", Required = Required.Always, Order = 1)]
		public ReadOnlyCollection<Geometry> Geometries
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> class. 
		/// </summary>
		/// <param name="geometries">
		/// List of geometries.
		/// </param>
		public GeometryCollection(IList<Geometry> geometries)
			: this(geometries, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> class.
		/// </summary>
		/// <param name="geometries">
		/// Child geometries.
		/// </param>
		/// <param name="geometryParams">
		/// Additional geometry parameters.
		/// </param>
		public GeometryCollection(IList<Geometry> geometries, GeometryParams geometryParams)
			: base(GeometryType.GeometryCollection, geometryParams)
		{
			if (geometries == null)
			{
				throw new ArgumentNullException("geometries");
			}
			Geometries = new ReadOnlyCollection<Geometry>(geometries);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> class.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal GeometryCollection()
			: base(GeometryType.GeometryCollection, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" />.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as GeometryCollection);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Geometries.Aggregate(base.GetHashCode(), (int current, Geometry value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> is equal to the <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.GeometryCollection" />.</param>
		/// <returns><c>true</c> if geometry collections are equal. <c>false</c> otherwise.</returns>
		public bool Equals(GeometryCollection other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (base.Equals((object)other))
			{
				return Geometries.SequenceEqual(other.Geometries);
			}
			return false;
		}
	}
}
