using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// Implements the abstraction to resolve target location for geo-replicated DatabaseAccount
	/// with multiple writable and readable locations.
	/// </summary>
	internal sealed class LocationCache
	{
		private sealed class LocationUnavailabilityInfo
		{
			public DateTime LastUnavailabilityCheckTimeStamp
			{
				get;
				set;
			}

			public OperationType UnavailableOperations
			{
				get;
				set;
			}
		}

		private sealed class DatabaseAccountLocationsInfo
		{
			public ReadOnlyCollection<string> PreferredLocations
			{
				get;
				set;
			}

			public ReadOnlyCollection<string> AvailableWriteLocations
			{
				get;
				set;
			}

			public ReadOnlyCollection<string> AvailableReadLocations
			{
				get;
				set;
			}

			public ReadOnlyDictionary<string, Uri> AvailableWriteEndpointByLocation
			{
				get;
				set;
			}

			public ReadOnlyDictionary<string, Uri> AvailableReadEndpointByLocation
			{
				get;
				set;
			}

			public ReadOnlyCollection<Uri> WriteEndpoints
			{
				get;
				set;
			}

			public ReadOnlyCollection<Uri> ReadEndpoints
			{
				get;
				set;
			}

			public DatabaseAccountLocationsInfo(ReadOnlyCollection<string> preferredLocations, Uri defaultEndpoint)
			{
				PreferredLocations = preferredLocations;
				AvailableWriteLocations = new List<string>().AsReadOnly();
				AvailableReadLocations = new List<string>().AsReadOnly();
				AvailableWriteEndpointByLocation = new ReadOnlyDictionary<string, Uri>(new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase));
				AvailableReadEndpointByLocation = new ReadOnlyDictionary<string, Uri>(new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase));
				WriteEndpoints = new List<Uri>
				{
					defaultEndpoint
				}.AsReadOnly();
				ReadEndpoints = new List<Uri>
				{
					defaultEndpoint
				}.AsReadOnly();
			}

			public DatabaseAccountLocationsInfo(DatabaseAccountLocationsInfo other)
			{
				PreferredLocations = other.PreferredLocations;
				AvailableWriteLocations = other.AvailableWriteLocations;
				AvailableReadLocations = other.AvailableReadLocations;
				AvailableWriteEndpointByLocation = other.AvailableWriteEndpointByLocation;
				AvailableReadEndpointByLocation = other.AvailableReadEndpointByLocation;
				WriteEndpoints = other.WriteEndpoints;
				ReadEndpoints = other.ReadEndpoints;
			}
		}

		[Flags]
		private enum OperationType
		{
			None = 0x0,
			Read = 0x1,
			Write = 0x2
		}

		private static int DefaultUnavailableLocationsExpirationTimeInSeconds = 300;

		private const string UnavailableLocationsExpirationTimeInSeconds = "UnavailableLocationsExpirationTimeInSeconds";

		private readonly bool enableEndpointDiscovery;

		private readonly Uri defaultEndpoint;

		private readonly bool useMultipleWriteLocations;

		private readonly object lockObject;

		private readonly TimeSpan unavailableLocationsExpirationTime;

		private readonly int connectionLimit;

		private readonly ConcurrentDictionary<Uri, LocationUnavailabilityInfo> locationUnavailablityInfoByEndpoint;

		private DatabaseAccountLocationsInfo locationInfo;

		private DateTime lastCacheUpdateTimestamp;

		private bool enableMultipleWriteLocations;

		/// <summary>
		/// Gets list of read endpoints ordered by
		/// 1. Preferred location
		/// 2. Endpoint availablity
		/// </summary>
		public ReadOnlyCollection<Uri> ReadEndpoints
		{
			get
			{
				if (locationUnavailablityInfoByEndpoint.Count > 0 && DateTime.UtcNow - lastCacheUpdateTimestamp > unavailableLocationsExpirationTime)
				{
					UpdateLocationCache();
				}
				return locationInfo.ReadEndpoints;
			}
		}

		/// <summary>
		/// Gets list of write endpoints ordered by
		/// 1. Preferred location
		/// 2. Endpoint availablity
		/// </summary>
		public ReadOnlyCollection<Uri> WriteEndpoints
		{
			get
			{
				if (locationUnavailablityInfoByEndpoint.Count > 0 && DateTime.UtcNow - lastCacheUpdateTimestamp > unavailableLocationsExpirationTime)
				{
					UpdateLocationCache();
				}
				return locationInfo.WriteEndpoints;
			}
		}

		public LocationCache(ReadOnlyCollection<string> preferredLocations, Uri defaultEndpoint, bool enableEndpointDiscovery, int connectionLimit, bool useMultipleWriteLocations)
		{
			locationInfo = new DatabaseAccountLocationsInfo(preferredLocations, defaultEndpoint);
			this.defaultEndpoint = defaultEndpoint;
			this.enableEndpointDiscovery = enableEndpointDiscovery;
			this.useMultipleWriteLocations = useMultipleWriteLocations;
			this.connectionLimit = connectionLimit;
			lockObject = new object();
			locationUnavailablityInfoByEndpoint = new ConcurrentDictionary<Uri, LocationUnavailabilityInfo>();
			lastCacheUpdateTimestamp = DateTime.MinValue;
			enableMultipleWriteLocations = false;
			unavailableLocationsExpirationTime = TimeSpan.FromSeconds(DefaultUnavailableLocationsExpirationTimeInSeconds);
		}

		/// <summary>
		/// Returns the location corresponding to the endpoint if location specific endpoint is provided.
		/// For the defaultEndPoint, we will return the first available write location.
		/// Returns null, in other cases.
		/// </summary>
		/// <remarks>
		/// Today we return null for defaultEndPoint if multiple write locations can be used.
		/// This needs to be modifed to figure out proper location in such case.
		/// </remarks>
		public string GetLocation(Uri endpoint)
		{
			string text = locationInfo.AvailableWriteEndpointByLocation.FirstOrDefault((KeyValuePair<string, Uri> uri) => uri.Value == endpoint).Key ?? locationInfo.AvailableReadEndpointByLocation.FirstOrDefault((KeyValuePair<string, Uri> uri) => uri.Value == endpoint).Key;
			if (text == null && endpoint == defaultEndpoint && !CanUseMultipleWriteLocations() && locationInfo.AvailableWriteEndpointByLocation.Any())
			{
				return locationInfo.AvailableWriteEndpointByLocation.First().Key;
			}
			return text;
		}

		/// <summary>
		/// Marks the current location unavailable for read
		/// </summary>
		public void MarkEndpointUnavailableForRead(Uri endpoint)
		{
			MarkEndpointUnavailable(endpoint, OperationType.Read);
		}

		/// <summary>
		/// Marks the current location unavailable for write
		/// </summary>
		public void MarkEndpointUnavailableForWrite(Uri endpoint)
		{
			MarkEndpointUnavailable(endpoint, OperationType.Write);
		}

		/// <summary>
		/// Invoked when <see cref="T:Microsoft.Azure.Documents.DatabaseAccount" /> is read
		/// </summary>
		/// <param name="databaseAccount">Read DatabaseAccoaunt </param>
		public void OnDatabaseAccountRead(DatabaseAccount databaseAccount)
		{
			UpdateLocationCache(databaseAccount.WritableLocations, databaseAccount.ReadableLocations, null, databaseAccount.EnableMultipleWriteLocations);
		}

		/// <summary>
		/// Invoked when <see cref="P:Microsoft.Azure.Documents.Client.ConnectionPolicy.PreferredLocations" /> changes
		/// </summary>
		/// <param name="preferredLocations"></param>
		public void OnLocationPreferenceChanged(ReadOnlyCollection<string> preferredLocations)
		{
			UpdateLocationCache(null, null, preferredLocations);
		}

		/// <summary>
		/// Resolves request to service endpoint. 
		/// 1. If this is a write request
		///    (a) If UseMultipleWriteLocations = true
		///        (i) For document writes, resolve to most preferred and available write endpoint.
		///            Once the endpoint is marked unavailable, it is moved to the end of available write endpoint. Current request will
		///            be retried on next preferred available write endpoint.
		///        (ii) For all other resources, always resolve to first/second (regardless of preferred locations)
		///             write endpoint in <see cref="P:Microsoft.Azure.Documents.DatabaseAccount.WritableLocations" />.
		///             Endpoint of first write location in <see cref="P:Microsoft.Azure.Documents.DatabaseAccount.WritableLocations" /> is the only endpoint that supports
		///             write operation on all resource types (except during that region's failover). 
		///             Only during manual failover, client would retry write on second write location in <see cref="P:Microsoft.Azure.Documents.DatabaseAccount.WritableLocations" />.
		///    (b) Else resolve the request to first write endpoint in <see cref="F:Microsoft.Azure.Documents.DatabaseAccount.writeLocations" /> OR 
		///        second write endpoint in <see cref="P:Microsoft.Azure.Documents.DatabaseAccount.WritableLocations" /> in case of manual failover of that location.
		/// 2. Else resolve the request to most preferred available read endpoint (automatic failover for read requests)
		/// </summary>
		/// <param name="request">Request for which endpoint is to be resolved</param>
		/// <returns>Resolved endpoint</returns>
		public Uri ResolveServiceEndpoint(DocumentServiceRequest request)
		{
			if (request.RequestContext != null && request.RequestContext.LocationEndpointToRoute != null)
			{
				return request.RequestContext.LocationEndpointToRoute;
			}
			int valueOrDefault = request.RequestContext.LocationIndexToRoute.GetValueOrDefault(0);
			Uri uri = defaultEndpoint;
			if (!request.RequestContext.UsePreferredLocations.GetValueOrDefault(true) || (request.OperationType.IsWriteOperation() && !CanUseMultipleWriteLocations(request)))
			{
				DatabaseAccountLocationsInfo databaseAccountLocationsInfo = locationInfo;
				if (enableEndpointDiscovery && databaseAccountLocationsInfo.AvailableWriteLocations.Count > 0)
				{
					valueOrDefault = Math.Min(valueOrDefault % 2, databaseAccountLocationsInfo.AvailableWriteLocations.Count - 1);
					string key = databaseAccountLocationsInfo.AvailableWriteLocations[valueOrDefault];
					uri = databaseAccountLocationsInfo.AvailableWriteEndpointByLocation[key];
				}
			}
			else
			{
				ReadOnlyCollection<Uri> readOnlyCollection = request.OperationType.IsWriteOperation() ? WriteEndpoints : ReadEndpoints;
				uri = readOnlyCollection[valueOrDefault % readOnlyCollection.Count];
			}
			request.RequestContext.RouteToLocation(uri);
			return uri;
		}

		public bool ShouldRefreshEndpoints(out bool canRefreshInBackground)
		{
			canRefreshInBackground = true;
			DatabaseAccountLocationsInfo databaseAccountLocationsInfo = locationInfo;
			string text = databaseAccountLocationsInfo.PreferredLocations.FirstOrDefault();
			if (enableEndpointDiscovery)
			{
				bool flag = useMultipleWriteLocations && !enableMultipleWriteLocations;
				ReadOnlyCollection<Uri> readEndpoints = databaseAccountLocationsInfo.ReadEndpoints;
				if (IsEndpointUnavailable(readEndpoints[0], OperationType.Read))
				{
					canRefreshInBackground = (readEndpoints.Count > 1);
					DefaultTrace.TraceInformation("ShouldRefreshEndpoints = true since the first read endpoint {0} is not available for read. canRefreshInBackground = {1}", readEndpoints[0], canRefreshInBackground);
					return true;
				}
				if (!string.IsNullOrEmpty(text))
				{
					if (!databaseAccountLocationsInfo.AvailableReadEndpointByLocation.TryGetValue(text, out Uri value))
					{
						DefaultTrace.TraceInformation("ShouldRefreshEndpoints = true since most preferred location {0} is not in available read locations.", text);
						return true;
					}
					if (value != readEndpoints[0])
					{
						DefaultTrace.TraceInformation("ShouldRefreshEndpoints = true since most preferred location {0} is not available for read.", text);
						return true;
					}
				}
				ReadOnlyCollection<Uri> writeEndpoints = databaseAccountLocationsInfo.WriteEndpoints;
				if (!CanUseMultipleWriteLocations())
				{
					if (IsEndpointUnavailable(writeEndpoints[0], OperationType.Write))
					{
						canRefreshInBackground = (writeEndpoints.Count > 1);
						DefaultTrace.TraceInformation("ShouldRefreshEndpoints = true since most preferred location {0} endpoint {1} is not available for write. canRefreshInBackground = {2}", text, writeEndpoints[0], canRefreshInBackground);
						return true;
					}
					return flag;
				}
				if (!string.IsNullOrEmpty(text))
				{
					if (databaseAccountLocationsInfo.AvailableWriteEndpointByLocation.TryGetValue(text, out Uri value2))
					{
						flag |= (value2 != writeEndpoints[0]);
						DefaultTrace.TraceInformation("ShouldRefreshEndpoints = {0} since most preferred location {1} is not available for write.", flag, text);
						return flag;
					}
					DefaultTrace.TraceInformation("ShouldRefreshEndpoints = true since most preferred location {0} is not in available write locations", text);
					return true;
				}
				return flag;
			}
			return false;
		}

		public bool CanUseMultipleWriteLocations(DocumentServiceRequest request)
		{
			if (CanUseMultipleWriteLocations())
			{
				if (request.ResourceType != ResourceType.Document)
				{
					if (request.ResourceType == ResourceType.StoredProcedure)
					{
						return request.OperationType == Microsoft.Azure.Documents.OperationType.ExecuteJavaScript;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private void ClearStaleEndpointUnavailabilityInfo()
		{
			if (locationUnavailablityInfoByEndpoint.Any())
			{
				foreach (Uri item in locationUnavailablityInfoByEndpoint.Keys.ToList())
				{
					if (locationUnavailablityInfoByEndpoint.TryGetValue(item, out LocationUnavailabilityInfo value) && DateTime.UtcNow - value.LastUnavailabilityCheckTimeStamp > unavailableLocationsExpirationTime && locationUnavailablityInfoByEndpoint.TryRemove(item, out LocationUnavailabilityInfo _))
					{
						DefaultTrace.TraceInformation("Removed endpoint {0} unavailable for operations {1} from unavailableEndpoints", item, value.UnavailableOperations);
					}
				}
			}
		}

		private bool IsEndpointUnavailable(Uri endpoint, OperationType expectedAvailableOperations)
		{
			LocationUnavailabilityInfo value;
			if (expectedAvailableOperations == OperationType.None || !locationUnavailablityInfoByEndpoint.TryGetValue(endpoint, out value) || !value.UnavailableOperations.HasFlag(expectedAvailableOperations))
			{
				return false;
			}
			if (DateTime.UtcNow - value.LastUnavailabilityCheckTimeStamp > unavailableLocationsExpirationTime)
			{
				return false;
			}
			DefaultTrace.TraceInformation("Endpoint {0} unavailable for operations {1} present in unavailableEndpoints", endpoint, value.UnavailableOperations);
			return true;
		}

		private void MarkEndpointUnavailable(Uri unavailableEndpoint, OperationType unavailableOperationType)
		{
			DateTime currentTime = DateTime.UtcNow;
			LocationUnavailabilityInfo locationUnavailabilityInfo = locationUnavailablityInfoByEndpoint.AddOrUpdate(unavailableEndpoint, (Uri endpoint) => new LocationUnavailabilityInfo
			{
				LastUnavailabilityCheckTimeStamp = currentTime,
				UnavailableOperations = unavailableOperationType
			}, delegate(Uri endpoint, LocationUnavailabilityInfo info)
			{
				info.LastUnavailabilityCheckTimeStamp = currentTime;
				info.UnavailableOperations |= unavailableOperationType;
				return info;
			});
			UpdateLocationCache();
			DefaultTrace.TraceInformation("Endpoint {0} unavailable for {1} added/updated to unavailableEndpoints with timestamp {2}", unavailableEndpoint, unavailableOperationType, locationUnavailabilityInfo.LastUnavailabilityCheckTimeStamp);
		}

		private void UpdateLocationCache(IEnumerable<DatabaseAccountLocation> writeLocations = null, IEnumerable<DatabaseAccountLocation> readLocations = null, ReadOnlyCollection<string> preferenceList = null, bool? enableMultipleWriteLocations = default(bool?))
		{
			lock (lockObject)
			{
				DatabaseAccountLocationsInfo databaseAccountLocationsInfo = new DatabaseAccountLocationsInfo(locationInfo);
				if (preferenceList != null)
				{
					databaseAccountLocationsInfo.PreferredLocations = preferenceList;
				}
				if (enableMultipleWriteLocations.HasValue)
				{
					this.enableMultipleWriteLocations = enableMultipleWriteLocations.Value;
				}
				ClearStaleEndpointUnavailabilityInfo();
				if (readLocations != null)
				{
					databaseAccountLocationsInfo.AvailableReadEndpointByLocation = GetEndpointByLocation(readLocations, out ReadOnlyCollection<string> orderedLocations);
					databaseAccountLocationsInfo.AvailableReadLocations = orderedLocations;
				}
				if (writeLocations != null)
				{
					databaseAccountLocationsInfo.AvailableWriteEndpointByLocation = GetEndpointByLocation(writeLocations, out ReadOnlyCollection<string> orderedLocations2);
					databaseAccountLocationsInfo.AvailableWriteLocations = orderedLocations2;
				}
				databaseAccountLocationsInfo.WriteEndpoints = GetPreferredAvailableEndpoints(databaseAccountLocationsInfo.AvailableWriteEndpointByLocation, databaseAccountLocationsInfo.AvailableWriteLocations, OperationType.Write, defaultEndpoint);
				databaseAccountLocationsInfo.ReadEndpoints = GetPreferredAvailableEndpoints(databaseAccountLocationsInfo.AvailableReadEndpointByLocation, databaseAccountLocationsInfo.AvailableReadLocations, OperationType.Read, databaseAccountLocationsInfo.WriteEndpoints[0]);
				lastCacheUpdateTimestamp = DateTime.UtcNow;
				DefaultTrace.TraceInformation("Current WriteEndpoints = ({0}) ReadEndpoints = ({1})", string.Join(", ", from endpoint in databaseAccountLocationsInfo.WriteEndpoints
				select endpoint.ToString()), string.Join(", ", from endpoint in databaseAccountLocationsInfo.ReadEndpoints
				select endpoint.ToString()));
				locationInfo = databaseAccountLocationsInfo;
			}
		}

		private ReadOnlyCollection<Uri> GetPreferredAvailableEndpoints(ReadOnlyDictionary<string, Uri> endpointsByLocation, ReadOnlyCollection<string> orderedLocations, OperationType expectedAvailableOperation, Uri fallbackEndpoint)
		{
			List<Uri> list = new List<Uri>();
			DatabaseAccountLocationsInfo databaseAccountLocationsInfo = locationInfo;
			if (enableEndpointDiscovery)
			{
				if (CanUseMultipleWriteLocations() || expectedAvailableOperation.HasFlag(OperationType.Read))
				{
					List<Uri> list2 = new List<Uri>();
					foreach (string preferredLocation in databaseAccountLocationsInfo.PreferredLocations)
					{
						if (endpointsByLocation.TryGetValue(preferredLocation, out Uri value))
						{
							if (IsEndpointUnavailable(value, expectedAvailableOperation))
							{
								list2.Add(value);
							}
							else
							{
								list.Add(value);
							}
						}
					}
					if (list.Count == 0)
					{
						list.Add(fallbackEndpoint);
						list2.Remove(fallbackEndpoint);
					}
					list.AddRange(list2);
				}
				else
				{
					foreach (string orderedLocation in orderedLocations)
					{
						if (!string.IsNullOrEmpty(orderedLocation) && endpointsByLocation.TryGetValue(orderedLocation, out Uri value2))
						{
							list.Add(value2);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				list.Add(fallbackEndpoint);
			}
			return list.AsReadOnly();
		}

		private ReadOnlyDictionary<string, Uri> GetEndpointByLocation(IEnumerable<DatabaseAccountLocation> locations, out ReadOnlyCollection<string> orderedLocations)
		{
			Dictionary<string, Uri> dictionary = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
			List<string> list = new List<string>();
			foreach (DatabaseAccountLocation location in locations)
			{
				if (!string.IsNullOrEmpty(location.Name) && Uri.TryCreate(location.DatabaseAccountEndpoint, UriKind.Absolute, out Uri result))
				{
					dictionary[location.Name] = result;
					list.Add(location.Name);
					SetServicePointConnectionLimit(result);
				}
				else
				{
					DefaultTrace.TraceInformation("GetAvailableEndpointsByLocation() - skipping add for location = {0} as it is location name is either empty or endpoint is malformed {1}", location.Name, location.DatabaseAccountEndpoint);
				}
			}
			orderedLocations = list.AsReadOnly();
			return new ReadOnlyDictionary<string, Uri>(dictionary);
		}

		private bool CanUseMultipleWriteLocations()
		{
			if (useMultipleWriteLocations)
			{
				return enableMultipleWriteLocations;
			}
			return false;
		}

		private void SetServicePointConnectionLimit(Uri endpoint)
		{
		}
	}
}
