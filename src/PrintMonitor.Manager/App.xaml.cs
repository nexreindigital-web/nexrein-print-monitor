using System.Threading;
using System.Windows;
using System.Windows.Threading;
using PrintMonitor.Manager.Utilities;

namespace PrintMonitor.Manager;

public partial class App : System.Windows.Application
{
    private static Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = @"Global\NexreinPrinterMonitorManagerMutex";
        _instanceMutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            // Another instance is already running (e.g., minimized to tray in background).
            // Signal the existing running instance to restore to foreground and exit this duplicate process.
            TrayIconManager.PostShowSignal();
            Shutdown();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Startup Error:\n\n{ex?.Message}\n\n{ex?.StackTrace}", "Nexrein Printer Monitor Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Application Error:\n\n{args.Exception?.Message}\n\n{args.Exception?.StackTrace}", "Nexrein Printer Monitor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
        }
        catch { }

        base.OnExit(e);
    }
}
