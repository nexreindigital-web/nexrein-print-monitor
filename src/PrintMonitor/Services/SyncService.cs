using System.Globalization;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Models;

namespace PrintMonitor.Services;

public interface ISyncService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<int> SyncPendingJobsAsync(CancellationToken cancellationToken = default);
}

public class SyncService : ISyncService
{
    private readonly IApiClient _apiClient;
    private readonly PrintMonitorDbContext _dbContext;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<SyncService> _logger;

    private CancellationTokenSource? _cts;
    private Task? _syncTask;
    private int _consecutiveFailures = 0;

    public SyncService(
        IApiClient apiClient,
        PrintMonitorDbContext dbContext,
        SettingsManager settingsManager,
        ILogger<SyncService> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _syncTask = Task.Run(() => SyncLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("SyncService background loop started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SyncService...");
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_syncTask != null)
        {
            try
            {
                await Task.WhenAny(_syncTask, Task.Delay(3000, cancellationToken));
            }
            catch
            {
                // Task cancelled
            }
        }
    }

    private async Task SyncLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await SyncPendingJobsAsync(token);

                var baseInterval = Math.Max(5, _settingsManager.Settings.SyncIntervalSeconds);

                // Exponential backoff if consecutive failures occur
                var delaySeconds = baseInterval;
                if (_consecutiveFailures > 0)
                {
                    var backoffFactor = Math.Min(6, _consecutiveFailures);
                    delaySeconds = baseInterval * (1 << (backoffFactor - 1)); // 30s, 60s, 120s, 240s...
                    delaySeconds = Math.Min(delaySeconds, 600); // Max 10 minutes
                    _logger.LogDebug("Sync backoff active ({Failures} failures). Next attempt in {Delay}s", _consecutiveFailures, delaySeconds);
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in sync loop");
                await Task.Delay(10000, token);
            }
        }
    }

    public async Task<int> SyncPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Max(10, _settingsManager.Settings.MaxBatchSize);
        var pendingJobs = _dbContext.GetPendingSyncJobs(batchSize);

        if (pendingJobs.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Found {Count} pending print jobs to synchronize", pendingJobs.Count);

        var request = new PrintJobSyncRequest
        {
            DeviceId = _settingsManager.Settings.DeviceId,
            Jobs = pendingJobs.Select(j => new PrintJobSyncDto
            {
                ClientId = j.Id,
                JobUid = j.JobUid,
                JobId = j.JobId,
                PrinterName = j.PrinterName,
                PrinterServer = j.PrinterServer,
                DocumentName = j.DocumentName,
                Username = j.Username,
                Domain = j.Domain,
                ComputerName = j.ComputerName,
                Pages = j.Pages,
                PagesPrinted = j.PagesPrinted,
                Copies = j.Copies,
                ColorMode = j.ColorMode,
                Duplex = j.Duplex,
                PaperSize = j.PaperSize,
                Status = j.Status,
                ErrorMessage = j.ErrorMessage,
                SubmittedAt = j.SubmittedAt.ToString("o", CultureInfo.InvariantCulture),
                StartedAt = j.StartedAt?.ToString("o", CultureInfo.InvariantCulture),
                CompletedAt = j.CompletedAt?.ToString("o", CultureInfo.InvariantCulture)
            }).ToList()
        };

        var (success, response, error) = await _apiClient.SyncPrintJobsAsync(request, cancellationToken);

        if (success && response != null && response.Success)
        {
            _consecutiveFailures = 0;

            var syncedSuccesses = new List<(long clientId, long remoteId)>();

            foreach (var item in response.Results)
            {
                if (item.Status == "accepted" || item.Status == "duplicate")
                {
                    syncedSuccesses.Add((item.ClientId, item.RemoteId));
                }
            }

            // Fallback: If server returned processed_count but empty results list, mark all as synced
            if (syncedSuccesses.Count == 0 && response.ProcessedCount > 0)
            {
                foreach (var j in pendingJobs.Take(response.ProcessedCount))
                {
                    syncedSuccesses.Add((j.Id, 0));
                }
            }

            if (syncedSuccesses.Count > 0)
            {
                _dbContext.MarkJobsSynced(syncedSuccesses);
                _logger.LogInformation("Successfully synchronized {Count} jobs with server", syncedSuccesses.Count);
            }

            return syncedSuccesses.Count;
        }
        else
        {
            _consecutiveFailures++;
            var jobIds = pendingJobs.Select(j => j.Id).ToList();
            _dbContext.IncrementRetryCount(jobIds, error);
            _logger.LogWarning("Failed to synchronize batch: {Error}. Retrying later (Failures: {Failures})",
                error ?? "Unknown error", _consecutiveFailures);
            return 0;
        }
    }
}
