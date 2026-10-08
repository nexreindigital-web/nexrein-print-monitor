using System.Windows;
using System.Windows.Threading;

namespace PrintMonitor.Manager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
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
}
