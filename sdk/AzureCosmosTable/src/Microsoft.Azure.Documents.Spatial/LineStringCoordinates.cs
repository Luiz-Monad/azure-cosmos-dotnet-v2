using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Line string coordinates.
	/// </summary>
	/// <seealso cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" />
	[JsonConverter(typeof(LineStringCoordinatesJsonConverter))]
	internal sealed class LineStringCoordinates : IEquatable<LineStringCoordinates>
	{
		/// <summary>
		/// Gets line string positions.
		/// </summary>
		/// <value>
		/// Positions of the line string.
		/// </value>
		public ReadOnlyCollection<Position> Positions
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> class.
		/// </summary>
		/// <param name="positions">
		/// Line string positions..
		/// </param>
		public LineStringCoordinates(IList<Position> positions)
		{
			if (positions == null)
			{
				throw new ArgumentException("points");
			}
			Positions = new ReadOnlyCollection<Position>(positions);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" />.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as LineStringCoordinates);
		}

		/// <summary>
		/// Serves as a hash function for <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" />.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" />.
		/// </returns>
		public override int GetHashCode()
		{
			return Positions.Aggregate(0, (int current, Position value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> is equal to the <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" />.</param>
		/// <returns><c>true</c> if objects are equal. <c>false</c> otherwise.</returns>
		public bool Equals(LineStringCoordinates other)
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
