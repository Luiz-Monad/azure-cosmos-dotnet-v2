using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class RntbdTransportClient : TransportClient
	{
		private readonly ConnectionPoolManager rntbdConnectionManager;

		public RntbdTransportClient(int requestTimeout, int maxConcurrentConnectionOpenRequests, UserAgentContainer userAgent = null, string overrideHostNameInCertificate = null, int openTimeoutInSeconds = 0, int idleTimeoutInSeconds = 100, int timerPoolGranularityInSeconds = 0)
		{
			rntbdConnectionManager = new ConnectionPoolManager(new RntbdConnectionDispenser(requestTimeout, overrideHostNameInCertificate, openTimeoutInSeconds, idleTimeoutInSeconds, timerPoolGranularityInSeconds, userAgent), maxConcurrentConnectionOpenRequests);
		}

		public override void Dispose()
		{
			base.Dispose();
			if (rntbdConnectionManager != null)
			{
				rntbdConnectionManager.Dispose();
			}
		}

		internal override async Task<StoreResponse> InvokeStoreAsync(Uri physicalAddress, ResourceOperation resourceOperation, DocumentServiceRequest request)
		{
			Guid activityId = Trace.CorrelationManager.ActivityId;
			if (!request.IsBodySeekableClonableAndCountable)
			{
				throw new InternalServerErrorException();
			}
			IConnection connection;
			try
			{
				connection = await rntbdConnectionManager.GetOpenConnection(activityId, physicalAddress);
			}
			catch (Exception ex)
			{
				DefaultTrace.TraceInformation("GetOpenConnection failed: RID: {0}, ResourceType {1}, Op: {2}, Address: {3}, Exception: {4}", request.ResourceAddress, request.ResourceType, resourceOperation, physicalAddress, ex);
				throw;
			}
			StoreResponse storeResponse;
			try
			{
				storeResponse = await connection.RequestAsync(request, physicalAddress, resourceOperation, activityId);
			}
			catch (Exception ex2)
			{
				DefaultTrace.TraceInformation("RequestAsync failed: RID: {0}, ResourceType {1}, Op: {2}, Address: {3}, Exception: {4}", request.ResourceAddress, request.ResourceType, resourceOperation, physicalAddress, ex2);
				connection.Close();
				throw;
			}
			rntbdConnectionManager.ReturnToPool(connection);
			TransportClient.ThrowServerException(request.ResourceAddress, storeResponse, physicalAddress, activityId, request);
			return storeResponse;
		}
	}
}
