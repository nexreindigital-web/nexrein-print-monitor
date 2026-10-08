using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Manager.Dialogs;
using PrintMonitor.Manager.Utilities;
using PrintMonitor.Models;
using PrintMonitor.Native;
using PrintMonitor.Services;

namespace PrintMonitor.Manager;

public partial class MainWindow : Window
{
    private const string ServiceName = "PrintMonitor";
    private readonly SettingsManager _settingsManager;
    private readonly PrintMonitorDbContext _dbContext;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isUpdatingUi = false;
    private bool _allowExit = false;

    public MainWindow()
    {
        InitializeComponent();

        _settingsManager = new SettingsManager();
        var connStr = $"Data Source={_settingsManager.Settings.DatabasePath}";
        _dbContext = new PrintMonitorDbContext(connStr);

        // Auto refresh every 3 seconds for live tracking
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadAllData();
        _refreshTimer.Start();
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateServiceStatusOnly();
        UpdateMetricsOnly();
    }

    private void LoadAllData()
    {
        UpdateServiceStatusOnly();
        LoadMetrics();
        LoadMachineAndStorageInfo();
        LoadPrinters();
        LoadJobs();
        LoadApiSettings();
        LoadAutostartState();
        LoadLogs();
    }

    #region Super Admin Authentication Helper

    private bool RequireSuperAdmin(string actionDescription)
    {
        var storedHash = _settingsManager.Settings.AdminPasswordHash;
        var dialog = new PasswordPromptDialog(actionDescription, storedHash)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsAuthenticated;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowExit)
            return;

