using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using System;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Represents a coordinate range for geometries in the Azure Cosmos DB service.
	/// </summary>
	[JsonConverter(typeof(BoundingBoxJsonConverter))]
	public sealed class BoundingBox : IEquatable<BoundingBox>
	{
		/// <summary>
		/// Gets lowest values for all axes of the bounding box in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Lowest values for all axes of the bounding box.
		/// </value>
		public Position Min
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets highest values for all axes of the bounding box in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Highest values for all axes of the bounding box.
		/// </value>
		public Position Max
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="min">
		/// Lowest values for all axes of the bounding box.
		/// </param>
		/// <param name="max">
		/// Highest values for all axes of the bounding box.
		/// </param>
		public BoundingBox(Position min, Position max)
		{
			if (max == null)
			{
				throw new ArgumentException("Max");
			}
			if (min == null)
			{
				throw new ArgumentException("Min");
			}
			if (max.Coordinates.Count != min.Coordinates.Count)
			{
				throw new ArgumentException("Max and min must have same cardinality.");
			}
			Max = max;
			Min = min;
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> is equal to the <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> to compare to this bounding box.</param>
		/// <returns><c>true</c> if bounding boxes are equal. <c>false</c> otherwise.</returns>
		public bool Equals(BoundingBox other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (Min.Equals(other.Min))
			{
				return Max.Equals(other.Max);
			}
			return false;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as BoundingBox);
		}

		/// <summary>
		/// Serves as a hash function for <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" /> type in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.BoundingBox" />.
		/// </returns>
		public override int GetHashCode()
		{
			return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
		}
	}
}
