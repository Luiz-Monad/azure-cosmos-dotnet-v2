using Microsoft.Azure.Documents.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;

namespace Microsoft.Azure.Documents
{
	internal sealed class DocumentServiceResponse : IDisposable
	{
		private readonly JsonSerializerSettings serializerSettings;

		private bool isDisposed;

		public ClientSideRequestStatistics RequestStats
		{
			get;
			private set;
		}

		public string ResourceId
		{
			get;
			set;
		}

		public HttpStatusCode StatusCode
		{
			get;
			set;
		}

		public string StatusDescription
		{
			get;
			set;
		}

		internal INameValueCollection Headers
		{
			get;
			set;
		}

		internal static Func<Stream, JsonReader> JsonReaderFactory
		{
			get;
			set;
		}

		public NameValueCollection ResponseHeaders => Headers.ToNameValueCollection();

		public Stream ResponseBody
		{
			get;
			set;
		}

		public SubStatusCodes SubStatusCode
		{
			get;
			private set;
		}

		internal DocumentServiceResponse(Stream body, INameValueCollection headers, HttpStatusCode statusCode, JsonSerializerSettings serializerSettings = null)
		{
			ResponseBody = body;
			Headers = headers;
			StatusCode = statusCode;
			this.serializerSettings = serializerSettings;
			SubStatusCode = GetSubStatusCodes();
		}

		internal DocumentServiceResponse(Stream body, INameValueCollection headers, HttpStatusCode statusCode, ClientSideRequestStatistics clientSideRequestStatistics, JsonSerializerSettings serializerSettings = null)
		{
			ResponseBody = body;
			Headers = headers;
			StatusCode = statusCode;
			RequestStats = clientSideRequestStatistics;
			this.serializerSettings = serializerSettings;
			SubStatusCode = GetSubStatusCodes();
		}

		public TResource GetResource<TResource>(ITypeResolver<TResource> typeResolver = null) where TResource : Resource, new()
		{
			if (ResponseBody != null && (!ResponseBody.CanSeek || ResponseBody.Length != 0L))
			{
				if (typeResolver == null)
				{
					typeResolver = GetTypeResolver<TResource>();
				}
				if (!ResponseBody.CanSeek)
				{
					MemoryStream memoryStream = new MemoryStream();
					ResponseBody.CopyTo(memoryStream);
					ResponseBody.Dispose();
					ResponseBody = memoryStream;
					ResponseBody.Seek(0L, SeekOrigin.Begin);
				}
				TResource val = JsonSerializable.LoadFrom(ResponseBody, typeResolver, serializerSettings);
				val.SerializerSettings = serializerSettings;
				ResponseBody.Seek(0L, SeekOrigin.Begin);
				if (PathsHelper.IsPublicResource(typeof(TResource)))
				{
					val.AltLink = PathsHelper.GeneratePathForNameBased(typeof(TResource), GetOwnerFullName(), val.Id);
				}
				else if (typeof(TResource).IsGenericType() && (object)typeof(TResource).GetGenericTypeDefinition() == typeof(FeedResource<>))
				{
					val.AltLink = GetOwnerFullName();
				}
				return val;
			}
			return null;
		}

		public TResource GetInternalResource<TResource>(Func<TResource> constructor) where TResource : Resource
		{
			if (ResponseBody != null && (!ResponseBody.CanSeek || ResponseBody.Length > 0))
			{
				return JsonSerializable.LoadFromWithConstructor(ResponseBody, constructor, serializerSettings);
			}
			return null;
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				if (ResponseBody != null)
				{
					ResponseBody.Dispose();
					ResponseBody = null;
				}
				isDisposed = true;
			}
		}

		public IEnumerable<dynamic> GetQueryResponse(Type resourceType, out int itemCount)
		{
			return GetQueryResponse<object>(resourceType, lazy: false, out itemCount);
		}

		public IEnumerable<T> GetQueryResponse<T>(Type resourceType, bool lazy, out int itemCount)
		{
			if (!int.TryParse(Headers["x-ms-item-count"], out itemCount))
			{
				itemCount = 0;
			}
			IEnumerable<T> enumerable;
			if ((object)typeof(T) == typeof(object))
			{
				string ownerName = null;
				if (PathsHelper.IsPublicResource(resourceType))
				{
					ownerName = GetOwnerFullName();
				}
				enumerable = (IEnumerable<T>)GetEnumerable(resourceType, (Func<JsonReader, object>)delegate(JsonReader jsonReader)
				{
					JToken jToken = JToken.Load(jsonReader);
					if (jToken.Type == JTokenType.Object || jToken.Type == JTokenType.Array)
					{
						return new QueryResult((JContainer)jToken, ownerName, serializerSettings);
					}
					return jToken;
				});
			}
			else
			{
				JsonSerializer serializer = (serializerSettings != null) ? JsonSerializer.Create(serializerSettings) : JsonSerializer.Create();
				enumerable = GetEnumerable(resourceType, (JsonReader jsonReader) => serializer.Deserialize<T>(jsonReader));
			}
			if (lazy)
			{
				return enumerable;
			}
			List<T> list = new List<T>(itemCount);
			list.AddRange(enumerable);
			return list;
		}

		internal SubStatusCodes GetSubStatusCodes()
		{
			string s = Headers["x-ms-substatus"];
			uint result = 0u;
			if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return (SubStatusCodes)result;
			}
			return SubStatusCodes.Unknown;
		}

		private IEnumerable<T> GetEnumerable<T>(Type resourceType, Func<JsonReader, T> callback)
		{
			if (ResponseBody != null)
			{
				using (JsonReader jsonReader = Create(ResponseBody))
				{
					Helpers.SetupJsonReader(jsonReader, serializerSettings);
					string b = resourceType.Name + "s";
					string a = string.Empty;
					while (jsonReader.Read())
					{
						if (jsonReader.TokenType == JsonToken.PropertyName)
						{
							a = jsonReader.Value.ToString();
						}
						if (jsonReader.Depth == 1 && jsonReader.TokenType == JsonToken.StartArray && string.Equals(a, b, StringComparison.Ordinal))
						{
							while (jsonReader.Read() && jsonReader.Depth == 2)
							{
								yield return callback(jsonReader);
							}
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Creates a JsonReader that can read a supplied stream (assumes UTF-8 encoding).
		/// </summary>
		/// <param name="stream">the stream to read.</param>
		/// <returns>a concrete JsonReader that can read the supplied stream.</returns>
		private static JsonReader Create(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (JsonReaderFactory != null)
			{
				return JsonReaderFactory(stream);
			}
			return new JsonTextReader(new StreamReader(stream));
		}

		private static ITypeResolver<TResource> GetTypeResolver<TResource>() where TResource : Resource, new()
		{
			ITypeResolver<TResource> result = null;
			if ((object)typeof(TResource) == typeof(Offer))
			{
				result = (ITypeResolver<TResource>)OfferTypeResolver.ResponseOfferTypeResolver;
			}
			return result;
		}

		private string GetOwnerFullName()
		{
			return Headers["x-ms-alt-content-path"];
		}
	}
}
