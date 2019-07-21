using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Documents
{
	internal sealed class DocumentServiceRequest : IDisposable
	{
		public sealed class SystemAuthorizationParameters
		{
			public string FederationId
			{
				get;
				set;
			}

			public string Verb
			{
				get;
				set;
			}

			public string ResourceId
			{
				get;
				set;
			}

			public SystemAuthorizationParameters Clone()
			{
				return new SystemAuthorizationParameters
				{
					FederationId = FederationId,
					Verb = Verb,
					ResourceId = ResourceId
				};
			}
		}

		private bool isDisposed;

		private const char PreferHeadersSeparator = ';';

		private const string PreferHeaderValueFormat = "{0}={1}";

		private ServiceIdentity serviceIdentity;

		private PartitionKeyRangeIdentity partitionKeyRangeIdentity;

		public bool IsNameBased
		{
			get;
			private set;
		}

		public string DatabaseName
		{
			get;
			private set;
		}

		public string CollectionName
		{
			get;
			private set;
		}

		/// <summary>
		/// This is currently used to force non-Windows .NET Core target platforms(Linux and OSX)
		/// and on 32-bit host process on Windows for NETFX, to always use Gateway mode for sending 
		/// cross partition query requests to get partition execution info as that logic is there in 
		/// ServiceInterop native dll which we haven't ported to Linux and OSX yet and it exists only 
		/// in 64 bit version on Windows.
		/// </summary>
		public bool UseGatewayMode
		{
			get;
			set;
		}

		/// <summary>
		/// This is a flag that indicates whether the DocumentClient internally
		/// throws exceptions for status codes 404, 412, and 409 or whether it returns
		/// the status codes as part of the result for failures.
		/// </summary>
		public bool UseStatusCodeForFailures
		{
			get;
			set;
		}

		/// <summary>
		/// This is a flag that indicates whether the DocumentClient internally
		/// throws exceptions for 429 status codes
		/// the status codes as part of the result for failures.
		/// </summary>
		public bool UseStatusCodeFor429
		{
			get;
			set;
		}

		/// <summary>
		/// ServiceIdentity of the target service where this request should reach
		/// Only valid for gateway
		/// </summary>
		public ServiceIdentity ServiceIdentity
		{
			get
			{
				return serviceIdentity;
			}
			private set
			{
				serviceIdentity = value;
			}
		}

		public SystemAuthorizationParameters SystemAuthorizationParams
		{
			get;
			set;
		}

		public PartitionKeyRangeIdentity PartitionKeyRangeIdentity
		{
			get
			{
				return partitionKeyRangeIdentity;
			}
			private set
			{
				partitionKeyRangeIdentity = value;
				if (value != null)
				{
					Headers["x-ms-documentdb-partitionkeyrangeid"] = value.ToHeader();
				}
				else
				{
					Headers.Remove("x-ms-documentdb-partitionkeyrangeid");
				}
			}
		}

		public string ResourceId
		{
			get;
			set;
		}

		public DocumentServiceRequestContext RequestContext
		{
			get;
			set;
		}

		/// <summary>
		/// Normalized resourcePath, for both Name based and Rid based.
		/// This is the string passed for AuthZ. 
		/// It is resourceId in Rid case passed for AuthZ
		/// </summary>
		public string ResourceAddress
		{
			get;
			private set;
		}

		public bool IsFeed
		{
			get;
			set;
		}

		public string EntityId
		{
			get;
			set;
		}

		public INameValueCollection Headers
		{
			get;
			private set;
		}

		/// <summary>
		/// Contains the context shared by handlers.
		/// </summary>
		public IDictionary<string, object> Properties
		{
			get;
			set;
		}

		public Stream Body
		{
			get;
			set;
		}

		public CloneableStream CloneableBody
		{
			get;
			private set;
		}

		/// <summary>
		/// Authorization token used for the request.
		/// This will be used to generate any child requests that are needed to process the request.
		/// </summary>
		public AuthorizationTokenType RequestAuthorizationTokenType
		{
			get;
			set;
		}

		public bool IsBodySeekableClonableAndCountable
		{
			get
			{
				if (Body != null)
				{
					return CloneableBody != null;
				}
				return true;
			}
		}

		public OperationType OperationType
		{
			get;
			private set;
		}

		public ResourceType ResourceType
		{
			get;
			private set;
		}

		public string QueryString
		{
			get;
			set;
		}

		public string Continuation
		{
			get;
			set;
		}

		public bool ForceNameCacheRefresh
		{
			get;
			set;
		}

		public bool ForcePartitionKeyRangeRefresh
		{
			get;
			set;
		}

		public bool ForceCollectionRoutingMapRefresh
		{
			get;
			set;
		}

		public bool ForceMasterRefresh
		{
			get;
			set;
		}

		public bool IsReadOnlyRequest
		{
			get
			{
				if (OperationType != OperationType.Read && OperationType != OperationType.ReadFeed && OperationType != OperationType.Head && OperationType != OperationType.HeadFeed && OperationType != OperationType.Query && OperationType != OperationType.SqlQuery)
				{
					return OperationType == OperationType.QueryPlan;
				}
				return true;
			}
		}

		public bool IsReadOnlyScript
		{
			get
			{
				string text = Headers.Get("x-ms-is-readonly-script");
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				if (OperationType == OperationType.ExecuteJavaScript)
				{
					return text.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}
		}

		public JsonSerializerSettings SerializerSettings
		{
			get;
			set;
		}

		public uint? DefaultReplicaIndex
		{
			get;
			set;
		}

		private DocumentServiceRequest()
		{
		}

		/// <summary>
		/// This is constructed from the existing request, either RId based or name based.
		/// resourceIdOrFullName can be either: (trimmed, RemoveTrailingSlashes, RemoveLeadingSlashes, urldecoded)
		/// 1. wo1ZAP7zFQA=
		/// 2. dbs/dbName/colls/collectionName/docs/documentName
		/// </summary>
		/// <param name="operationType"></param>
		/// <param name="resourceIdOrFullName"></param>
		/// <param name="resourceType"></param>
		/// <param name="body"></param>
		/// <param name="headers"></param>
		/// <param name="isNameBased">resourceIdOrFullName is resourceId or fullName</param>
		/// <param name="authorizationTokenType"></param>
		internal DocumentServiceRequest(OperationType operationType, string resourceIdOrFullName, ResourceType resourceType, Stream body, INameValueCollection headers, bool isNameBased, AuthorizationTokenType authorizationTokenType)
		{
			OperationType = operationType;
			ForceNameCacheRefresh = false;
			ResourceType = resourceType;
			Body = body;
			Headers = (headers ?? new StringKeyValueCollection());
			IsFeed = false;
			IsNameBased = isNameBased;
			if (isNameBased)
			{
				ResourceAddress = resourceIdOrFullName;
			}
			else
			{
				ResourceId = resourceIdOrFullName;
				ResourceAddress = resourceIdOrFullName;
			}
			RequestAuthorizationTokenType = authorizationTokenType;
			RequestContext = new DocumentServiceRequestContext();
			if (!string.IsNullOrEmpty(Headers["x-ms-documentdb-partitionkeyrangeid"]))
			{
				PartitionKeyRangeIdentity = PartitionKeyRangeIdentity.FromHeader(Headers["x-ms-documentdb-partitionkeyrangeid"]);
			}
		}

		/// <summary>
		///  The path is the incoming Uri.PathAndQuery, it can be:  (the name is url encoded).
		///  1. 	dbs/dbName/colls/collectionName/docs/documentName/attachments/  
		///  2.     dbs/wo1ZAA==/colls/wo1ZAP7zFQA=/
		/// </summary>
		/// <param name="operationType"></param>
		/// <param name="resourceType"></param>
		/// <param name="path"></param>
		/// <param name="body"></param>
		/// <param name="headers"></param>
		/// <param name="authorizationTokenType"></param>
		internal DocumentServiceRequest(OperationType operationType, ResourceType resourceType, string path, Stream body, AuthorizationTokenType authorizationTokenType, INameValueCollection headers)
		{
			OperationType = operationType;
			ForceNameCacheRefresh = false;
			ResourceType = resourceType;
			Body = body;
			Headers = (headers ?? new StringKeyValueCollection());
			RequestAuthorizationTokenType = authorizationTokenType;
			RequestContext = new DocumentServiceRequestContext();
			bool isNameBased = false;
			bool isFeed = false;
			string databaseName = string.Empty;
			string collectionName = string.Empty;
			if (resourceType == ResourceType.Address || resourceType == ResourceType.XPReplicatorAddress)
			{
				return;
			}
			if (PathsHelper.TryParsePathSegmentsWithDatabaseAndCollectionNames(path, out isFeed, out string resourcePath, out string resourceIdOrFullName, out isNameBased, out databaseName, out collectionName, "", parseDatabaseAndCollectionNames: true))
			{
				IsNameBased = isNameBased;
				IsFeed = isFeed;
				if (isNameBased)
				{
					ResourceAddress = resourceIdOrFullName;
					DatabaseName = databaseName;
					CollectionName = collectionName;
				}
				else
				{
					ResourceId = resourceIdOrFullName;
					ResourceAddress = resourceIdOrFullName;
					ResourceId rid = null;
					if (!string.IsNullOrEmpty(ResourceId) && !Microsoft.Azure.Documents.ResourceId.TryParse(ResourceId, out rid) && resourceType != ResourceType.Offer && resourceType != ResourceType.Media && resourceType != ResourceType.DatabaseAccount && resourceType != ResourceType.MasterPartition && resourceType != ResourceType.ServerPartition && resourceType != ResourceType.RidRange && resourceType != ResourceType.VectorClock)
					{
						throw new NotFoundException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResourceUrlQuery, path, "$resolveFor"));
					}
				}
				if (ResourceType == ResourceType.Unknown)
				{
					ResourceType = PathsHelper.GetResourcePathSegment(resourcePath);
				}
				if (!string.IsNullOrEmpty(Headers["x-ms-documentdb-partitionkeyrangeid"]))
				{
					PartitionKeyRangeIdentity = PartitionKeyRangeIdentity.FromHeader(Headers["x-ms-documentdb-partitionkeyrangeid"]);
				}
				return;
			}
			throw new NotFoundException(string.Format(CultureInfo.CurrentUICulture, RMResources.InvalidResourceUrlQuery, path, "$resolveFor"));
		}

		public void RouteTo(ServiceIdentity serviceIdentity)
		{
			if (PartitionKeyRangeIdentity != null)
			{
				DefaultTrace.TraceCritical("This request was going to be routed to partition key range");
				throw new InternalServerErrorException();
			}
			ServiceIdentity = serviceIdentity;
		}

		public void RouteTo(PartitionKeyRangeIdentity partitionKeyRangeIdentity)
		{
			if (ServiceIdentity != null)
			{
				DefaultTrace.TraceCritical("This request was going to be routed to service identity");
				throw new InternalServerErrorException();
			}
			PartitionKeyRangeIdentity = partitionKeyRangeIdentity;
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				if (Body != null)
				{
					Body.Dispose();
					Body = null;
				}
				if (CloneableBody != null)
				{
					CloneableBody.Dispose();
					CloneableBody = null;
				}
				isDisposed = true;
			}
		}

		/// <summary>
		/// Verify the address is same as claimed resourceType
		/// </summary>
		/// <returns></returns>
		public bool IsValidAddress(ResourceType resourceType = ResourceType.Unknown)
		{
			ResourceType resourceType2 = ResourceType.Unknown;
			if (resourceType != ResourceType.Unknown)
			{
				resourceType2 = resourceType;
			}
			else if (!IsFeed)
			{
				resourceType2 = ResourceType;
			}
			else
			{
				if (ResourceType == ResourceType.Database)
				{
					return true;
				}
				if (ResourceType == ResourceType.Collection || ResourceType == ResourceType.User || ResourceType == ResourceType.UserDefinedType)
				{
					resourceType2 = ResourceType.Database;
				}
				else if (ResourceType == ResourceType.Permission)
				{
					resourceType2 = ResourceType.User;
				}
				else if (ResourceType == ResourceType.Document || ResourceType == ResourceType.StoredProcedure || ResourceType == ResourceType.UserDefinedFunction || ResourceType == ResourceType.Trigger || ResourceType == ResourceType.Conflict || ResourceType == ResourceType.StoredProcedure || ResourceType == ResourceType.PartitionKeyRange || ResourceType == ResourceType.Schema)
				{
					resourceType2 = ResourceType.Collection;
				}
				else
				{
					if (ResourceType != ResourceType.Attachment)
					{
						return false;
					}
					resourceType2 = ResourceType.Document;
				}
			}
			if (IsNameBased)
			{
				return PathsHelper.ValidateResourceFullName((resourceType != ResourceType.Unknown) ? resourceType : resourceType2, ResourceAddress);
			}
			return PathsHelper.ValidateResourceId(resourceType2, ResourceId);
		}

		public void AddPreferHeader(string preferHeaderName, string preferHeaderValue)
		{
			string text = string.Format(CultureInfo.InvariantCulture, "{0}={1}", preferHeaderName, preferHeaderValue);
			string text2 = Headers["Prefer"];
			text2 = (string.IsNullOrEmpty(text2) ? text : (text2 + ";" + text));
			Headers["Prefer"] = text2;
		}

		public static DocumentServiceRequest CreateFromResource(DocumentServiceRequest request, Resource modifiedResource)
		{
			if (!request.IsNameBased)
			{
				return Create(request.OperationType, modifiedResource, request.ResourceType, request.RequestAuthorizationTokenType, request.Headers, request.ResourceId);
			}
			return CreateFromName(request.OperationType, modifiedResource, request.ResourceType, request.Headers, request.ResourceAddress, request.RequestAuthorizationTokenType);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Stream is disposed with request instance")]
		public static DocumentServiceRequest Create(OperationType operationType, Resource resource, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null, string ownerResourceId = null, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			MemoryStream memoryStream = new MemoryStream();
			resource.SaveTo(memoryStream, formattingPolicy);
			memoryStream.Position = 0L;
			return new DocumentServiceRequest(operationType, (ownerResourceId != null) ? ownerResourceId : resource.ResourceId, resourceType, memoryStream, headers, isNameBased: false, authorizationTokenType)
			{
				CloneableBody = new CloneableStream(memoryStream)
			};
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, MemoryStream stream, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, null, resourceType, stream, headers, isNameBased: false, authorizationTokenType)
			{
				CloneableBody = new CloneableStream(stream)
			};
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Stream is disposed with request instance")]
		public static DocumentServiceRequest Create(OperationType operationType, string ownerResourceId, byte[] seralizedResource, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			MemoryStream body = new MemoryStream(seralizedResource);
			return new DocumentServiceRequest(operationType, ownerResourceId, resourceType, body, headers, isNameBased: false, authorizationTokenType);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Stream is disposed with request instance")]
		public static DocumentServiceRequest Create(OperationType operationType, string ownerResourceId, ResourceType resourceType, bool isNameBased, AuthorizationTokenType authorizationTokenType, byte[] seralizedResource = null, INameValueCollection headers = null, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			MemoryStream body = (seralizedResource == null) ? null : new MemoryStream(seralizedResource);
			return new DocumentServiceRequest(operationType, ownerResourceId, resourceType, body, headers, isNameBased, authorizationTokenType);
		}

		public static DocumentServiceRequest Create(OperationType operationType, string resourceId, ResourceType resourceType, Stream body, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceId, resourceType, body, headers, isNameBased: false, authorizationTokenType);
		}

		public static DocumentServiceRequest Create(OperationType operationType, string resourceId, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceId, resourceType, null, headers, isNameBased: false, authorizationTokenType);
		}

		public static DocumentServiceRequest CreateFromName(OperationType operationType, string resourceFullName, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceFullName, resourceType, null, headers, isNameBased: true, authorizationTokenType);
		}

		public static DocumentServiceRequest CreateFromName(OperationType operationType, Resource resource, ResourceType resourceType, INameValueCollection headers, string resourceFullName, AuthorizationTokenType authorizationTokenType, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			MemoryStream memoryStream = new MemoryStream();
			resource.SaveTo(memoryStream, formattingPolicy);
			memoryStream.Position = 0L;
			return new DocumentServiceRequest(operationType, resourceFullName, resourceType, memoryStream, headers, isNameBased: true, authorizationTokenType);
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, AuthorizationTokenType authorizationTokenType)
		{
			return new DocumentServiceRequest(operationType, null, resourceType, null, null, isNameBased: false, authorizationTokenType);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Stream is disposed with request instance")]
		public static DocumentServiceRequest Create(OperationType operationType, string relativePath, Resource resource, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None, JsonSerializerSettings settings = null)
		{
			MemoryStream memoryStream = new MemoryStream();
			resource.SaveTo(memoryStream, formattingPolicy, settings);
			memoryStream.Position = 0L;
			return new DocumentServiceRequest(operationType, resourceType, relativePath, memoryStream, authorizationTokenType, headers)
			{
				SerializerSettings = settings,
				CloneableBody = new CloneableStream(memoryStream)
			};
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Stream is disposed with request instance")]
		public static DocumentServiceRequest Create(OperationType operationType, Uri requestUri, Resource resource, ResourceType resourceType, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null, SerializationFormattingPolicy formattingPolicy = SerializationFormattingPolicy.None)
		{
			MemoryStream memoryStream = new MemoryStream();
			resource.SaveTo(memoryStream, formattingPolicy);
			memoryStream.Position = 0L;
			return new DocumentServiceRequest(operationType, resourceType, requestUri.PathAndQuery, memoryStream, authorizationTokenType, headers)
			{
				CloneableBody = new CloneableStream(memoryStream)
			};
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, string relativePath, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceType, relativePath, null, authorizationTokenType, headers);
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, Uri requestUri, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceType, requestUri.PathAndQuery, null, authorizationTokenType, headers);
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, string relativePath, Stream resourceStream, AuthorizationTokenType authorizationTokenType, INameValueCollection headers = null)
		{
			return new DocumentServiceRequest(operationType, resourceType, relativePath, resourceStream, authorizationTokenType, headers);
		}

		public static DocumentServiceRequest Create(OperationType operationType, ResourceType resourceType, Uri requestUri, Stream resourceStream, AuthorizationTokenType authorizationTokenType, INameValueCollection headers)
		{
			return new DocumentServiceRequest(operationType, resourceType, requestUri.PathAndQuery, resourceStream, authorizationTokenType, headers);
		}

		public async Task EnsureBufferedBodyAsync()
		{
			if (Body != null && CloneableBody == null)
			{
				MemoryStream memoryStream = new MemoryStream();
				await Body.CopyToAsync(memoryStream);
				memoryStream.Position = 0L;
				CloneableBody = new CloneableStream(memoryStream);
			}
		}

		public void ClearRoutingHints()
		{
			PartitionKeyRangeIdentity = null;
			ServiceIdentity = null;
			RequestContext.TargetIdentity = null;
			RequestContext.ResolvedPartitionKeyRange = null;
		}

		public DocumentServiceRequest Clone()
		{
			if (!IsBodySeekableClonableAndCountable)
			{
				throw new InvalidOperationException();
			}
			return new DocumentServiceRequest
			{
				OperationType = OperationType,
				ForceNameCacheRefresh = ForceNameCacheRefresh,
				ResourceType = ResourceType,
				ServiceIdentity = ServiceIdentity,
				SystemAuthorizationParams = ((SystemAuthorizationParams == null) ? null : SystemAuthorizationParams.Clone()),
				CloneableBody = ((CloneableBody != null) ? CloneableBody.Clone() : null),
				Headers = Headers.Clone(),
				IsFeed = IsFeed,
				IsNameBased = IsNameBased,
				ResourceAddress = ResourceAddress,
				ResourceId = ResourceId,
				RequestAuthorizationTokenType = RequestAuthorizationTokenType,
				RequestContext = RequestContext.Clone(),
				PartitionKeyRangeIdentity = PartitionKeyRangeIdentity,
				UseGatewayMode = UseGatewayMode,
				QueryString = QueryString,
				Continuation = Continuation,
				ForcePartitionKeyRangeRefresh = ForcePartitionKeyRangeRefresh,
				ForceCollectionRoutingMapRefresh = ForceCollectionRoutingMapRefresh,
				ForceMasterRefresh = ForceMasterRefresh,
				DefaultReplicaIndex = DefaultReplicaIndex,
				Properties = Properties,
				UseStatusCodeForFailures = UseStatusCodeForFailures,
				UseStatusCodeFor429 = UseStatusCodeFor429
			};
		}
	}
}
