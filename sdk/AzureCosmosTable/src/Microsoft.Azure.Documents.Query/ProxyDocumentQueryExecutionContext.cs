using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// This class is used as a proxy to wrap the DefaultDocumentQueryExecutionContext which is needed 
	/// for sending the query to Gateway first and then uses PipelinedDocumentQueryExecutionContext after
	/// it gets the necessary info. This has been added since we
	/// haven't produced Linux/Mac version of the ServiceInterop native binary which holds the logic for
	/// parsing the query without having this extra hop to Gateway
	/// </summary>
	internal sealed class ProxyDocumentQueryExecutionContext : IDocumentQueryExecutionContext, IDisposable
	{
		private IDocumentQueryExecutionContext innerExecutionContext;

		private readonly IDocumentQueryClient client;

		private readonly ResourceType resourceTypeEnum;

		private readonly Type resourceType;

		private readonly Expression expression;

		private readonly FeedOptions feedOptions;

		private readonly string resourceLink;

		private readonly DocumentCollection collection;

		private readonly bool isContinuationExpected;

		private readonly Guid correlatedActivityId;

		public bool IsDone => innerExecutionContext.IsDone;

		private ProxyDocumentQueryExecutionContext(IDocumentQueryExecutionContext innerExecutionContext, IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, string resourceLink, DocumentCollection collection, bool isContinuationExpected, Guid correlatedActivityId)
		{
			this.innerExecutionContext = innerExecutionContext;
			this.client = client;
			this.resourceTypeEnum = resourceTypeEnum;
			this.resourceType = resourceType;
			this.expression = expression;
			this.feedOptions = feedOptions;
			this.resourceLink = resourceLink;
			this.collection = collection;
			this.isContinuationExpected = isContinuationExpected;
			this.correlatedActivityId = correlatedActivityId;
		}

		public static Task<ProxyDocumentQueryExecutionContext> CreateAsync(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, string resourceLink, CancellationToken token, DocumentCollection collection, bool isContinuationExpected, Guid correlatedActivityId)
		{
			token.ThrowIfCancellationRequested();
			return Task.FromResult(new ProxyDocumentQueryExecutionContext(new DefaultDocumentQueryExecutionContext(new DocumentQueryExecutionContextBase.InitParams(client, resourceTypeEnum, resourceType, expression, feedOptions, resourceLink, getLazyFeedResponse: false, correlatedActivityId), isContinuationExpected), client, resourceTypeEnum, resourceType, expression, feedOptions, resourceLink, collection, isContinuationExpected, correlatedActivityId));
		}

		public void Dispose()
		{
			innerExecutionContext.Dispose();
		}

		public async Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token)
		{
			if (IsDone)
			{
				throw new InvalidOperationException(RMResources.DocumentQueryExecutionContextIsDone);
			}
			Error error;
			try
			{
				return await innerExecutionContext.ExecuteNextAsync(token);
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode != HttpStatusCode.BadRequest || ex.GetSubStatus() != SubStatusCodes.CrossPartitionQueryNotServable)
				{
					throw;
				}
				error = ex.Error;
			}
			PartitionedQueryExecutionInfo partitionedQueryExecutionInfo = JsonConvert.DeserializeObject<PartitionedQueryExecutionInfo>(error.AdditionalErrorInfo);
			List<PartitionKeyRange> targetRanges = await((DefaultDocumentQueryExecutionContext)innerExecutionContext).GetTargetPartitionKeyRanges(collection.ResourceId, partitionedQueryExecutionInfo.QueryRanges);
			innerExecutionContext = await DocumentQueryExecutionContextFactory.CreateSpecializedDocumentQueryExecutionContext(new DocumentQueryExecutionContextBase.InitParams(client, resourceTypeEnum, resourceType, expression, feedOptions, resourceLink, getLazyFeedResponse: false, correlatedActivityId), partitionedQueryExecutionInfo, targetRanges, collection.ResourceId, isContinuationExpected, token);
			return await innerExecutionContext.ExecuteNextAsync(token);
		}
	}
}
