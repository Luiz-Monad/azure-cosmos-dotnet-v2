// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// Microsoft.Azure.Documents.ClientSideRequestStatistics
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal sealed class ClientSideRequestStatistics
	{
		private struct StoreResponseStatistics
		{
			public DateTime RequestResponseTime;

			public StoreResult StoreResult;

			public ResourceType RequestResourceType;

			public OperationType RequestOperationType;

			public override string ToString()
			{
				return string.Format(CultureInfo.InvariantCulture, "ResponseTime: {0}, StoreResult: {1}, ResourceType: {2}, OperationType: {3}", RequestResponseTime.ToString("o", CultureInfo.InvariantCulture), (StoreResult != null) ? StoreResult.ToString() : "", RequestResourceType, RequestOperationType);
			}
		}

		private class AddressResolutionStatistics
		{
			public DateTime StartTime
			{
				get;
				set;
			}

			public DateTime EndTime
			{
				get;
				set;
			}

			public string TargetEndpoint
			{
				get;
				set;
			}

			public override string ToString()
			{
				return string.Format(CultureInfo.InvariantCulture, "AddressResolution - StartTime: {0}, EndTime: {1}, TargetEndpoint: {2}", StartTime.ToString("o", CultureInfo.InvariantCulture), EndTime.ToString("o", CultureInfo.InvariantCulture), TargetEndpoint);
			}
		}

		private const int MaxSupplementalRequestsForToString = 10;

		private DateTime requestStartTime;

		private DateTime requestEndTime;

		private object lockObject = new object();

		private List<StoreResponseStatistics> responseStatisticsList;

		private List<StoreResponseStatistics> supplementalResponseStatisticsList;

		private Dictionary<string, AddressResolutionStatistics> addressResolutionStatistics;

		internal List<Uri> ContactedReplicas
		{
			get;
			set;
		}

		internal HashSet<Uri> FailedReplicas
		{
			get;
			private set;
		}

		internal HashSet<Uri> RegionsContacted
		{
			get;
			private set;
		}

		public TimeSpan RequestLatency => requestEndTime - requestStartTime;

		public bool IsCpuOverloaded
		{
			get
			{
				foreach (StoreResponseStatistics responseStatistics in responseStatisticsList)
				{
					if (responseStatistics.StoreResult.IsClientCpuOverloaded)
					{
						return true;
					}
				}
				foreach (StoreResponseStatistics supplementalResponseStatistics in supplementalResponseStatisticsList)
				{
					if (supplementalResponseStatistics.StoreResult.IsClientCpuOverloaded)
					{
						return true;
					}
				}
				return false;
			}
		}

		public ClientSideRequestStatistics()
		{
			requestStartTime = DateTime.UtcNow;
			requestEndTime = DateTime.UtcNow;
			responseStatisticsList = new List<StoreResponseStatistics>();
			supplementalResponseStatisticsList = new List<StoreResponseStatistics>();
			addressResolutionStatistics = new Dictionary<string, AddressResolutionStatistics>();
			ContactedReplicas = new List<Uri>();
			FailedReplicas = new HashSet<Uri>();
			RegionsContacted = new HashSet<Uri>();
		}

		public void RecordResponse(DocumentServiceRequest request, StoreResult storeResult)
		{
			StoreResponseStatistics storeResponseStatistics = default(StoreResponseStatistics);
			DateTime t = storeResponseStatistics.RequestResponseTime = DateTime.UtcNow;
			storeResponseStatistics.StoreResult = storeResult;
			storeResponseStatistics.RequestOperationType = request.OperationType;
			storeResponseStatistics.RequestResourceType = request.ResourceType;
			Uri locationEndpointToRoute = request.RequestContext.LocationEndpointToRoute;
			lock (lockObject)
			{
				if (t > requestEndTime)
				{
					requestEndTime = t;
				}
				if (locationEndpointToRoute != null)
				{
					RegionsContacted.Add(locationEndpointToRoute);
				}
				if (storeResponseStatistics.RequestOperationType == OperationType.Head || storeResponseStatistics.RequestOperationType == OperationType.HeadFeed)
				{
					supplementalResponseStatisticsList.Add(storeResponseStatistics);
				}
				else
				{
					responseStatisticsList.Add(storeResponseStatistics);
				}
			}
		}

		public string RecordAddressResolutionStart(Uri targetEndpoint)
		{
			string text = Guid.NewGuid().ToString();
			AddressResolutionStatistics value = new AddressResolutionStatistics
			{
				StartTime = DateTime.UtcNow,
				EndTime = DateTime.MaxValue,
				TargetEndpoint = ((targetEndpoint == null) ? "<NULL>" : targetEndpoint.ToString())
			};
			lock (lockObject)
			{
				addressResolutionStatistics.Add(text, value);
				return text;
			}
		}

		public void RecordAddressResolutionEnd(string identifier)
		{
			if (!string.IsNullOrEmpty(identifier))
			{
				DateTime utcNow = DateTime.UtcNow;
				lock (lockObject)
				{
					if (!addressResolutionStatistics.ContainsKey(identifier))
					{
						throw new ArgumentException("Identifier {0} does not exist. Please call start before calling end.", identifier);
					}
					if (utcNow > requestEndTime)
					{
						requestEndTime = utcNow;
					}
					addressResolutionStatistics[identifier].EndTime = utcNow;
				}
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			lock (lockObject)
			{
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "RequestStartTime: {0}, RequestEndTime: {1}, Number of regions attempted: {2}", requestStartTime.ToString("o", CultureInfo.InvariantCulture), requestEndTime.ToString("o", CultureInfo.InvariantCulture), (RegionsContacted.Count == 0) ? 1 : RegionsContacted.Count));
				foreach (StoreResponseStatistics responseStatistics in responseStatisticsList)
				{
					stringBuilder.AppendLine(responseStatistics.ToString());
				}
				foreach (AddressResolutionStatistics value in addressResolutionStatistics.Values)
				{
					stringBuilder.AppendLine(value.ToString());
				}
				int count = supplementalResponseStatisticsList.Count;
				int num = Math.Max(count - 10, 0);
				if (num != 0)
				{
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  -- Displaying only the last {0} head/headfeed requests. Total head/headfeed requests: {1}", 10, count));
				}
				for (int i = num; i < count; i++)
				{
					stringBuilder.AppendLine(supplementalResponseStatisticsList[i].ToString());
				}
			}
			string text = stringBuilder.ToString();
			if (text.Length > 0)
			{
				return Environment.NewLine + text;
			}
			return string.Empty;
		}
	}
}
