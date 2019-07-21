using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Query;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Linq
{
	internal sealed class DocumentQueryProvider : IDocumentQueryProvider, IQueryProvider
	{
		private readonly IDocumentQueryClient client;

		private readonly ResourceType resourceTypeEnum;

		private readonly Type resourceType;

		private readonly string documentsFeedOrDatabaseLink;

		private readonly FeedOptions feedOptions;

		private readonly object partitionKey;

		private readonly Action<IQueryable> onExecuteScalarQueryCallback;

		public DocumentQueryProvider(IDocumentQueryClient client, ResourceType resourceTypeEnum, Type resourceType, string documentsFeedOrDatabaseLink, FeedOptions feedOptions, object partitionKey = null, Action<IQueryable> onExecuteScalarQueryCallback = null)
		{
			this.client = client;
			this.resourceTypeEnum = resourceTypeEnum;
			this.resourceType = resourceType;
			this.documentsFeedOrDatabaseLink = documentsFeedOrDatabaseLink;
			this.feedOptions = feedOptions;
			this.partitionKey = partitionKey;
			this.onExecuteScalarQueryCallback = onExecuteScalarQueryCallback;
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new DocumentQuery<TElement>(client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey);
		}

		public IQueryable CreateQuery(Expression expression)
		{
			Type elementType = TypeSystem.GetElementType(expression.Type);
			return (IQueryable)Activator.CreateInstance(typeof(DocumentQuery<bool>).GetGenericTypeDefinition().MakeGenericType(elementType), client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			DocumentQuery<TResult> documentQuery = (DocumentQuery<TResult>)Activator.CreateInstance(typeof(DocumentQuery<bool>).GetGenericTypeDefinition().MakeGenericType(typeof(TResult)), client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey);
			onExecuteScalarQueryCallback?.Invoke(documentQuery);
			return documentQuery.ToList().FirstOrDefault();
		}

		public object Execute(Expression expression)
		{
			DocumentQuery<object> documentQuery = (DocumentQuery<object>)Activator.CreateInstance(typeof(DocumentQuery<bool>).GetGenericTypeDefinition().MakeGenericType(typeof(object)), client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey);
			onExecuteScalarQueryCallback?.Invoke(documentQuery);
			return documentQuery.ToList().FirstOrDefault();
		}

		public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			DocumentQuery<TResult> documentQuery = (DocumentQuery<TResult>)Activator.CreateInstance(typeof(DocumentQuery<bool>).GetGenericTypeDefinition().MakeGenericType(typeof(TResult)), client, resourceTypeEnum, resourceType, documentsFeedOrDatabaseLink, expression, feedOptions, partitionKey);
			onExecuteScalarQueryCallback?.Invoke(documentQuery);
			return (await documentQuery.ExecuteAllAsync()).FirstOrDefault();
		}
	}
}
