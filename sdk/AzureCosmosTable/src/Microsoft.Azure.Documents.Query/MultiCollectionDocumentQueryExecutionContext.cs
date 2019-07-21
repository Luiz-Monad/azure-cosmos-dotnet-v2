using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query
{
	/// <summary>
	/// This is for routing cross partition queries through the old client side partition collections.
	/// Please ignore.
	/// </summary>
	internal sealed class MultiCollectionDocumentQueryExecutionContext : IDocumentQueryExecutionContext, IDisposable
	{
		private readonly List<IDocumentQueryExecutionContext> childQueryExecutionContexts;

		private int currentChildQueryExecutionContextIndex;

		public bool IsDone => currentChildQueryExecutionContextIndex >= childQueryExecutionContexts.Count();

		public static async Task<IDocumentQueryExecutionContext> CreateAsync(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, Expression expression, FeedOptions feedOptions, IEnumerable<string> documentFeedLinks, bool isContinuationExpected, CancellationToken token, Guid correlatedActivityId)
		{
			if (client == null)
			{
				throw new ArgumentNullException("client");
			}
			if (feedOptions == null)
			{
				throw new ArgumentNullException("feedOptions");
			}
			if (documentFeedLinks == null)
			{
				throw new ArgumentNullException("documentFeedLinks");
			}
			List<IDocumentQueryExecutionContext> childQueryExecutionContexts = new List<IDocumentQueryExecutionContext>();
			foreach (string documentFeedLink in documentFeedLinks)
			{
				List<IDocumentQueryExecutionContext> list = childQueryExecutionContexts;
				list.Add(await DocumentQueryExecutionContextFactory.CreateDocumentQueryExecutionContextAsync(client, resourceTypeEnum, resourceType, expression, feedOptions, documentFeedLink, isContinuationExpected, token, correlatedActivityId));
			}
			return new MultiCollectionDocumentQueryExecutionContext(childQueryExecutionContexts);
		}

		private MultiCollectionDocumentQueryExecutionContext(List<IDocumentQueryExecutionContext> childQueryExecutionContexts)
		{
			if (childQueryExecutionContexts == null)
			{
				throw new ArgumentNullException("childQueryExecutionContexts");
			}
			this.childQueryExecutionContexts = childQueryExecutionContexts;
		}

		public void Dispose()
		{
			foreach (IDocumentQueryExecutionContext childQueryExecutionContext in childQueryExecutionContexts)
			{
				childQueryExecutionContext.Dispose();
			}
		}

		public async Task<FeedResponse<dynamic>> ExecuteNextAsync(CancellationToken token)
		{
			if (IsDone)
			{
				throw new InvalidOperationException(RMResources.DocumentQueryExecutionContextIsDone);
			}
			FeedResponse<object> result = await childQueryExecutionContexts[currentChildQueryExecutionContextIndex].ExecuteNextAsync(token);
			if (childQueryExecutionContexts[currentChildQueryExecutionContextIndex].IsDone)
			{
				currentChildQueryExecutionContextIndex++;
			}
			return result;
		}
	}
}
