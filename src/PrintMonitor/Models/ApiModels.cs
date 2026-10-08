using System.Text.Json.Serialization;

namespace PrintMonitor.Models;

public class DeviceRegisterRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("computer_name")]
    public string ComputerName { get; set; } = string.Empty;

    [JsonPropertyName("windows_user")]
    public string WindowsUser { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("os_version")]
    public string OsVersion { get; set; } = string.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;

    [JsonPropertyName("application_version")]
    public string ApplicationVersion { get; set; } = string.Empty;

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("shop_name")]
    public string? ShopName { get; set; }
}

public class DeviceRegisterResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("dashboard_url")]
    public string? DashboardUrl { get; set; }

    [JsonPropertyName("default_password_note")]
    public string? DefaultPasswordNote { get; set; }
}

public class HeartbeatRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("computer_name")]
    public string ComputerName { get; set; } = string.Empty;

    [JsonPropertyName("shop_name")]
    public string? ShopName { get; set; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("application_version")]
    public string ApplicationVersion { get; set; } = "2.0.0";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "online";

    [JsonPropertyName("last_seen")]
    public string LastSeen { get; set; } = string.Empty;

    [JsonPropertyName("printer_count")]
    public int PrinterCount { get; set; }

    [JsonPropertyName("pending_sync_count")]
    public int PendingSyncCount { get; set; }
}

public class HeartbeatResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("new_password")]
    public string? NewPassword { get; set; }
}

public class PrintJobSyncDto
{
    [JsonPropertyName("client_id")]
    public long ClientId { get; set; }

    [JsonPropertyName("job_uid")]
    public string JobUid { get; set; } = string.Empty;

    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [JsonPropertyName("printer_name")]
    public string PrinterName { get; set; } = string.Empty;

    [JsonPropertyName("printer_server")]
    public string? PrinterServer { get; set; }

    [JsonPropertyName("document_name")]
    public string DocumentName { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("computer_name")]
    public string ComputerName { get; set; } = string.Empty;

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("pages_printed")]
    public int PagesPrinted { get; set; }

    [JsonPropertyName("copies")]
    public int Copies { get; set; }

    [JsonPropertyName("color_mode")]
    public string ColorMode { get; set; } = "Unknown";

    [JsonPropertyName("duplex")]
    public string Duplex { get; set; } = "Unknown";

    [JsonPropertyName("paper_size")]
    public string PaperSize { get; set; } = "Unknown";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Completed";

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("submitted_at")]
    public string SubmittedAt { get; set; } = string.Empty;

    [JsonPropertyName("started_at")]
    public string? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }
}

public class PrintJobSyncRequest
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("jobs")]
    public List<PrintJobSyncDto> Jobs { get; set; } = new();
}

public class SyncResultItem
{
    [JsonPropertyName("job_uid")]
    public string JobUid { get; set; } = string.Empty;

    [JsonPropertyName("client_id")]
    public long ClientId { get; set; }

    [JsonPropertyName("remote_id")]
    public long RemoteId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "accepted"; // accepted, duplicate, error

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class PrintJobSyncResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processed_count")]
    public int ProcessedCount { get; set; }

    [JsonPropertyName("results")]
    public List<SyncResultItem> Results { get; set; } = new();
}
