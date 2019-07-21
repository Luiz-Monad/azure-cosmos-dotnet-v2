using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Helper class to invoke User Defined Functions via Linq queries in the Azure Cosmos DB service.
	/// </summary>
	public static class UserDefinedFunctionProvider
	{
		/// <summary>
		/// Helper method to invoke User Defined Functions via Linq queries in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="udfName">the UserDefinedFunction name</param>
		/// <param name="arguments">the arguments of the UserDefinedFunction</param>
		/// <returns></returns>
		/// <remarks>
		/// This is a stub helper method for use within LINQ expressions. Cannot be called directly. 
		/// Refer to http://azure.microsoft.com/documentation/articles/documentdb-sql-query/#linq-to-documentdb-sql for more details about the LINQ provider.
		/// Refer to http://azure.microsoft.com/documentation/articles/documentdb-sql-query/#javascript-integration for more details about user defined functions.
		/// </remarks>
		/// <example> 
		/// <code language="c#">
		/// <![CDATA[
		///  await client.CreateUserDefinedFunctionAsync(collectionLink, new UserDefinedFunction { Id = "calculateTax", Body = @"function(amt) { return amt * 0.05; }" });
		///  var queryable = client.CreateDocumentQuery<Book>(collectionLink).Select(b => UserDefinedFunctionProvider.Invoke("calculateTax", b.Price));
		///
		/// // Equivalent to SELECT * FROM books b WHERE udf.toLowerCase(b.title) = 'war and peace'" 
		/// await client.CreateUserDefinedFunctionAsync(collectionLink, new UserDefinedFunction { Id = "toLowerCase", Body = @"function(s) { return s.ToLowerCase(); }" });
		/// queryable = client.CreateDocumentQuery<Book>(collectionLink).Where(b => UserDefinedFunctionProvider.Invoke("toLowerCase", b.Title) == "war and peace");
		/// ]]>
		/// </code>
		/// </example>
		/// <seealso cref="T:Microsoft.Azure.Documents.UserDefinedFunction" />
		public static object Invoke(string udfName, params object[] arguments)
		{
			throw new Exception(string.Format(CultureInfo.CurrentCulture, ClientResources.InvalidCallToUserDefinedFunctionProvider));
		}
	}
}
