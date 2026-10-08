namespace PrintMonitor.Models;

public class AgentSettings
{
    public const string SectionName = "PrintMonitor";

    public string ApiBaseUrl { get; set; } = "https://printmonitor.nexreindigital.co.ke/api";
    public string ApiKey { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public int SyncIntervalSeconds { get; set; } = 30;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 50;
    public int MaxRetryAttempts { get; set; } = 5;
    public string DatabasePath { get; set; } = @"C:\ProgramData\PrintMonitor\printmonitor.db";
    public string LogDirectory { get; set; } = @"C:\ProgramData\PrintMonitor\Logs";
    public string LogLevel { get; set; } = "Information";
    public string AdminPasswordHash { get; set; } = string.Empty;
    public bool AutostartOnBoot { get; set; } = true;
}
