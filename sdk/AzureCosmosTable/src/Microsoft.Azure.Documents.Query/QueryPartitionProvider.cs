using Microsoft.Azure.Documents.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Azure.Documents.Query
{
	internal sealed class QueryPartitionProvider : IDisposable
	{
		private static readonly int InitialBufferSize = 1024;

		private static readonly uint DISP_E_BUFFERTOOSMALL = 2147614739u;

		private static readonly PartitionedQueryExecutionInfoInternal DefaultInfoInternal = new PartitionedQueryExecutionInfoInternal
		{
			QueryInfo = new QueryInfo(),
			QueryRanges = new List<Range<PartitionKeyInternal>>
			{
				new Range<PartitionKeyInternal>(PartitionKeyInternal.InclusiveMinimum, PartitionKeyInternal.ExclusiveMaximum, isMinInclusive: true, isMaxInclusive: false)
			}
		};

		private readonly object serviceProviderStateLock = new object();

		private IntPtr serviceProvider;

		private bool disposed;

		private string queryengineConfiguration;

		public QueryPartitionProvider(IDictionary<string, object> queryengineConfiguration)
		{
			if (queryengineConfiguration == null)
			{
				throw new ArgumentNullException("queryengineConfiguration");
			}
			if (queryengineConfiguration.Count == 0)
			{
				throw new ArgumentException("queryengineConfiguration cannot be empty!");
			}
			disposed = false;
			this.queryengineConfiguration = JsonConvert.SerializeObject(queryengineConfiguration);
			serviceProvider = IntPtr.Zero;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		public void Update(IDictionary<string, object> queryengineConfiguration)
		{
			if (queryengineConfiguration == null)
			{
				throw new ArgumentNullException("queryengineConfiguration");
			}
			if (queryengineConfiguration.Count == 0)
			{
				throw new ArgumentException("queryengineConfiguration cannot be empty!");
			}
			if (!disposed)
			{
				lock (serviceProviderStateLock)
				{
					this.queryengineConfiguration = JsonConvert.SerializeObject(queryengineConfiguration);
					if (!disposed && serviceProvider != IntPtr.Zero)
					{
						Exception exceptionForHR = Marshal.GetExceptionForHR((int)ServiceInteropWrapper.UpdateServiceProvider(serviceProvider, this.queryengineConfiguration));
						if (exceptionForHR != null)
						{
							throw exceptionForHR;
						}
					}
				}
				return;
			}
			throw new ObjectDisposedException(typeof(QueryPartitionProvider).Name);
		}

		public PartitionedQueryExecutionInfo GetPartitionedQueryExecutionInfo(SqlQuerySpec querySpec, PartitionKeyDefinition partitionKeyDefinition, bool requireFormattableOrderByQuery, bool isContinuationExpected, bool allowNonValueAggregateQuery)
		{
			PartitionedQueryExecutionInfoInternal partitionedQueryExecutionInfoInternal = GetPartitionedQueryExecutionInfoInternal(querySpec, partitionKeyDefinition, requireFormattableOrderByQuery, isContinuationExpected, allowNonValueAggregateQuery);
			return ConvertPartitionedQueryExecutionInfo(partitionedQueryExecutionInfoInternal, partitionKeyDefinition);
		}

		internal PartitionedQueryExecutionInfo ConvertPartitionedQueryExecutionInfo(PartitionedQueryExecutionInfoInternal queryInfoInternal, PartitionKeyDefinition partitionKeyDefinition)
		{
			List<Range<string>> list = new List<Range<string>>(queryInfoInternal.QueryRanges.Count);
			foreach (Range<PartitionKeyInternal> queryRange in queryInfoInternal.QueryRanges)
			{
				list.Add(new Range<string>(queryRange.Min.GetEffectivePartitionKeyString(partitionKeyDefinition, strict: false), queryRange.Max.GetEffectivePartitionKeyString(partitionKeyDefinition, strict: false), queryRange.IsMinInclusive, queryRange.IsMaxInclusive));
			}
			list.Sort(Range<string>.MinComparer.Instance);
			return new PartitionedQueryExecutionInfo
			{
				QueryInfo = queryInfoInternal.QueryInfo,
				QueryRanges = list
			};
		}

		internal unsafe PartitionedQueryExecutionInfoInternal GetPartitionedQueryExecutionInfoInternal(SqlQuerySpec querySpec, PartitionKeyDefinition partitionKeyDefinition, bool requireFormattableOrderByQuery, bool isContinuationExpected, bool allowNonValueAggregateQuery)
		{
			if (querySpec == null || partitionKeyDefinition == null)
			{
				return DefaultInfoInternal;
			}
			string query = JsonConvert.SerializeObject(querySpec);
			List<string> list = new List<string>(partitionKeyDefinition.Paths);
			List<string[]> pathParts = new List<string[]>();
			list.ForEach(delegate(string path)
			{
				pathParts.Add(PathParser.GetPathParts(path).ToArray());
			});
			string[] partitionKeyDefinitionPathTokens = pathParts.SelectMany((string[] parts) => parts).ToArray();
			uint[] partitionKeyDefinitionPathTokenLengths = (from parts in pathParts
			select (uint)parts.Length).ToArray();
			PartitionKind kind = partitionKeyDefinition.Kind;
			Initialize();
			byte[] array = new byte[InitialBufferSize];
			uint partitionKeyRangesFromQuery;
			uint serializedQueryExecutionInfoResultLength;
			fixed (byte* value = array)
			{
				partitionKeyRangesFromQuery = ServiceInteropWrapper.GetPartitionKeyRangesFromQuery(serviceProvider, query, requireFormattableOrderByQuery, isContinuationExpected, allowNonValueAggregateQuery, partitionKeyDefinitionPathTokens, partitionKeyDefinitionPathTokenLengths, (uint)partitionKeyDefinition.Paths.Count, kind, new IntPtr(value), (uint)array.Length, out serializedQueryExecutionInfoResultLength);
				if (partitionKeyRangesFromQuery == DISP_E_BUFFERTOOSMALL)
				{
					array = new byte[serializedQueryExecutionInfoResultLength];
					fixed (byte* value2 = array)
					{
						partitionKeyRangesFromQuery = ServiceInteropWrapper.GetPartitionKeyRangesFromQuery(serviceProvider, query, requireFormattableOrderByQuery, isContinuationExpected, allowNonValueAggregateQuery, partitionKeyDefinitionPathTokens, partitionKeyDefinitionPathTokenLengths, (uint)partitionKeyDefinition.Paths.Count, kind, new IntPtr(value2), (uint)array.Length, out serializedQueryExecutionInfoResultLength);
					}
				}
			}
			string @string = Encoding.UTF8.GetString(array, 0, (int)serializedQueryExecutionInfoResultLength);
			Exception exceptionForHR = Marshal.GetExceptionForHR((int)partitionKeyRangesFromQuery);
			if (exceptionForHR != null)
			{
				DefaultTrace.TraceInformation("QueryEngineConfiguration: " + queryengineConfiguration);
				throw new BadRequestException("Message: " + @string, exceptionForHR);
			}
			return JsonConvert.DeserializeObject<PartitionedQueryExecutionInfoInternal>(@string, new JsonSerializerSettings
			{
				DateParseHandling = DateParseHandling.None
			});
		}

		~QueryPartitionProvider()
		{
			Dispose(disposing: false);
		}

		private void Initialize()
		{
			if (!disposed)
			{
				if (serviceProvider == IntPtr.Zero)
				{
					lock (serviceProviderStateLock)
					{
						if (!disposed && serviceProvider == IntPtr.Zero)
						{
							Exception exceptionForHR = Marshal.GetExceptionForHR((int)ServiceInteropWrapper.CreateServiceProvider(queryengineConfiguration, out serviceProvider));
							if (exceptionForHR != null)
							{
								throw exceptionForHR;
							}
						}
					}
				}
				return;
			}
			throw new ObjectDisposedException(typeof(QueryPartitionProvider).Name);
		}

		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				lock (serviceProviderStateLock)
				{
					if (serviceProvider != IntPtr.Zero)
					{
						Marshal.Release(serviceProvider);
						serviceProvider = IntPtr.Zero;
					}
					disposed = true;
				}
			}
		}
	}
}
