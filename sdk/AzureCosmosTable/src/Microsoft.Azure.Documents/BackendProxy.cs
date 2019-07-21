using Microsoft.Azure.Documents.Collections;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal abstract class BackendProxy : IBackendProxy, IDisposable
	{
		private readonly IMediaHandler mediaHandler;

		private readonly IServiceConfigurationReader serviceConfigurationReader;

		public DatabaseAccount DocumentService
		{
			get
			{
				DatabaseAccount databaseAccount = new DatabaseAccount();
				databaseAccount.ReplicationPolicy.AsyncReplication = serviceConfigurationReader.UserReplicationPolicy.AsyncReplication;
				databaseAccount.ReplicationPolicy.MinReplicaSetSize = serviceConfigurationReader.UserReplicationPolicy.MinReplicaSetSize;
				databaseAccount.ReplicationPolicy.MaxReplicaSetSize = serviceConfigurationReader.UserReplicationPolicy.MaxReplicaSetSize;
				databaseAccount.ConsistencyPolicy.DefaultConsistencyLevel = serviceConfigurationReader.DefaultConsistencyLevel;
				databaseAccount.SystemReplicationPolicy.AsyncReplication = serviceConfigurationReader.SystemReplicationPolicy.AsyncReplication;
				databaseAccount.SystemReplicationPolicy.MinReplicaSetSize = serviceConfigurationReader.SystemReplicationPolicy.MinReplicaSetSize;
				databaseAccount.SystemReplicationPolicy.MaxReplicaSetSize = serviceConfigurationReader.SystemReplicationPolicy.MaxReplicaSetSize;
				databaseAccount.ReadPolicy.PrimaryReadCoefficient = serviceConfigurationReader.ReadPolicy.PrimaryReadCoefficient;
				databaseAccount.ReadPolicy.SecondaryReadCoefficient = serviceConfigurationReader.ReadPolicy.SecondaryReadCoefficient;
				return databaseAccount;
			}
		}

		public IServiceConfigurationReader ConfigurationReader => serviceConfigurationReader;

		protected BackendProxy(IServiceConfigurationReader serviceConfigurationReader, IMediaHandler mediaHandler)
		{
			this.mediaHandler = mediaHandler;
			this.serviceConfigurationReader = serviceConfigurationReader;
		}

		public abstract void Dispose();

		public abstract Task StartAsync();

		public abstract Task<DocumentServiceResponse> CreateAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> UpsertAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> ReadAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> ReplaceAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> DeleteAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> ExecuteAsync(DocumentServiceRequest request);

		public abstract Task<DocumentServiceResponse> ReadFeedAsync(DocumentServiceRequest request, ReadType readType);

		public virtual Task UploadMediaAsync(string mediaId, Stream mediaStream, INameValueCollection headers, int singleBlobUploadThresholdInBytes, TimeSpan blobUploadTiemoutSeconds)
		{
			if (mediaHandler == null)
			{
				throw new MethodNotAllowedException();
			}
			return mediaHandler.UploadMediaAsync(mediaId, mediaStream, headers, singleBlobUploadThresholdInBytes, blobUploadTiemoutSeconds);
		}

		public virtual Task DeleteMediaAsync(string mediaId, INameValueCollection headers)
		{
			if (mediaHandler == null)
			{
				throw new MethodNotAllowedException();
			}
			return mediaHandler.DeleteMediaAsync(mediaId, headers);
		}

		public virtual Task<Tuple<INameValueCollection, INameValueCollection>> HeadMediaAsync(string mediaId, INameValueCollection headers)
		{
			if (mediaHandler == null)
			{
				throw new MethodNotAllowedException();
			}
			return mediaHandler.HeadMediaAsync(mediaId, headers);
		}

		public virtual Task<Tuple<Stream, INameValueCollection, INameValueCollection>> DownloadMediaAsync(string mediaId, INameValueCollection headers, TimeSpan blobDownloadTimeoutSeconds)
		{
			if (mediaHandler == null)
			{
				throw new MethodNotAllowedException();
			}
			return mediaHandler.DownloadMediaAsync(mediaId, headers, blobDownloadTimeoutSeconds);
		}
	}
}
