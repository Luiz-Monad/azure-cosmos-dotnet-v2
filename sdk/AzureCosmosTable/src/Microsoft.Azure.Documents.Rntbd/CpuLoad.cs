using System;
using System.Globalization;

namespace Microsoft.Azure.Documents.Rntbd
{
	internal struct CpuLoad
	{
		public DateTime Timestamp;

		public float Value;

		public CpuLoad(DateTime timestamp, float value)
		{
			if ((double)value < 0.0 || (double)value > 100.0)
			{
				throw new ArgumentOutOfRangeException("value", value, "Valid CPU load values must be between 0.0 and 100.0");
			}
			Timestamp = timestamp;
			Value = value;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0:O} {1:F3})", Timestamp, Value);
		}
	}
}
