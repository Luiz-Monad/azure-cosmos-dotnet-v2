using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents.Routing
{
	internal static class PartitionRoutingHelper
	{
		public struct ResolvedRangeInfo
		{
			public readonly PartitionKeyRange ResolvedRange;

			public readonly List<CompositeContinuationToken> ContinuationTokens;

			public ResolvedRangeInfo(PartitionKeyRange range, List<CompositeContinuationToken> continuationTokens)
			{
				ResolvedRange = range;
				ContinuationTokens = continuationTokens;
			}
		}

		public static IReadOnlyList<Range<string>> GetProvidedPartitionKeyRanges(SqlQuerySpec querySpec, bool enableCrossPartitionQuery, bool parallelizeCrossPartitionQuery, bool isContinuationExpected, PartitionKeyDefinition partitionKeyDefinition, QueryPartitionProvider queryPartitionProvider, string clientApiVersion, out QueryInfo queryInfo)
		{
			if (querySpec == null)
			{
				throw new ArgumentNullException("querySpec");
			}
			if (partitionKeyDefinition == null)
			{
				throw new ArgumentNullException("partitionKeyDefinition");
			}
			if (queryPartitionProvider == null)
			{
				throw new ArgumentNullException("queryPartitionProvider");
			}
			PartitionedQueryExecutionInfo partitionedQueryExecutionInfo = null;
			partitionedQueryExecutionInfo = queryPartitionProvider.GetPartitionedQueryExecutionInfo(querySpec, partitionKeyDefinition, VersionUtility.IsLaterThan(clientApiVersion, HttpConstants.Versions.v2016_11_14), isContinuationExpected, allowNonValueAggregateQuery: false);
			if (partitionedQueryExecutionInfo == null || partitionedQueryExecutionInfo.QueryRanges == null || partitionedQueryExecutionInfo.QueryInfo == null || partitionedQueryExecutionInfo.QueryRanges.Any(delegate(Range<string> range)
			{
				if (range.Min != null)
				{
					return range.Max == null;
				}
				return true;
			}))
			{
				DefaultTrace.TraceInformation("QueryPartitionProvider returned bad query info");
			}
			bool flag = partitionedQueryExecutionInfo.QueryRanges.Count == 1 && partitionedQueryExecutionInfo.QueryRanges[0].IsSingleValue;
			if (partitionKeyDefinition.Paths.Count > 0 && !flag)
			{
				if (!enableCrossPartitionQuery)
				{
					throw new BadRequestException(RMResources.CrossPartitionQueryDisabled);
				}
				if (parallelizeCrossPartitionQuery || partitionedQueryExecutionInfo.QueryInfo.HasTop || partitionedQueryExecutionInfo.QueryInfo.HasOrderBy || partitionedQueryExecutionInfo.QueryInfo.HasAggregates || partitionedQueryExecutionInfo.QueryInfo.HasDistinct || partitionedQueryExecutionInfo.QueryInfo.HasOffset || partitionedQueryExecutionInfo.QueryInfo.HasLimit || partitionedQueryExecutionInfo.QueryInfo.HasGroupBy)
				{
					if (!IsSupportedPartitionedQueryExecutionInfo(partitionedQueryExecutionInfo, clientApiVersion))
					{
						throw new BadRequestException(RMResources.UnsupportedCrossPartitionQuery);
					}
					if (partitionedQueryExecutionInfo.QueryInfo.HasAggregates && !IsAggregateSupportedApiVersion(clientApiVersion))
					{
						throw new BadRequestException(RMResources.UnsupportedCrossPartitionQueryWithAggregate);
					}
					DocumentClientException ex = new DocumentClientException(RMResources.UnsupportedCrossPartitionQuery, HttpStatusCode.BadRequest, SubStatusCodes.CrossPartitionQueryNotServable);
					ex.Error.AdditionalErrorInfo = JsonConvert.SerializeObject(partitionedQueryExecutionInfo);
					throw ex;
				}
			}
			else
			{
				if (partitionedQueryExecutionInfo.QueryInfo.HasAggregates && !isContinuationExpected)
				{
					if (IsAggregateSupportedApiVersion(clientApiVersion))
					{
						DocumentClientException ex2 = new DocumentClientException(RMResources.UnsupportedQueryWithFullResultAggregate, HttpStatusCode.BadRequest, SubStatusCodes.CrossPartitionQueryNotServable);
						ex2.Error.AdditionalErrorInfo = JsonConvert.SerializeObject(partitionedQueryExecutionInfo);
						throw ex2;
					}
					throw new BadRequestException(RMResources.UnsupportedQueryWithFullResultAggregate);
				}
				if (partitionedQueryExecutionInfo.QueryInfo.HasDistinct)
				{
					DocumentClientException ex3 = new DocumentClientException(RMResources.UnsupportedCrossPartitionQuery, HttpStatusCode.BadRequest, SubStatusCodes.CrossPartitionQueryNotServable);
					ex3.Error.AdditionalErrorInfo = JsonConvert.SerializeObject(partitionedQueryExecutionInfo);
					throw ex3;
				}
				if (partitionedQueryExecutionInfo.QueryInfo.HasGroupBy)
				{
					throw new DocumentClientException(RMResources.UnsupportedCrossPartitionQuery, HttpStatusCode.BadRequest, SubStatusCodes.Unknown);
				}
			}
			queryInfo = partitionedQueryExecutionInfo.QueryInfo;
			return partitionedQueryExecutionInfo.QueryRanges;
		}

		/// <summary>
		/// Gets <see cref="T:Microsoft.Azure.Documents.PartitionKeyRange" /> instance which corresponds to <paramref name="rangeFromContinuationToken" />
		/// </summary>
		/// <param name="providedPartitionKeyRanges"></param>
		/// <param name="routingMapProvider"></param>
		/// <param name="collectionRid"></param>
		/// <param name="rangeFromContinuationToken"></param>
		/// <param name="suppliedTokens"></param>
		/// <param name="direction"></param>
		/// <returns>null if collection with specified <paramref name="collectionRid" /> doesn't exist, which potentially means
		/// that collection was resolved to outdated Rid by name. Also null can be returned if <paramref name="rangeFromContinuationToken" />
		/// is not found - this means it was split.
		/// </returns>
		public static async Task<ResolvedRangeInfo> TryGetTargetRangeFromContinuationTokenRange(IReadOnlyList<Range<string>> providedPartitionKeyRanges, IRoutingMapProvider routingMapProvider, string collectionRid, Range<string> rangeFromContinuationToken, List<CompositeContinuationToken> suppliedTokens, RntbdConstants.RntdbEnumerationDirection direction = RntbdConstants.RntdbEnumerationDirection.Forward)
		{
			if (providedPartitionKeyRanges.Count == 0)
			{
				return new ResolvedRangeInfo(await routingMapProvider.TryGetRangeByEffectivePartitionKey(collectionRid, PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey), suppliedTokens);
			}
			if (rangeFromContinuationToken.IsEmpty)
			{
				if (direction == RntbdConstants.RntdbEnumerationDirection.Reverse)
				{
					IReadOnlyList<PartitionKeyRange> obj = await routingMapProvider.TryGetOverlappingRangesAsync(collectionRid, providedPartitionKeyRanges.Single());
					return new ResolvedRangeInfo(obj[obj.Count - 1], suppliedTokens);
				}
				return new ResolvedRangeInfo(await routingMapProvider.TryGetRangeByEffectivePartitionKey(collectionRid, Min(providedPartitionKeyRanges, Range<string>.MinComparer.Instance).Min), suppliedTokens);
			}
			PartitionKeyRange partitionKeyRange = await routingMapProvider.TryGetRangeByEffectivePartitionKey(collectionRid, rangeFromContinuationToken.Min);
			if (partitionKeyRange == null)
			{
				return new ResolvedRangeInfo(null, suppliedTokens);
			}
			if (!rangeFromContinuationToken.Equals(partitionKeyRange.ToRange()))
			{
				List<PartitionKeyRange> list = (await routingMapProvider.TryGetOverlappingRangesAsync(collectionRid, rangeFromContinuationToken, forceRefresh: true)).ToList();
				if (list == null || list.Count < 1)
				{
					return new ResolvedRangeInfo(null, null);
				}
				if (!list[0].MinInclusive.Equals(rangeFromContinuationToken.Min) || !list[list.Count - 1].MaxExclusive.Equals(rangeFromContinuationToken.Max))
				{
					return new ResolvedRangeInfo(null, null);
				}
				if (direction == RntbdConstants.RntdbEnumerationDirection.Reverse)
				{
					list.Reverse();
				}
				List<CompositeContinuationToken> list2 = null;
				if (suppliedTokens != null && suppliedTokens.Count > 0)
				{
					list2 = new List<CompositeContinuationToken>(list.Count + suppliedTokens.Count - 1);
					foreach (PartitionKeyRange item in list)
					{
						CompositeContinuationToken compositeContinuationToken = (CompositeContinuationToken)suppliedTokens[0].ShallowCopy();
						compositeContinuationToken.Range = item.ToRange();
						list2.Add(compositeContinuationToken);
					}
					list2.AddRange(suppliedTokens.Skip(1));
				}
				return new ResolvedRangeInfo(list[0], list2);
			}
			return new ResolvedRangeInfo(partitionKeyRange, suppliedTokens);
		}

		public static async Task<List<PartitionKeyRange>> GetReplacementRanges(PartitionKeyRange targetRange, IRoutingMapProvider routingMapProvider, string collectionRid)
		{
			return (await routingMapProvider.TryGetOverlappingRangesAsync(collectionRid, targetRange.ToRange(), forceRefresh: true)).ToList();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns><c>false</c> if collectionRid is likely wrong because range was not found. Cache needs to be refreshed probably.</returns>
		public static async Task<bool> TryAddPartitionKeyRangeToContinuationTokenAsync(INameValueCollection backendResponseHeaders, IReadOnlyList<Range<string>> providedPartitionKeyRanges, IRoutingMapProvider routingMapProvider, string collectionRid, ResolvedRangeInfo resolvedRangeInfo, RntbdConstants.RntdbEnumerationDirection direction = RntbdConstants.RntdbEnumerationDirection.Forward)
		{
			PartitionKeyRange currentRange = resolvedRangeInfo.ResolvedRange;
			if (resolvedRangeInfo.ContinuationTokens != null && resolvedRangeInfo.ContinuationTokens.Count > 1)
			{
				if (!string.IsNullOrEmpty(backendResponseHeaders["x-ms-continuation"]))
				{
					resolvedRangeInfo.ContinuationTokens[0].Token = backendResponseHeaders["x-ms-continuation"];
				}
				else
				{
					resolvedRangeInfo.ContinuationTokens.RemoveAt(0);
				}
				backendResponseHeaders["x-ms-continuation"] = JsonConvert.SerializeObject(resolvedRangeInfo.ContinuationTokens);
			}
			else
			{
				PartitionKeyRange partitionKeyRange = currentRange;
				if (string.IsNullOrEmpty(backendResponseHeaders["x-ms-continuation"]))
				{
					if (direction == RntbdConstants.RntdbEnumerationDirection.Reverse)
					{
						partitionKeyRange = MinBefore((await routingMapProvider.TryGetOverlappingRangesAsync(collectionRid, providedPartitionKeyRanges.Single())).ToList(), currentRange);
					}
					else
					{
						Range<string> range = MinAfter(providedPartitionKeyRanges, currentRange.ToRange(), Range<string>.MaxComparer.Instance);
						if (range == null)
						{
							return true;
						}
						string text = (string.CompareOrdinal(range.Min, currentRange.MaxExclusive) > 0) ? range.Min : currentRange.MaxExclusive;
						if (string.CompareOrdinal(text, PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey) == 0)
						{
							return true;
						}
						PartitionKeyRange partitionKeyRange2 = await routingMapProvider.TryGetRangeByEffectivePartitionKey(collectionRid, text);
						if (partitionKeyRange2 == null)
						{
							return false;
						}
						partitionKeyRange = partitionKeyRange2;
					}
				}
				if (partitionKeyRange != null)
				{
					backendResponseHeaders["x-ms-continuation"] = AddPartitionKeyRangeToContinuationToken(backendResponseHeaders["x-ms-continuation"], partitionKeyRange);
				}
			}
			return true;
		}

		public static Range<string> ExtractPartitionKeyRangeFromContinuationToken(INameValueCollection headers, out List<CompositeContinuationToken> compositeContinuationTokens)
		{
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			compositeContinuationTokens = null;
			Range<string> result = Range<string>.GetEmptyRange(PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey);
			if (string.IsNullOrEmpty(headers["x-ms-continuation"]))
			{
				return result;
			}
			string text = headers["x-ms-continuation"];
			CompositeContinuationToken compositeContinuationToken = null;
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					if (text.Trim().StartsWith("[", StringComparison.Ordinal))
					{
						compositeContinuationTokens = JsonConvert.DeserializeObject<List<CompositeContinuationToken>>(text);
						if (compositeContinuationTokens != null && compositeContinuationTokens.Count > 0)
						{
							headers["x-ms-continuation"] = compositeContinuationTokens[0].Token;
							compositeContinuationToken = compositeContinuationTokens[0];
						}
						else
						{
							headers.Remove("x-ms-continuation");
						}
					}
					else
					{
						compositeContinuationToken = JsonConvert.DeserializeObject<CompositeContinuationToken>(text);
						if (compositeContinuationToken == null)
						{
							throw new BadRequestException(RMResources.InvalidContinuationToken);
						}
						compositeContinuationTokens = new List<CompositeContinuationToken>
						{
							compositeContinuationToken
						};
					}
					if (compositeContinuationToken != null && compositeContinuationToken.Range != null)
					{
						result = compositeContinuationToken.Range;
					}
					if (compositeContinuationToken != null && !string.IsNullOrEmpty(compositeContinuationToken.Token))
					{
						headers["x-ms-continuation"] = compositeContinuationToken.Token;
						return result;
					}
					headers.Remove("x-ms-continuation");
					return result;
				}
				catch (JsonException innerException)
				{
					DefaultTrace.TraceWarning(string.Format(CultureInfo.InvariantCulture, "{0} Invalid JSON in the continuation token {1}", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), text));
					throw new BadRequestException(RMResources.InvalidContinuationToken, innerException);
				}
			}
			headers.Remove("x-ms-continuation");
			return result;
		}

		private static string AddPartitionKeyRangeToContinuationToken(string continuationToken, PartitionKeyRange partitionKeyRange)
		{
			return JsonConvert.SerializeObject(new CompositeContinuationToken
			{
				Token = continuationToken,
				Range = partitionKeyRange.ToRange()
			});
		}

		private static bool IsSupportedPartitionedQueryExecutionInfo(PartitionedQueryExecutionInfo partitionedQueryExecutionInfoueryInfo, string clientApiVersion)
		{
			if (VersionUtility.IsLaterThan(clientApiVersion, HttpConstants.Versions.v2016_07_11))
			{
				return partitionedQueryExecutionInfoueryInfo.Version <= 2;
			}
			return false;
		}

		private static bool IsAggregateSupportedApiVersion(string clientApiVersion)
		{
			return VersionUtility.IsLaterThan(clientApiVersion, HttpConstants.Versions.v2016_11_14);
		}

		private static T Min<T>(IReadOnlyList<T> values, IComparer<T> comparer)
		{
			if (values.Count == 0)
			{
				throw new ArgumentException("values");
			}
			T val = values[0];
			for (int i = 1; i < values.Count; i++)
			{
				if (comparer.Compare(values[i], val) < 0)
				{
					val = values[i];
				}
			}
			return val;
		}

		private static T MinAfter<T>(IReadOnlyList<T> values, T minValue, IComparer<T> comparer) where T : class
		{
			if (values.Count == 0)
			{
				throw new ArgumentException("values");
			}
			T val = null;
			foreach (T value in values)
			{
				if (comparer.Compare(value, minValue) > 0 && (val == null || comparer.Compare(value, val) < 0))
				{
					val = value;
				}
			}
			return val;
		}

		private static PartitionKeyRange MinBefore(IReadOnlyList<PartitionKeyRange> values, PartitionKeyRange minValue)
		{
			if (values.Count == 0)
			{
				throw new ArgumentException("values");
			}
			IComparer<Range<string>> instance = Range<string>.MinComparer.Instance;
			PartitionKeyRange partitionKeyRange = null;
			foreach (PartitionKeyRange value in values)
			{
				if (instance.Compare(value.ToRange(), minValue.ToRange()) < 0 && (partitionKeyRange == null || instance.Compare(value.ToRange(), partitionKeyRange.ToRange()) > 0))
				{
					partitionKeyRange = value;
				}
			}
			return partitionKeyRange;
		}
	}
}
