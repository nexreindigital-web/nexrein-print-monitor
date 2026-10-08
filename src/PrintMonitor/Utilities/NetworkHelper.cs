using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PrintMonitor.Utilities;

public static class NetworkHelper
{
    public static (string? ip, string? mac) GetPrimaryNetworkDetails()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                var ipProps = nic.GetIPProperties();
                var unicastAddresses = ipProps.UnicastAddresses;

                foreach (var addr in unicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        var mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
                        {
                            mac = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                        }
                        return (addr.Address.ToString(), mac);
                    }
                }
            }
        }
        catch
        {
            // Suppress errors during network queries
        }

        return (null, null);
    }

    public static bool IsNetworkAvailable()
    {
        try
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
        catch
        {
            return false;
        }
    }
}
