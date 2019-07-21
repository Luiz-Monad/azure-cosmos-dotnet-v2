using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class ConnectionPool : IDisposable
	{
		private const int MaxPoolFailureRetries = 3;

		private readonly string address;

		private readonly IConnectionDispenser connectionDispenser;

		private ConcurrentStack<IConnection> connections;

		private int ConcurrentConnectionMaxOpen;

		private SemaphoreSlim semaphore;

		private bool isDisposed;

		public ConnectionPool(string address, IConnectionDispenser connectionDispenser, int maxConcurrentConnectionOpenRequests)
		{
			this.address = address;
			this.connectionDispenser = connectionDispenser;
			connections = new ConcurrentStack<IConnection>();
			ConcurrentConnectionMaxOpen = maxConcurrentConnectionOpenRequests;
			semaphore = new SemaphoreSlim(maxConcurrentConnectionOpenRequests);
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
					DisposeAllConnections();
					connections = null;
					semaphore.Dispose();
					semaphore = null;
					DefaultTrace.TraceInformation("Connection Pool Disposed");
				}
				isDisposed = true;
			}
		}

		private void DisposeAllConnections()
		{
			IConnection result;
			while (connections.TryPop(out result))
			{
				result.Close();
			}
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("ConnectionPool");
			}
		}

		public async Task<IConnection> GetOpenConnection(Guid activityId, Uri fullUri, string poolKey)
		{
			ThrowIfDisposed();
			int num = 0;
			while (true)
			{
				if (num > 3)
				{
					DisposeAllConnections();
					throw new GoneException();
				}
				if (!connections.TryPop(out IConnection result))
				{
					break;
				}
				if (result.HasExpired())
				{
					result.Close();
					continue;
				}
				if (!result.ConfirmOpen())
				{
					num++;
					result.Close();
					continue;
				}
				return result;
			}
			try
			{
				if (semaphore.CurrentCount == 0)
				{
					DefaultTrace.TraceWarning("Too Many Concurrent Connections being opened. Current Pending Count: {0}", ConcurrentConnectionMaxOpen);
				}
				await semaphore.WaitAsync();
				return await connectionDispenser.OpenNewConnection(activityId, fullUri, poolKey);
			}
			finally
			{
				semaphore.Release();
			}
		}

		public void ReturnConnection(IConnection connection)
		{
			connections.Push(connection);
		}
	}
}
