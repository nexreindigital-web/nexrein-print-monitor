using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Native;
using PrintMonitor.Services;
using PrintMonitor.Utilities;

namespace PrintMonitor;

public class Program
{
    public const string AppVersion = "1.0.0";
    public const string ServiceName = "PrintMonitor";

    public static async Task<int> Main(string[] args)
    {
        // 1. Handle CLI diagnostic/admin commands if supplied
        if (args.Length > 0)
        {
            return await HandleCliCommandsAsync(args);
        }

        // 2. Default: Run as Windows Service / Worker Service
        try
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal exception in PrintMonitor service: {ex.Message}");
            return 1;
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = ServiceName;
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Configuration
                var settingsManager = new SettingsManager(hostContext.Configuration);
                services.AddSingleton(settingsManager);

                // Logging setup
                var logDir = settingsManager.Settings.LogDirectory;
                var minLevel = Enum.TryParse<LogLevel>(settingsManager.Settings.LogLevel, true, out var lvl)
                    ? lvl
                    : LogLevel.Information;

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddProvider(new PrintMonitorLoggerProvider(logDir, minLevel));
                    loggingBuilder.SetMinimumLevel(minLevel);
                });

                // Database
                var connStr = $"Data Source={settingsManager.Settings.DatabasePath}";
                services.AddSingleton(sp => new DatabaseInitializer(connStr, sp.GetService<ILogger<DatabaseInitializer>>()));
                services.AddSingleton(sp => new PrintMonitorDbContext(connStr, sp.GetService<ILogger<PrintMonitorDbContext>>()));

                // HTTP & API
                services.AddHttpClient<IApiClient, ApiClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                // Spooler & Core Services
                services.AddSingleton<IPrintSpoolerService, PrintSpoolerService>();
                services.AddSingleton<IPrinterDiscoveryService, PrinterDiscoveryService>();
                services.AddSingleton<IPrintJobMonitorService, PrintJobMonitorService>();
                services.AddSingleton<IDeviceRegistrationService, DeviceRegistrationService>();
                services.AddSingleton<ISyncService, SyncService>();
                services.AddSingleton<IHealthCheckService, HealthCheckService>();

                // Worker Coordinator
                services.AddHostedService<Worker>();
            });

    private static async Task<int> HandleCliCommandsAsync(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        var settingsManager = new SettingsManager();
        var dbPath = settingsManager.Settings.DatabasePath;
        var connStr = $"Data Source={dbPath}";

        try
        {
            var initializer = new DatabaseInitializer(connStr);
            initializer.Initialize();
        }
        catch
        {
            // Best effort initialization for CLI commands
        }

        switch (command)
        {
            case "--version":
            case "-v":
                Console.WriteLine($"PrintMonitor v{AppVersion}");
                return 0;

            case "--status":
                return await PrintStatusAsync(settingsManager, connStr);

            case "--printers":
                return PrintPrintersList(connStr);

            case "--test-api":
                return await TestApiConnectivityAsync(settingsManager);

            case "--sync":
                return await TriggerSyncAsync(settingsManager, connStr);

            case "--install":
                return InstallWindowsService();

            case "--uninstall":
                return UninstallWindowsService();

            case "--gui":
            case "--ui":
            case "--manager":
                return LaunchManagerGui();

            case "--help":
            case "-h":
                PrintHelp();
                return 0;

            default:
                Console.WriteLine($"Unknown option: {args[0]}");
                PrintHelp();
                return 1;
        }
    }

    private static async Task<int> PrintStatusAsync(SettingsManager settingsManager, string connStr)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"  PrintMonitor Agent Diagnostic Status (v{AppVersion})");
        Console.WriteLine("==================================================");

        // Check Windows Service state
        string serviceStatus = "Not Installed / Unknown";
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                serviceStatus = sc.Status.ToString();
            }
            catch
            {
                serviceStatus = "Not Installed";
            }
        }
        Console.WriteLine($"Service:      {serviceStatus}");
        Console.WriteLine($"Device ID:    {settingsManager.Settings.DeviceId}");

        // Check Database
        string dbStatus = "OK";
        int printerCount = 0;
        int pendingSync = 0;
        int totalJobs = 0;
        try
        {
            var db = new PrintMonitorDbContext(connStr);
            printerCount = db.GetPrinterCount();
            pendingSync = db.GetPendingSyncCount();
            totalJobs = db.GetTotalJobCount();
        }
        catch (Exception ex)
        {
            dbStatus = $"Error ({ex.Message})";
        }
        Console.WriteLine($"Database:     {dbStatus} ({dbPathOrInfo(settingsManager.Settings.DatabasePath)})");
        Console.WriteLine($"Printers:     {printerCount}");
        Console.WriteLine($"Total Jobs:   {totalJobs}");
        Console.WriteLine($"Pending Sync: {pendingSync}");

        // Check API
        Console.Write("API:          Testing... ");
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var apiClient = new ApiClient(httpClient, settingsManager, LoggerFactory.Create(b => { }).CreateLogger<ApiClient>());
        var (apiOk, apiMsg) = await apiClient.TestConnectionAsync();
        Console.WriteLine(apiOk ? $"Connected ({settingsManager.Settings.ApiBaseUrl})" : $"Offline ({apiMsg})");
        Console.WriteLine("==================================================");

        return 0;

        static string dbPathOrInfo(string path) => File.Exists(path) ? $"{new FileInfo(path).Length / 1024} KB" : "Not yet created";
    }

    private static int PrintPrintersList(string connStr)
    {
        Console.WriteLine($"Scanning printers from Windows Print Spooler and local database...");
        var discovered = Win32PrintSpooler.EnumeratePrinters();
        var db = new PrintMonitorDbContext(connStr);

        if (discovered.Count > 0)
        {
            try
            {
                db.SaveOrUpdatePrinters(discovered);
            }
            catch { }
        }
        else
        {
            // Try SQLite fallback
            try
            {
                discovered = db.GetAllPrinters();
            }
            catch { }
        }

        Console.WriteLine($"Found {discovered.Count} printer(s):");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        Console.WriteLine($"{"Name",-30} | {"Status",-12} | {"Port",-15} | {"Driver",-25} | {"Net",-4} | {"Jobs",-4}");
        Console.WriteLine("----------------------------------------------------------------------------------------------------");

        foreach (var p in discovered)
        {
            var net = p.IsNetwork ? "Yes" : "No";
            var port = (p.PortName ?? "").Length > 15 ? p.PortName?.Substring(0, 15) : p.PortName ?? "";
            var driver = (p.DriverName ?? "").Length > 25 ? p.DriverName?.Substring(0, 25) : p.DriverName ?? "";
            Console.WriteLine($"{p.Name,-30} | {p.Status,-12} | {port,-15} | {driver,-25} | {net,-4} | {p.JobCount,-4}");
        }
        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        return 0;
    }

    private static async Task<int> TestApiConnectivityAsync(SettingsManager settingsManager)
    {
        Console.WriteLine($"Testing API connectivity to: {settingsManager.Settings.ApiBaseUrl}...");
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var apiClient = new ApiClient(httpClient, settingsManager, LoggerFactory.Create(b => { }).CreateLogger<ApiClient>());
        var (ok, message) = await apiClient.TestConnectionAsync();

        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUCCESS] {message}");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAILURE] {message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task<int> TriggerSyncAsync(SettingsManager settingsManager, string connStr)
    {
        Console.WriteLine("Triggering manual batch synchronization...");
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var apiClient = new ApiClient(httpClient, settingsManager, loggerFactory.CreateLogger<ApiClient>());
        var db = new PrintMonitorDbContext(connStr, loggerFactory.CreateLogger<PrintMonitorDbContext>());
        var syncService = new SyncService(apiClient, db, settingsManager, loggerFactory.CreateLogger<SyncService>());

        var syncedCount = await syncService.SyncPendingJobsAsync();
        Console.WriteLine($"Synchronization complete. {syncedCount} job(s) sent and accepted by server.");
        return 0;
    }

    private static int InstallWindowsService()
    {
        var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        Console.WriteLine($"Installing Windows Service '{ServiceName}' with binary: {exePath}...");

        try
        {
            var createCmd = $"create \"{ServiceName}\" binPath= \"\\\"{exePath}\\\"\" start= auto DisplayName= \"Nexrein Printer Monitor - Print Monitoring Agent\"";
            var result = RunProcess("sc.exe", createCmd);

            var descCmd = $"description \"{ServiceName}\" \"Monitors Windows print jobs and synchronizes print activity with the Nexrein Printer Monitor management system.\"";
            RunProcess("sc.exe", descCmd);

            // Configure failure recovery: restart on first, second, subsequent
            var failureCmd = $"failure \"{ServiceName}\" reset= 86400 actions= restart/60000/restart/60000/restart/60000";
            RunProcess("sc.exe", failureCmd);

            Console.WriteLine(result);
            Console.WriteLine("Service installed and configured successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing service: {ex.Message}");
            return 1;
        }
    }

    private static int UninstallWindowsService()
    {
        Console.WriteLine($"Stopping and removing Windows Service '{ServiceName}'...");
        try
        {
            RunProcess("sc.exe", $"stop \"{ServiceName}\"");
            Thread.Sleep(1000);
            var result = RunProcess("sc.exe", $"delete \"{ServiceName}\"");
            Console.WriteLine(result);
            Console.WriteLine("Service removed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling service: {ex.Message}");
            return 1;
        }
    }

    private static string RunProcess(string filename, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        proc?.WaitForExit();
        return proc?.StandardOutput.ReadToEnd() ?? string.Empty;
    }

    private static int LaunchManagerGui()
    {
        var baseDir = AppContext.BaseDirectory;
        var managerCandidates = new[]
        {
            Path.Combine(baseDir, "PrintMonitor.Manager.exe"),
            Path.Combine(baseDir, "..", "PrintMonitor.Manager", "bin", "Release", "net10.0-windows", "PrintMonitor.Manager.exe"),
            Path.Combine(baseDir, "..", "PrintMonitor.Manager", "bin", "Debug", "net10.0-windows", "PrintMonitor.Manager.exe")
        };

        var managerExe = managerCandidates.FirstOrDefault(File.Exists);
        if (managerExe != null)
        {
            Console.WriteLine($"Launching Nexrein Printer Monitor Manager GUI: {managerExe}");
            Process.Start(new ProcessStartInfo { FileName = managerExe, UseShellExecute = true });
            return 0;
        }

        Console.Error.WriteLine("PrintMonitor.Manager.exe was not found in the application directory.");
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"Nexrein Printer Monitor - Windows Print Monitoring Agent

Usage:
  PrintMonitor.exe [command]

Commands:
  --gui          Launch the graphical Control Panel & Manager
  --status       Show current service, database, printer, and API status
  --printers     List all discovered printers
  --test-api     Test connection to configured Laravel API
  --sync         Manually synchronize pending print jobs
  --install      Install as Windows Service (requires Admin)
  --uninstall    Uninstall Windows Service (requires Admin)
  --version      Display version information
  --help         Show this help screen

When run without arguments, PrintMonitor runs as a Windows Service or background worker.
");
    }
}
