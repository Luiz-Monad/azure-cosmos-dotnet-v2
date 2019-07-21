using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// <para>
	/// Polygon geometry class in the Azure Cosmos DB service.
	/// </para>
	/// <para>
	/// A polygon is represented by the set of "polygon rings". Each ring is closed line string.
	/// First ring defines external ring. All subsequent rings define "holes" in the external ring.
	/// </para>
	/// <para>
	/// Rings must be specified using Left Hand Rule: traversing the ring in the order of its points, should result
	/// in internal area of the polygon being to the left side.
	/// </para>
	/// </summary>
	/// <example>
	/// This example shows how to define a polygon which covers small portion of the Earth:
	/// <code language="c#">
	/// <![CDATA[
	/// var polygon = new Polygon(
	///         new[]
	///         {
	///             new Position(20.0, 20.0),
	///             new Position(30.0, 20.0),
	///             new Position(30.0, 30.0),
	///             new Position(20.0, 30.0)
	///             new Position(20.0, 20.0)
	///         });
	/// ]]>        
	/// </code>
	/// </example>
	/// <example>
	/// This example shows how to define a polygon which covers area more than one hemisphere:
	/// (Notice that only order of coordinates was reversed).
	/// <code language="c#">
	/// <![CDATA[
	/// var polygon = new Polygon(
	///         new[]
	///         {
	///             new Position(20.0, 20.0),
	///             new Position(20.0, 30.0),
	///             new Position(30.0, 30.0),
	///             new Position(30.0, 20.0)
	///             new Position(20.0, 20.0)
	///         });
	/// ]]>        
	/// </code>
	/// </example>
	public sealed class Polygon : Geometry, IEquatable<Polygon>
	{
		/// <summary>
		/// Gets the polygon rings in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Polygon rings.
		/// </value>
		[JsonProperty("coordinates", Required = Required.Always, Order = 1)]
		public ReadOnlyCollection<LinearRing> Rings
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> class,
		/// from external ring (the polygon contains no holes) in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="externalRingPositions">
		/// External polygon ring coordinates.
		/// </param>
		public Polygon(IList<Position> externalRingPositions)
			: this(new LinearRing[1]
			{
				new LinearRing(externalRingPositions)
			}, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="rings">
		/// <para>
		/// Polygon rings.
		/// </para>
		/// <para>
		/// First ring is external ring. Following rings define 'holes' in the polygon.
		/// </para>
		/// </param>
		public Polygon(IList<LinearRing> rings)
			: this(rings, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="rings">
		/// Polygon rings.
		/// </param>
		/// <param name="geometryParams">
		/// Additional geometry parameters.
		/// </param>
		public Polygon(IList<LinearRing> rings, GeometryParams geometryParams)
			: base(GeometryType.Polygon, geometryParams)
		{
			if (rings == null)
			{
				throw new ArgumentNullException("rings");
			}
			Rings = new ReadOnlyCollection<LinearRing>(rings);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal Polygon()
			: base(GeometryType.Polygon, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as Polygon);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> type in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Rings.Aggregate(base.GetHashCode(), (int current, LinearRing value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> is equal to the <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(Polygon other)
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
				return Rings.SequenceEqual(other.Rings);
			}
			return false;
		}
	}
}
