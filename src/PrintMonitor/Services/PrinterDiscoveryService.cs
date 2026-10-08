using Microsoft.Extensions.Logging;
using PrintMonitor.Data;
using PrintMonitor.Models;
using PrintMonitor.Native;

namespace PrintMonitor.Services;

public interface IPrinterDiscoveryService
{
    List<PrinterInfo> DiscoverPrinters();
    List<PrinterInfo> GetCachedPrinters();
}

public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    private readonly PrintMonitorDbContext _dbContext;
    private readonly ILogger<PrinterDiscoveryService> _logger;
    private readonly List<PrinterInfo> _cachedPrinters = new();
    private readonly object _lock = new();

    public PrinterDiscoveryService(PrintMonitorDbContext dbContext, ILogger<PrinterDiscoveryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public List<PrinterInfo> DiscoverPrinters()
    {
        try
        {
            var discovered = Win32PrintSpooler.EnumeratePrinters();
            _logger.LogInformation("Discovered {Count} printers from Windows Print Spooler", discovered.Count);

            lock (_lock)
            {
                _cachedPrinters.Clear();
                _cachedPrinters.AddRange(discovered);
            }

            // Persist to SQLite
            _dbContext.SaveOrUpdatePrinters(discovered);

            return discovered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover printers from Print Spooler");
            return GetCachedPrinters();
        }
    }

    public List<PrinterInfo> GetCachedPrinters()
    {
        lock (_lock)
        {
            if (_cachedPrinters.Count > 0)
                return new List<PrinterInfo>(_cachedPrinters);
        }

        // Fallback to SQLite
        var fromDb = _dbContext.GetAllPrinters();
        lock (_lock)
        {
            _cachedPrinters.Clear();
            _cachedPrinters.AddRange(fromDb);
        }
        return fromDb;
    }
}
