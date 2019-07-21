namespace Microsoft.Azure.Documents.Client
{
	/// <summary> 
	/// Options used with attachment content (aka media) creation in the Azure Cosmos DB service.
	/// </summary>
	public sealed class MediaOptions
	{
		/// <summary>
		/// Gets or sets the Slug header in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The slug header.
		/// </value>
		public string Slug
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the ContentType header in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The ContentType header
		/// </value>
		public string ContentType
		{
			get;
			set;
		}
	}
}
