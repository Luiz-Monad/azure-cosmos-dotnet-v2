using System;

namespace Microsoft.Azure.Documents.Routing
{
	/// <summary>
	/// https://azure.microsoft.com/en-us/regions/
	/// </summary>
	internal static class LocationHelper
	{
		/// <summary>
		/// For example, for https://contoso.documents.azure.com:443/ and "West US", this will return https://contoso-westus.documents.azure.com:443/
		/// NOTE: This ONLY called by client first boot when the input endpoint is not available.
		/// </summary>
		/// <param name="serviceEndpoint"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		internal static Uri GetLocationEndpoint(Uri serviceEndpoint, string location)
		{
			UriBuilder uriBuilder = new UriBuilder(serviceEndpoint);
			string[] array = uriBuilder.Host.Split(new char[1]
			{
				'.'
			}, 2);
			if (array.Length != 0)
			{
				array[0] = array[0] + "-" + location.DataCenterToUriPostfix();
				uriBuilder.Host = string.Join(".", array);
			}
			return uriBuilder.Uri;
		}

		private static string DataCenterToUriPostfix(this string datacenter)
		{
			return datacenter.Replace(" ", string.Empty);
		}
	}
}
