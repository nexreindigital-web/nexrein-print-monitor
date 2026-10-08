namespace PrintMonitor.Models;

public class ComputerInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string WindowsUser { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string OsVersion { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = "1.0.0";
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeatAt { get; set; }
    public bool IsRegistered { get; set; }
}
