using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Microsoft.Azure.Documents
{
	/// <summary>
	/// Extension class for defining methods/properties on Type class that are 
	/// not available on .NET Standard 1.6. This allows us to keep the same code
	/// we had earlier and when compiling for .NET Standard 1.6, we use these 
	/// extension methods that call GetTypeInfo() on the Type instance and call
	/// the corresponding method on it.
	///
	/// IsGenericType, IsEnum, IsValueType, IsInterface and BaseType are properties
	/// on Type class but since we cannot define "extension properties", I've converted 
	/// them to methods and return the underlying property value from the call to
	/// GetTypeInfo(). For .NET Framework, these extension methods simply return 
	/// the underlying property value.
	/// </summary>
	internal static class CustomTypeExtensions
	{
		public const int UnicodeEncodingCharSize = 2;

		public const string SDKName = "documentdb-netcore-sdk";

		public const string SDKVersion = "2.4.0";

		public static bool IsAssignableFrom(this Type type, Type c)
		{
			return type.GetTypeInfo().IsAssignableFrom(c);
		}

		public static bool IsSubclassOf(this Type type, Type c)
		{
			return type.GetTypeInfo().IsSubclassOf(c);
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingAttr)
		{
			return type.GetTypeInfo().GetMethod(name, bindingAttr);
		}

		public static MethodInfo GetMethod(this Type type, string name)
		{
			return type.GetTypeInfo().GetMethod(name);
		}

		public static MethodInfo GetMethod(this Type type, string name, Type[] types)
		{
			return type.GetTypeInfo().GetMethod(name, types);
		}

		public static Type[] GetGenericArguments(this Type type)
		{
			return type.GetTypeInfo().GetGenericArguments();
		}

		public static PropertyInfo GetProperty(this Type type, string name)
		{
			return type.GetTypeInfo().GetProperty(name);
		}

		public static PropertyInfo[] GetProperties(this Type type)
		{
			return type.GetTypeInfo().GetProperties();
		}

		public static PropertyInfo[] GetProperties(this Type type, BindingFlags bindingAttr)
		{
			return type.GetTypeInfo().GetProperties(bindingAttr);
		}

		public static Type[] GetInterfaces(this Type type)
		{
			return type.GetTypeInfo().GetInterfaces();
		}

		public static ConstructorInfo GetConstructor(this Type type, Type[] types)
		{
			return type.GetTypeInfo().GetConstructor(types);
		}

		public static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute
		{
			return CustomAttributeExtensions.GetCustomAttribute<T>(type.GetTypeInfo(), inherit);
		}

		public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType, bool inherit)
		{
			return CustomAttributeExtensions.GetCustomAttributes(type.GetTypeInfo(), attributeType, inherit);
		}

		public static byte[] GetBuffer(this MemoryStream stream)
		{
			stream.TryGetBuffer(out ArraySegment<byte> buffer);
			return buffer.Array;
		}

		public static void Close(this Stream stream)
		{
			stream.Dispose();
		}

		public static void Close(this TcpClient tcpClient)
		{
			tcpClient.Dispose();
		}

		public static string GetLeftPart(this Uri uri, UriPartial part)
		{
			switch (part)
			{
			case UriPartial.Authority:
				return uri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped);
			case UriPartial.Path:
				return uri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);
			case UriPartial.Query:
				return uri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path | UriComponents.Query, UriFormat.UriEscaped);
			case UriPartial.Scheme:
				return uri.GetComponents(UriComponents.Scheme | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
			default:
				throw new ArgumentException("Invalid part", "part");
			}
		}

		public static Delegate CreateDelegate(Type delegateType, object target, MethodInfo methodInfo)
		{
			return methodInfo.CreateDelegate(delegateType, target);
		}

		public static IntPtr SecureStringToCoTaskMemAnsi(SecureString secureString)
		{
			return SecureStringMarshal.SecureStringToCoTaskMemAnsi(secureString);
		}

		public static void SetActivityId(ref Guid id)
		{
			EventSource.SetCurrentThreadActivityId(id);
		}

		public static Random GetRandomNumber()
		{
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				byte[] array = new byte[4];
				randomNumberGenerator.GetBytes(array);
				return new Random(BitConverter.ToInt32(array, 0));
			}
		}

		public static QueryRequestPerformanceActivity StartActivity(DocumentServiceRequest request)
		{
			return null;
		}

		public static string GenerateBaseUserAgentString()
		{
			string oSVersion = PlatformApis.GetOSVersion();
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1} {2}/{3}", PlatformApis.GetOSPlatform(), string.IsNullOrEmpty(oSVersion) ? "Unknown" : oSVersion.Trim(), "documentdb-netcore-sdk", "2.4.0");
		}

		public static bool ConfirmOpen(Socket socket)
		{
			bool blocking = socket.Blocking;
			try
			{
				byte[] buffer = new byte[1];
				socket.Blocking = false;
				socket.Send(buffer, 0, SocketFlags.None);
				return true;
			}
			catch (SocketException ex)
			{
				return ex.SocketErrorCode == SocketError.WouldBlock;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			finally
			{
				socket.Blocking = blocking;
			}
		}

		public static bool ByPassQueryParsing()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || IntPtr.Size != 8 || !ServiceInteropWrapper.AssembliesExist.Value)
			{
				DefaultTrace.TraceVerbose($"Bypass query parsing. IsWindowsOSPlatform {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)} IntPtr.Size is {IntPtr.Size} ServiceInteropWrapper.AssembliesExist {ServiceInteropWrapper.AssembliesExist.Value}");
				return true;
			}
			return false;
		}

		public static bool IsGenericType(this Type type)
		{
			return type.GetTypeInfo().IsGenericType;
		}

		public static bool IsEnum(this Type type)
		{
			return type.GetTypeInfo().IsEnum;
		}

		public static bool IsValueType(this Type type)
		{
			return type.GetTypeInfo().IsValueType;
		}

		public static bool IsInterface(this Type type)
		{
			return type.GetTypeInfo().IsInterface;
		}

		public static Type GetBaseType(this Type type)
		{
			return type.GetTypeInfo().BaseType;
		}

		public static Type GeUnderlyingSystemType(this Type type)
		{
			return type.GetTypeInfo().UnderlyingSystemType;
		}

		public static Assembly GetAssembly(this Type type)
		{
			return type.GetTypeInfo().Assembly;
		}

		public static IEnumerable<CustomAttributeData> GetsCustomAttributes(this Type type)
		{
			return type.GetTypeInfo().CustomAttributes;
		}
	}
}
