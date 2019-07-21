using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Query.ExecutionComponent
{
	internal sealed class TakeDocumentQueryExecutionComponent : DocumentQueryExecutionComponentBase
	{
		private enum TakeEnum
		{
			Limit,
			Top
		}

		private abstract class TakeContinuationToken
		{
		}

		/// <summary>
		/// A LimitContinuationToken is a composition of a source continuation token and how many items we have left to drain from that source.
		/// </summary>
		private sealed class LimitContinuationToken : TakeContinuationToken
		{
			/// <summary>
			/// Gets the limit to the number of document drained for the remainder of the query.
			/// </summary>
			[JsonProperty("limit")]
			public int Limit
			{
				get;
			}

			/// <summary>
			/// Gets the continuation token for the source component of the query.
			/// </summary>
			[JsonProperty("sourceToken")]
			public string SourceToken
			{
				get;
			}

			/// <summary>
			/// Initializes a new instance of the LimitContinuationToken struct.
			/// </summary>
			/// <param name="limit">The limit to the number of document drained for the remainder of the query.</param>
			/// <param name="sourceToken">The continuation token for the source component of the query.</param>
			public LimitContinuationToken(int limit, string sourceToken)
			{
				if (limit < 0)
				{
					throw new ArgumentException(string.Format("{0} must be a non negative number.", "limit"));
				}
				Limit = limit;
				SourceToken = sourceToken;
			}

			/// <summary>
			/// Parses the LimitContinuationToken from it's string form.
			/// </summary>
			/// <param name="value">The string form to parse from.</param>
			/// <returns>The parsed LimitContinuationToken.</returns>
			public static LimitContinuationToken Parse(string value)
			{
				if (!TryParse(value, out LimitContinuationToken LimitContinuationToken))
				{
					throw new BadRequestException($"Invalid LimitContinuationToken: {value}");
				}
				return LimitContinuationToken;
			}

			/// <summary>
			/// Tries to parse out the LimitContinuationToken.
			/// </summary>
			/// <param name="value">The value to parse from.</param>
			/// <param name="LimitContinuationToken">The result of parsing out the token.</param>
			/// <returns>Whether or not the LimitContinuationToken was successfully parsed out.</returns>
			public static bool TryParse(string value, out LimitContinuationToken LimitContinuationToken)
			{
				LimitContinuationToken = null;
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}
				try
				{
					LimitContinuationToken = JsonConvert.DeserializeObject<LimitContinuationToken>(value);
					return true;
				}
				catch (JsonException ex)
				{
					DefaultTrace.TraceWarning(string.Format(CultureInfo.InvariantCulture, "{0} Invalid continuation token {1} for limit~Component, exception: {2}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), value, ex.Message));
					return false;
				}
			}

			/// <summary>
			/// Gets the string version of the continuation token that can be passed in a response header.
			/// </summary>
			/// <returns>The string version of the continuation token that can be passed in a response header.</returns>
			public override string ToString()
			{
				return JsonConvert.SerializeObject(this);
			}
		}

		/// <summary>
		/// A TopContinuationToken is a composition of a source continuation token and how many items we have left to drain from that source.
		/// </summary>
		private sealed class TopContinuationToken : TakeContinuationToken
		{
			/// <summary>
			/// Gets the limit to the number of document drained for the remainder of the query.
			/// </summary>
			[JsonProperty("top")]
			public int Top
			{
				get;
			}

			/// <summary>
			/// Gets the continuation token for the source component of the query.
			/// </summary>
			[JsonProperty("sourceToken")]
			public string SourceToken
			{
				get;
			}

			/// <summary>
			/// Initializes a new instance of the TopContinuationToken struct.
			/// </summary>
			/// <param name="top">The limit to the number of document drained for the remainder of the query.</param>
			/// <param name="sourceToken">The continuation token for the source component of the query.</param>
			public TopContinuationToken(int top, string sourceToken)
			{
				Top = top;
				SourceToken = sourceToken;
			}

			/// <summary>
			/// Parses the TopContinuationToken from it's string form.
			/// </summary>
			/// <param name="value">The string form to parse from.</param>
			/// <returns>The parsed TopContinuationToken.</returns>
			public static TopContinuationToken Parse(string value)
			{
				if (!TryParse(value, out TopContinuationToken topContinuationToken))
				{
					throw new BadRequestException($"Invalid TopContinuationToken: {value}");
				}
				return topContinuationToken;
			}

			/// <summary>
			/// Tries to parse out the TopContinuationToken.
			/// </summary>
			/// <param name="value">The value to parse from.</param>
			/// <param name="topContinuationToken">The result of parsing out the token.</param>
			/// <returns>Whether or not the TopContinuationToken was successfully parsed out.</returns>
			public static bool TryParse(string value, out TopContinuationToken topContinuationToken)
			{
				topContinuationToken = null;
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}
				try
				{
					topContinuationToken = JsonConvert.DeserializeObject<TopContinuationToken>(value);
					return true;
				}
				catch (JsonException ex)
				{
					DefaultTrace.TraceWarning(string.Format(CultureInfo.InvariantCulture, "{0} Invalid continuation token {1} for Top~Component, exception: {2}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), value, ex.Message));
					return false;
				}
			}

			/// <summary>
			/// Gets the string version of the continuation token that can be passed in a response header.
			/// </summary>
			/// <returns>The string version of the continuation token that can be passed in a response header.</returns>
			public override string ToString()
			{
				return JsonConvert.SerializeObject(this);
			}
		}

		private readonly TakeEnum takeEnum;

		private int takeCount;

		public override bool IsDone
		{
			get
			{
				if (!Source.IsDone)
				{
					return takeCount <= 0;
				}
				return true;
			}
		}

		private TakeDocumentQueryExecutionComponent(IDocumentQueryExecutionComponent source, int takeCount, TakeEnum takeEnum)
			: base(source)
		{
			if (takeCount < 0)
			{
				throw new ArgumentException(string.Format("{0} must be a non negative number.", "takeCount"));
			}
			this.takeCount = takeCount;
			this.takeEnum = takeEnum;
		}

		public static async Task<TakeDocumentQueryExecutionComponent> CreateLimitDocumentQueryExecutionComponentAsync(int limitCount, string continuationToken, Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback)
		{
			LimitContinuationToken limitContinuationToken = (continuationToken == null) ? new LimitContinuationToken(limitCount, null) : LimitContinuationToken.Parse(continuationToken);
			if (limitContinuationToken.Limit > limitCount)
			{
				throw new BadRequestException($"limit count in continuation token: {limitContinuationToken.Limit} can not be greater than the limit count in the query: {limitCount}.");
			}
			return new TakeDocumentQueryExecutionComponent(await createSourceCallback(limitContinuationToken.SourceToken), limitContinuationToken.Limit, TakeEnum.Limit);
		}

		public static async Task<TakeDocumentQueryExecutionComponent> CreateTopDocumentQueryExecutionComponentAsync(int topCount, string continuationToken, Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback)
		{
			TopContinuationToken topContinuationToken = (continuationToken == null) ? new TopContinuationToken(topCount, null) : TopContinuationToken.Parse(continuationToken);
			if (topContinuationToken.Top > topCount)
			{
				throw new BadRequestException($"top count in continuation token: {topContinuationToken.Top} can not be greater than the top count in the query: {topCount}.");
			}
			return new TakeDocumentQueryExecutionComponent(await createSourceCallback(topContinuationToken.SourceToken), topContinuationToken.Top, TakeEnum.Top);
		}

		public override async Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token)
		{
			FeedResponse<object> feedResponse = await base.DrainAsync(maxElements, token);
			List<object> list = feedResponse.Take(takeCount).ToList();
			feedResponse = new FeedResponse<object>(list, list.Count, feedResponse.Headers, feedResponse.UseETagAsContinuation, feedResponse.QueryMetrics, feedResponse.RequestStatistics, feedResponse.DisallowContinuationTokenMessage, feedResponse.ResponseLengthBytes);
			takeCount -= list.Count;
			if (feedResponse.DisallowContinuationTokenMessage == null)
			{
				if (!IsDone)
				{
					string responseContinuation = feedResponse.ResponseContinuation;
					TakeContinuationToken takeContinuationToken;
					switch (takeEnum)
					{
					case TakeEnum.Limit:
						takeContinuationToken = new LimitContinuationToken(takeCount, responseContinuation);
						break;
					case TakeEnum.Top:
						takeContinuationToken = new TopContinuationToken(takeCount, responseContinuation);
						break;
					default:
						throw new ArgumentException(string.Format("Unknown {0}: {1}", "TakeEnum", takeEnum));
					}
					feedResponse.ResponseContinuation = takeContinuationToken.ToString();
				}
				else
				{
					feedResponse.ResponseContinuation = null;
				}
			}
			return feedResponse;
		}
	}
}
