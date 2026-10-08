using System.ComponentModel;
using System.Runtime.InteropServices;
using PrintMonitor.Models;

namespace PrintMonitor.Native;

public class Win32PrintSpooler
{
    public static List<PrinterInfo> EnumeratePrinters()
    {
        var printers = new List<PrinterInfo>();
        if (!OperatingSystem.IsWindows())
            return printers;

        int flags = SpoolerConstants.PRINTER_ENUM_LOCAL | SpoolerConstants.PRINTER_ENUM_CONNECTIONS;
        int bytesNeeded = 0;
        int count = 0;

        // First call to determine buffer size
        NativeMethods.EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out bytesNeeded, out count);

        if (bytesNeeded <= 0)
            return printers;

        var pBuffer = Marshal.AllocHGlobal(bytesNeeded);
        try
        {
            if (NativeMethods.EnumPrinters(flags, null, 2, pBuffer, bytesNeeded, out bytesNeeded, out count))
            {
                var structSize = Marshal.SizeOf<PRINTER_INFO_2>();
                for (int i = 0; i < count; i++)
                {
                    var pCurrent = IntPtr.Add(pBuffer, i * structSize);
                    var pi2 = Marshal.PtrToStructure<PRINTER_INFO_2>(pCurrent);

                    var name = pi2.pPrinterName ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var isNetwork = (pi2.Attributes & SpoolerConstants.PRINTER_ENUM_NETWORK) != 0 ||
                                    !string.IsNullOrEmpty(pi2.pServerName);
                    var isShared = (pi2.Attributes & SpoolerConstants.PRINTER_ENUM_SHARED) != 0;
                    var isDefault = (pi2.Attributes & SpoolerConstants.PRINTER_ENUM_DEFAULT) != 0;

                    var statusText = InterpretPrinterStatus(pi2.Status);

                    printers.Add(new PrinterInfo
                    {
                        Name = name,
                        ServerName = pi2.pServerName,
                        ShareName = pi2.pShareName,
                        PortName = pi2.pPortName,
                        DriverName = pi2.pDriverName,
                        Comment = pi2.pComment,
                        Location = pi2.pLocation,
                        Status = statusText,
                        StatusRaw = pi2.Status,
                        JobCount = pi2.cJobs,
                        Attributes = pi2.Attributes,
                        IsDefault = isDefault,
                        IsNetwork = isNetwork,
                        IsShared = isShared,
                        IsActive = true,
                        FirstSeenAt = DateTime.UtcNow,
                        LastSeenAt = DateTime.UtcNow
                    });
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pBuffer);
        }

        return printers;
    }

    public static List<PrintJob> EnumerateJobs(string printerName, string deviceId)
    {
        var jobs = new List<PrintJob>();
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(printerName))
            return jobs;

