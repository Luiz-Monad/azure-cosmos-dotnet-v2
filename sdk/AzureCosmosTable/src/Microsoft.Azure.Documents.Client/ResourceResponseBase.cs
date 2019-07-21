using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Represents the non-resource specific service response headers returned by any request in the Azure Cosmos DB service.
	/// </summary>
	public abstract class ResourceResponseBase : IResourceResponseBase
	{
		internal DocumentServiceResponse response;

		private Dictionary<string, long> usageHeaders;

		private Dictionary<string, long> quotaHeaders;

		/// <summary>
		/// Gets the maximum quota for database resources within the account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota for the account.
		/// </value>
		public long DatabaseQuota => GetMaxQuotaHeader("databases");

		/// <summary>
		/// Gets the current number of database resources within the account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The number of databases.
		/// </value>
		public long DatabaseUsage => GetCurrentQuotaHeader("databases");

		/// <summary>
		/// Gets the maximum quota for collection resources within an account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota for the account.
		/// </value>
		public long CollectionQuota => GetMaxQuotaHeader("collections");

		/// <summary>
		/// Gets the current number of collection resources within the account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The number of collections.
		/// </value>
		public long CollectionUsage => GetCurrentQuotaHeader("collections");

		/// <summary>
		/// Gets the maximum quota for user resources within an account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota for the account.
		/// </value>
		public long UserQuota => GetMaxQuotaHeader("users");

		/// <summary>
		/// Gets the current number of user resources within the account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The number of users.
		/// </value>
		public long UserUsage => GetCurrentQuotaHeader("users");

		/// <summary>
		/// Gets the maximum quota for permission resources within an account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota for the account.
		/// </value>
		public long PermissionQuota => GetMaxQuotaHeader("permissions");

		/// <summary>
		/// Gets the current number of permission resources within the account from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The number of permissions.
		/// </value>
		public long PermissionUsage => GetCurrentQuotaHeader("permissions");

		/// <summary>
		/// Gets the maximum size of a collection in kilobytes from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Quota in kilobytes.
		/// </value>
		public long CollectionSizeQuota => GetMaxQuotaHeader("collectionSize");

		/// <summary>
		/// Gets the current size of a collection in kilobytes from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current collection size in kilobytes.
		/// </value>
		public long CollectionSizeUsage => GetCurrentQuotaHeader("collectionSize");

		/// <summary>
		/// Gets the maximum size of a documents within a collection in kilobytes from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Quota in kilobytes.
		/// </value>
		public long DocumentQuota => GetMaxQuotaHeader("documentsSize");

		/// <summary>
		/// Gets the current size of documents within a collection in kilobytes from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current documents size in kilobytes.
		/// </value>
		public long DocumentUsage => GetCurrentQuotaHeader("documentsSize");

		/// <summary>
		/// Gets the maximum quota of stored procedures for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota.
		/// </value>
		public long StoredProceduresQuota => GetMaxQuotaHeader("storedProcedures");

		/// <summary>
		/// Gets the current number of stored procedures for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current number of stored procedures.
		/// </value>
		public long StoredProceduresUsage => GetCurrentQuotaHeader("storedProcedures");

		/// <summary>
		/// Gets the maximum quota of triggers for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota.
		/// </value>
		public long TriggersQuota => GetMaxQuotaHeader("triggers");

		/// <summary>
		/// Gets the current number of triggers for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current number of triggers.
		/// </value>
		public long TriggersUsage => GetCurrentQuotaHeader("triggers");

		/// <summary>
		/// Gets the maximum quota of user defined functions for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum quota.
		/// </value>
		public long UserDefinedFunctionsQuota => GetMaxQuotaHeader("functions");

		/// <summary>
		/// Gets the current number of user defined functions for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current number of user defined functions.
		/// </value>
		public long UserDefinedFunctionsUsage => GetCurrentQuotaHeader("functions");

		/// <summary>
		/// Gets the current count of documents within a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current count of documents.
		/// </value>
		internal long DocumentCount => GetCurrentQuotaHeader("documentsCount");

		/// <summary>
		/// Gets the activity ID for the request from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The activity ID for the request.
		/// </value>
		public string ActivityId => response.Headers["x-ms-activity-id"];

		/// <summary>
		/// Gets the session token for use in sesssion consistency reads from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The session token for use in session consistency.
		/// </value>
		public string SessionToken => response.Headers["x-ms-session-token"];

		/// <summary>
		/// Gets the HTTP status code associated with the response from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The HTTP status code associated with the response.
		/// </value>
		public HttpStatusCode StatusCode => response.StatusCode;

		/// <summary>
		/// Gets the maximum size limit for this entity from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum size limit for this entity. Measured in kilobytes for document resources 
		/// and in counts for other resources.
		/// </value>
		public string MaxResourceQuota => response.Headers["x-ms-resource-quota"];

		/// <summary>
		/// Gets the current size of this entity from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The current size for this entity. Measured in kilobytes for document resources 
		/// and in counts for other resources.
		/// </value>
		public string CurrentResourceQuotaUsage => response.Headers["x-ms-resource-usage"];

		/// <summary>
		/// Gets the underlying stream of the response from the Azure Cosmos DB service.
		/// </summary>
		public Stream ResponseStream => response.ResponseBody;

		/// <summary>
		/// Gets the request charge for this request from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The request charge measured in reqest units.
		/// </value>
		public double RequestCharge => Helpers.GetHeaderValueDouble(response.Headers, "x-ms-request-charge", 0.0);

		/// <summary>
		/// Gets the flag associated with the response from the Azure Cosmos DB service whether this request is served from Request Units(RUs)/minute capacity or not.
		/// </summary>
		/// <value>
		/// True if this request is served from RUs/minute capacity. Otherwise, false.
		/// </value>
		public bool IsRUPerMinuteUsed
		{
			get
			{
				if (Helpers.GetHeaderValueByte(response.Headers, "x-ms-documentdb-is-ru-per-minute-used", 0) != 0)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the response headers from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The response headers.
		/// </value>
		public NameValueCollection ResponseHeaders => response.ResponseHeaders;

		internal INameValueCollection Headers => response.Headers;

		/// <summary>
		/// The content parent location, for example, dbs/foo/colls/bar in the Azure Cosmos DB service.
		/// </summary>
		public string ContentLocation => response.Headers["x-ms-alt-content-path"];

		/// <summary>
		/// Gets the progress of an index transformation, if one is underway from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// An integer from 0 to 100 representing percentage completion of the index transformation process.
		/// Returns -1 if the index transformation progress header could not be found.
		/// </value>
		/// <remarks>
		/// An index will be rebuilt when the IndexPolicy of a collection is updated.
		/// </remarks>
		public long IndexTransformationProgress => Helpers.GetHeaderValueLong(response.Headers, "x-ms-documentdb-collection-index-transformation-progress", -1L);

		/// <summary>
		/// Gets the progress of lazy indexing from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// An integer from 0 to 100 representing percentage completion of the lazy indexing process.
		/// Returns -1 if the lazy indexing progress header could not be found.
		/// </value>
		/// <remarks>
		/// Lazy indexing progress only applies to the collection with indexing mode Lazy.
		/// </remarks>
		public long LazyIndexingProgress => Helpers.GetHeaderValueLong(response.Headers, "x-ms-documentdb-collection-lazy-indexing-progress", -1L);

		/// <summary>
		/// Gets the end-to-end request latency for the current request to Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This field is only valid when the request uses direct connectivity.
		/// </remarks>
		public TimeSpan RequestLatency
		{
			get
			{
				if (response.RequestStats == null)
				{
					return TimeSpan.Zero;
				}
				return response.RequestStats.RequestLatency;
			}
		}

		/// <summary>
		/// Gets the diagnostics information for the current request to Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This field is only valid when the request uses direct connectivity.
		/// </remarks>
		public string RequestDiagnosticsString
		{
			get
			{
				if (response.RequestStats == null)
				{
					return string.Empty;
				}
				return response.RequestStats.ToString();
			}
		}

		/// <summary>
		/// Gets the request statistics for the current request to Azure Cosmos DB service.
		/// </summary>
		/// <remarks>
		/// This field is only valid when the request uses direct connectivity.
		/// </remarks>
		internal ClientSideRequestStatistics RequestStatistics => response.RequestStats;

		/// <summary>
		/// Constructor exposed for mocking purposes for the Azure Cosmos DB service.
		/// </summary>
		public ResourceResponseBase()
		{
		}

		internal ResourceResponseBase(DocumentServiceResponse response)
		{
			this.response = response;
			usageHeaders = new Dictionary<string, long>();
			quotaHeaders = new Dictionary<string, long>();
		}

		internal long GetCurrentQuotaHeader(string headerName)
		{
			long value = 0L;
			if (usageHeaders.Count == 0 && !string.IsNullOrEmpty(MaxResourceQuota) && !string.IsNullOrEmpty(CurrentResourceQuotaUsage))
			{
				PopulateQuotaHeader(MaxResourceQuota, CurrentResourceQuotaUsage);
			}
			if (usageHeaders.TryGetValue(headerName, out value))
			{
				return value;
			}
			return 0L;
		}

		internal long GetMaxQuotaHeader(string headerName)
		{
			long value = 0L;
			if (quotaHeaders.Count == 0 && !string.IsNullOrEmpty(MaxResourceQuota) && !string.IsNullOrEmpty(CurrentResourceQuotaUsage))
			{
				PopulateQuotaHeader(MaxResourceQuota, CurrentResourceQuotaUsage);
			}
			if (quotaHeaders.TryGetValue(headerName, out value))
			{
				return value;
			}
			return 0L;
		}

		private void PopulateQuotaHeader(string headerMaxQuota, string headerCurrentUsage)
		{
			string[] array = headerMaxQuota.Split(Constants.Quota.DelimiterChars, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = headerCurrentUsage.Split(Constants.Quota.DelimiterChars, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				if (string.Equals(array[i], "databases", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("databases", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("databases", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "collections", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("collections", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("collections", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "users", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("users", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("users", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "permissions", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("permissions", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("permissions", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "collectionSize", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("collectionSize", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("collectionSize", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "documentsSize", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("documentsSize", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("documentsSize", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "documentsCount", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("documentsCount", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("documentsCount", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "storedProcedures", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("storedProcedures", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("storedProcedures", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "triggers", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("triggers", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("triggers", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
				else if (string.Equals(array[i], "functions", StringComparison.OrdinalIgnoreCase))
				{
					quotaHeaders.Add("functions", long.Parse(array[i + 1], CultureInfo.InvariantCulture));
					usageHeaders.Add("functions", long.Parse(array2[i + 1], CultureInfo.InvariantCulture));
				}
			}
		}
	}
}