        // Require super admin to close the app
        var authed = RequireSuperAdmin("Super Admin password is required to close and terminate the Control Panel application.");
        if (authed)
        {
            _allowExit = true;
        }
        else
        {
            e.Cancel = true;
            TxtStatusBar.Text = "Close cancelled: Super Admin authentication required.";
        }
    }

    private void BtnExitApp_Click(object sender, RoutedEventArgs e)
    {
        var authed = RequireSuperAdmin("Super Admin password is required to exit the Control Panel.");
        if (authed)
        {
            _allowExit = true;
            Close();
        }
    }

    private void BtnUpdatePassword_Click(object sender, RoutedEventArgs e)
    {
        var p1 = PbNewAdminPassword.Password;
        var p2 = PbConfirmAdminPassword.Password;

        if (string.IsNullOrWhiteSpace(p1) || p1.Length < 4)
        {
            MessageBox.Show("Password must be at least 4 characters long.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (p1 != p2)
        {
            MessageBox.Show("The new passwords do not match. Please re-enter.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Require current admin authorization first
        if (!RequireSuperAdmin("Enter current Super Admin password to change credentials."))
        {
            return;
        }

        try
        {
            var newHash = SecurityManager.HashPassword(p1);
            _settingsManager.SaveSettings(s =>
            {
                s.AdminPasswordHash = newHash;
            });

            PbNewAdminPassword.Clear();
            PbConfirmAdminPassword.Clear();

            MessageBox.Show("Super Admin password updated successfully! Your new password is now active.", "Security Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtStatusBar.Text = "Super Admin password updated successfully.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update password: {ex.Message}", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Service Control & Monitoring

    private void UpdateServiceStatusOnly()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            var status = sc.Status;

            if (status == ServiceControllerStatus.Running)
            {
                ServiceBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(6, 78, 59));
                ServiceBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                ServiceDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                TxtServiceBadge.Text = "SERVICE RUNNING";
                TxtServiceBadge.Foreground = new SolidColorBrush(Color.FromRgb(110, 231, 183));

                TxtServiceDetailStatus.Text = "Running (Active & Monitoring Spooler)";
                TxtServiceDetailStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                BtnQuickStart.IsEnabled = false;
                BtnQuickStop.IsEnabled = true;
            }
            else if (status == ServiceControllerStatus.Stopped)
            {
                ServiceBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(127, 29, 29));
                ServiceBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                ServiceDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                TxtServiceBadge.Text = "SERVICE STOPPED";
                TxtServiceBadge.Foreground = new SolidColorBrush(Color.FromRgb(254, 202, 202));

                TxtServiceDetailStatus.Text = "Stopped";
                TxtServiceDetailStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));

                BtnQuickStart.IsEnabled = true;
                BtnQuickStop.IsEnabled = false;
            }
            else
            {
                ServiceBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(120, 53, 15));
                ServiceBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                ServiceDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                TxtServiceBadge.Text = $"SERVICE {status.ToString().ToUpper()}";
                TxtServiceBadge.Foreground = new SolidColorBrush(Color.FromRgb(253, 230, 138));

                TxtServiceDetailStatus.Text = status.ToString();
                TxtServiceDetailStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
        }
        catch
        {
            ServiceBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            ServiceBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            ServiceDot.Fill = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            TxtServiceBadge.Text = "SERVICE UNKNOWN";
            TxtServiceBadge.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            TxtServiceDetailStatus.Text = "Not Registered or Query Failed";
            TxtServiceDetailStatus.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            BtnQuickStart.IsEnabled = true;
            BtnQuickStop.IsEnabled = true;
        }
    }

    private void BtnStartService_Click(object sender, RoutedEventArgs e)
    {
        ExecuteServiceCommand("start");
    }

    private void BtnStopService_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSuperAdmin("Super Admin password is required to stop the PrintMonitor Windows Service."))
            return;

        ExecuteServiceCommand("stop");
    }

    private void BtnRestartService_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSuperAdmin("Super Admin password is required to restart the PrintMonitor Windows Service."))
            return;

        ExecuteServiceCommand("stop");
        Thread.Sleep(1200);
        ExecuteServiceCommand("start");
    }

    private void ExecuteServiceCommand(string action)
    {
        try
        {
            TxtStatusBar.Text = $"Executing 'sc.exe {action} {ServiceName}'...";
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"{action} \"{ServiceName}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(5000);

            Thread.Sleep(1000);
            UpdateServiceStatusOnly();
            TxtStatusBar.Text = $"Service action '{action}' executed.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not {action} service: {ex.Message}", "Service Control", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion

    #region Autostart on Boot

    private void LoadAutostartState()
    {
        _isUpdatingUi = true;
        ChkAutostartOnBoot.IsChecked = SecurityManager.IsAutostartEnabled();
        _isUpdatingUi = false;
    }

    private void ChkAutostartOnBoot_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi) return;

        var targetState = ChkAutostartOnBoot.IsChecked == true;
        var actionText = targetState ? "enable autostart on Windows user login" : "disable autostart on Windows boot";

        if (!RequireSuperAdmin($"Super Admin password is required to {actionText}."))
        {
            // Revert state
            _isUpdatingUi = true;
            ChkAutostartOnBoot.IsChecked = !targetState;
            _isUpdatingUi = false;
            return;
        }

        var success = SecurityManager.SetAutostart(targetState);
        if (success)
        {
            _settingsManager.SaveSettings(s => s.AutostartOnBoot = targetState);
            TxtStatusBar.Text = $"Autostart on boot {(targetState ? "enabled" : "disabled")} successfully.";
        }
        else
        {
            MessageBox.Show("Failed to update Windows Registry Run key.", "Registry Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _isUpdatingUi = true;
            ChkAutostartOnBoot.IsChecked = !targetState;
            _isUpdatingUi = false;
        }
    }

    #endregion

    #region Metrics & Live Accounting

    private void LoadMetrics()
    {
        try
        {
            var stats = _dbContext.GetDetailedJobStats();
            var printerCount = _dbContext.GetPrinterCount();

            // Daily Stats
            TxtTodayPages.Text = stats.todayPages.ToString("N0");
            TxtTodayBreakdown.Text = $"{stats.todayMono:N0} B&W / Gray • {stats.todayColor:N0} Color today";
            TxtTodayRatio.Text = $"Today: {stats.todayPages:N0} Total Pages ({stats.todayJobs:N0} jobs)";

            // Lifetime Stats
            TxtTotalPages.Text = stats.totalPages.ToString("N0");
            TxtPagesSub.Text = $"{stats.totalMono:N0} B&W / Gray • {stats.totalColor:N0} Color";

            // Color vs Mono ratio progress bar
            var todayTotal = stats.todayPages > 0 ? stats.todayPages : 1;
            var colorRatio = Math.Clamp((double)stats.todayColor / todayTotal, 0.05, 0.95);
            var monoRatio = 1.0 - colorRatio;
            ColColorBar.Width = new GridLength(colorRatio * 100, GridUnitType.Star);
            ColMonoBar.Width = new GridLength(monoRatio * 100, GridUnitType.Star);

            TxtColorLegend.Text = $"🎨 Color Pages: {stats.todayColor:N0}";
            TxtMonoLegend.Text = $"🔲 Black & White / Grayscale: {stats.todayMono:N0}";

            // Printers & Jobs
            TxtPrintersCount.Text = printerCount.ToString();
            TxtPrintersSub.Text = $"{printerCount} active devices monitored";

            TxtTotalJobs.Text = stats.totalJobs.ToString("N0");
            TxtJobsSub.Text = $"{stats.pendingSync} Pending cloud synchronization";

            TxtJobTableSummary.Text = $"Tracking {stats.totalJobs:N0} total job(s) | {stats.totalPages:N0} printed page(s)";
        }
        catch (Exception ex)
        {
            TxtStatusBar.Text = $"Metrics update error: {ex.Message}";
        }
    }

    private void UpdateMetricsOnly()
    {
        try
        {
            var stats = _dbContext.GetDetailedJobStats();
            TxtTodayPages.Text = stats.todayPages.ToString("N0");
            TxtTodayBreakdown.Text = $"{stats.todayMono:N0} B&W / Gray • {stats.todayColor:N0} Color today";
            TxtTotalPages.Text = stats.totalPages.ToString("N0");
            TxtPagesSub.Text = $"{stats.totalMono:N0} B&W / Gray • {stats.totalColor:N0} Color";
            TxtTotalJobs.Text = stats.totalJobs.ToString("N0");
            TxtJobsSub.Text = $"{stats.pendingSync} Pending cloud sync";
        }
        catch { }
    }

    private void LoadMachineAndStorageInfo()
    {
        try
        {
            TxtMachineName.Text = Environment.MachineName;
            TxtDeviceId.Text = _settingsManager.Settings.DeviceId;

            var dbPath = _settingsManager.Settings.DatabasePath;
            if (File.Exists(dbPath))
            {
                var lenKb = Math.Round(new FileInfo(dbPath).Length / 1024.0, 1);
                TxtDbSize.Text = $"{lenKb} KB (WAL Mode)";
            }
            else
            {
                TxtDbSize.Text = "Not created yet";
            }
        }
        catch { }
    }

    private void BtnOpenDataDir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = @"C:\ProgramData\PrintMonitor";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Process.Start("explorer.exe", dir);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open directory: {ex.Message}", "Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Print Jobs Tab & Document Type Detection

    private void LoadJobs()
    {
        try
        {
            var search = TxtSearchJob.Text;
            var selectedPrinter = (CmbPrinterFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedSync = (CmbSyncFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var jobs = _dbContext.GetFilteredJobs(150, search, selectedPrinter, selectedSync);

            var items = jobs.Select(j =>
            {
                var docType = DocumentTypeClassifier.Classify(j.DocumentName);
                var isColor = string.Equals(j.ColorMode, "Color", StringComparison.OrdinalIgnoreCase);

                return new PrintJobDisplayItem
                {
                    JobUid = j.JobUid,
                    JobId = j.JobId,
                    DocumentName = j.DocumentName,
                    DocumentTypeIcon = docType.Icon,
                    DocumentTypeName = docType.TypeName,
                    Username = j.Username,
                    ComputerName = j.ComputerName,
                    PrinterName = j.PrinterName,
                    Pages = j.Pages,
                    Copies = j.Copies,
                    TotalPagesCalculated = (j.PagesPrinted > 0 ? j.PagesPrinted : (j.Pages * j.Copies)),
                    ColorMode = j.ColorMode,
                    ColorModeDisplay = isColor ? "🎨 Color" : "🔲 B&W / Gray",
                    ColorBadgeBg = isColor ? new SolidColorBrush(Color.FromRgb(88, 28, 135)) : new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    ColorBadgeFg = isColor ? new SolidColorBrush(Color.FromRgb(233, 213, 255)) : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    Duplex = j.Duplex,
                    PaperSize = j.PaperSize,
                    Status = j.Status,
                    SyncStatus = j.SyncStatus,
                    SubmittedAtLocal = j.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();

            DgJobs.ItemsSource = items;
            TxtJobTableSummary.Text = $"Showing {items.Count} job(s) | Total pages in view: {items.Sum(x => x.TotalPagesCalculated):N0}";
        }
        catch (Exception ex)
        {
            TxtStatusBar.Text = $"Error loading jobs: {ex.Message}";
        }
    }

    private void TxtSearchJob_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadJobs();
    }

    private void CmbPrinterFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingUi) LoadJobs();
    }

    private void CmbSyncFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingUi) LoadJobs();
    }

    private void BtnRefreshJobs_Click(object sender, RoutedEventArgs e)
    {
        LoadJobs();
        LoadMetrics();
    }

    private void BtnSimulateJob_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printers = _dbContext.GetAllPrinters();
            var printerName = printers.Count > 0 ? printers[0].Name : "Canon G2010 series";

            var sampleFiles = new[]
            {
                "Q3_Financial_Analysis.docx",
                "Product_Catalog_Brochure.pub",
                "Annual_Tax_Statement.pdf",
                "Sales_Forecast_2026.xlsx",
                "Marketing_Campaign_Banner.png",
                "Meeting_Minutes.txt"
            };

            var docName = sampleFiles[Random.Shared.Next(sampleFiles.Length)];
            var isColor = Random.Shared.Next(0, 2) == 1;

            var job = new PrintJob
            {
                JobUid = $"{_settingsManager.Settings.DeviceId}_{printerName}_{Random.Shared.Next(100, 999)}_{DateTime.UtcNow.Ticks}",
                JobId = Random.Shared.Next(10, 500),
                PrinterName = printerName,
                DocumentName = docName,
                Username = Environment.UserName,
                ComputerName = Environment.MachineName,
                Pages = Random.Shared.Next(1, 12),
                Copies = Random.Shared.Next(1, 3),
                ColorMode = isColor ? "Color" : "Monochrome",
                Duplex = "Simplex",
                PaperSize = "A4",
                Status = "Completed",
                SubmittedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                SyncStatus = "Pending"
            };

            _dbContext.SaveOrUpdateJob(job);
            LoadJobs();
            LoadMetrics();

            var docType = DocumentTypeClassifier.Classify(job.DocumentName);
            MessageBox.Show($"Test Print Job Recorded!\n\nDocument: {job.DocumentName}\nDetected Type: {docType.Icon} {docType.TypeName}\nColor Mode: {(isColor ? "Color" : "Black and White / Grayscale")}\nPages: {job.Pages} (Copies: {job.Copies})\nTotal: {job.Pages * job.Copies} page(s)", "Print Job Accounting", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error simulating job: {ex.Message}", "Simulate Job", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var jobs = _dbContext.GetFilteredJobs(500);
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var exportFile = Path.Combine(desktop, $"PrintMonitor_Jobs_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using var sw = new StreamWriter(exportFile);
            sw.WriteLine("JobUid,JobId,SubmittedAtLocal,DocumentName,DocumentType,Username,Computer,Printer,Pages,Copies,TotalPages,ColorMode,Duplex,PaperSize,Status,SyncStatus");
            foreach (var j in jobs)
            {
                var dt = DocumentTypeClassifier.Classify(j.DocumentName);
                var total = j.PagesPrinted > 0 ? j.PagesPrinted : (j.Pages * j.Copies);
                sw.WriteLine($"\"{j.JobUid}\",{j.JobId},\"{j.SubmittedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\",\"{j.DocumentName}\",\"{dt.TypeName}\",\"{j.Username}\",\"{j.ComputerName}\",\"{j.PrinterName}\",{j.Pages},{j.Copies},{total},\"{j.ColorMode}\",\"{j.Duplex}\",\"{j.PaperSize}\",\"{j.Status}\",\"{j.SyncStatus}\"");
            }

            MessageBox.Show($"Exported {jobs.Count} jobs with document types to:\n{exportFile}", "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Installed Printers Tab

    private void LoadPrinters()
    {
        try
        {
            List<PrinterInfo> printers;
            try
            {
                printers = Win32PrintSpooler.EnumeratePrinters();
                if (printers.Count > 0)
                {
                    _dbContext.SaveOrUpdatePrinters(printers);
                }
            }
            catch
            {
                printers = _dbContext.GetAllPrinters();
            }

            if (printers.Count == 0)
            {
                printers = _dbContext.GetAllPrinters();
            }

            TxtPrinterCountHeader.Text = $"{printers.Count} printer(s) detected";

            var items = printers.Select(p => new PrinterDisplayItem
            {
                Name = p.Name,
                Status = string.IsNullOrWhiteSpace(p.Status) ? "Ready" : p.Status,
                PortName = p.PortName ?? "N/A",
                DriverName = p.DriverName ?? "Generic",
                TopologyType = p.IsNetwork ? "Network Printer" : (p.IsShared ? "Shared Printer" : "Local / USB"),
                IsDefaultDisplay = p.IsDefault ? "⭐ Default" : "No",
                JobCount = p.JobCount,
                LastSeenAtLocal = p.LastSeenAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            DgPrinters.ItemsSource = items;

            _isUpdatingUi = true;
            CmbPrinterFilter.Items.Clear();
            CmbPrinterFilter.Items.Add(new ComboBoxItem { Content = "All Printers", IsSelected = true });
            foreach (var p in printers)
            {
                CmbPrinterFilter.Items.Add(new ComboBoxItem { Content = p.Name });
            }
            _isUpdatingUi = false;
        }
        catch (Exception ex)
        {
            TxtStatusBar.Text = $"Error scanning printers: {ex.Message}";
        }
    }

    private void BtnRescanPrinters_Click(object sender, RoutedEventArgs e)
    {
        LoadPrinters();
        LoadMetrics();
        MessageBox.Show("Windows Print Spooler re-scanned successfully.", "Printers", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenWindowsPrinters_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                Arguments = "printers",
                UseShellExecute = true
            });
        }
        catch
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:printers",
                UseShellExecute = true
            });
        }
    }

    #endregion

    #region API & Endpoint Management

    private void LoadApiSettings()
    {
        var s = _settingsManager.Settings;
        TxtApiBaseUrl.Text = s.ApiBaseUrl;
        TxtApiKey.Text = s.ApiKey;
        TxtSyncInterval.Text = s.SyncIntervalSeconds.ToString();
        TxtHeartbeatInterval.Text = s.HeartbeatIntervalSeconds.ToString();
        TxtPollingInterval.Text = s.PollingIntervalSeconds.ToString();

        foreach (ComboBoxItem item in CmbLogLevel.Items)
        {
            if (string.Equals(item.Content.ToString(), s.LogLevel, StringComparison.OrdinalIgnoreCase))
            {
                item.IsSelected = true;
                break;
            }
        }
    }

    private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireSuperAdmin("Super Admin password is required to save changes to the API endpoint and service configuration."))
            return;

        try
        {
            var url = TxtApiBaseUrl.Text?.Trim() ?? "https://your-domain.com/api";
            var key = TxtApiKey.Text?.Trim() ?? string.Empty;

            int.TryParse(TxtSyncInterval.Text, out var syncSec);
            if (syncSec <= 0) syncSec = 30;

            int.TryParse(TxtHeartbeatInterval.Text, out var hbSec);
            if (hbSec <= 0) hbSec = 60;

            int.TryParse(TxtPollingInterval.Text, out var pollSec);
            if (pollSec <= 0) pollSec = 5;

            var logLevel = (CmbLogLevel.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Information";

            _settingsManager.SaveSettings(s =>
            {
                s.ApiBaseUrl = url;
                s.ApiKey = key;
                s.SyncIntervalSeconds = syncSec;
                s.HeartbeatIntervalSeconds = hbSec;
                s.PollingIntervalSeconds = pollSec;
                s.LogLevel = logLevel;
            });

            TxtStatusBar.Text = $"Configuration updated and persisted to C:\\ProgramData\\PrintMonitor\\config.json at {DateTime.Now:HH:mm:ss}";
            MessageBox.Show("Configuration updated successfully!\n\nPersisted to C:\\ProgramData\\PrintMonitor\\config.json.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnTestApi_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtApiBaseUrl.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Please specify an API Base URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BorderTestResult.Visibility = Visibility.Visible;
        TxtTestResultMsg.Text = $"Testing connection to {url}...";
        TxtTestResultMsg.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));

        try
        {
            var sw = Stopwatch.StartNew();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var fullUrl = url.TrimEnd('/') + "/health";
            var resp = await http.GetAsync(fullUrl);
            sw.Stop();

            TxtTestResultMsg.Text = $"HTTP {(int)resp.StatusCode} {resp.StatusCode} (Response time: {sw.ElapsedMilliseconds} ms)\nEndpoint reachable!";
            TxtTestResultMsg.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        catch (Exception ex)
        {
            TxtTestResultMsg.Text = $"Connection note: {ex.Message}\n(Ensure server is running or offline queue will buffer records)";
            TxtTestResultMsg.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        }
    }

    private async void BtnTriggerSync_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TxtStatusBar.Text = "Triggering manual batch synchronization...";
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
            var apiClient = new ApiClient(httpClient, _settingsManager, LoggerFactory.Create(b => { }).CreateLogger<ApiClient>());
            var syncService = new SyncService(apiClient, _dbContext, _settingsManager, LoggerFactory.Create(b => { }).CreateLogger<SyncService>());

            var syncedCount = await syncService.SyncPendingJobsAsync();
            LoadMetrics();
            LoadJobs();

            MessageBox.Show($"Batch synchronization completed!\n\nJobs transmitted and confirmed: {syncedCount}", "Sync Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtStatusBar.Text = $"Batch sync complete. {syncedCount} jobs accepted.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sync notice: {ex.Message}\nJobs are preserved in local SQLite queue and will retry automatically.", "Sync Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region Diagnostic Logs

    private void LoadLogs()
    {
        try
        {
            var logDir = _settingsManager.Settings.LogDirectory;
            TxtActiveLogFile.Text = logDir;

            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, "printmonitor-*.log")
                                     .OrderByDescending(f => f)
                                     .ToList();

                if (files.Count > 0)
                {
                    var latestFile = files[0];
                    TxtActiveLogFile.Text = latestFile;
                    var lines = File.ReadLines(latestFile).TakeLast(200);
                    TxtLogsView.Text = string.Join(Environment.NewLine, lines);
                    TxtLogsView.ScrollToEnd();
                    return;
                }
            }

            TxtLogsView.Text = "No log files found yet in " + logDir;
        }
        catch (Exception ex)
        {
            TxtLogsView.Text = $"Error reading logs: {ex.Message}";
        }
    }

    private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void BtnOpenLogsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = _settingsManager.Settings.LogDirectory;
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            Process.Start("explorer.exe", logDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open logs folder: {ex.Message}", "Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    private void BtnRefreshAll_Click(object sender, RoutedEventArgs e)
    {
        LoadAllData();
        TxtStatusBar.Text = $"All statistics, services, and printers refreshed at {DateTime.Now:HH:mm:ss}";
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            if (MainTabs.SelectedIndex == 1) LoadJobs();
            else if (MainTabs.SelectedIndex == 2) LoadPrinters();
            else if (MainTabs.SelectedIndex == 4) LoadLogs();
        }
    }
}

public class PrintJobDisplayItem
{
    public string JobUid { get; set; } = string.Empty;
    public int JobId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentTypeIcon { get; set; } = "📋";
    public string DocumentTypeName { get; set; } = "Document";
    public string Username { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public int Pages { get; set; }
    public int Copies { get; set; }
    public int TotalPagesCalculated { get; set; }
    public string ColorMode { get; set; } = string.Empty;
    public string ColorModeDisplay { get; set; } = string.Empty;
    public SolidColorBrush ColorBadgeBg { get; set; } = Brushes.Transparent;
    public SolidColorBrush ColorBadgeFg { get; set; } = Brushes.White;
    public string Duplex { get; set; } = string.Empty;
    public string PaperSize { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = string.Empty;
    public string SubmittedAtLocal { get; set; } = string.Empty;
}

public class PrinterDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PortName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string TopologyType { get; set; } = string.Empty;
    public string IsDefaultDisplay { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public string LastSeenAtLocal { get; set; } = string.Empty;
}
