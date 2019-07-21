using System;

namespace Microsoft.Azure.Documents
{
	internal static class MathUtils
	{
		public static int CeilingMultiple(int x, int n)
		{
			if (x <= 0)
			{
				throw new ArgumentOutOfRangeException("x");
			}
			if (n <= 0)
			{
				throw new ArgumentOutOfRangeException("n");
			}
			x--;
			checked
			{
				return x + n - unchecked(x % n);
			}
		}
	}
}
