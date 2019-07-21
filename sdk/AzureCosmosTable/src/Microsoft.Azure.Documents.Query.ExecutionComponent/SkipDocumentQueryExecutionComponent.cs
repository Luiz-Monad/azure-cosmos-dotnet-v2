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
	internal sealed class SkipDocumentQueryExecutionComponent : DocumentQueryExecutionComponentBase
	{
		/// <summary>
		/// A OffsetContinuationToken is a composition of a source continuation token and how many items to skip from that source.
		/// </summary>
		private struct OffsetContinuationToken
		{
			/// <summary>
			/// The number of items to skip in the query.
			/// </summary>
			[JsonProperty("offset")]
			public int Offset
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
			/// Initializes a new instance of the OffsetContinuationToken struct.
			/// </summary>
			/// <param name="offset">The number of items to skip in the query.</param>
			/// <param name="sourceToken">The continuation token for the source component of the query.</param>
			public OffsetContinuationToken(int offset, string sourceToken)
			{
				if (offset < 0)
				{
					throw new ArgumentException(string.Format("{0} must be a non negative number.", "offset"));
				}
				Offset = offset;
				SourceToken = sourceToken;
			}

			/// <summary>
			/// Parses the OffsetContinuationToken from it's string form.
			/// </summary>
			/// <param name="value">The string form to parse from.</param>
			/// <returns>The parsed OffsetContinuationToken.</returns>
			public static OffsetContinuationToken Parse(string value)
			{
				if (!TryParse(value, out OffsetContinuationToken offsetContinuationToken))
				{
					throw new BadRequestException($"Invalid OffsetContinuationToken: {value}");
				}
				return offsetContinuationToken;
			}

			/// <summary>
			/// Tries to parse out the OffsetContinuationToken.
			/// </summary>
			/// <param name="value">The value to parse from.</param>
			/// <param name="offsetContinuationToken">The result of parsing out the token.</param>
			/// <returns>Whether or not the LimitContinuationToken was successfully parsed out.</returns>
			public static bool TryParse(string value, out OffsetContinuationToken offsetContinuationToken)
			{
				offsetContinuationToken = default(OffsetContinuationToken);
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}
				try
				{
					offsetContinuationToken = JsonConvert.DeserializeObject<OffsetContinuationToken>(value);
					return true;
				}
				catch (JsonException arg)
				{
					DefaultTrace.TraceWarning(string.Format("{0} Invalid continuation token {1} for offset~Component, exception: {2}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), value, arg));
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

		private int skipCount;

		public override bool IsDone => Source.IsDone;

		private SkipDocumentQueryExecutionComponent(IDocumentQueryExecutionComponent source, int skipCount)
			: base(source)
		{
			this.skipCount = skipCount;
		}

		public static async Task<SkipDocumentQueryExecutionComponent> CreateAsync(int offsetCount, string continuationToken, Func<string, Task<IDocumentQueryExecutionComponent>> createSourceCallback)
		{
			OffsetContinuationToken offsetContinuationToken = (continuationToken == null) ? new OffsetContinuationToken(offsetCount, null) : OffsetContinuationToken.Parse(continuationToken);
			if (offsetContinuationToken.Offset > offsetCount)
			{
				throw new BadRequestException("offset count in continuation token can not be greater than the offsetcount in the query.");
			}
			return new SkipDocumentQueryExecutionComponent(await createSourceCallback(offsetContinuationToken.SourceToken), offsetContinuationToken.Offset);
		}

		public override async Task<FeedResponse<object>> DrainAsync(int maxElements, CancellationToken token)
		{
			FeedResponse<object> feedResponse = await base.DrainAsync(maxElements, token);
			List<object> list = feedResponse.Skip(skipCount).ToList();
			FeedResponse<object> feedResponse2 = new FeedResponse<object>(list, list.Count(), feedResponse.Headers, feedResponse.UseETagAsContinuation, feedResponse.QueryMetrics, feedResponse.RequestStatistics, feedResponse.DisallowContinuationTokenMessage, feedResponse.ResponseLengthBytes);
			int num = feedResponse.Count - list.Count;
			skipCount -= num;
			if (feedResponse.DisallowContinuationTokenMessage == null)
			{
				if (!IsDone)
				{
					string responseContinuation = feedResponse.ResponseContinuation;
					feedResponse2.ResponseContinuation = new OffsetContinuationToken(skipCount, responseContinuation).ToString();
				}
				else
				{
					feedResponse2.ResponseContinuation = null;
				}
			}
			return feedResponse2;
		}
	}
}
