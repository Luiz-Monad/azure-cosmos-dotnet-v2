namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Specifies the supported geospatial types in the Azure Cosmos DB service.
	/// </summary> 
	/// <remarks>
	/// Each geospatial type further supports different sub-types like point, linestring, polygon.
	/// </remarks>
	internal enum GeospatialType
	{
		/// <summary>
		/// Represents data in round-earth coordinate system.
		/// </summary>
		Geography,
		/// <summary>
		/// Represents data in Eucledian(flat) coordinate system.
		/// </summary>
		Geometry
	}
}
