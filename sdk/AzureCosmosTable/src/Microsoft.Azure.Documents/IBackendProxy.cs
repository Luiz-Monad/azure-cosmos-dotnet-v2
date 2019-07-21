using Microsoft.Azure.Documents.Collections;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal interface IBackendProxy : IDisposable
	{
		IServiceConfigurationReader ConfigurationReader
		{
			get;
		}

		Task StartAsync();

		Task<DocumentServiceResponse> CreateAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> UpsertAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> ReadAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> ReplaceAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> DeleteAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> ExecuteAsync(DocumentServiceRequest request);

		Task<DocumentServiceResponse> ReadFeedAsync(DocumentServiceRequest request, ReadType readType);

		Task UploadMediaAsync(string mediaId, Stream mediaStream, INameValueCollection headers, int singleBlobUploadThresholdInBytes, TimeSpan blobUploadTimeoutSeconds);

		Task<Tuple<INameValueCollection, INameValueCollection>> HeadMediaAsync(string mediaId, INameValueCollection headers = null);

		Task<Tuple<Stream, INameValueCollection, INameValueCollection>> DownloadMediaAsync(string mediaId, INameValueCollection headers, TimeSpan blobDownloadTimeoutSeconds);

		Task DeleteMediaAsync(string mediaId, INameValueCollection headers = null);
	}
}
