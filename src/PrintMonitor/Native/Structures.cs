using System.Runtime.InteropServices;

namespace PrintMonitor.Native;

public static class SpoolerConstants
{
    // Printer enumeration flags
    public const int PRINTER_ENUM_DEFAULT = 0x00000001;
    public const int PRINTER_ENUM_LOCAL = 0x00000002;
    public const int PRINTER_ENUM_CONNECTIONS = 0x00000004;
    public const int PRINTER_ENUM_FAVORITE = 0x00000004;
    public const int PRINTER_ENUM_NAME = 0x00000008;
    public const int PRINTER_ENUM_REMOTE = 0x00000010;
    public const int PRINTER_ENUM_SHARED = 0x00000020;
    public const int PRINTER_ENUM_NETWORK = 0x00000040;

    // Printer access
    public const int PRINTER_ACCESS_ADMINISTER = 0x00000004;
    public const int PRINTER_ACCESS_USE = 0x00000008;
    public const int PRINTER_ALL_ACCESS = 0x000F000C;

    // Notification flags
    public const int PRINTER_CHANGE_ADD_PRINTER = 0x00000001;
    public const int PRINTER_CHANGE_SET_PRINTER = 0x00000002;
    public const int PRINTER_CHANGE_DELETE_PRINTER = 0x00000004;
    public const int PRINTER_CHANGE_FAILED_CONNECTION_PRINTER = 0x00000008;
    public const int PRINTER_CHANGE_PRINTER = 0x000000FF;

    public const int PRINTER_CHANGE_ADD_JOB = 0x00000100;
    public const int PRINTER_CHANGE_SET_JOB = 0x00000200;
    public const int PRINTER_CHANGE_DELETE_JOB = 0x00000400;
    public const int PRINTER_CHANGE_WRITE_JOB = 0x00000800;
    public const int PRINTER_CHANGE_JOB = 0x0000FF00;

    public const int PRINTER_CHANGE_ADD_FORM = 0x00010000;
    public const int PRINTER_CHANGE_SET_FORM = 0x00020000;
    public const int PRINTER_CHANGE_DELETE_FORM = 0x00040000;
    public const int PRINTER_CHANGE_FORM = 0x00070000;

    public const int PRINTER_CHANGE_ADD_PORT = 0x00100000;
    public const int PRINTER_CHANGE_CONFIGURE_PORT = 0x00200000;
    public const int PRINTER_CHANGE_DELETE_PORT = 0x00400000;
    public const int PRINTER_CHANGE_PORT = 0x00700000;

    public const int PRINTER_CHANGE_ADD_PRINT_PROCESSOR = 0x01000000;
    public const int PRINTER_CHANGE_DELETE_PRINT_PROCESSOR = 0x04000000;
    public const int PRINTER_CHANGE_PRINT_PROCESSOR = 0x07000000;

    public const int PRINTER_CHANGE_ADD_PRINTER_DRIVER = 0x10000000;
    public const int PRINTER_CHANGE_SET_PRINTER_DRIVER = 0x20000000;
    public const int PRINTER_CHANGE_DELETE_PRINTER_DRIVER = 0x40000000;
    public const int PRINTER_CHANGE_PRINTER_DRIVER = 0x70000000;

    public const int PRINTER_CHANGE_TIMEOUT = 0x00000000;
    public const int PRINTER_CHANGE_ALL = 0x7777FFFF;

    // Job Status
    public const int JOB_STATUS_PAUSED = 0x00000001;
    public const int JOB_STATUS_ERROR = 0x00000002;
    public const int JOB_STATUS_DELETING = 0x00000004;
    public const int JOB_STATUS_SPOOLING = 0x00000008;
    public const int JOB_STATUS_PRINTING = 0x00000010;
    public const int JOB_STATUS_OFFLINE = 0x00000020;
    public const int JOB_STATUS_PAPEROUT = 0x00000040;
    public const int JOB_STATUS_PRINTED = 0x00000080;
    public const int JOB_STATUS_DELETED = 0x00000100;
    public const int JOB_STATUS_BLOCKED_DEVQ = 0x00000200;
    public const int JOB_STATUS_USER_INTERVENTION = 0x00000400;
    public const int JOB_STATUS_RESTART = 0x00000800;
    public const int JOB_STATUS_COMPLETE = 0x00001000;
    public const int JOB_STATUS_RETAINED = 0x00002000;
    public const int JOB_STATUS_RENDERING_LOCALLY = 0x00004000;

