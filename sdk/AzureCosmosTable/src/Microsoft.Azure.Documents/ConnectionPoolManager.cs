using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class ConnectionPoolManager : IDisposable
	{
		private ConcurrentDictionary<string, ConnectionPool> connectionPools;

		private readonly IConnectionDispenser connectionDispenser;

		private int maxConcurrentConnectionOpenRequests;

		private bool isDisposed;

		public ConnectionPoolManager(IConnectionDispenser connectionDispenser, int maxConcurrentConnectionOpenRequests)
		{
			connectionPools = new ConcurrentDictionary<string, ConnectionPool>();
			this.connectionDispenser = connectionDispenser;
			this.maxConcurrentConnectionOpenRequests = maxConcurrentConnectionOpenRequests;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					foreach (KeyValuePair<string, ConnectionPool> connectionPool in connectionPools)
					{
						connectionPool.Value.Dispose();
					}
					connectionPools = null;
					((IDisposable)connectionDispenser).Dispose();
				}
				isDisposed = true;
			}
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("ConnectionPoolManager");
			}
		}

		public Task<IConnection> GetOpenConnection(Guid activityId, Uri fullUri)
		{
			ThrowIfDisposed();
			string poolKey = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", fullUri.Host, fullUri.Port);
			return GetConnectionPool(poolKey).GetOpenConnection(activityId, fullUri, poolKey);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Disposable object returned by method")]
		private ConnectionPool GetConnectionPool(string poolKey)
		{
			ConnectionPool value = null;
			if (!connectionPools.TryGetValue(poolKey, out value))
			{
				value = new ConnectionPool(poolKey, connectionDispenser, maxConcurrentConnectionOpenRequests);
				value = connectionPools.GetOrAdd(poolKey, value);
			}
			return value;
		}

		public void ReturnToPool(IConnection connection)
		{
			if (!connectionPools.TryGetValue(connection.PoolKey, out ConnectionPool value))
			{
				connection.Close();
			}
			else
			{
				value.ReturnConnection(connection);
			}
		}
	}
}