        if (!NativeMethods.OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            return jobs;

        try
        {
            int bytesNeeded = 0;
            int count = 0;

            // First call to determine buffer size
            NativeMethods.EnumJobs(hPrinter, 0, 1000, 2, IntPtr.Zero, 0, out bytesNeeded, out count);

            if (bytesNeeded <= 0)
                return jobs;

            var pBuffer = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (NativeMethods.EnumJobs(hPrinter, 0, 1000, 2, pBuffer, bytesNeeded, out bytesNeeded, out count))
                {
                    var structSize = Marshal.SizeOf<JOB_INFO_2>();
                    for (int i = 0; i < count; i++)
                    {
                        var pCurrent = IntPtr.Add(pBuffer, i * structSize);
                        var ji2 = Marshal.PtrToStructure<JOB_INFO_2>(pCurrent);

                        var job = ParseJobInfo2(ji2, printerName, deviceId);
                        if (job != null)
                        {
                            jobs.Add(job);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }
        }
        finally
        {
            NativeMethods.ClosePrinter(hPrinter);
        }

        return jobs;
    }

    public static PrintJob? GetJob(string printerName, int jobId, string deviceId)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(printerName))
            return null;

        if (!NativeMethods.OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            return null;

        try
        {
            int bytesNeeded = 0;
            NativeMethods.GetJob(hPrinter, jobId, 2, IntPtr.Zero, 0, out bytesNeeded);

            if (bytesNeeded <= 0)
                return null;

            var pBuffer = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (NativeMethods.GetJob(hPrinter, jobId, 2, pBuffer, bytesNeeded, out bytesNeeded))
                {
                    var ji2 = Marshal.PtrToStructure<JOB_INFO_2>(pBuffer);
                    return ParseJobInfo2(ji2, printerName, deviceId);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }
        }
        finally
        {
            NativeMethods.ClosePrinter(hPrinter);
        }

        return null;
    }

    public static PrintJob? ParseJobInfo2(JOB_INFO_2 ji2, string printerName, string deviceId)
    {
        var submittedUtc = ji2.Submitted.ToDateTimeUtc() ?? DateTime.UtcNow;
        var cleanPrinterName = !string.IsNullOrEmpty(ji2.pPrinterName) ? ji2.pPrinterName : printerName;

        // Build stable fingerprint UID
        // JobUid: {DeviceId}_{PrinterName}_{JobId}_{SubmittedAtUtcTicks}
        var jobUid = $"{deviceId}_{cleanPrinterName}_{ji2.JobId}_{submittedUtc.Ticks}";

        var (colorMode, duplex, paperSize, copies) = ExtractDevModeDetails(ji2.pDevMode);

        var (status, errorMsg) = InterpretJobStatus(ji2.Status, ji2.pStatus, ji2.PagesPrinted, ji2.TotalPages);

        var job = new PrintJob
        {
            JobId = ji2.JobId,
            JobUid = jobUid,
            PrinterName = cleanPrinterName,
            PrinterServer = ji2.pMachineName,
            DocumentName = !string.IsNullOrEmpty(ji2.pDocument) ? ji2.pDocument : "Untitled Document",
            Username = !string.IsNullOrEmpty(ji2.pUserName) ? ji2.pUserName : Environment.UserName,
            Domain = Environment.UserDomainName,
            ComputerName = !string.IsNullOrEmpty(ji2.pMachineName) ? ji2.pMachineName.TrimStart('\\') : Environment.MachineName,
            Pages = ji2.TotalPages > 0 ? ji2.TotalPages : (ji2.PagesPrinted > 0 ? ji2.PagesPrinted : 1),
            PagesPrinted = ji2.PagesPrinted,
            Copies = copies,
            ColorMode = colorMode,
            Duplex = duplex,
            PaperSize = paperSize,
            Status = status,
            ErrorMessage = errorMsg,
            SubmittedAt = submittedUtc,
            StartedAt = (status == "Printing" || ji2.PagesPrinted > 0) ? submittedUtc : null,
            CompletedAt = (status == "Completed" || status == "Printed") ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SyncStatus = "Pending"
        };

        return job;
    }

    public static (string colorMode, string duplex, string paperSize, int copies) ExtractDevModeDetails(IntPtr pDevMode)
    {
        var color = "Unknown";
        var duplex = "Unknown";
        var paper = "Unknown";
        var copies = 1;

        if (pDevMode == IntPtr.Zero)
            return (color, duplex, paper, copies);

        try
        {
            var dm = Marshal.PtrToStructure<DEVMODE>(pDevMode);

            if ((dm.dmFields & SpoolerConstants.DM_COPIES) != 0 && dm.dmCopies > 0)
            {
                copies = dm.dmCopies;
            }

            if ((dm.dmFields & SpoolerConstants.DM_COLOR) != 0)
            {
                color = dm.dmColor switch
                {
                    SpoolerConstants.DMCOLOR_MONOCHROME => "Monochrome",
                    SpoolerConstants.DMCOLOR_COLOR => "Color",
                    _ => "Unknown"
                };
            }

            if ((dm.dmFields & SpoolerConstants.DM_DUPLEX) != 0)
            {
                duplex = dm.dmDuplex switch
                {
                    SpoolerConstants.DMDUP_SIMPLEX => "Simplex",
                    SpoolerConstants.DMDUP_VERTICAL => "DuplexLongEdge",
                    SpoolerConstants.DMDUP_HORIZONTAL => "DuplexShortEdge",
                    _ => "Unknown"
                };
            }

            if (!string.IsNullOrWhiteSpace(dm.dmFormName))
            {
                paper = dm.dmFormName.Trim();
            }
            else if ((dm.dmFields & SpoolerConstants.DM_PAPERSIZE) != 0 && dm.dmPaperSize > 0)
            {
                paper = GetPaperSizeName(dm.dmPaperSize);
            }
        }
        catch
        {
            // DEVMODE parsing failure fallback
        }

        return (color, duplex, paper, copies);
    }

    public static (string status, string? error) InterpretJobStatus(int statusFlags, string? stringStatus, int pagesPrinted, int totalPages)
    {
        if (!string.IsNullOrWhiteSpace(stringStatus))
        {
            var lower = stringStatus.ToLowerInvariant();
            if (lower.Contains("error"))
                return ("Error", stringStatus);
            if (lower.Contains("delet"))
                return ("Deleted", null);
            if (lower.Contains("pause"))
                return ("Paused", null);
            if (lower.Contains("printed") || lower.Contains("complete"))
                return ("Completed", null);
            if (lower.Contains("printing"))
                return ("Printing", null);
        }

        if ((statusFlags & SpoolerConstants.JOB_STATUS_ERROR) != 0)
            return ("Error", "Spooler reported job error");

        if ((statusFlags & SpoolerConstants.JOB_STATUS_DELETED) != 0 ||
            (statusFlags & SpoolerConstants.JOB_STATUS_DELETING) != 0)
        {
            // If it was already printed or completed most pages, it completed before deletion
            if (pagesPrinted > 0 && totalPages > 0 && pagesPrinted >= totalPages)
                return ("Completed", null);

            return ("Cancelled", "Job cancelled or deleted from spooler");
        }

        if ((statusFlags & SpoolerConstants.JOB_STATUS_COMPLETE) != 0 ||
            (statusFlags & SpoolerConstants.JOB_STATUS_PRINTED) != 0)
        {
            return ("Completed", null);
        }

        if ((statusFlags & SpoolerConstants.JOB_STATUS_PRINTING) != 0)
            return ("Printing", null);

        if ((statusFlags & SpoolerConstants.JOB_STATUS_PAUSED) != 0)
            return ("Paused", null);

        if ((statusFlags & SpoolerConstants.JOB_STATUS_SPOOLING) != 0)
            return ("Submitted", null);

        if (statusFlags == 0 && pagesPrinted > 0)
            return ("Printing", null);

        return ("Submitted", null);
    }

    private static string InterpretPrinterStatus(int statusFlags)
    {
        if (statusFlags == 0) return "Ready";

        var list = new List<string>();
        if ((statusFlags & 0x00000001) != 0) list.Add("Paused");
        if ((statusFlags & 0x00000002) != 0) list.Add("Error");
        if ((statusFlags & 0x00000004) != 0) list.Add("PendingDeletion");
        if ((statusFlags & 0x00000008) != 0) list.Add("PaperJam");
        if ((statusFlags & 0x00000010) != 0) list.Add("PaperOut");
        if ((statusFlags & 0x00000020) != 0) list.Add("ManualFeed");
        if ((statusFlags & 0x00000080) != 0) list.Add("IOActive");
        if ((statusFlags & 0x00000100) != 0) list.Add("Busy");
        if ((statusFlags & 0x00000200) != 0) list.Add("Printing");
        if ((statusFlags & 0x00000400) != 0) list.Add("OutputBinFull");
        if ((statusFlags & 0x00000800) != 0) list.Add("NotAvailable");
        if ((statusFlags & 0x00001000) != 0) list.Add("Waiting");
        if ((statusFlags & 0x00002000) != 0) list.Add("Processing");
        if ((statusFlags & 0x00004000) != 0) list.Add("Initializing");
        if ((statusFlags & 0x00008000) != 0) list.Add("WarmingUp");
        if ((statusFlags & 0x00010000) != 0) list.Add("TonerLow");
        if ((statusFlags & 0x00020000) != 0) list.Add("NoToner");
        if ((statusFlags & 0x00080000) != 0) list.Add("Offline");

        return list.Count > 0 ? string.Join(", ", list) : "Ready";
    }

    private static string GetPaperSizeName(int paperSize)
    {
        return paperSize switch
        {
            1 => "Letter",
            2 => "Letter Small",
            3 => "Tabloid",
            4 => "Ledger",
            5 => "Legal",
            6 => "Statement",
            7 => "Executive",
            8 => "A3",
            9 => "A4",
            10 => "A4 Small",
            11 => "A5",
            12 => "B4",
            13 => "B5",
            14 => "Folio",
            15 => "Quarto",
            _ => $"Custom ({paperSize})"
        };
    }
}
