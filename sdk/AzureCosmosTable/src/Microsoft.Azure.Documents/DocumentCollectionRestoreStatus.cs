namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Provides status for a documention collection restore.
	/// </summary>
	internal sealed class DocumentCollectionRestoreStatus
	{
		/// <summary>
		/// Gets the <see cref="P:Microsoft.Azure.Documents.DocumentCollectionRestoreStatus.State" /> from the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// The state of the restore process.
		/// </value>
		public string State
		{
			get;
			internal set;
		}
	}
}
