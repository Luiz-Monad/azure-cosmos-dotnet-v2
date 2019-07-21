using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Collections;
using Microsoft.Azure.Documents.Rntbd;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Instantiated to issue direct connectivity requests to the backend on:
	///     - Gateway (for gateway mode clients)
	///     - Client (for direct mode clients)
	/// StoreClient uses the ReplicatedResourceClient to make requests to the backend.
	/// </summary>
	internal sealed class StoreClient : IStoreClient
	{
		private readonly ISessionContainer sessionContainer;

		private readonly ReplicatedResourceClient replicatedResourceClient;

		private readonly TransportClient transportClient;

		private readonly IServiceConfigurationReader serviceConfigurationReader;

		private readonly bool enableRequestDiagnostics;

		internal JsonSerializerSettings SerializerSettings
		{
			get;
			set;
		}

		public string LastReadAddress
		{
			get
			{
				return replicatedResourceClient.LastReadAddress;
			}
			set
			{
				replicatedResourceClient.LastReadAddress = value;
			}
		}

		public string LastWriteAddress => replicatedResourceClient.LastWriteAddress;

		public bool ForceAddressRefresh
		{
			get
			{
				return replicatedResourceClient.ForceAddressRefresh;
			}
			set
			{
				replicatedResourceClient.ForceAddressRefresh = value;
			}
		}

		public StoreClient(IAddressResolver addressResolver, ISessionContainer sessionContainer, IServiceConfigurationReader serviceConfigurationReader, IAuthorizationTokenProvider userTokenProvider, Protocol protocol, TransportClient transportClient, bool enableRequestDiagnostics = false, bool enableReadRequestsFallback = false, bool useMultipleWriteLocations = false, bool detectClientConnectivityIssues = false)
		{
			this.transportClient = transportClient;
			this.serviceConfigurationReader = serviceConfigurationReader;
			this.sessionContainer = sessionContainer;
			this.enableRequestDiagnostics = enableRequestDiagnostics;
			replicatedResourceClient = new ReplicatedResourceClient(addressResolver, sessionContainer, protocol, this.transportClient, this.serviceConfigurationReader, userTokenProvider, enableReadRequestsFallback, useMultipleWriteLocations, detectClientConnectivityIssues);
		}

		public Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, IRetryPolicy retryPolicy = null, Func<DocumentServiceRequest, Task> prepareRequestAsyncDelegate = null)
		{
			return ProcessMessageAsync(request, default(CancellationToken), retryPolicy, prepareRequestAsyncDelegate);
		}

		public async Task<DocumentServiceResponse> ProcessMessageAsync(DocumentServiceRequest request, CancellationToken cancellationToken, IRetryPolicy retryPolicy = null, Func<DocumentServiceRequest, Task> prepareRequestAsyncDelegate = null)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			await request.EnsureBufferedBodyAsync();
			StoreResponse storeResponse;
			try
			{
				Func<Task<StoreResponse>> func = () => replicatedResourceClient.InvokeAsync(request, prepareRequestAsyncDelegate, cancellationToken);
				storeResponse = ((retryPolicy == null) ? (await func()) : (await BackoffRetryUtility<StoreResponse>.ExecuteAsync(func, retryPolicy, cancellationToken)));
				if (request.IsValidStatusCodeForExceptionlessRetry(storeResponse.Status, storeResponse.SubStatusCode))
				{
					INameValueCollection headersFromStoreResponse = GetHeadersFromStoreResponse(storeResponse);
					HandleUnsuccessfulStoreResponse(request, (HttpStatusCode)storeResponse.Status, storeResponse.SubStatusCode, headersFromStoreResponse);
				}
			}
			catch (DocumentClientException ex)
			{
				if (request.RequestContext.ClientRequestStatistics != null)
				{
					ex.RequestStatistics = request.RequestContext.ClientRequestStatistics;
				}
				HandleUnsuccessfulStoreResponse(request, ex.StatusCode, ex.GetSubStatus(), ex.Headers);
				throw;
			}
			return CompleteResponse(storeResponse, request);
		}

		private void HandleUnsuccessfulStoreResponse(DocumentServiceRequest request, HttpStatusCode? statusCode, SubStatusCodes subStatusCode, INameValueCollection responseHeaders)
		{
			UpdateResponseHeader(request, responseHeaders);
			if (!ReplicatedResourceClient.IsMasterResource(request.ResourceType) && (statusCode == HttpStatusCode.PreconditionFailed || statusCode == HttpStatusCode.Conflict || (statusCode == HttpStatusCode.NotFound && subStatusCode != SubStatusCodes.PartitionKeyRangeGone)))
			{
				CaptureSessionToken(request, responseHeaders);
			}
		}

		private DocumentServiceResponse CompleteResponse(StoreResponse storeResponse, DocumentServiceRequest request)
		{
			if (storeResponse.ResponseHeaderNames.Length != storeResponse.ResponseHeaderValues.Length)
			{
				throw new InternalServerErrorException(RMResources.InvalidBackendResponse);
			}
			INameValueCollection nameValueCollection = new StringKeyValueCollection();
			for (int i = 0; i < storeResponse.ResponseHeaderNames.Length; i++)
			{
				string key = storeResponse.ResponseHeaderNames[i];
				string value = storeResponse.ResponseHeaderValues[i];
				nameValueCollection.Add(key, value);
			}
			UpdateResponseHeader(request, nameValueCollection);
			CaptureSessionToken(request, nameValueCollection);
			return new DocumentServiceResponse(storeResponse.ResponseBody, nameValueCollection, (HttpStatusCode)storeResponse.Status, enableRequestDiagnostics ? request.RequestContext.ClientRequestStatistics : null, request.SerializerSettings ?? SerializerSettings);
		}

		private long GetLSN(INameValueCollection headers)
		{
			long result = -1L;
			string text = headers["lsn"];
			if (!string.IsNullOrEmpty(text) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result;
			}
			return -1L;
		}

		private void UpdateResponseHeader(DocumentServiceRequest request, INameValueCollection headers)
		{
			string text = request.Headers["x-ms-consistency-level"];
			bool num = serviceConfigurationReader.DefaultConsistencyLevel == ConsistencyLevel.Session || (!string.IsNullOrEmpty(text) && string.Equals(text, ConsistencyLevel.Session.ToString(), StringComparison.OrdinalIgnoreCase));
			long lSN = GetLSN(headers);
			if (lSN == -1)
			{
				return;
			}
			string text2 = request.Headers["x-ms-version"];
			text2 = (string.IsNullOrEmpty(text2) ? HttpConstants.Versions.CurrentVersion : text2);
			if (string.Compare(text2, HttpConstants.Versions.v2015_12_16, StringComparison.Ordinal) < 0)
			{
				headers["x-ms-session-token"] = string.Format(CultureInfo.InvariantCulture, "{0}", lSN);
			}
			else
			{
				string text3 = headers["x-ms-documentdb-partitionkeyrangeid"];
				if (string.IsNullOrEmpty(text3))
				{
					string text4 = request.Headers["x-ms-session-token"];
					text3 = ((string.IsNullOrEmpty(text4) || text4.IndexOf(":", StringComparison.Ordinal) < 1) ? "0" : text4.Substring(0, text4.IndexOf(":", StringComparison.Ordinal)));
				}
				ISessionToken sessionToken = null;
				string text5 = headers["x-ms-session-token"];
				if (!string.IsNullOrEmpty(text5))
				{
					sessionToken = SessionTokenHelper.Parse(text5);
				}
				else if (!VersionUtility.IsLaterThan(text2, HttpConstants.Versions.v2018_06_18))
				{
					sessionToken = new SimpleSessionToken(lSN);
				}
				if (sessionToken != null)
				{
					headers["x-ms-session-token"] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", text3, sessionToken.ConvertToString());
				}
			}
			headers.Remove("x-ms-documentdb-partitionkeyrangeid");
		}

		private void CaptureSessionToken(DocumentServiceRequest request, INameValueCollection headers)
		{
			if (request.ResourceType == ResourceType.Collection && request.OperationType == OperationType.Delete)
			{
				string resourceId = (!request.IsNameBased) ? request.ResourceId : headers["x-ms-content-path"];
				sessionContainer.ClearTokenByResourceId(resourceId);
			}
			else
			{
				sessionContainer.SetSessionToken(request, headers);
			}
		}

		private static INameValueCollection GetHeadersFromStoreResponse(StoreResponse storeResponse)
		{
			INameValueCollection nameValueCollection = new StringKeyValueCollection(storeResponse.ResponseHeaderNames.Length);
			for (int i = 0; i < storeResponse.ResponseHeaderNames.Length; i++)
			{
				nameValueCollection.Add(storeResponse.ResponseHeaderNames[i], storeResponse.ResponseHeaderValues[i]);
			}
			return nameValueCollection;
		}

		internal void AddDisableRntbdChannelCallback(Action action)
		{
			Microsoft.Azure.Documents.Rntbd.TransportClient transportClient = this.transportClient as Microsoft.Azure.Documents.Rntbd.TransportClient;
			if (transportClient != null)
			{
				transportClient.OnDisableRntbdChannel += action;
			}
		}
	}
}
