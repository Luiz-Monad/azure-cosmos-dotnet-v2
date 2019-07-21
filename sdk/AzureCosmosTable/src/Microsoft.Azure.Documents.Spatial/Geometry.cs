using Microsoft.Azure.Documents.Spatial.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Base class for spatial geometry objects in the Azure Cosmos DB service.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	[JsonConverter(typeof(GeometryJsonConverter))]
	public abstract class Geometry
	{
		/// <summary>
		/// Gets the Coordinate Reference System for this geometry in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The Coordinate Reference System for this geometry.
		/// </value>
		public Crs Crs => CrsForSerialization ?? Crs.Default;

		/// <summary>
		/// Gets geometry type in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Type of geometry.
		/// </value>
		[JsonProperty("type", Required = Required.Always, Order = 0)]
		[JsonConverter(typeof(StringEnumConverter))]
		public GeometryType Type
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets bounding box for this geometry in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Bounding box of the geometry.
		/// </value>
		[JsonProperty("bbox", DefaultValueHandling = DefaultValueHandling.Ignore, Order = 3)]
		public BoundingBox BoundingBox
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets additional properties in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Additional geometry properties.
		/// </value>
		[JsonExtensionData]
		public IDictionary<string, object> AdditionalProperties
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets CRS value used for serialization in the Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This is artificial property needed for serialization. If CRS is default one, we don't want
		/// to serialize anything.
		/// </remarks>
		[JsonProperty("crs", DefaultValueHandling = DefaultValueHandling.Ignore, Order = 2)]
		private Crs CrsForSerialization
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> class in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="type">
		/// Geometry type.
		/// </param>
		/// <param name="geometryParams">
		/// Coordinate reference system, additional properties etc.
		/// </param>
		protected Geometry(GeometryType type, GeometryParams geometryParams)
		{
			if (geometryParams == null)
			{
				throw new ArgumentNullException("geometryParams");
			}
			Type = type;
			if (geometryParams.Crs == null || geometryParams.Crs.Equals(Crs.Default))
			{
				CrsForSerialization = null;
			}
			else
			{
				CrsForSerialization = geometryParams.Crs;
			}
			BoundingBox = geometryParams.BoundingBox;
			AdditionalProperties = (geometryParams.AdditionalProperties ?? new Dictionary<string, object>());
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as Geometry);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> type in the Azure Cosmos DB service.
		/// </summary>
		/// <returns>
		/// A hash code for the current geometry.
		/// </returns>
		public override int GetHashCode()
		{
			int hashCode = Crs.GetHashCode();
			hashCode = ((hashCode * 397) ^ (int)Type);
			hashCode = ((hashCode * 397) ^ ((BoundingBox != null) ? BoundingBox.GetHashCode() : 0));
			return AdditionalProperties.Aggregate(hashCode, (int current, KeyValuePair<string, object> value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> is equal to the <paramref name="other" /> in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.Geometry" />.</param>
		/// <returns><c>true</c> if geometries are equal. <c>false</c> otherwise.</returns>
		private bool Equals(Geometry other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (Crs.Equals(other.Crs) && Type == other.Type && object.Equals(BoundingBox, other.BoundingBox))
			{
				return AdditionalProperties.SequenceEqual(other.AdditionalProperties);
			}
			return false;
		}
	}
}
