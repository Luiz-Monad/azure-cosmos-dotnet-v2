using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// Represents a geometry consisting of multiple <see cref="T:Microsoft.Azure.Documents.Spatial.LineString" />.
	/// </summary>
	/// <seealso cref="T:Microsoft.Azure.Documents.Spatial.LineString" />.
	internal sealed class MultiLineString : Geometry, IEquatable<MultiLineString>
	{
		/// <summary>
		/// Gets collection of <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> representing individual line strings.
		/// </summary>
		/// <value>
		/// Collection of <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> representing individual line strings.
		/// </value>
		[JsonProperty("coordinates", Required = Required.Always, Order = 1)]
		public ReadOnlyCollection<LineStringCoordinates> LineStrings
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> class. 
		/// </summary>
		/// <param name="lineStrings">
		/// List of <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> instances representing individual line strings.
		/// </param>
		public MultiLineString(IList<LineStringCoordinates> lineStrings)
			: this(lineStrings, new GeometryParams())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> class.
		/// </summary>
		/// <param name="lineStrings">
		/// List of <see cref="T:Microsoft.Azure.Documents.Spatial.LineStringCoordinates" /> instances representing individual line strings.
		/// </param>
		/// <param name="geometryParams">
		/// Additional geometry parameters.
		/// </param>
		public MultiLineString(IList<LineStringCoordinates> lineStrings, GeometryParams geometryParams)
			: base(GeometryType.MultiLineString, geometryParams)
		{
			if (lineStrings == null)
			{
				throw new ArgumentNullException("lineStrings");
			}
			LineStrings = new ReadOnlyCollection<LineStringCoordinates>(lineStrings);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> class.
		/// </summary>
		/// <remarks>
		/// This constructor is used only during deserialization.
		/// </remarks>
		internal MultiLineString()
			: base(GeometryType.MultiLineString, new GeometryParams())
		{
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> is equal to the current <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" />.
		/// </summary>
		/// <returns>
		/// true if the specified object is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override bool Equals(object obj)
		{
			return Equals(obj as MultiLineString);
		}

		/// <summary>
		/// Serves as a hash function for the <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" />.
		/// </returns>
		public override int GetHashCode()
		{
			return LineStrings.Aggregate(base.GetHashCode(), (int current, LineStringCoordinates value) => (current * 397) ^ value.GetHashCode());
		}

		/// <summary>
		/// Determines if this <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> is equal to <paramref name="other" />.
		/// </summary>
		/// <param name="other"><see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" /> to compare to this <see cref="T:Microsoft.Azure.Documents.Spatial.MultiLineString" />.</param>
		/// <returns><c>true</c> if line strings are equal. <c>false</c> otherwise.</returns>
		public bool Equals(MultiLineString other)
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
				return LineStrings.SequenceEqual(other.LineStrings);
			}
			return false;
		}
	}
}
