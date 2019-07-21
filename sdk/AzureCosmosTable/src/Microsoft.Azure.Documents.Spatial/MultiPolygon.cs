using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Geometry which is comprised of multiple polygons.
	/// </summary>
	/// <seealso cref="T:Microsoft.Azure.Documents.Spatial.Polygon" />
	internal sealed class MultiPolygon : Geometry, IEquatable<MultiPolygon>
	{
		/// <summary>
		/// Gets collection of <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> instances. Each <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> represents separate polygon.
		/// </summary>
		/// <value>
		/// Collection of <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> instances. Each <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> represents separate polygon.
		/// </value>
		[JsonProperty("coordinates", Required = Required.Always, Order = 1)]
		public ReadOnlyCollection<PolygonCoordinates> Polygons
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> class.
		/// </summary>
		/// <param name="polygons">
		/// List of <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> instances. Each <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> represents separate polygon.
		/// </param>
		public MultiPolygon(IList<PolygonCoordinates> polygons)
			: this(polygons, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> class.
		/// </summary>
		/// <param name="polygons">
		/// List of <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> instances. Each <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> represents separate polygon.
		/// </param>
		/// <param name="geometryParams">Additional geometry parameters.</param>
		public MultiPolygon(IList<PolygonCoordinates> polygons, GeometryParams geometryParams)
			: base(GeometryType.MultiPolygon, geometryParams)
		{
			if (polygons == null)
			{
				throw new ArgumentNullException("polygons");
			}
			Polygons = new ReadOnlyCollection<PolygonCoordinates>(polygons);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> class.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal MultiPolygon()
			: base(GeometryType.MultiPolygon, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" />.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as MultiPolygon);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Polygons.Aggregate(base.GetHashCode(), (int current, PolygonCoordinates value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(MultiPolygon other)
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
				return Polygons.SequenceEqual(other.Polygons);
			}
			return false;
		}
	}
}
