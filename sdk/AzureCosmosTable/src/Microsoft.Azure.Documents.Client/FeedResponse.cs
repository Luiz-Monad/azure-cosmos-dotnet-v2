using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Azure.Documents.Client
{
	/// <summary>
	/// Represents the template class used by feed methods (enumeration operations) for the Azure Cosmos DB service.
	/// </summary>
	/// <typeparam name="T">The feed type.</typeparam>
	public class FeedResponse<T> : IEnumerable<T>, IEnumerable, IDynamicMetaObjectProvider, IFeedResponse<T>
	{
		private class ResourceFeedDynamicObject : DynamicMetaObject
		{
			public ResourceFeedDynamicObject(FeedResponse<T> parent, Expression expression)
				: base(expression, BindingRestrictions.Empty, parent)
			{
			}

			public override DynamicMetaObject BindConvert(ConvertBinder binder)
			{
				Type genericTypeDefinition = typeof(FeedResponse<bool>).GetGenericTypeDefinition();
				if ((object)binder.Type != typeof(IEnumerable) && (!binder.Type.IsGenericType() || ((object)binder.Type.GetGenericTypeDefinition() != genericTypeDefinition && (object)binder.Type.GetGenericTypeDefinition() != typeof(IEnumerable<string>).GetGenericTypeDefinition() && (object)binder.Type.GetGenericTypeDefinition() != typeof(IQueryable<string>).GetGenericTypeDefinition())))
				{
					return base.BindConvert(binder);
				}
				Expression arg = Expression.Convert(base.Expression, base.LimitType);
				return new DynamicMetaObject(Expression.Call(typeof(FeedResponseBinder).GetMethod("Convert", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(CustomTypeExtensions.GetGenericArguments(binder.Type)[0]), arg), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}
		}

		private readonly IEnumerable<T> inner;

		private INameValueCollection responseHeaders;

		private readonly Dictionary<string, long> usageHeaders;

		private readonly Dictionary<string, long> quotaHeaders;

		private readonly bool useETagAsContinuation;

		private readonly IReadOnlyDictionary<string, QueryMetrics> queryMetrics;

		private readonly string disallowContinuationTokenMessage;

		/// <summary>
		/// Get the client side request statistics for the current request.
		/// </summary>
		/// <remarks>
		/// This value is currently used for tracking replica Uris.
		/// </remarks>
		internal ClientSideRequestStatistics RequestStatistics
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the response length in bytes
		/// </summary>
		/// <remarks>
		/// This value is only set for Direct mode.
		/// </remarks>
		internal long ResponseLengthBytes
		{
			get;
			private set;
		}

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
		/// <vallue>
		/// Current collection size in kilobytes.
		/// </vallue>
		public long CollectionSizeUsage => GetCurrentQuotaHeader("collectionSize");

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
		/// Get the current number of triggers for a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Current number of triggers.
		/// </value>
		public long TriggersUsage => GetCurrentQuotaHeader("triggers");

		/// <summary>
		/// Gets the maximum quota of user defined functions for a collection from the Azure Cosmos DB service. 
		/// </summary>
		/// <value>
		/// Maximum quota.
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
		/// Gets the number of items returned in the response from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// Count of items in the response.
		/// </value>
		public int Count
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the maximum size limit for this entity from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The maximum size limit for this entity. Measured in kilobytes for document resources 
		/// and in counts for other resources.
		/// </value>
		public string MaxResourceQuota => responseHeaders["x-ms-resource-quota"];

		/// <summary>
		/// Gets the current size of this entity from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The current size for this entity. Measured in kilobytes for document resources 
		/// and in counts for other resources.
		/// </value>
		public string CurrentResourceQuotaUsage => responseHeaders["x-ms-resource-usage"];

		/// <summary>
		/// Gets the request charge for this request from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The request charge measured in reqest units.
		/// </value>
		public double RequestCharge => Helpers.GetHeaderValueDouble(responseHeaders, "x-ms-request-charge", 0.0);

		/// <summary>
		/// Gets the activity ID for the request from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The activity ID for the request.
		/// </value>
		public string ActivityId
		{
			get
			{
				return responseHeaders["x-ms-activity-id"];
			}
			internal set
			{
				responseHeaders["x-ms-activity-id"] = value;
			}
		}

		/// <summary>
		/// Gets the continuation token to be used for continuing enumeration of the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The continuation token to be used for continuing enumeration.
		/// </value>
		public string ResponseContinuation
		{
			get
			{
				if (disallowContinuationTokenMessage != null)
				{
					throw new ArgumentException(disallowContinuationTokenMessage);
				}
				if (!useETagAsContinuation)
				{
					return responseHeaders["x-ms-continuation"];
				}
				return ETag;
			}
			internal set
			{
				if (disallowContinuationTokenMessage != null)
				{
					throw new ArgumentException(disallowContinuationTokenMessage);
				}
				responseHeaders["x-ms-continuation"] = value;
			}
		}

		/// <summary>
		/// Gets the session token for use in sesssion consistency reads from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The session token for use in session consistency.
		/// </value>
		public string SessionToken => responseHeaders["x-ms-session-token"];

		/// <summary>
		/// Gets the content parent location, for example, dbs/foo/colls/bar, from the Azure Cosmos DB service.
		/// </summary>
		public string ContentLocation => responseHeaders["x-ms-alt-content-path"];

		/// <summary>
		/// Gets the entity tag associated with last transaction in the Azure Cosmos DB service,
		/// which can be used as If-Non-Match Access condition for ReadFeed REST request or 
		/// ContinuationToken property of <see cref="T:Microsoft.Azure.Documents.Client.ChangeFeedOptions" /> parameter for
		/// <see cref="M:Microsoft.Azure.Documents.Client.DocumentClient.CreateDocumentChangeFeedQuery(System.String,Microsoft.Azure.Documents.Client.ChangeFeedOptions)" /> 
		/// to get feed changes since the transaction specified by this entity tag.
		/// </summary>
		public string ETag => responseHeaders["etag"];

		internal INameValueCollection Headers
		{
			get
			{
				return responseHeaders;
			}
			set
			{
				responseHeaders = value;
			}
		}

		/// <summary>
		/// Gets the response headers from the Azure Cosmos DB service.
		/// </summary>
		/// <value>
		/// The response headers.
		/// </value>
		public NameValueCollection ResponseHeaders => responseHeaders.ToNameValueCollection();

		/// <summary>
		/// Get <see cref="T:Microsoft.Azure.Documents.QueryMetrics" /> for each individual partition in the Azure Cosmos DB service
		/// </summary>
		public IReadOnlyDictionary<string, QueryMetrics> QueryMetrics => queryMetrics;

		/// <summary>
		/// Gets the flag associated with the response from the Azure Cosmos DB service whether this feed request is served from Request Units(RUs)/minute capacity or not.
		/// </summary>
		/// <value>
		/// True if this request is served from RUs/minute capacity. Otherwise, false.
		/// </value>
		public bool IsRUPerMinuteUsed
		{
			get
			{
				if (Helpers.GetHeaderValueByte(responseHeaders, "x-ms-documentdb-is-ru-per-minute-used", 0) != 0)
				{
					return true;
				}
				return false;
			}
		}

		internal bool UseETagAsContinuation => useETagAsContinuation;

		internal string DisallowContinuationTokenMessage => disallowContinuationTokenMessage;

		/// <summary>
		/// Constructor exposed for mocking purposes.
		/// </summary>
		public FeedResponse()
		{
		}

		/// <summary>
		/// Constructor exposed for mocking purposes.
		/// </summary>
		/// <param name="result"></param>
		public FeedResponse(IEnumerable<T> result)
			: this()
		{
			inner = ((result != null) ? result : Enumerable.Empty<T>());
		}

		internal FeedResponse(IEnumerable<T> result, int count, INameValueCollection responseHeaders, bool useETagAsContinuation = false, IReadOnlyDictionary<string, QueryMetrics> queryMetrics = null, ClientSideRequestStatistics requestStats = null, string disallowContinuationTokenMessage = null, long responseLengthBytes = 0L)
			: this(result)
		{
			Count = count;
			this.responseHeaders = responseHeaders.Clone();
			usageHeaders = new Dictionary<string, long>();
			quotaHeaders = new Dictionary<string, long>();
			this.useETagAsContinuation = useETagAsContinuation;
			this.queryMetrics = queryMetrics;
			RequestStatistics = requestStats;
			this.disallowContinuationTokenMessage = disallowContinuationTokenMessage;
			ResponseLengthBytes = responseLengthBytes;
		}

		internal FeedResponse(IEnumerable<T> result, int count, INameValueCollection responseHeaders, long responseLengthBytes)
			: this(result, count, responseHeaders, useETagAsContinuation: false, (IReadOnlyDictionary<string, QueryMetrics>)null, (ClientSideRequestStatistics)null, (string)null, 0L)
		{
			ResponseLengthBytes = responseLengthBytes;
		}

		internal FeedResponse(IEnumerable<T> result, int count, INameValueCollection responseHeaders, ClientSideRequestStatistics requestStats, long responseLengthBytes)
			: this(result, count, responseHeaders, useETagAsContinuation: false, (IReadOnlyDictionary<string, QueryMetrics>)null, requestStats, (string)null, responseLengthBytes)
		{
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection from the Azure Cosmos DB service.
		/// </summary>
		/// <returns>An IEnumerator object that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<T> GetEnumerator()
		{
			return inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return inner.GetEnumerator();
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			return new ResourceFeedDynamicObject(this, parameter);
		}

		private long GetCurrentQuotaHeader(string headerName)
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

		private long GetMaxQuotaHeader(string headerName)
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
