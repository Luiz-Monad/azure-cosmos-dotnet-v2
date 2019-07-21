using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Point geometry class in the Azure Cosmos DB service.
	/// </summary>
	public sealed class Point : Geometry, IEquatable<Point>
	{
		/// <summary>
		/// Gets point coordinates in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Coordinates of the point.
		/// </value>
		[JsonProperty("coordinates", Required = Required.Always, Order = 1)]
		public Position Position
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="longitude">
		/// Longitude of the point.
		/// </param>
		/// <param name="latitude">
		/// Latitude of the point.
		/// </param>
		public Point(double longitude, double latitude)
			: this(new Position(longitude, latitude), new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="position">
		/// Position of the point.
		/// </param>
		public Point(Position position)
			: this(position, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="position">
		/// Point coordinates.
		/// </param>
		/// <param name="geometryParams">
		/// Additional geometry parameters.
		/// </param>
		public Point(Position position, GeometryParams geometryParams)
			: base(GeometryType.Point, geometryParams)
		{
			if (position == null)
			{
				throw new ArgumentNullException("position");
			}
			Position = position;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal Point()
			: base(GeometryType.Point, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> is equal to <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.Point" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(Point other)
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
				return Position.Equals(other.Position);
			}
			return false;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as Point);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.Point" /> type in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current geometry.
		/// </returns>
		public override int GetHashCode()
		{
			return (base.GetHashCode() * 397) ^ Position.GetHashCode();
		}
	}
}
