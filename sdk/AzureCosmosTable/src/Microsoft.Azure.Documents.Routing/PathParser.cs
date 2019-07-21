using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents.Routing
{
	internal sealed class PathParser
	{
		private static char segmentSeparator = '/';

		private static string errorMessageFormat = "Invalid path, failed at {0}";

		/// <summary>
		/// Extract parts from path
		/// </summary>
		/// <remarks>
		/// This code doesn't do as much validation as the backend, as it assumes that IndexingPolicy path coming from the backend is valid.
		/// </remarks>
		/// <param name="path">A path string</param>
		/// <returns>An array of parts of path</returns>
		public static string[] GetPathParts(string path)
		{
			List<string> list = new List<string>();
			int currentIndex = 0;
			while (currentIndex < path.Length)
			{
				if (path[currentIndex] != segmentSeparator)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, errorMessageFormat, currentIndex));
				}
				if (++currentIndex == path.Length)
				{
					break;
				}
				if (path[currentIndex] == '"' || path[currentIndex] == '\'')
				{
					list.Add(GetEscapedToken(path, ref currentIndex));
				}
				else
				{
					list.Add(GetToken(path, ref currentIndex));
				}
			}
			return list.ToArray();
		}

		private static string GetEscapedToken(string path, ref int currentIndex)
		{
			char value = path[currentIndex];
			int startIndex = ++currentIndex;
			while (true)
			{
				startIndex = path.IndexOf(value, startIndex);
				if (startIndex == -1)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, errorMessageFormat, currentIndex));
				}
				if (path[startIndex - 1] != '\\')
				{
					break;
				}
				startIndex++;
			}
			string result = path.Substring(currentIndex, startIndex - currentIndex);
			currentIndex = startIndex + 1;
			return result;
		}

		private static string GetToken(string path, ref int currentIndex)
		{
			int num = path.IndexOf(segmentSeparator, currentIndex);
			string text = null;
			if (num == -1)
			{
				text = path.Substring(currentIndex);
				currentIndex = path.Length;
			}
			else
			{
				text = path.Substring(currentIndex, num - currentIndex);
				currentIndex = num;
			}
			return text.Trim();
		}
	}
}
