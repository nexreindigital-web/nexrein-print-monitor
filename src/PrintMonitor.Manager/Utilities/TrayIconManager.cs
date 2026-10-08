using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PrintMonitor.Manager.Utilities;

public enum TrayNotificationType
{
    None = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public class TrayIconManager : IDisposable
{
    private const int WM_USER = 0x0400;
    public const int WM_TRAYICON = WM_USER + 110;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIM_SETVERSION = 0x00000004;

    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_INFO = 0x00000010;

    private const int NIIF_NONE = 0x00000000;
    private const int NIIF_INFO = 0x00000001;
    private const int NIIF_WARNING = 0x00000002;
    private const int NIIF_ERROR = 0x00000003;

    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint MF_DEFAULT = 0x00001000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;

    private const int CMD_OPEN = 1001;
    private const int CMD_CLOUD = 1002;
    private const int CMD_CHECK_UPDATES = 1003;
    private const int CMD_EXIT = 1004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetMenuDefaultItem(IntPtr hMenu, uint uItem, uint fByPos);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public static readonly uint WM_SHOW_EXISTING = RegisterWindowMessage("NEXREIN_PRINT_MONITOR_SHOW_EXISTING");
    private static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;

    public static void PostShowSignal()
    {
        if (WM_SHOW_EXISTING != 0)
        {
            PostMessage(HWND_BROADCAST, WM_SHOW_EXISTING, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private readonly IntPtr _hwnd;
    private readonly IntPtr _hIcon;
    private bool _isAdded;
    private bool _disposed;

    public event Action? OpenRequested;
    public event Action? WebPortalRequested;
    public event Action? CheckUpdatesRequested;
    public event Action? ExitRequested;

    public TrayIconManager(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.EnsureHandle();

        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
        {
            _hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
        }

        var source = HwndSource.FromHwnd(_hwnd);
        source?.AddHook(WndProc);

        AddTrayIcon();
    }

    public void AddTrayIcon()
    {
        if (_isAdded) return;

        var nid = CreateNotifyData();
        nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        nid.szTip = "Nexrein Printer Monitor (Running in background)";

        _isAdded = Shell_NotifyIcon(NIM_ADD, ref nid);

        // Set version for modern behavior
        nid.uTimeoutOrVersion = 4; // NOTIFYICON_VERSION_4
        Shell_NotifyIcon(NIM_SETVERSION, ref nid);
    }

    public void ShowBalloon(string title, string text, TrayNotificationType type = TrayNotificationType.Info)
    {
        if (!_isAdded) return;

        var nid = CreateNotifyData();
        nid.uFlags = NIF_INFO;
        nid.szInfoTitle = title.Length > 63 ? title[..63] : title;
        nid.szInfo = text.Length > 255 ? text[..255] : text;
        nid.dwInfoFlags = type switch
        {
            TrayNotificationType.Info => NIIF_INFO,
            TrayNotificationType.Warning => NIIF_WARNING,
            TrayNotificationType.Error => NIIF_ERROR,
            _ => NIIF_NONE
        };

        Shell_NotifyIcon(NIM_MODIFY, ref nid);
    }

    private NOTIFYICONDATA CreateNotifyData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon,
            szTip = string.Empty,
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == (int)WM_SHOW_EXISTING)
        {
            OpenRequested?.Invoke();
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_TRAYICON)
        {
            var eventCode = lParam.ToInt32() & 0xFFFF;
            switch (eventCode)
            {
                case WM_LBUTTONUP:
                case WM_LBUTTONDBLCLK:
                    OpenRequested?.Invoke();
                    handled = true;
                    break;

                case WM_RBUTTONUP:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            AppendMenu(hMenu, MF_STRING, CMD_OPEN, "🖥️ Open Nexrein Printer Monitor");
            SetMenuDefaultItem(hMenu, CMD_OPEN, 0);

            AppendMenu(hMenu, MF_STRING, CMD_CLOUD, "☁️ Open Cloud Web Portal");
            AppendMenu(hMenu, MF_STRING, CMD_CHECK_UPDATES, "🔄 Check for Updates...");
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, CMD_EXIT, "⏹️ Exit / Terminate Application (Requires Password)");

            GetCursorPos(out var pt);
            SetForegroundWindow(_hwnd);

            var cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, _hwnd, IntPtr.Zero);
            switch (cmd)
            {
                case CMD_OPEN:
                    OpenRequested?.Invoke();
                    break;
                case CMD_CLOUD:
                    WebPortalRequested?.Invoke();
                    break;
                case CMD_CHECK_UPDATES:
                    CheckUpdatesRequested?.Invoke();
                    break;
                case CMD_EXIT:
                    ExitRequested?.Invoke();
                    break;
            }
        }
        finally
        {
            DestroyMenu(hMenu);
        }
    }

    public void Remove()
    {
        if (_isAdded)
        {
            var nid = CreateNotifyData();
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            _isAdded = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Remove();

        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
        }

        GC.SuppressFinalize(this);
    }
}
