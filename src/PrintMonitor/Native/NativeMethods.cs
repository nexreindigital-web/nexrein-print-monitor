using System.Runtime.InteropServices;

namespace PrintMonitor.Native;

public static class NativeMethods
{
    private const string WinSpoolDriver = "winspool.drv";
    private const string Kernel32 = "kernel32.dll";

    [DllImport(WinSpoolDriver, EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenPrinter(
        string pPrinterName,
        out IntPtr phPrinter,
        IntPtr pDefault);

    [DllImport(WinSpoolDriver, EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenPrinterWithDefaults(
        string pPrinterName,
        out IntPtr phPrinter,
        ref PRINTER_DEFAULTS pDefault);

    [DllImport(WinSpoolDriver, EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport(WinSpoolDriver, EntryPoint = "EnumPrintersW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumPrinters(
        int Flags,
        string? Name,
        int Level,
        IntPtr pPrinterEnum,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [DllImport(WinSpoolDriver, EntryPoint = "EnumJobsW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumJobs(
        IntPtr hPrinter,
        int FirstJob,
        int NoJobs,
        int Level,
        IntPtr pJob,
        int cbBuf,
        out int pcbNeeded,
        out int pcReturned);

    [DllImport(WinSpoolDriver, EntryPoint = "GetJobW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetJob(
        IntPtr hPrinter,
        int JobId,
        int Level,
        IntPtr pJob,
        int cbBuf,
        out int pcbNeeded);

    [DllImport(WinSpoolDriver, EntryPoint = "FindFirstPrinterChangeNotification", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr FindFirstPrinterChangeNotification(
        IntPtr hPrinter,
        int fdwFilter,
        int fdwOptions,
        IntPtr pPrinterNotifyOptions);

    [DllImport(WinSpoolDriver, EntryPoint = "FindNextPrinterChangeNotification", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindNextPrinterChangeNotification(
        IntPtr hChange,
        out int pdwChange,
        IntPtr pPrinterNotifyOptions,
        out IntPtr ppPrinterNotifyInfo);

    [DllImport(WinSpoolDriver, EntryPoint = "FindClosePrinterChangeNotification", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FindClosePrinterChangeNotification(IntPtr hChange);

    [DllImport(WinSpoolDriver, EntryPoint = "FreePrinterNotifyInfo", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreePrinterNotifyInfo(IntPtr pPrinterNotifyInfo);

    [DllImport(Kernel32, SetLastError = true, ExactSpelling = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    public const uint WAIT_OBJECT_0 = 0x00000000;
    public const uint WAIT_TIMEOUT = 0x00000102;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
    public const uint INFINITE = 0xFFFFFFFF;
}
