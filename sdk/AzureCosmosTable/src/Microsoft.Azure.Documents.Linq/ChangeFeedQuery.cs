using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Routing;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Linq
{
	/// <summary>
	/// Provides interface for historical change feed.
	/// </summary>
	/// <typeparam name="TResource">Source Resource Type (e.g. Document)</typeparam>
	internal sealed class ChangeFeedQuery<TResource> : IDocumentQuery<TResource>, IDocumentQuery, IDisposable where TResource : Resource, new()
	{
		private const string IfNoneMatchAllHeaderValue = "*";

		private readonly ResourceType resourceType;

		private readonly DocumentClient client;

		private readonly string resourceLink;

		private readonly ChangeFeedOptions feedOptions;

		private HttpStatusCode lastStatusCode = HttpStatusCode.OK;

		private string nextIfNoneMatch;

		private string ifModifiedSince;

		/// <summary>
		/// Gets a value indicating whether there are potentially additional results that can be retrieved.
		/// </summary>
		/// <value>Boolean value representing if whether there are potentially additional results that can be retrieved.</value>
		/// <remarks>Initially returns true. This value is set based on whether the last execution returned a continuation token.</remarks>
		public bool HasMoreResults => lastStatusCode != HttpStatusCode.NotModified;

		public ChangeFeedQuery(DocumentClient client, ResourceType resourceType, string resourceLink, ChangeFeedOptions feedOptions)
		{
			this.client = client;
			this.resourceType = resourceType;
			this.resourceLink = resourceLink;
			this.feedOptions = (feedOptions ?? new ChangeFeedOptions());
			if (feedOptions.PartitionKey != null && !string.IsNullOrEmpty(feedOptions.PartitionKeyRangeId))
			{
				throw new ArgumentException(RMResources.PartitionKeyAndPartitionKeyRangeRangeIdBothSpecified, "feedOptions");
			}
			bool flag = true;
			if (feedOptions.RequestContinuation != null)
			{
				nextIfNoneMatch = feedOptions.RequestContinuation;
				flag = false;
			}
			if (feedOptions.StartTime.HasValue)
			{
				ifModifiedSince = ConvertToHttpTime(feedOptions.StartTime.Value);
				flag = false;
			}
			if (flag && !feedOptions.StartFromBeginning)
			{
				nextIfNoneMatch = "*";
			}
		}

		public void Dispose()
		{
		}

		/// <summary>
		/// Read feed and retrieves the next page of results in the Azure Cosmos DB service.
		/// </summary>
		/// <typeparam name="TResult">The type of the object returned in the query result.</typeparam>
		/// <returns>The Task object for the asynchronous response from query execution.</returns>
		public Task<FeedResponse<TResult>> ExecuteNextAsync<TResult>(CancellationToken cancellationToken = default(CancellationToken))
		{
			return ReadDocumentChangeFeedAsync<TResult>(resourceLink, cancellationToken);
		}

		/// <summary>
		/// Executes the query and retrieves the next page of results as dynamic objects in the Azure Cosmos DB service.
		/// </summary>
		/// <param name="cancellationToken">(Optional) The <see cref="T:System.Threading.CancellationToken" /> allows for notification that operations should be cancelled.</param>
		/// <returns>The Task object for the asynchronous response from query execution.</returns>
		public Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return ExecuteNextAsync<object>(cancellationToken);
		}

		public Task<FeedResponse<TResult>> ReadDocumentChangeFeedAsync<TResult>(string resourceLink, CancellationToken cancellationToken)
		{
			IDocumentClientRetryPolicy retryPolicy = client.ResetSessionTokenRetryPolicy.GetRequestPolicy();
			return TaskHelper.InlineIfPossible(() => ReadDocumentChangeFeedPrivateAsync<TResult>(resourceLink, retryPolicy, cancellationToken), retryPolicy, cancellationToken);
		}

		private async Task<FeedResponse<TResult>> ReadDocumentChangeFeedPrivateAsync<TResult>(string link, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			using (DocumentServiceResponse documentServiceResponse = await GetFeedResponseAsync(link, resourceType, retryPolicyInstance, cancellationToken))
			{
				lastStatusCode = documentServiceResponse.StatusCode;
				nextIfNoneMatch = documentServiceResponse.Headers["etag"];
				if (documentServiceResponse.ResponseBody != null && documentServiceResponse.ResponseBody.Length > 0)
				{
					long length = documentServiceResponse.ResponseBody.Length;
					int itemCount = 0;
					return (dynamic)new FeedResponse<object>(documentServiceResponse.GetQueryResponse(typeof(TResource), out itemCount), itemCount, documentServiceResponse.Headers, useETagAsContinuation: true, null, documentServiceResponse.RequestStats, null, length);
				}
				return new FeedResponse<TResult>(Enumerable.Empty<TResult>(), 0, documentServiceResponse.Headers, useETagAsContinuation: true, null, documentServiceResponse.RequestStats, null, 0L);
			}
		}

		private async Task<DocumentServiceResponse> GetFeedResponseAsync(string resourceLink, ResourceType resourceType, IDocumentClientRetryPolicy retryPolicyInstance, CancellationToken cancellationToken)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			if (feedOptions.MaxItemCount.HasValue)
			{
				nameValueCollection.Set("x-ms-max-item-count", feedOptions.MaxItemCount.ToString());
			}
			if (feedOptions.SessionToken != null)
			{
				nameValueCollection.Set("x-ms-session-token", feedOptions.SessionToken);
			}
			if (resourceType.IsPartitioned() && feedOptions.PartitionKeyRangeId == null && feedOptions.PartitionKey == null)
			{
				throw new ForbiddenException(RMResources.PartitionKeyRangeIdOrPartitionKeyMustBeSpecified);
			}
			if (nextIfNoneMatch != null)
			{
				nameValueCollection.Set("If-None-Match", nextIfNoneMatch);
			}
			if (ifModifiedSince != null)
			{
				nameValueCollection.Set("If-Modified-Since", ifModifiedSince);
			}
			nameValueCollection.Set("A-IM", "Incremental Feed");
			if (feedOptions.PartitionKey != null)
			{
				PartitionKeyInternal internalKey = feedOptions.PartitionKey.InternalKey;
				nameValueCollection.Set("x-ms-documentdb-partitionkey", internalKey.ToJsonString());
			}
			if (feedOptions.IncludeTentativeWrites)
			{
				nameValueCollection.Set("x-ms-cosmos-include-tentative-writes", bool.TrueString);
			}
			using (DocumentServiceRequest request = client.CreateDocumentServiceRequest(OperationType.ReadFeed, resourceLink, resourceType, nameValueCollection))
			{
				if (resourceType.IsPartitioned() && feedOptions.PartitionKeyRangeId != null)
				{
					request.RouteTo(new PartitionKeyRangeIdentity(feedOptions.PartitionKeyRangeId));
				}
				return await client.ReadFeedAsync(request, retryPolicyInstance, cancellationToken);
			}
		}

		private string ConvertToHttpTime(DateTime time)
		{
			return time.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);
		}
	}
}
