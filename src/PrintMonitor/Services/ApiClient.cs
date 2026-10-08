using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Models;

namespace PrintMonitor.Services;

public interface IApiClient
{
    Task<(bool success, DeviceRegisterResponse? response, string? error)> RegisterDeviceAsync(DeviceRegisterRequest request, CancellationToken ct = default);
    Task<(bool success, HeartbeatResponse? response, string? error)> SendHeartbeatAsync(HeartbeatRequest request, CancellationToken ct = default);
    Task<(bool success, PrintJobSyncResponse? response, string? error)> SyncPrintJobsAsync(PrintJobSyncRequest request, CancellationToken ct = default);
    Task<(bool success, string? message)> TestConnectionAsync(CancellationToken ct = default);
    Task<bool> AcknowledgePasswordAsync(string deviceId, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<ApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ApiClient(HttpClient httpClient, SettingsManager settingsManager, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    private void PrepareHeaders()
    {
        var settings = _settingsManager.Settings;
        var rawUrl = (settings.ApiBaseUrl ?? "https://printmonitor.nexreindigital.co.ke/api").Trim().TrimEnd('/');
        
        // Ensure the URL targets the API root and terminates with a trailing slash for RFC 3986 relative URI resolution
        if (!rawUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            rawUrl += "/api";
        }
        var baseUrl = rawUrl + "/";

        if (_httpClient.BaseAddress == null || _httpClient.BaseAddress.ToString() != baseUrl)
        {
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            {
                _httpClient.BaseAddress = uri;
            }
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexrein-PrinterMonitor-Agent/2.0.0");

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey.Trim());
        }

        if (!string.IsNullOrWhiteSpace(settings.DeviceId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Device-Id", settings.DeviceId.Trim());
        }
    }

    public async Task<(bool success, DeviceRegisterResponse? response, string? error)> RegisterDeviceAsync(
        DeviceRegisterRequest request,
        CancellationToken ct = default)
    {
        try
        {
            PrepareHeaders();
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending device registration for {DeviceId} ({Computer})", request.DeviceId, request.ComputerName);

            var response = await _httpClient.PostAsync("devices/register", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<DeviceRegisterResponse>(responseBody, JsonOptions);
                if (result != null && !string.IsNullOrWhiteSpace(result.ApiKey))
                {
                    _settingsManager.SaveSettings(s => s.ApiKey = result.ApiKey);
                }
                return (true, result, null);
            }

            var error = $"HTTP {(int)response.StatusCode}: {responseBody}";
            _logger.LogWarning("Device registration returned non-success: {Error}", error);
            return (false, null, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Device registration failed (network/server offline): {Message}", ex.Message);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, HeartbeatResponse? response, string? error)> SendHeartbeatAsync(
        HeartbeatRequest request,
        CancellationToken ct = default)
    {
        try
        {
            PrepareHeaders();
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("devices/heartbeat", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<HeartbeatResponse>(responseBody, JsonOptions);
                return (true, result, null);
            }

            var error = $"HTTP {(int)response.StatusCode}: {responseBody}";
            return (false, null, error);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, PrintJobSyncResponse? response, string? error)> SyncPrintJobsAsync(
        PrintJobSyncRequest request,
        CancellationToken ct = default)
    {
        try
        {
            PrepareHeaders();
            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending batch sync of {Count} print jobs to backend", request.Jobs.Count);

            var response = await _httpClient.PostAsync("print-jobs/sync", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PrintJobSyncResponse>(responseBody, JsonOptions);
                return (true, result, null);
            }

            var error = $"HTTP {(int)response.StatusCode}: {responseBody}";
            _logger.LogWarning("Print job sync returned non-success: {Error}", error);
            return (false, null, error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Print job sync failed: {Message}", ex.Message);
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, string? message)> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            PrepareHeaders();
            var settings = _settingsManager.Settings;
            var response = await _httpClient.GetAsync("health", ct);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"Connected successfully to {settings.ApiBaseUrl} (HTTP {(int)response.StatusCode})");
            }

            return (false, $"Received HTTP {(int)response.StatusCode} from {settings.ApiBaseUrl}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection test failed: {ex.Message}");
        }
    }

    public async Task<bool> AcknowledgePasswordAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            PrepareHeaders();
            var json = JsonSerializer.Serialize(new { device_id = deviceId }, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("devices/acknowledge-password", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
