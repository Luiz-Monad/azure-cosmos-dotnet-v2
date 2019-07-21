using Microsoft.Azure.Documents.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Microsoft.Azure.Documents
{
	internal static class UrlUtility
	{
		private class Element
		{
			public string Name
			{
				get;
				set;
			}

			public string Id
			{
				get;
				set;
			}

			public Element()
			{
			}

			public Element(string name, string id)
			{
				Name = name;
				Id = id;
			}
		}

		internal static string ConcatenateUrlsString(string baseUrl, params string[] relativeParts)
		{
			StringBuilder stringBuilder = new StringBuilder(RemoveTrailingSlash(baseUrl));
			foreach (string path in relativeParts)
			{
				stringBuilder.Append(RuntimeConstants.Separators.Url[0]);
				stringBuilder.Append(RemoveLeadingSlash(path));
			}
			return stringBuilder.ToString();
		}

		internal static string ConcatenateUrlsString(string baseUrl, string relativePart)
		{
			return AddTrailingSlash(baseUrl) + RemoveLeadingSlash(relativePart);
		}

		internal static string ConcatenateUrlsString(Uri baseUrl, string relativePart)
		{
			return ConcatenateUrlsString(GetLeftPartOfPath(baseUrl), relativePart);
		}

		internal static void ExtractTargetInfo(Uri uri, out string tenantId, out string applicationName, out string serviceId, out string partitionKey, out string replicaId)
		{
			if (uri.Segments == null || uri.Segments.Length < 9)
			{
				DefaultTrace.TraceError("Uri {0} is invalid", uri);
				throw new ArgumentException("uri");
			}
			tenantId = ExtractTenantIdFromUri(uri);
			applicationName = uri.Segments[2].Substring(0, uri.Segments[2].Length - 1);
			serviceId = uri.Segments[4].Substring(0, uri.Segments[4].Length - 1);
			partitionKey = uri.Segments[6].Substring(0, uri.Segments[6].Length - 1);
			replicaId = uri.Segments[8].Substring(0, uri.Segments[8].Length);
		}

		internal static string ConcatenateUrlsString(Uri baseUrl, Uri relativePart)
		{
			if (relativePart.IsAbsoluteUri)
			{
				return relativePart.ToString();
			}
			return ConcatenateUrlsString(GetLeftPartOfPath(baseUrl), relativePart.OriginalString);
		}

		internal static Uri ConcatenateUrls(string baseUrl, string relativePart)
		{
			return new Uri(ConcatenateUrlsString(baseUrl, relativePart));
		}

		internal static Uri ConcatenateUrls(Uri baseUrl, string relativePart)
		{
			return new Uri(ConcatenateUrlsString(baseUrl, relativePart));
		}

		internal static Uri ConcatenateUrls(Uri baseUrl, Uri relativePart)
		{
			if (relativePart.IsAbsoluteUri)
			{
				return relativePart;
			}
			return new Uri(ConcatenateUrlsString(baseUrl, relativePart));
		}

		internal static NameValueCollection ParseQuery(string queryString)
		{
			NameValueCollection nameValueCollection = null;
			queryString = RemoveLeadingQuestionMark(queryString);
			if (string.IsNullOrEmpty(queryString))
			{
				nameValueCollection = new NameValueCollection(0);
			}
			else
			{
				string[] array = SplitAndRemoveEmptyEntries(queryString, new char[1]
				{
					RuntimeConstants.Separators.Query[1]
				});
				nameValueCollection = new NameValueCollection(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = SplitAndRemoveEmptyEntries(array[i], new char[1]
					{
						RuntimeConstants.Separators.Query[2]
					}, 2);
					nameValueCollection.Add(array2[0], (array2.Length > 1) ? array2[1] : null);
				}
			}
			return nameValueCollection;
		}

		internal static string CreateQuery(INameValueCollection parsedQuery)
		{
			if (parsedQuery == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			parsedQuery.Count();
			foreach (string item in parsedQuery)
			{
				string text2 = parsedQuery[item];
				if (!string.IsNullOrEmpty(item))
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(RuntimeConstants.Separators.Query[1]);
					}
					stringBuilder.Append(item);
					if (text2 != null)
					{
						stringBuilder.Append(RuntimeConstants.Separators.Query[2]);
						stringBuilder.Append(text2);
					}
				}
			}
			return stringBuilder.ToString();
		}

		internal static Uri SetQuery(Uri url, string query)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			string text;
			UriKind uriKind;
			if (url.IsAbsoluteUri)
			{
				text = url.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);
				uriKind = UriKind.Absolute;
			}
			else
			{
				uriKind = UriKind.Relative;
				text = url.ToString();
				int num = text.LastIndexOf(RuntimeConstants.Separators.Query[0]);
				if (num >= 0)
				{
					text = text.Remove(num, text.Length - num);
				}
			}
			query = RemoveLeadingQuestionMark(query);
			if (!string.IsNullOrEmpty(query))
			{
				return new Uri(AddTrailingSlash(text) + RuntimeConstants.Separators.Query[0].ToString() + query, uriKind);
			}
			return new Uri(AddTrailingSlash(text), uriKind);
		}

		internal static string RemoveLeadingQuestionMark(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			if (path[0] == RuntimeConstants.Separators.Query[0])
			{
				return path.Remove(0, 1);
			}
			return path;
		}

		internal static string RemoveTrailingSlash(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			int length = path.Length;
			if (path[length - 1] == RuntimeConstants.Separators.Url[0])
			{
				return path.Remove(length - 1, 1);
			}
			return path;
		}

		internal static string RemoveTrailingSlashes(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			return path.TrimEnd(RuntimeConstants.Separators.Url);
		}

		internal static string RemoveLeadingSlash(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			if (path[0] == RuntimeConstants.Separators.Url[0])
			{
				return path.Remove(0, 1);
			}
			return path;
		}

		internal static string RemoveLeadingSlashes(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return path;
			}
			return path.TrimStart(RuntimeConstants.Separators.Url);
		}

		internal static string AddTrailingSlash(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = new string(RuntimeConstants.Separators.Url);
			}
			else if (path[path.Length - 1] != RuntimeConstants.Separators.Url[0])
			{
				path += RuntimeConstants.Separators.Url[0].ToString();
			}
			return path;
		}

		internal static string AddLeadingSlash(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = new string(RuntimeConstants.Separators.Url);
			}
			else if (path[0] != RuntimeConstants.Separators.Url[0])
			{
				path = RuntimeConstants.Separators.Url[0].ToString() + path;
			}
			return path;
		}

		internal static string GetLeftPartOfAuthority(Uri uri)
		{
			return uri.GetLeftPart(UriPartial.Authority);
		}

		internal static string GetLeftPartOfPath(Uri uri)
		{
			return uri.GetLeftPart(UriPartial.Path);
		}

		public static string[] SplitAndRemoveEmptyEntries(string str, char[] seperators)
		{
			return SplitAndRemoveEmptyEntries(str, seperators, int.MaxValue);
		}

		public static string[] SplitAndRemoveEmptyEntries(string str, char[] seperators, int count)
		{
			return str.Split(seperators, count, StringSplitOptions.RemoveEmptyEntries);
		}

		internal static string ExtractIdFromItemUri(Uri uri, int i)
		{
			return RemoveTrailingSlash(uri.Segments[i]);
		}

		internal static string ExtractTenantIdFromUri(Uri uri)
		{
			string dnsSafeHost = uri.DnsSafeHost;
			int num = dnsSafeHost.IndexOf('.');
			if (num != -1)
			{
				return dnsSafeHost.Substring(0, num);
			}
			return dnsSafeHost;
		}

		internal static string ExtractIdOrFullNameFromUri(string path, out bool isNameBased)
		{
			if (PathsHelper.TryParsePathSegments(path, out bool _, out string _, out string resourceIdOrFullName, out isNameBased))
			{
				return resourceIdOrFullName;
			}
			return null;
		}

		internal static string ExtractIdFromItemUri(Uri uri)
		{
			return RemoveTrailingSlash(uri.Segments[uri.Segments.Length - 1]);
		}

		internal static string ExtractIdFromCollectionUri(Uri uri)
		{
			return RemoveTrailingSlash(uri.Segments[uri.Segments.Length - 2]);
		}

		internal static string ExtractItemIdAndCollectionIdFromUri(Uri uri, out string collectionId)
		{
			collectionId = RemoveTrailingSlash(uri.Segments[uri.Segments.Length - 3]);
			return RemoveTrailingSlash(uri.Segments[uri.Segments.Length - 1]);
		}

		internal static string ExtractFileNameFromUri(Uri uri)
		{
			return RemoveTrailingSlash(uri.Segments[uri.Segments.Length - 1]);
		}

		internal static bool IsLocalHostUri(Uri uri)
		{
			if (!IPAddress.TryParse(uri.DnsSafeHost, out IPAddress address))
			{
				throw new ArgumentException("uri");
			}
			if (IPAddress.IsLoopback(address))
			{
				return true;
			}
			new List<IPAddress>();
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			for (int i = 0; i < allNetworkInterfaces.Length; i++)
			{
				foreach (UnicastIPAddressInformation unicastAddress in allNetworkInterfaces[i].GetIPProperties().UnicastAddresses)
				{
					if (unicastAddress.Address.Equals(address))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal static bool IsAstoriaUrl(Uri url)
		{
			if (url.AbsolutePath.IndexOf(RuntimeConstants.Separators.Parenthesis[0]) != -1)
			{
				return url.AbsolutePath.IndexOf(RuntimeConstants.Separators.Parenthesis[1]) != -1;
			}
			return false;
		}

		internal static Uri ToNativeUrl(Uri astoriaUrl)
		{
			Uri uri = null;
			if (astoriaUrl.IsAbsoluteUri)
			{
				uri = new Uri(GetLeftPartOfAuthority(astoriaUrl));
			}
			string query = astoriaUrl.Query;
			string absolutePath = astoriaUrl.AbsolutePath;
			string text = null;
			Element[] urlElements = null;
			if (!ParseAstoriaUrl(absolutePath, out urlElements))
			{
				return astoriaUrl;
			}
			List<string> list = new List<string>();
			Element[] array = urlElements;
			foreach (Element element in array)
			{
				if (!string.IsNullOrEmpty(element.Name))
				{
					list.Add(element.Name);
				}
				if (!string.IsNullOrEmpty(element.Id))
				{
					string text2 = element.Id.Trim(RuntimeConstants.Separators.Quote);
					if (text2.StartsWith("urn:uuid:", StringComparison.Ordinal))
					{
						text2 = text2.Substring("urn:uuid:".Length);
					}
					list.Add(text2);
				}
			}
			string baseUrl = list[0];
			list.RemoveAt(0);
			text = ConcatenateUrlsString(baseUrl, list.ToArray());
			Uri uri2 = null;
			uri2 = ((!(uri != null)) ? new Uri(text, UriKind.Relative) : new Uri(uri, text));
			if (!string.IsNullOrEmpty(query))
			{
				SetQuery(uri2, query);
			}
			return uri2;
		}

		private static bool ParseAstoriaUrl(string astoriaUrl, out Element[] urlElements)
		{
			urlElements = null;
			if (astoriaUrl == null)
			{
				return false;
			}
			string[] array = SplitAndRemoveEmptyEntries(astoriaUrl, RuntimeConstants.Separators.Url);
			if (array == null || array.Length < 1)
			{
				return false;
			}
			List<Element> list = new List<Element>();
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				if (!ParseAstoriaUrlPart(array2[i], out string name, out string id))
				{
					return false;
				}
				list.Add(new Element(name, id));
			}
			urlElements = list.ToArray();
			return true;
		}

		private static bool ParseAstoriaUrlPart(string urlPart, out string name, out string id)
		{
			name = null;
			id = null;
			int num = urlPart.IndexOf(RuntimeConstants.Separators.Parenthesis[0]);
			int num2 = urlPart.IndexOf(RuntimeConstants.Separators.Parenthesis[1]);
			if (num == -1)
			{
				if (num2 != -1)
				{
					return false;
				}
				name = urlPart;
			}
			else
			{
				if (num2 == -1 || num2 != urlPart.Length - 1)
				{
					return false;
				}
				name = urlPart.Substring(0, num);
				id = urlPart.Substring(num, num2 - num).Trim(RuntimeConstants.Separators.Parenthesis);
			}
			return true;
		}
	}
}
