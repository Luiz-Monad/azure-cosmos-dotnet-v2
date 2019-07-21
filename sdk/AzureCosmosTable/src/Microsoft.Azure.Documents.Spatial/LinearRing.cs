using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// A <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> is closed LineString with 4 or more positions. The first and last positions are
	/// equivalent (they represent equivalent points).
	/// Though a <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> is not explicitly represented as a GeoJSON geometry type, it is referred to in
	/// the <see cref="T:Microsoft.Azure.Documents.Spatial.Polygon" /> geometry type definition in the Azure Cosmos DB service.
	/// </summary>
	[JsonConverter(typeof(LinearRingJsonConverter))]
	public sealed class LinearRing : IEquatable<LinearRing>
	{
		/// <summary>
		/// Gets the <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> positions in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Positions of the <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" />.
		/// </value>
		public ReadOnlyCollection<Position> Positions
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="coordinates">
		/// The coordinates. 4 or more positions. The first and last positions are equivalent (they represent equivalent
		/// points).
		/// </param>
		public LinearRing(IList<Position> coordinates)
		{
			if (coordinates == null)
			{
				throw new ArgumentNullException("coordinates");
			}
			Positions = new ReadOnlyCollection<Position>(coordinates);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as LinearRing);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> positions in the Azure Cosmos DB service. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Positions.Aggregate(0, (int current, Position value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> is equal to the <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.LinearRing" /> to compare to this one.</param>
		/// <returns><c>true</c> if linear rings are equal. <c>false</c> otherwise.</returns>
		public bool Equals(LinearRing other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return Positions.SequenceEqual(other.Positions);
		}
	}
}
