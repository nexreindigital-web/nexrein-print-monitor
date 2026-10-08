namespace PrintMonitor.Models;

public class PrinterInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public string? ShareName { get; set; }
    public string? PortName { get; set; }
    public string? DriverName { get; set; }
    public string? Comment { get; set; }
    public string? Location { get; set; }

    public string Status { get; set; } = "Ready";
    public int StatusRaw { get; set; }
    public int JobCount { get; set; }
    public int Attributes { get; set; }

    public bool IsDefault { get; set; }
    public bool IsNetwork { get; set; }
    public bool IsShared { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
