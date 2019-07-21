using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Represents a SQL query in the Azure Cosmos DB service.
	/// </summary>
	[DataContract]
	public sealed class SqlQuerySpec
	{
		private string queryText;

		private SqlParameterCollection parameters;

		/// <summary>
		/// Gets or sets the text of the Azure Cosmos DB database query.
		/// </summary>
		/// <value>The text of the database query.</value>
		[DataMember(Name = "query")]
		public string QueryText
		{
			get
			{
				return queryText;
			}
			set
			{
				queryText = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Microsoft.Azure.Documents.SqlParameterCollection" /> instance, which represents the collection of Azure Cosmos DB query parameters.
		/// </summary>
		/// <value>The <see cref="T:Microsoft.Azure.Documents.SqlParameterCollection" /> instance.</value>
		[DataMember(Name = "parameters")]
		public SqlParameterCollection Parameters
		{
			get
			{
				return parameters;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				parameters = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" /> class for the Azure Cosmos DB service.</summary>
		/// <remarks> 
		/// The default constructor initializes any fields to their default values.
		/// </remarks>
		public SqlQuerySpec()
			: this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" /> class for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="queryText">The text of the query.</param>
		public SqlQuerySpec(string queryText)
			: this(queryText, new SqlParameterCollection())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Microsoft.Azure.Documents.SqlQuerySpec" /> class for the Azure Cosmos DB service.
		/// </summary>
		/// <param name="queryText">The text of the database query.</param>
		/// <param name="parameters">The <see cref="T:Microsoft.Azure.Documents.SqlParameterCollection" /> instance, which represents the collection of query parameters.</param>
		public SqlQuerySpec(string queryText, SqlParameterCollection parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			this.queryText = queryText;
			this.parameters = parameters;
		}

		/// <summary>
		/// Returns a value that indicates whether the Azure Cosmos DB database <see cref="P:Microsoft.Azure.Documents.SqlQuerySpec.Parameters" /> property should be serialized.
		/// </summary>
		public bool ShouldSerializeParameters()
		{
			return parameters.Count > 0;
		}
	}
}
