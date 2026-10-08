using Microsoft.Extensions.Logging;
using PrintMonitor.Models;
using PrintMonitor.Native;

namespace PrintMonitor.Services;

public interface IPrintSpoolerService
{
    List<PrintJob> GetCurrentJobs(string printerName, string deviceId);
    PrintJob? GetJob(string printerName, int jobId, string deviceId);
    IntPtr CreateNotificationHandle(string printerName);
    bool CheckNotification(IntPtr hNotification, out int changeFlags);
    void CloseNotificationHandle(IntPtr hNotification);
}

public class PrintSpoolerService : IPrintSpoolerService
{
    private readonly ILogger<PrintSpoolerService> _logger;

    public PrintSpoolerService(ILogger<PrintSpoolerService> logger)
    {
        _logger = logger;
    }

    public List<PrintJob> GetCurrentJobs(string printerName, string deviceId)
    {
        try
        {
            return Win32PrintSpooler.EnumerateJobs(printerName, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating jobs for printer {Printer}", printerName);
            return new List<PrintJob>();
        }
    }

    public PrintJob? GetJob(string printerName, int jobId, string deviceId)
    {
        try
        {
            return Win32PrintSpooler.GetJob(printerName, jobId, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job {JobId} on printer {Printer}", jobId, printerName);
            return null;
        }
    }

    public IntPtr CreateNotificationHandle(string printerName)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(printerName))
            return IntPtr.Zero;

        try
        {
            if (!NativeMethods.OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            {
                _logger.LogWarning("Could not OpenPrinter for notification on {Printer}", printerName);
                return IntPtr.Zero;
            }

            var hNotification = NativeMethods.FindFirstPrinterChangeNotification(
                hPrinter,
                SpoolerConstants.PRINTER_CHANGE_JOB,
                0,
                IntPtr.Zero);

            // We must close the printer handle after setting up notification, or keep it depending on usage.
            // Notice: FindFirstPrinterChangeNotification creates an event handle tied to the printer handle.
            // We return the notification handle.
            NativeMethods.ClosePrinter(hPrinter);

            if (hNotification == IntPtr.Zero || hNotification == new IntPtr(-1))
            {
                _logger.LogWarning("FindFirstPrinterChangeNotification returned invalid handle for {Printer}", printerName);
                return IntPtr.Zero;
            }

            return hNotification;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create change notification handle for {Printer}", printerName);
            return IntPtr.Zero;
        }
    }

    public bool CheckNotification(IntPtr hNotification, out int changeFlags)
    {
        changeFlags = 0;
        if (hNotification == IntPtr.Zero || hNotification == new IntPtr(-1))
            return false;

        try
        {
            return NativeMethods.FindNextPrinterChangeNotification(
                hNotification,
                out changeFlags,
                IntPtr.Zero,
                out _);
        }
        catch
        {
            return false;
        }
    }

    public void CloseNotificationHandle(IntPtr hNotification)
    {
        if (hNotification != IntPtr.Zero && hNotification != new IntPtr(-1))
        {
            try
            {
                NativeMethods.FindClosePrinterChangeNotification(hNotification);
            }
            catch
            {
                // Suppress on shutdown
            }
        }
    }
}
