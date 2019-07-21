using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Represents a geometry consisting of connected line segments.
	/// </summary>
	public sealed class LineString : Geometry, IEquatable<LineString>
	{
		/// <summary>
		/// Gets line string positions.
		/// </summary>
		/// <value>
		/// Positions of the line string.
		/// </value>
		[JsonProperty("coordinates", Required = Required.Always, Order = 1)]
		public ReadOnlyCollection<Position> Positions
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> class. 
		/// </summary>
		/// <param name="coordinates">
		/// List of positions through which the line string goes.
		/// </param>
		public LineString(IList<Position> coordinates)
			: this(coordinates, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> class.
		/// </summary>
		/// <param name="coordinates">
		/// The coordinates.
		/// </param>
		/// <param name="geometryParams">
		/// Additional geometry parameters.
		/// </param>
		public LineString(IList<Position> coordinates, GeometryParams geometryParams)
			: base(GeometryType.LineString, geometryParams)
		{
			if (coordinates == null)
			{
				throw new ArgumentNullException("coordinates");
			}
			Positions = new ReadOnlyCollection<Position>(coordinates);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> class.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal LineString()
			: base(GeometryType.LineString, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" />.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as LineString);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Positions.Aggregate(base.GetHashCode(), (int current, Position value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" /> is equal to the <paramref name="other" />.
		/// </summary>
		/// <param name="other">LineString to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" />.</param>
		/// <returns><c>true</c> if line strings are equal. <c>false</c> otherwise.</returns>
		public bool Equals(LineString other)
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
				return Positions.SequenceEqual(other.Positions);
			}
			return false;
		}
	}
}
