using System.Globalization;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Models;

namespace PrintMonitor.Services;

public interface IHealthCheckService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<bool> SendHeartbeatOnceAsync(CancellationToken cancellationToken = default);
}

public class HealthCheckService : IHealthCheckService
{
    private readonly IApiClient _apiClient;
    private readonly PrintMonitorDbContext _dbContext;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<HealthCheckService> _logger;

    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;

    public HealthCheckService(
        IApiClient apiClient,
        PrintMonitorDbContext dbContext,
        SettingsManager settingsManager,
        ILogger<HealthCheckService> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("HealthCheckService background loop started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping HealthCheckService...");
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_heartbeatTask != null)
        {
            try
            {
                await Task.WhenAny(_heartbeatTask, Task.Delay(3000, cancellationToken));
            }
            catch
            {
                // Task cancelled
            }
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _settingsManager.Settings.HeartbeatIntervalSeconds));

        while (!token.IsCancellationRequested)
        {
            try
            {
                await SendHeartbeatOnceAsync(token);
                await Task.Delay(interval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error in Heartbeat loop");
                await Task.Delay(15000, token);
            }
        }
    }

    public async Task<bool> SendHeartbeatOnceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceId = _settingsManager.Settings.DeviceId;
            var printerCount = _dbContext.GetPrinterCount();
            var pendingCount = _dbContext.GetPendingSyncCount();

            var request = new HeartbeatRequest
            {
                DeviceId = deviceId,
                ComputerName = Environment.MachineName,
                ShopName = _settingsManager.Settings.ShopName,
                UserEmail = _settingsManager.Settings.UserEmail,
                ApplicationVersion = "2.0.0",
                Status = "online",
                LastSeen = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                PrinterCount = printerCount,
                PendingSyncCount = pendingCount
            };

            var (success, response, error) = await _apiClient.SendHeartbeatAsync(request, cancellationToken);
            if (success)
            {
                _dbContext.UpdateHeartbeat(deviceId, DateTime.UtcNow);
                _logger.LogDebug("Heartbeat sent successfully.");

                // Process remote password updates from the Laravel Web Portal
                if (response != null &&
                    response.Command == "update_software_password" &&
                    !string.IsNullOrWhiteSpace(response.NewPassword))
                {
                    _logger.LogInformation("Received remote password update command from Nexrein Printer Monitor Web Portal.");
                    var newHash = PrintMonitor.Utilities.SecurityHelper.HashPassword(response.NewPassword);
                    _settingsManager.SaveSettings(s =>
                    {
                        s.AdminPasswordHash = newHash;
                    });

                    await _apiClient.AcknowledgePasswordAsync(deviceId, cancellationToken);
                    _logger.LogInformation("Desktop Super Admin password successfully updated and synchronized with cloud portal.");
                }

                return true;
            }
            else
            {
                _logger.LogDebug("Heartbeat failed: {Error}", error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Exception while sending heartbeat");
            return false;
        }
    }
}
