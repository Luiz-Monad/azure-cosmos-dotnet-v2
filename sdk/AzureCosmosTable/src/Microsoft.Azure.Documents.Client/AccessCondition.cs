namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Represents a set of access conditions to be used for operations in the Azure Cosmos DB service.
	/// </summary>
	/// <example>
	/// The following example shows how to use AccessCondition with DocumentClient.
	/// <code language="c#">
	/// <![CDATA[
	/// // If ETag is current, then this will succeed. Otherwise the request will fail with HTTP 412 Precondition Failure
	/// await client.ReplaceDocumentAsync(
	///     readCopyOfBook.SelfLink, 
	///     new Book { Title = "Moby Dick", Price = 14.99 },
	///     new RequestOptions 
	///     { 
	///         AccessCondition = new AccessCondition 
	///         { 
	///             Condition = readCopyOfBook.ETag, 
	///             Type = AccessConditionType.IfMatch 
	///         } 
	///      });
	/// ]]>
	/// </code>
	/// </example>
	/// <seealso cref="T:Microsoft.Azure.Documents.Client.AccessConditionType" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Client.RequestOptions" />
	/// <seealso cref="T:Microsoft.Azure.Documents.Resource" />
	public sealed class AccessCondition
	{
		/// <summary>
		/// Gets or sets the condition type in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The condition type. Can be IfMatch or IfNoneMatch.
		/// </value>
		public AccessConditionType Type
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the value of the condition in the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The value of the condition. For <see cref="T:Microsoft.Azure.Documents.Client.AccessConditionType" /> IfMatch and IfNotMatch, this is the ETag that has to be compared to.
		/// </value>
		/// <seealso cref="T:Microsoft.Azure.Documents.Resource" />
		public string Condition
		{
			get;
			set;
		}
	}
}
