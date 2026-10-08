using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrintMonitor.Data;
using PrintMonitor.Services;

namespace PrintMonitor;

public class Worker : BackgroundService
{
    private readonly DatabaseInitializer _dbInitializer;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly IPrinterDiscoveryService _discoveryService;
    private readonly IPrintJobMonitorService _jobMonitorService;
    private readonly ISyncService _syncService;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<Worker> _logger;

    public Worker(
        DatabaseInitializer dbInitializer,
        IDeviceRegistrationService registrationService,
        IPrinterDiscoveryService discoveryService,
        IPrintJobMonitorService jobMonitorService,
        ISyncService syncService,
        IHealthCheckService healthCheckService,
        ILogger<Worker> logger)
    {
        _dbInitializer = dbInitializer;
        _registrationService = registrationService;
        _discoveryService = discoveryService;
        _jobMonitorService = jobMonitorService;
        _syncService = syncService;
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=================================================");
        _logger.LogInformation("PrintMonitor Service starting up...");
        _logger.LogInformation("OS: {OS}, Machine: {Machine}, User: {User}",
            Environment.OSVersion, Environment.MachineName, Environment.UserName);
        _logger.LogInformation("=================================================");

        try
        {
            // 1. Initialize SQLite database & directories
            _dbInitializer.Initialize();

            // 2. Discover printers
            _discoveryService.DiscoverPrinters();

            // 3. Attempt device registration (offline safe)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _registrationService.EnsureRegisteredAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Initial device registration check failed; will retry later.");
                }
            }, cancellationToken);

            // 4. Start background monitors
            await _jobMonitorService.StartAsync(cancellationToken);
            await _syncService.StartAsync(cancellationToken);
            await _healthCheckService.StartAsync(cancellationToken);

            await base.StartAsync(cancellationToken);
            _logger.LogInformation("PrintMonitor Service started and actively monitoring.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error during PrintMonitor Service startup");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Keep worker alive until stoppingToken is signalled
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PrintMonitor Service stopping cleanly...");

        try
        {
            await _healthCheckService.StopAsync(cancellationToken);
            await _syncService.StopAsync(cancellationToken);
            await _jobMonitorService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping background tasks");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("PrintMonitor Service stopped successfully.");
    }
}
