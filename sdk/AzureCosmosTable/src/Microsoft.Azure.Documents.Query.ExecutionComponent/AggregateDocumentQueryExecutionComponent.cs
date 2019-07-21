using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Query.Aggregation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query.ExecutionComponent
{
	/// <summary>
	/// Execution component that is able to aggregate local aggregates from multiple continuations and partitions.
	/// At a high level aggregates queries only return a local aggregate meaning that the value that is returned is only valid for that one continuation (and one partition).
	/// For example suppose you have the query "SELECT Count(1) from c" and you have a single partition collection, 
	/// then you will get one count for each continuation of the query.
	/// If you wanted the true result for this query, then you will have to take the sum of all continuations.
	/// The reason why we have multiple continuations is because for a long running query we have to break up the results into multiple continuations.
	/// Fortunately all the aggregates can be aggregated across continuations and partitions.
	/// </summary>
	internal sealed class AggregateDocumentQueryExecutionComponent : DocumentQueryExecutionComponentBase
	{
		/// <summary>
		/// aggregators[i] is the i'th aggregate in this query execution component.
		/// </summary>
		private readonly IAggregator[] aggregators;

		/// <summary>
		/// Initializes a new instance of the AggregateDocumentQueryExecutionComponent class.
		/// </summary>
		/// <param name="source">The source component that will supply the local aggregates from multiple continuations and partitions.</param>
		/// <param name="aggregateOperators">The aggregate operators for this query.</param>
		/// <remarks>This constructor is private since there is some async initialization that needs to happen in CreateAsync().</remarks>
		private AggregateDocumentQueryExecutionComponent(IDocumentQueryExecutionComponent source, AggregateOperator[] aggregateOperators)
			: base(source)
		{
			aggregators = new IAggregator[aggregateOperators.Length];
			for (int i = 0; i < aggregateOperators.Length; i++)
			{
				switch (aggregateOperators[i])
				{
				case AggregateOperator.Average:
					aggregators[i] = new AverageAggregator();
					break;
				case AggregateOperator.Count:
					aggregators[i] = new CountAggregator();
					break;
				case AggregateOperator.Max:
					aggregators[i] = new MinMaxAggregator(isMinAggregation: false);
					break;
				case AggregateOperator.Min:
					aggregators[i] = new MinMaxAggregator(isMinAggregation: true);
					break;
				case AggregateOperator.Sum:
					aggregators[i] = new SumAggregator();
					break;
				default:
					throw new InvalidProgramException("Unexpected value: " + aggregateOperators[i].ToString());
				}
			}
		}

		/// <summary>
		/// Creates a AggregateDocumentQueryExecutionComponent.
		/// </summary>
		/// <param name="aggregateOperators">The aggregate operators for this query.</param>
		/// <param name="requestContinuation">The continuation token to resume from.</param>
		/// <param name="createSourceCallback">The callback to create the source component that supplies the local aggregates.</param>
		/// <returns>The AggregateDocumentQueryExecutionComponent.</returns>
		public static async Task<AggregateDocumentQueryExecutionComponent> CreateAsync(AggregateOperator[] aggregateOperators, string requestContinuation, Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback)
		{
			return new AggregateDocumentQueryExecutionComponent(await createSourceCallback(requestContinuation), aggregateOperators);
		}

		/// <summary>
		/// Drains at most 'maxElements' documents from the AggregateDocumentQueryExecutionComponent.
		/// </summary>
		/// <param name="maxElements">This value is ignored, since the aggregates are aggregated for you.</param>
		/// <param name="token">The cancellation token.</param>
		/// <returns>The aggregate result after all the continuations have been followed.</returns>
		/// <remarks>
		/// Note that this functions follows all continuations meaning that it won't return until all continuations are drained.
		/// This means that if you have a long running query this function will take a very long time to return.
		/// </remarks>
		public override async Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token)
		{
			double requestCharge = 0.0;
			long responseLengthBytes = 0L;
			List<Uri> replicaUris = new List<Uri>();
			ClientSideRequestStatistics requestStatistics = new ClientSideRequestStatistics();
			PartitionedQueryMetrics partitionedQueryMetrics = new PartitionedQueryMetrics();
			while (!IsDone)
			{
				FeedResponse<object> feedResponse = await base.DrainAsync(int.MaxValue, token);
				requestCharge += feedResponse.RequestCharge;
				responseLengthBytes += feedResponse.ResponseLengthBytes;
				partitionedQueryMetrics += new PartitionedQueryMetrics(feedResponse.QueryMetrics);
				if (feedResponse.RequestStatistics != null)
				{
					replicaUris.AddRange(feedResponse.RequestStatistics.ContactedReplicas);
				}
				foreach (object item in feedResponse)
				{
					AggregateItem[] array = (AggregateItem[])(dynamic)item;
					for (int i = 0; i < aggregators.Length; i++)
					{
						aggregators[i].Aggregate(array[i].GetItem());
					}
				}
			}
			List<object> list = BindAggregateResults((from aggregator in aggregators
			select aggregator.GetResult()).ToArray());
			requestStatistics.ContactedReplicas.AddRange(replicaUris);
			return new FeedResponse<object>(list, list.Count, new StringKeyValueCollection
			{
				{
					"x-ms-request-charge",
					requestCharge.ToString(CultureInfo.InvariantCulture)
				}
			}, useETagAsContinuation: false, partitionedQueryMetrics, requestStatistics, null, responseLengthBytes);
		}

		/// <summary>
		/// Filters out all the aggregate results that are Undefined.
		/// </summary>
		/// <param name="aggregateResults">The result for each aggregator.</param>
		/// <returns>The aggregate results that are not Undefined.</returns>
		private List<object> BindAggregateResults(object[] aggregateResults)
		{
			string message = "Only support binding 1 aggregate function to projection.";
			if (aggregators.Length != 1)
			{
				throw new NotSupportedException(message);
			}
			List<object> list = new List<object>();
			for (int i = 0; i < aggregateResults.Length; i++)
			{
				if (!Undefined.Value.Equals(aggregateResults[i]))
				{
					list.Add(aggregateResults[i]);
				}
			}
			return list;
		}
	}
}
