namespace Microsoft.Azure.Documents.Query
{
	internal sealed class ServiceInteropQueryPlanHandler
	{
		private readonly QueryPlanHandler queryPlanHandler;

		private readonly QueryFeatures supportedQueryFeatures;

		public ServiceInteropQueryPlanHandler(QueryPartitionProvider queryPartitionProvider, QueryFeatures supportedQueryFeatures)
		{
			queryPlanHandler = new QueryPlanHandler(queryPartitionProvider);
			this.supportedQueryFeatures = supportedQueryFeatures;
		}

		public PartitionedQueryExecutionInfo GetPlanForQuery(SqlQuerySpec sqlQuerySpec, PartitionKeyDefinition partitionKeyDefinition)
		{
			return queryPlanHandler.GetQueryPlan(sqlQuerySpec, partitionKeyDefinition, supportedQueryFeatures);
		}
	}
}
