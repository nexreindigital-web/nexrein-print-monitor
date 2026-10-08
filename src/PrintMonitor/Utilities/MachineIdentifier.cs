using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using PrintMonitor.Models;

namespace PrintMonitor.Utilities;

public static class MachineIdentifier
{
    private const string IdFileName = "device_id.txt";

    public static string GetOrCreateDeviceId(string dataDirectory, string? configuredDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(configuredDeviceId))
        {
            return configuredDeviceId.Trim();
        }

        try
        {
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            var idPath = Path.Combine(dataDirectory, IdFileName);
            if (File.Exists(idPath))
            {
                var existingId = File.ReadAllText(idPath).Trim();
                if (Guid.TryParse(existingId, out var parsedGuid))
                {
                    return parsedGuid.ToString("D");
                }
            }

            // Generate new persistent GUID
            var newId = Guid.NewGuid().ToString("D");
            File.WriteAllText(idPath, newId);
            return newId;
        }
        catch
        {
            // Fallback in case of temporary IO constraints
            return Guid.NewGuid().ToString("D");
        }
    }

    public static ComputerInfo GetCurrentComputerInfo(string deviceId, string appVersion = "1.0.0")
    {
        var (ip, mac) = NetworkHelper.GetPrimaryNetworkDetails();

        return new ComputerInfo
        {
            DeviceId = deviceId,
            ComputerName = Environment.MachineName,
            WindowsUser = Environment.UserName,
            Domain = Environment.UserDomainName,
            OsVersion = RuntimeInformation.OSDescription,
            OsArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            ApplicationVersion = appVersion,
            IpAddress = ip,
            MacAddress = mac,
            InstalledAt = DateTime.UtcNow
        };
    }
}
