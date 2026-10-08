namespace PrintMonitor.Models;

public class PrintJob
{
    public long Id { get; set; }
    public long? RemoteId { get; set; }
    
    // Windows Job ID reported by Spooler
    public int JobId { get; set; }
    
    // Unique fingerprint: {DeviceId}_{PrinterName}_{JobId}_{SubmittedAtUtcTicks}
    public string JobUid { get; set; } = string.Empty;

    public string PrinterName { get; set; } = string.Empty;
    public string? PrinterServer { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string ComputerName { get; set; } = string.Empty;

    public int Pages { get; set; }
    public int PagesPrinted { get; set; }
    public int Copies { get; set; } = 1;
    public string ColorMode { get; set; } = "Unknown"; // Monochrome, Color, Unknown
    public string Duplex { get; set; } = "Unknown"; // Simplex, DuplexLongEdge, DuplexShortEdge, Unknown
    public string PaperSize { get; set; } = "Unknown";
    
    // Status: Submitted, Printing, Printed, Completed, Paused, Error, Deleted, Cancelled, Unknown
    public string Status { get; set; } = "Submitted";
    public string? ErrorMessage { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }

    // Sync status: Pending, Synced, Failed
    public string SyncStatus { get; set; } = "Pending";
    public int RetryCount { get; set; }
}