    // DevMode Color
    public const short DMCOLOR_MONOCHROME = 1;
    public const short DMCOLOR_COLOR = 2;

    // DevMode Duplex
    public const short DMDUP_SIMPLEX = 1;
    public const short DMDUP_VERTICAL = 2; // Long edge
    public const short DMDUP_HORIZONTAL = 3; // Short edge

    // DevMode Fields flags
    public const int DM_ORIENTATION = 0x00000001;
    public const int DM_PAPERSIZE = 0x00000002;
    public const int DM_PAPERLENGTH = 0x00000004;
    public const int DM_PAPERWIDTH = 0x00000008;
    public const int DM_COPIES = 0x00000100;
    public const int DM_DEFAULTSOURCE = 0x00000200;
    public const int DM_PRINTQUALITY = 0x00000400;
    public const int DM_COLOR = 0x00000800;
    public const int DM_DUPLEX = 0x00001000;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct SYSTEMTIME
{
    public short wYear;
    public short wMonth;
    public short wDayOfWeek;
    public short wDay;
    public short wHour;
    public short wMinute;
    public short wSecond;
    public short wMilliseconds;

    public DateTime? ToDateTimeUtc()
    {
        if (wYear == 0 && wMonth == 0 && wDay == 0)
            return null;

        try
        {
            // WinSpool SYSTEMTIME is represented in local time
            var local = new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds, DateTimeKind.Local);
            return local.ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct PRINTER_DEFAULTS
{
    public IntPtr pDatatype;
    public IntPtr pDevMode;
    public int DesiredAccess;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct PRINTER_INFO_2
{
    public string? pServerName;
    public string? pPrinterName;
    public string? pShareName;
    public string? pPortName;
    public string? pDriverName;
    public string? pComment;
    public string? pLocation;
    public IntPtr pDevMode;
    public string? pSepFile;
    public string? pPrintProcessor;
    public string? pDatatype;
    public string? pParameters;
    public IntPtr pSecurityDescriptor;
    public int Attributes;
    public int Priority;
    public int DefaultPriority;
    public int StartTime;
    public int UntilTime;
    public int Status;
    public int cJobs;
    public int AveragePPM;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct PRINTER_INFO_4
{
    public string? pPrinterName;
    public string? pServerName;
    public int Attributes;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct JOB_INFO_1
{
    public int JobId;
    public string? pPrinterName;
    public string? pMachineName;
    public string? pUserName;
    public string? pDocument;
    public string? pDatatype;
    public string? pStatus;
    public int Status;
    public int Priority;
    public int Position;
    public int TotalPages;
    public int PagesPrinted;
    public SYSTEMTIME Submitted;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct JOB_INFO_2
{
    public int JobId;
    public string? pPrinterName;
    public string? pMachineName;
    public string? pUserName;
    public string? pDocument;
    public string? pNotifyName;
    public string? pDatatype;
    public string? pPrintProcessor;
    public string? pParameters;
    public string? pDriverName;
    public IntPtr pDevMode;
    public string? pStatus;
    public IntPtr pSecurityDescriptor;
    public int Status;
    public int Priority;
    public int Position;
    public int StartTime;
    public int UntilTime;
    public int TotalPages;
    public int Size;
    public SYSTEMTIME Submitted;
    public int Time;
    public int PagesPrinted;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DEVMODE
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmDeviceName;
    public short dmSpecVersion;
    public short dmDriverVersion;
    public short dmSize;
    public short dmDriverExtra;
    public int dmFields;
    public short dmOrientation;
    public short dmPaperSize;
    public short dmPaperLength;
    public short dmPaperWidth;
    public short dmScale;
    public short dmCopies;
    public short dmDefaultSource;
    public short dmPrintQuality;
    public short dmColor;
    public short dmDuplex;
    public short dmYResolution;
    public short dmTTOption;
    public short dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmFormName;
    public short dmLogPixels;
    public int dmBitsPerPel;
    public int dmPelsWidth;
    public int dmPelsHeight;
    public int dmDisplayFlags;
    public int dmDisplayFrequency;
    public int dmICMMethod;
    public int dmICMIntent;
    public int dmMediaType;
    public int dmDitherType;
    public int dmReserved1;
    public int dmReserved2;
    public int dmPanningWidth;
    public int dmPanningHeight;
}
