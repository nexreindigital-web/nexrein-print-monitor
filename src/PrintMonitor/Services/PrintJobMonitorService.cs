using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Models;

namespace PrintMonitor.Services;

public interface IPrintJobMonitorService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    void ScanPrintersOnce();
}

public class PrintJobMonitorService : IPrintJobMonitorService
{
    private readonly IPrintSpoolerService _spoolerService;
    private readonly IPrinterDiscoveryService _discoveryService;
    private readonly PrintMonitorDbContext _dbContext;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<PrintJobMonitorService> _logger;

    // Track active jobs in memory: Key = printerName, Value = Dictionary<int (JobId), PrintJob>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, PrintJob>> _activeJobs = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public PrintJobMonitorService(
        IPrintSpoolerService spoolerService,
        IPrinterDiscoveryService discoveryService,
        PrintMonitorDbContext dbContext,
        SettingsManager settingsManager,
        ILogger<PrintJobMonitorService> logger)
    {
        _spoolerService = spoolerService;
        _discoveryService = discoveryService;
        _dbContext = dbContext;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("PrintJobMonitorService background loop started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PrintJobMonitorService...");
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_monitorTask != null)
        {
            try
            {
                await Task.WhenAny(_monitorTask, Task.Delay(3000, cancellationToken));
            }
            catch
            {
                // Task cancelled
            }
        }
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        // First printer discovery pass
        _discoveryService.DiscoverPrinters();

        var pollInterval = TimeSpan.FromSeconds(Math.Max(2, _settingsManager.Settings.PollingIntervalSeconds));
        var discoveryInterval = TimeSpan.FromMinutes(2);
        var lastDiscovery = DateTime.UtcNow;

        while (!token.IsCancellationRequested)
        {
            try
            {
                // Periodically re-discover printers (detect added/removed printers)
                if (DateTime.UtcNow - lastDiscovery > discoveryInterval)
                {
                    _discoveryService.DiscoverPrinters();
                    lastDiscovery = DateTime.UtcNow;
                }

                ScanPrintersOnce();

                await Task.Delay(pollInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PrintJobMonitor loop");
                await Task.Delay(3000, token);
            }
        }
    }

    public void ScanPrintersOnce()
    {
        var printers = _discoveryService.GetCachedPrinters();
        var deviceId = _settingsManager.Settings.DeviceId;

        foreach (var printer in printers)
        {
            try
            {
                ScanPrinterJobs(printer.Name, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error scanning jobs for printer {Printer}", printer.Name);
            }
        }
    }

    private void ScanPrinterJobs(string printerName, string deviceId)
    {
        var currentSpoolerJobs = _spoolerService.GetCurrentJobs(printerName, deviceId);
        var activeForPrinter = _activeJobs.GetOrAdd(printerName, _ => new ConcurrentDictionary<int, PrintJob>());

        var currentJobIds = new HashSet<int>();

        // 1. Process jobs currently reported by the spooler
        foreach (var job in currentSpoolerJobs)
        {
            currentJobIds.Add(job.JobId);

            if (activeForPrinter.TryGetValue(job.JobId, out var trackedJob))
            {
                // Job already tracked; check for status or page updates
                bool updated = false;

                if (job.PagesPrinted > trackedJob.PagesPrinted)
                {
                    trackedJob.PagesPrinted = job.PagesPrinted;
                    updated = true;
                }

                if (job.Pages > trackedJob.Pages)
                {
                    trackedJob.Pages = job.Pages;
                    updated = true;
                }

                if (job.Status != trackedJob.Status)
                {
                    trackedJob.Status = job.Status;
                    trackedJob.UpdatedAt = DateTime.UtcNow;
                    if (job.Status == "Completed" || job.Status == "Printed")
                    {
                        trackedJob.CompletedAt ??= DateTime.UtcNow;
                    }
                    updated = true;
                }

                if (updated)
                {
                    _dbContext.SaveOrUpdateJob(trackedJob);
                    _logger.LogInformation("Job {JobId} on {Printer} updated: Status={Status}, Pages={PagesPrinted}/{Pages}",
                        job.JobId, printerName, trackedJob.Status, trackedJob.PagesPrinted, trackedJob.Pages);
                }
            }
            else
            {
                // Brand new job detected!
                _dbContext.SaveOrUpdateJob(job);
                activeForPrinter[job.JobId] = job;

                _logger.LogInformation("New print job detected: JobId={JobId}, Document='{Document}', User='{User}', Pages={Pages}, Printer='{Printer}'",
                    job.JobId, job.DocumentName, job.Username, job.Pages, printerName);
            }
        }

        // 2. Identify jobs that have disappeared from the spooler
        var disappearedIds = activeForPrinter.Keys.Where(id => !currentJobIds.Contains(id)).ToList();

        foreach (var disappearedId in disappearedIds)
        {
            if (activeForPrinter.TryRemove(disappearedId, out var completedJob))
            {
                // If it was not already cancelled or marked error, it successfully completed spooling/printing
                if (completedJob.Status != "Cancelled" && completedJob.Status != "Error")
                {
                    completedJob.Status = "Completed";
                    completedJob.CompletedAt ??= DateTime.UtcNow;
                    if (completedJob.PagesPrinted == 0 && completedJob.Pages > 0)
                    {
                        completedJob.PagesPrinted = completedJob.Pages;
                    }
                }

                completedJob.UpdatedAt = DateTime.UtcNow;
                _dbContext.SaveOrUpdateJob(completedJob);

                _logger.LogInformation("Print job finished and left spooler: JobId={JobId}, FinalStatus={Status}, Printer='{Printer}'",
                    completedJob.JobId, completedJob.Status, printerName);
            }
        }
    }
}
