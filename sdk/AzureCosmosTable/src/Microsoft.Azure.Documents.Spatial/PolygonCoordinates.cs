using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Polygon coordinates.
	/// </summary>
	/// <seealso cref="T:Microsoft.Azure.Documents.Spatial.MultiPolygon" />
	[JsonConverter(typeof(PolygonCoordinatesJsonConverter))]
	internal sealed class PolygonCoordinates : IEquatable<PolygonCoordinates>
	{
		/// <summary>
		/// Gets polygon rings.
		/// </summary>
		/// <value>
		/// Rings of the polygon.
		/// </value>
		public ReadOnlyCollection<LinearRing> Rings
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> class.
		/// </summary>
		/// <param name="rings">
		/// The rings of the polygon.
		/// </param>
		public PolygonCoordinates(IList<LinearRing> rings)
		{
			if (rings == null)
			{
				throw new ArgumentException("rings");
			}
			Rings = new ReadOnlyCollection<LinearRing>(rings);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" />.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as PolygonCoordinates);
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" />. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Rings.Aggregate(0, (int current, LinearRing value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> is equal to the <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.PolygonCoordinates" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(PolygonCoordinates other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return Rings.SequenceEqual(other.Rings);
		}
	}
}
