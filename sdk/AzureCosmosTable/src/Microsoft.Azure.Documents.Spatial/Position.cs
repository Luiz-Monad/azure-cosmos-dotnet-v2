using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// <para>
	/// A position is represented by an array of numbers in the Azure Cosmos DB service. There must be at least two elements, and may be more.
	/// </para>
	/// <para>
	/// The order of elements must follow longitude, latitude, altitude.
	/// Any number of additional elements are allowed - interpretation and meaning of additional elements is up to the application.
	/// </para>
	/// </summary>
	[JsonConverter(typeof(PositionJsonConverter))]
	public sealed class Position : IEquatable<Position>
	{
		/// <summary>
		/// Gets position coordinates in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Coordinate values.
		/// </value>
		public ReadOnlyCollection<double> Coordinates
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets longitude in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Longitude value.
		/// </value>
		public double Longitude => Coordinates[0];

		/// <summary>
		/// Gets latitude in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Latitude value.
		/// </value>
		public double Latitude => Coordinates[1];

		/// <summary>
		/// Gets optional altitude in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Altitude value.
		/// </value>
		public double? Altitude
		{
			get
			{
				if (Coordinates.Count <= 2)
				{
					return null;
				}
				return Coordinates[2];
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="longitude">
		/// Longitude value.
		/// </param>
		/// <param name="latitude">
		/// Latitude value.
		/// </param>
		public Position(double longitude, double latitude)
			: this(longitude, latitude, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="longitude">
		/// Longitude value.
		/// </param>
		/// <param name="latitude">
		/// Latitude value.
		/// </param>
		/// <param name="altitude">
		/// Optional altitude value.
		/// </param>
		public Position(double longitude, double latitude, double? altitude)
		{
			if (altitude.HasValue)
			{
				Coordinates = new ReadOnlyCollection<double>(new double[3]
				{
					longitude,
					latitude,
					altitude.Value
				});
			}
			else
			{
				Coordinates = new ReadOnlyCollection<double>(new double[2]
				{
					longitude,
					latitude
				});
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="coordinates">
		/// Position values.
		/// </param>
		public Position(IList<double> coordinates)
		{
			if (coordinates.Count < 2)
			{
				throw new ArgumentException("coordinates");
			}
			Coordinates = new ReadOnlyCollection<double>(coordinates);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> to compare to the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as Position);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> type in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.Position" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Coordinates.Aggregate(0, (int current, double value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> is equal to the <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.Position" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.Position" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(Position other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return Coordinates.SequenceEqual(other.Coordinates);
		}
	}
}
