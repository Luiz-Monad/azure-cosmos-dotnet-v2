using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Documents
{
	internal static class QueryMetricsUtils
	{
		public static Dictionary<string, double> ParseDelimitedString(string delimitedString)
		{
			if (delimitedString == null)
			{
				throw new ArgumentNullException("delimitedString");
			}
			Dictionary<string, double> dictionary = new Dictionary<string, double>();
			string[] array = delimitedString.Split(new char[1]
			{
				';'
			});
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1]
				{
					'='
				});
				if (array2.Length != 2)
				{
					throw new ArgumentException("recieved a malformed delimited string");
				}
				string key = array2[0];
				double num2 = dictionary[key] = double.Parse(array2[1], CultureInfo.InvariantCulture);
			}
			return dictionary;
		}

		public static TimeSpan DoubleMillisecondsToTimeSpan(double milliseconds)
		{
			return TimeSpan.FromTicks((long)(10000.0 * milliseconds));
		}

		public static TimeSpan TimeSpanFromMetrics(Dictionary<string, double> metrics, string key)
		{
			if (metrics.TryGetValue(key, out double value))
			{
				return DoubleMillisecondsToTimeSpan(value);
			}
			return default(TimeSpan);
		}
	}
}
