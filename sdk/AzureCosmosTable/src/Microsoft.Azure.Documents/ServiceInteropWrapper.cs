using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Documents
{
	internal static class ServiceInteropWrapper
	{
		internal static Lazy<bool> AssembliesExist = new Lazy<bool>(delegate
		{
			if (!IsGatewayAllowedToParseQueries())
			{
				return true;
			}
			if (typeof(ServiceInteropWrapper).GetTypeInfo().Assembly.IsDynamic)
			{
				return true;
			}
			string directoryName = Path.GetDirectoryName(typeof(ServiceInteropWrapper).GetTypeInfo().Assembly.Location);
			string[] array = new string[1]
			{
				"Microsoft.Azure.Documents.ServiceInterop.dll"
			};
			foreach (string path in array)
			{
				string text = Path.Combine(directoryName, path);
				if (!File.Exists(text))
				{
					DefaultTrace.TraceVerbose($"ServiceInteropWrapper assembly not found at {text}");
					return false;
				}
			}
			return true;
		});

		private const string DisableSkipInterop = "DisableSkipInterop";

		private const string AllowGatewayToParseQueries = "AllowGatewayToParseQueries";

		[DllImport("Microsoft.Azure.Documents.ServiceInterop.dll", BestFitMapping = false, CallingConvention = CallingConvention.Cdecl, SetLastError = true, ThrowOnUnmappableChar = true)]
		public static extern uint GetPartitionKeyRangesFromQuery([In] IntPtr serviceProvider, [In] [MarshalAs(UnmanagedType.LPWStr)] string query, [In] bool requireFormattableOrderByQuery, [In] bool isContinuationExpected, [In] bool allowNonValueAggregateQuery, [In] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] partitionKeyDefinitionPathTokens, [In] [MarshalAs(UnmanagedType.LPArray)] uint[] partitionKeyDefinitionPathTokenLengths, [In] uint partitionKeyDefinitionPathCount, [In] PartitionKind partitionKind, [In] [Out] IntPtr serializedQueryExecutionInfoBuffer, [In] uint serializedQueryExecutionInfoBufferLength, out uint serializedQueryExecutionInfoResultLength);

		[DllImport("Microsoft.Azure.Documents.ServiceInterop.dll", BestFitMapping = false, CallingConvention = CallingConvention.Cdecl, SetLastError = true, ThrowOnUnmappableChar = true)]
		public static extern uint CreateServiceProvider([In] [MarshalAs(UnmanagedType.LPStr)] string configJsonString, out IntPtr serviceProvider);

		[DllImport("Microsoft.Azure.Documents.ServiceInterop.dll", BestFitMapping = false, CallingConvention = CallingConvention.Cdecl, SetLastError = true, ThrowOnUnmappableChar = true)]
		public static extern uint UpdateServiceProvider([In] IntPtr serviceProvider, [In] [MarshalAs(UnmanagedType.LPStr)] string configJsonString);

		internal static bool IsGatewayAllowedToParseQueries()
		{
			string environmentVariable = Environment.GetEnvironmentVariable("DisableSkipInterop");
			DefaultTrace.TraceInformation(string.Format("ServiceInteropWrapper read {0} ENV as {1} ", "DisableSkipInterop", environmentVariable));
			bool? flag = BoolParse(environmentVariable);
			if (flag.HasValue)
			{
				return !flag.Value;
			}
			return true;
		}

		private static bool? BoolParse(string boolValueString)
		{
			if (!string.IsNullOrEmpty(boolValueString))
			{
				if (string.Equals(bool.TrueString, boolValueString, StringComparison.OrdinalIgnoreCase) || string.Equals(1.ToString(), boolValueString, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (string.Equals(bool.FalseString, boolValueString, StringComparison.OrdinalIgnoreCase) || string.Equals(0.ToString(), boolValueString, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				bool result = false;
				if (!bool.TryParse(boolValueString, out result))
				{
					return result;
				}
			}
			return null;
		}
	}
}
