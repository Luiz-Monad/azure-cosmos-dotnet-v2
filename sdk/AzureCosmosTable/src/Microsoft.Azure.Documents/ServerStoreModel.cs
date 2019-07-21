using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class ServerStoreModel : IStoreModel, IDisposable
	{
		private readonly StoreClient storeClient;

		private EventHandler<SendingRequestEventArgs> sendingRequest;

		private readonly EventHandler<ReceivedResponseEventArgs> receivedResponse;

		public uint? DefaultReplicaIndex
		{
			get;
			set;
		}

		public string LastReadAddress
		{
			get
			{
				return storeClient.LastReadAddress;
			}
			set
			{
				storeClient.LastReadAddress = value;
			}
		}

		public bool ForceAddressRefresh
		{
			get
			{
				return storeClient.ForceAddressRefresh;
			}
			set
			{
				storeClient.ForceAddressRefresh = value;
			}
		}

		public ServerStoreModel(StoreClient storeClient)
		{
			this.storeClient = storeClient;
		}

		public ServerStoreModel(StoreClient storeClient, EventHandler<SendingRequestEventArgs> sendingRequest, EventHandler<ReceivedResponseEventArgs> receivedResponse)
			: this(storeClient)
		{
			this.sendingRequest = sendingRequest;
			this.receivedResponse = receivedResponse;
		}

		public async Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (DefaultReplicaIndex.HasValue)
			{
				request.DefaultReplicaIndex = DefaultReplicaIndex;
			}
			string text = request.Headers["x-ms-consistency-level"];
			request.RequestContext.OriginalRequestConsistencyLevel = null;
			if (!string.IsNullOrEmpty(text))
			{
				if (!Enum.TryParse(text, out ConsistencyLevel result))
				{
					throw new BadRequestException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidHeaderValue, text, "x-ms-consistency-level"));
				}
				request.RequestContext.OriginalRequestConsistencyLevel = result;
			}
			if (ReplicatedResourceClient.IsMasterResource(request.ResourceType))
			{
				request.Headers["x-ms-consistency-level"] = ConsistencyLevel.Strong.ToString();
			}
			sendingRequest?.Invoke(this, new SendingRequestEventArgs(request));
			DocumentServiceResponse documentServiceResponse = await storeClient.ProcessMessageAsync(request, cancellationToken);
			receivedResponse?.Invoke(this, new ReceivedResponseEventArgs(request, documentServiceResponse));
			return documentServiceResponse;
		}

		public void Dispose()
		{
		}
	}
}
