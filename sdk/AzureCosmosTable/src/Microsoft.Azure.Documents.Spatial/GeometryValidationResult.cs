using Newtonsoft.Json;

namespace Microsoft.Azure.Documents.Spatial
{
	/// <summary>
	/// <para>
	/// Return value of <see cref="M:Microsoft.Azure.Documents.Spatial.GeometryOperationExtensions.IsValidDetailed(Microsoft.Azure.Documents.Spatial.Geometry)" /> in the Azure Cosmos DB service.
	/// </para>
	/// <para>
	/// Contains detailed description why a geometyr is invalid.
	/// </para>
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public class GeometryValidationResult
	{
		/// <summary>
		/// Returns a value indicating whether geometry for which <see cref="M:Microsoft.Azure.Documents.Spatial.GeometryOperationExtensions.IsValidDetailed(Microsoft.Azure.Documents.Spatial.Geometry)" />
		/// was called is valid or not in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// <c>true</c> if geometry for which <see cref="M:Microsoft.Azure.Documents.Spatial.GeometryOperationExtensions.IsValidDetailed(Microsoft.Azure.Documents.Spatial.Geometry)" /> was called is valid. <c>false</c> otherwise.
		/// </value>
		[JsonProperty("valid", Required = Required.Always, Order = 0)]
		public bool IsValid
		{
			get;
			private set;
		}

		/// <summary>
		/// If geometry is invalid, returns detailed reason in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Description why a geometry is invalid.
		/// </value>
		[JsonProperty("reason", Order = 1)]
		public string Reason
		{
			get;
			private set;
		}
	}
}
