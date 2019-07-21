using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Microsoft.Azure.Documents
{
	internal static class NetUtil
	{
		/// <summary>
		/// Get a single non-loopback (i.e., not 127.0.0.0/8)
		/// IP address of the local machine.
		/// </summary>
		/// <returns></returns>
		public static string GetNonLoopbackIpV4Address()
		{
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet && networkInterface.OperationalStatus == OperationalStatus.Up)
				{
					foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
					{
						if (unicastAddress.IsDnsEligible && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							return unicastAddress.Address.ToString();
						}
					}
				}
			}
			DefaultTrace.TraceCritical("ERROR: Could not locate any usable IPv4 address");
			throw new ConfigurationErrorsException("ERROR: Could not locate any usable IPv4 address");
		}

		/// <summary>
		/// Get a single non-loopback (i.e., not 127.0.0.0/8)
		/// IP address of the local machine.  Similar to GetNonLoopbackIpV4Address but allows
		/// non-dns eligible adapters
		/// </summary>
		/// <returns></returns>
		public static string GetLocalEmulatorIpV4Address()
		{
			string text = null;
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && networkInterface.OperationalStatus == OperationalStatus.Up)
				{
					foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
					{
						if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							if (unicastAddress.IsDnsEligible)
							{
								return unicastAddress.Address.ToString();
							}
							if (text == null)
							{
								text = unicastAddress.Address.ToString();
							}
						}
					}
				}
			}
			if (text != null)
			{
				return text;
			}
			DefaultTrace.TraceCritical("ERROR: Could not locate any usable IPv4 address for local emulator");
			throw new ConfigurationErrorsException("ERROR: Could not locate any usable IPv4 address for local emulator");
		}

		public static bool GetIPv6ServiceTunnelAddress(bool isEmulated, out IPAddress ipv6LoopbackAddress)
		{
			if (isEmulated)
			{
				ipv6LoopbackAddress = IPAddress.IPv6Loopback;
				return true;
			}
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			for (int i = 0; i < allNetworkInterfaces.Length; i++)
			{
				foreach (UnicastIPAddressInformation unicastAddress in allNetworkInterfaces[i].GetIPProperties().UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6 && IsServiceTunneledIPAddress(unicastAddress.Address))
					{
						DefaultTrace.TraceInformation("Found VNET service tunnel destination: {0}", unicastAddress.Address.ToString());
						ipv6LoopbackAddress = unicastAddress.Address;
						return true;
					}
					DefaultTrace.TraceVerbose("{0} is skipped because it is not IPv6 or is not a service tunneled IP address.", unicastAddress.Address.ToString());
				}
			}
			DefaultTrace.TraceInformation("Cannot find the IPv6 address of the Loopback NetworkInterface.");
			ipv6LoopbackAddress = null;
			return false;
		}

		private static bool IsServiceTunneledIPAddress(IPAddress ipAddress)
		{
			return BitConverter.ToUInt64(ipAddress.GetAddressBytes(), 0) == BitConverter.ToUInt64(new byte[8]
			{
				38,
				3,
				16,
				225,
				1,
				0,
				0,
				2
			}, 0);
		}
	}
}
