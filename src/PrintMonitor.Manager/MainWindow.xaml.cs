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
    private readonly SettingsManager _settingsManager = null!;
    private readonly PrintMonitorDbContext _dbContext = null!;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isUpdatingUi = true;
    private bool _isLoaded = false;
    private bool _allowExit = false;
    private TrayIconManager? _trayManager;

    public MainWindow()
    {
        _isUpdatingUi = true;
        _isLoaded = false;

        try
        {
            _settingsManager = new SettingsManager();
            var connStr = $"Data Source={_settingsManager.Settings.DatabasePath}";
            _dbContext = new PrintMonitorDbContext(connStr);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Settings/Database initialization notice: {ex.Message}", "Nexrein Printer Monitor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        InitializeComponent();

        // Auto refresh every 3 seconds for live tracking
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        _isUpdatingUi = false;

        // Initialize Native Windows Tray Icon for background operation
        try
        {
            _trayManager = new TrayIconManager(this);
            _trayManager.OpenRequested += () => Dispatcher.Invoke(RestoreFromTray);
            _trayManager.WebPortalRequested += () => Dispatcher.Invoke(OpenCloudWebPortal);
            _trayManager.CheckUpdatesRequested += () => Dispatcher.Invoke(async () => await RunUpdateCheckAsync(isManualClick: true));
            _trayManager.ExitRequested += () => Dispatcher.Invoke(RequestApplicationExit);
        }
        catch { }

        // Initialize Theme System (Follows Windows system theme by default)
        ThemeManager.Initialize(this.Resources);
        ThemeManager.ThemeChanged += OnThemeChanged;
        if (CmbThemeSelector != null)
        {
            CmbThemeSelector.SelectedIndex = (int)ThemeManager.CurrentMode;
        }

        LoadAllData();
        _refreshTimer.Start();

        // If launched with --background or --minimized, start directly in the system tray
        var cmdArgs = Environment.GetCommandLineArgs();
        if (cmdArgs.Any(a => a.Equals("--background", StringComparison.OrdinalIgnoreCase) || a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            Hide();
            TxtStatusBar.Text = "Running in background (System Tray). Double-click tray icon to open.";
        }

        // Check for updates in the background (non-blocking)
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            await Dispatcher.InvokeAsync(() => RunUpdateCheckAsync(isManualClick: false));
        });
    }

    private void CmbThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbThemeSelector == null) return;
        var mode = (AppThemeMode)CmbThemeSelector.SelectedIndex;
        ThemeManager.ApplyTheme(this.Resources, mode);
        LoadDashboardPrints();
        LoadJobs();
    }

    private void OnThemeChanged(bool isDark)
    {
        Dispatcher.Invoke(() =>
        {
            if (_isLoaded)
            {
                LoadDashboardPrints();
                LoadJobs();
            }
        });
    }

    private static (SolidColorBrush bg, SolidColorBrush fg) GetColorBadgeBrushes(bool isColor)
    {
        bool isDark = ThemeManager.IsDarkThemeActive;
        if (isColor)
        {
            // Matches reference: cyan pill in dark mode, deep teal in light mode
            return isDark
                ? (new SolidColorBrush(Color.FromRgb(34, 211, 238)), new SolidColorBrush(Color.FromRgb(8, 51, 68)))
                : (new SolidColorBrush(Color.FromRgb(15, 118, 110)), new SolidColorBrush(Colors.White));
        }
        else
        {
            return isDark
                ? (new SolidColorBrush(Color.FromRgb(71, 85, 105)), new SolidColorBrush(Color.FromRgb(226, 232, 240)))
                : (new SolidColorBrush(Color.FromRgb(71, 85, 105)), new SolidColorBrush(Colors.White));
        }
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateServiceStatusOnly();
        UpdateMetricsOnly();
        if (_isLoaded && !_isUpdatingUi && MainTabs?.SelectedIndex == 0)
        {
            LoadDashboardPrints();
        }
    }

    private void LoadAllData()
    {
        UpdateServiceStatusOnly();
        LoadMetrics();
        LoadDashboardPrints();
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
        // Dynamically reload in case the password was updated remotely from the Laravel Cloud Web Portal
        _settingsManager.Reload();
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
        {
            _trayManager?.Dispose();
            return;
        }

        // When closed, run in the background. It cannot be terminated fully without Super Admin password.
        e.Cancel = true;
        Hide();

        _trayManager?.ShowBalloon(
            "Nexrein Printer Monitor",
            "Application is running in the background to ensure continuous print auditing.\nFully terminating requires Super Admin password.",
            TrayNotificationType.Info);

        TxtStatusBar.Text = "Running in background (System Tray). Double-click tray icon to open.";
    }

    public void RequestApplicationExit()
    {
        // Bring window to foreground to prompt
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();

        var authed = RequireSuperAdmin("Super Admin password is required to exit and terminate Nexrein Printer Monitor.");
        if (authed)
        {
            _allowExit = true;
            _trayManager?.Dispose();
            Close();
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            TxtStatusBar.Text = "Exit prevented: Super Admin authentication required.";
            _trayManager?.ShowBalloon(
                "Access Denied",
                "Super Admin authentication is required to terminate Nexrein Printer Monitor.",
                TrayNotificationType.Warning);
        }
    }

    private void BtnExitApp_Click(object sender, RoutedEventArgs e)
    {
        RequestApplicationExit();
    }

    private void RestoreFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Focus();
    }

    private void OpenCloudWebPortal()
    {
        var url = _settingsManager?.Settings?.ApiBaseUrl;
        if (string.IsNullOrWhiteSpace(url) || url.Contains("your-domain.com"))
        {
            url = "https://printmonitor.nexreindigital.co.ke/dashboard";
        }
        else
        {
            url = url.Replace("/api", "") + "/dashboard";
        }
        VersionController.OpenUrl(url);
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
        UpdateMetricsOnly();
    }

    private void UpdateMetricsOnly()
    {
        try
        {
            var stats = _dbContext.GetDetailedJobStats();
            if (TxtJobTableSummary != null)
                TxtJobTableSummary.Text = $"Tracking {stats.totalJobs:N0} total job(s) | {stats.totalPages:N0} printed page(s) | {stats.pendingSync:N0} pending cloud sync";
            if (TxtAdminIdentity != null)
                TxtAdminIdentity.Text = $"Super Admin • {Environment.MachineName.ToUpperInvariant()}";
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

    #region Dashboard Print Activity & Date Filtering

    private (DateTime? fromDate, DateTime? toDate, string label) GetDashboardDateRange()
    {
        var idx = CmbDashDateFilter?.SelectedIndex ?? 0;
        var today = DateTime.Today;

        switch (idx)
        {
            case 0: // Today
                return (today, today.AddDays(1).AddTicks(-1), "Today");

            case 1: // Yesterday
                var yesterday = today.AddDays(-1);
                return (yesterday, today.AddTicks(-1), "Yesterday");

            case 2: // Last 7 Days
                return (today.AddDays(-6), DateTime.Now, "Last 7 Days");

            case 3: // This Month
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                return (startOfMonth, DateTime.Now, "This Month");

            case 4: // All Time
                return (null, null, "All Time");

            case 5: // Custom Range
                var from = DpStartDate?.SelectedDate?.Date;
                var to = DpEndDate?.SelectedDate?.Date.AddDays(1).AddTicks(-1);
                var lbl = from.HasValue && to.HasValue 
                    ? $"{from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}" 
                    : (from.HasValue ? $"From {from.Value:yyyy-MM-dd}" : "Custom Range");
                return (from, to, lbl);

            default:
                return (today, today.AddDays(1).AddTicks(-1), "Today");
        }
    }

    private void LoadDashboardPrints()
    {
        if (!_isLoaded || _isUpdatingUi || _dbContext == null || DgDashboardJobs == null)
            return;

        try
        {
            var (fromDate, toDate, label) = GetDashboardDateRange();

            var jobs = _dbContext.GetFilteredJobs(50, null, null, null, fromDate, toDate);
            var stats = _dbContext.GetDateFilteredStats(fromDate, toDate);

            var items = jobs.Select(j =>
            {
                var docType = DocumentTypeClassifier.Classify(j.DocumentName);
                var isColor = string.Equals(j.ColorMode, "Color", StringComparison.OrdinalIgnoreCase);
                var badge = GetColorBadgeBrushes(isColor);

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
                    ColorModeDisplay = isColor ? "Color" : "B&W",
                    ModeBadgeText = isColor ? "COLOR" : "B&W",
                    ColorBadgeBg = badge.bg,
                    ColorBadgeFg = badge.fg,
                    Duplex = j.Duplex,
                    PaperSize = j.PaperSize,
                    Status = j.Status,
                    StatusBadgeText = (j.Status ?? "Completed").ToUpperInvariant(),
                    SyncStatus = j.SyncStatus,
                    SubmittedAtLocal = j.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    SubmittedLocal = j.SubmittedAt.ToLocalTime()
                };
            }).ToList();

            DgDashboardJobs.ItemsSource = items;

            // ---- KPI cards ----
            int total = stats.pagesCount;
            int color = stats.colorPages;
            int mono = stats.monoPages;
            int jobCount = stats.jobsCount;
            int failed = items.Count(i => (i.Status ?? "").Contains("fail", StringComparison.OrdinalIgnoreCase)
                                       || (i.Status ?? "").Contains("error", StringComparison.OrdinalIgnoreCase));
            double colorPct = total > 0 ? (double)color / total : 0;

            TxtTodayPages.Text = total.ToString("N0");
            TxtTotalPagesSub.Text = "All printers";
            TxtColorPagesVal.Text = color.ToString("N0");
            TxtColorPagesSub.Text = $"{Math.Round(colorPct * 100):0}% of output";
            TxtMonoPagesVal.Text = mono.ToString("N0");
            TxtMonoPagesSub.Text = $"{(total > 0 ? Math.Round((double)mono / total * 100) : 0):0}% of output";
            TxtTodayJobsVal.Text = jobCount.ToString("N0");
            TxtJobsSub.Text = label == "Today" ? "Recorded today" : $"Recorded · {label}";
            TxtSuccessRateVal.Text = jobCount > 0 ? $"{Math.Round((double)(jobCount - failed) / jobCount * 100):0}%" : "100%";
            TxtFailedJobsSub.Text = $"{failed} failed jobs";
            TxtAvgPagesJobVal.Text = jobCount > 0 ? ((double)total / jobCount).ToString("0.0") : "0.0";

            // ---- Overview title ----
            TxtOverviewDate.Text = label == "Today"
                ? $"Print Accounting Overview · Today, {DateTime.Now:dd MMM yyyy}"
                : $"Print Accounting Overview · {label}";

            // ---- Donut ----
            UpdateDonut(colorPct, total);
            TxtDonutColorText.Text = $"Color · {color:N0} pages";
            TxtDonutMonoText.Text = $"B&W · {mono:N0} pages";

            // ---- Pages by hour ----
            UpdateHourlyBars(items);

            // ---- Printers card ----
            UpdatePrintersCard(items);

            TxtLiveActivityCount.Text = $"{jobCount:N0} jobs · {total:N0} pages";
        }
        catch (Exception ex)
        {
            if (TxtStatusBar != null)
                TxtStatusBar.Text = $"Dashboard prints notice: {ex.Message}";
        }
    }

    private void UpdateDonut(double colorPct, int total)
    {
        TxtDonutPercent.Text = total > 0 ? $"{Math.Round(colorPct * 100):0}%" : "0%";
        DonutFullRing.Visibility = colorPct >= 0.999 && total > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (total == 0 || colorPct <= 0.001 || colorPct >= 0.999)
        {
            DonutColorArc.Data = null;
            return;
        }

        const double size = 92, stroke = 11;
        double r = (size - stroke) / 2, c = size / 2;
        double angle = colorPct * 360.0;
        double rad = (angle - 90) * Math.PI / 180.0;
        var start = new Point(c, c - r);
        var end = new Point(c + r * Math.Cos(rad), c + r * Math.Sin(rad));

        var fig = new PathFigure { StartPoint = start, IsClosed = false };
        fig.Segments.Add(new ArcSegment(end, new Size(r, r), 0, angle > 180, SweepDirection.Clockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        DonutColorArc.Data = geo;
    }

    private void UpdateHourlyBars(List<PrintJobDisplayItem> items)
    {
        GridHourBars.Children.Clear();
        GridHourBars.ColumnDefinitions.Clear();
        GridHourLabels.Children.Clear();
        GridHourLabels.ColumnDefinitions.Clear();

        var byHour = items.GroupBy(i => i.SubmittedLocal.Hour)
                          .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPagesCalculated));

        int nowHour = DateTime.Now.Hour;
        int startHour = byHour.Count > 0 ? Math.Min(byHour.Keys.Min(), nowHour) : Math.Max(0, nowHour - 4);
        int endHour = byHour.Count > 0 ? Math.Max(byHour.Keys.Max(), nowHour) : nowHour;
        if (endHour - startHour < 4) startHour = Math.Max(0, endHour - 4);
        if (endHour - startHour > 11) startHour = endHour - 11;

        int max = Math.Max(1, byHour.Values.DefaultIfEmpty(0).Max());
        var barBrush = (Brush)FindResource("AccentPrimary");
        var labelBrush = (Brush)FindResource("TextSecondary");

        for (int h = startHour, col = 0; h <= endHour; h++, col++)
        {
            GridHourBars.ColumnDefinitions.Add(new ColumnDefinition());
            GridHourLabels.ColumnDefinitions.Add(new ColumnDefinition());

            byHour.TryGetValue(h, out var pages);
            var bar = new Border
            {
                Width = 22,
                Height = Math.Max(3, pages / (double)max * 85),
                Background = barBrush,
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                ToolTip = $"{h:00}:00 — {pages} page(s)"
            };
            Grid.SetColumn(bar, col);
            GridHourBars.Children.Add(bar);

            var lbl = new TextBlock
            {
                Text = h.ToString("00"),
                FontSize = 10,
                Foreground = labelBrush,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(lbl, col);
            GridHourLabels.Children.Add(lbl);
        }
    }

    private void UpdatePrintersCard(List<PrintJobDisplayItem> items)
    {
        try
        {
            var pagesByPrinter = items.GroupBy(i => i.PrinterName ?? "")
                                      .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPagesCalculated), StringComparer.OrdinalIgnoreCase);
            var printers = _dbContext.GetAllPrinters().Where(p => p.IsActive).ToList();

            var rows = printers
                .Select(p => (p, pages: pagesByPrinter.TryGetValue(p.Name, out var n) ? n : 0))
                .OrderByDescending(x => x.pages).ThenBy(x => x.p.Name)
                .Select(x => new DashPrinterRow
                {
                    Name = x.p.Name,
                    PagesDisplay = $"{x.pages} pages",
                    StatusDisplay = string.Equals(x.p.Status, "Offline", StringComparison.OrdinalIgnoreCase) ? "OFFLINE" : "ONLINE"
                }).ToList();

            IcDashPrinters.ItemsSource = rows;
            int online = rows.Count(r => r.StatusDisplay == "ONLINE");
            TxtPrintersOnlineSub.Text = $"{online} printer{(online == 1 ? "" : "s")} online";
        }
        catch { }
    }

    private void CmbDashDateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdatingUi) return;

        if (PnlCustomDateRange != null)
        {
            PnlCustomDateRange.Visibility = CmbDashDateFilter.SelectedIndex == 5 ? Visibility.Visible : Visibility.Collapsed;
        }

        LoadDashboardPrints();
    }

    private void DpDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdatingUi) return;
        LoadDashboardPrints();
    }

    private void BtnRefreshDash_Click(object sender, RoutedEventArgs e)
    {
        LoadMetrics();
        LoadDashboardPrints();
        if (TxtStatusBar != null)
            TxtStatusBar.Text = $"Dashboard refreshed at {DateTime.Now:HH:mm:ss}";
    }

    private void BtnExportDashCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var (fromDate, toDate, label) = GetDashboardDateRange();
            var jobs = _dbContext.GetFilteredJobs(500, null, null, null, fromDate, toDate);

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var safeLabel = label.Replace(" ", "_").Replace(":", "-");
            var exportFile = Path.Combine(desktop, $"PrintMonitor_{safeLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using var sw = new StreamWriter(exportFile);
            sw.WriteLine("JobUid,JobId,SubmittedAtLocal,DocumentName,DocumentType,Username,Computer,Printer,Pages,Copies,TotalPages,ColorMode,Duplex,PaperSize,Status,SyncStatus");
            foreach (var j in jobs)
            {
                var dt = DocumentTypeClassifier.Classify(j.DocumentName);
                var total = j.PagesPrinted > 0 ? j.PagesPrinted : (j.Pages * j.Copies);
                sw.WriteLine($"\"{j.JobUid}\",{j.JobId},\"{j.SubmittedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\",\"{j.DocumentName}\",\"{dt.TypeName}\",\"{j.Username}\",\"{j.ComputerName}\",\"{j.PrinterName}\",{j.Pages},{j.Copies},{total},\"{j.ColorMode}\",\"{j.Duplex}\",\"{j.PaperSize}\",\"{j.Status}\",\"{j.SyncStatus}\"");
            }

            MessageBox.Show($"Exported {jobs.Count} jobs for {label} to:\n{exportFile}", "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGoToHistoryTab_Click(object sender, RoutedEventArgs e)
    {
        if (MainTabs != null)
        {
            MainTabs.SelectedIndex = 1;
        }
    }

    #endregion

    #region Print Jobs Tab & Document Type Detection

    private void LoadJobs()
    {
        if (!_isLoaded || _isUpdatingUi || _dbContext == null || DgJobs == null || TxtSearchJob == null || CmbPrinterFilter == null || CmbSyncFilter == null)
            return;

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
                    ColorBadgeBg = GetColorBadgeBrushes(isColor).bg,
                    ColorBadgeFg = GetColorBadgeBrushes(isColor).fg,
                    Duplex = j.Duplex,
                    PaperSize = j.PaperSize,
                    Status = j.Status,
                    SyncStatus = j.SyncStatus,
                    SubmittedAtLocal = j.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();

            DgJobs.ItemsSource = items;
            if (TxtJobTableSummary != null)
                TxtJobTableSummary.Text = $"Showing {items.Count} job(s) | Total pages in view: {items.Sum(x => x.TotalPagesCalculated):N0}";
        }
        catch (Exception ex)
        {
            if (TxtStatusBar != null)
                TxtStatusBar.Text = $"Error loading jobs: {ex.Message}";
        }
    }

    private void TxtSearchJob_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdatingUi) return;
        LoadJobs();
    }

    private void CmbPrinterFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdatingUi) return;
        LoadJobs();
    }

    private void CmbSyncFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || _isUpdatingUi) return;
        LoadJobs();
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
            LoadDashboardPrints();

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
        if (!_isLoaded || _isUpdatingUi || _dbContext == null || DgPrinters == null) return;
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

            if (TxtPrinterCountHeader != null)
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

            if (CmbPrinterFilter != null)
            {
                _isUpdatingUi = true;
                CmbPrinterFilter.Items.Clear();
                CmbPrinterFilter.Items.Add(new ComboBoxItem { Content = "All Printers", IsSelected = true });
                foreach (var p in printers)
                {
                    CmbPrinterFilter.Items.Add(new ComboBoxItem { Content = p.Name });
                }
                _isUpdatingUi = false;
            }
        }
        catch (Exception ex)
        {
            if (TxtStatusBar != null)
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
        if (!_isLoaded || _settingsManager == null || TxtApiBaseUrl == null) return;
        var s = _settingsManager.Settings;
        if (TxtUserEmail != null) TxtUserEmail.Text = s.UserEmail ?? string.Empty;
        if (TxtShopName != null) TxtShopName.Text = s.ShopName ?? string.Empty;
        TxtApiBaseUrl.Text = "https://printmonitor.nexreindigital.co.ke/api"; // Permanently locked & hardcoded
        TxtApiKey.Text = s.ApiKey;
        TxtSyncInterval.Text = (s.SyncIntervalSeconds > 0 ? s.SyncIntervalSeconds : 10).ToString();
        TxtHeartbeatInterval.Text = (s.HeartbeatIntervalSeconds > 0 ? s.HeartbeatIntervalSeconds : 10).ToString();
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
            var userEmail = TxtUserEmail?.Text?.Trim() ?? string.Empty;
            var shopName = TxtShopName?.Text?.Trim() ?? string.Empty;
            var url = "https://printmonitor.nexreindigital.co.ke/api"; // Permanently locked & hardcoded
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
                s.UserEmail = userEmail;
                s.ShopName = shopName;
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

    private void BtnOpenOnlineDashboard_Click(object sender, RoutedEventArgs e)
    {
        VersionController.OpenUrl("https://printmonitor.nexreindigital.co.ke");
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
            UpdateNavStrip(MainTabs.SelectedIndex);

        if (!_isLoaded || _isUpdatingUi) return;
        if (e.Source is TabControl)
        {
            if (MainTabs.SelectedIndex == 0) LoadDashboardPrints();
            else if (MainTabs.SelectedIndex == 1) LoadJobs();
            else if (MainTabs.SelectedIndex == 2) LoadPrinters();
            else if (MainTabs.SelectedIndex == 4) { LoadMachineAndStorageInfo(); LoadLogs(); }
        }
    }

    private void NavTab_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Prevent the header drag handler from swallowing tab clicks
        e.Handled = true;
        if (sender is FrameworkElement fe && int.TryParse(fe.Tag?.ToString(), out var idx) && MainTabs != null)
        {
            MainTabs.SelectedIndex = idx;
            UpdateNavStrip(idx);
        }
    }

    private void UpdateNavStrip(int selected)
    {
        var indicators = new[] { TabIndicator0, TabIndicator1, TabIndicator2, TabIndicator3, TabIndicator4 };
        var headers = new[] { TabHeader0, TabHeader1, TabHeader2, TabHeader3, TabHeader4 };
        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] == null || headers[i] == null) continue;
            bool active = i == selected;
            if (active) indicators[i].SetResourceReference(Border.BorderBrushProperty, "TabActiveBorder");
            else indicators[i].BorderBrush = Brushes.Transparent;
            headers[i].SetResourceReference(TextBlock.ForegroundProperty, active ? "TabActiveFg" : "TabInactiveFg");
            headers[i].FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
        }
    }

    #region Window Caption Controls (Minimize, Maximize / Restore, Close)

    private void TopBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // Ignore transient drag exceptions
                }
            }
        }
    }

    private void BtnWinMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnWinMaximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (BtnWinMaximize != null)
        {
            if (WindowState == WindowState.Maximized)
            {
                BtnWinMaximize.Content = "🗗";
                BtnWinMaximize.ToolTip = "Restore Window Down";
            }
            else
            {
                BtnWinMaximize.Content = "🗖";
                BtnWinMaximize.ToolTip = "Maximize Window";
            }
        }
    }

    private void BtnWinClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Software Updates & Version Controller

    private VersionCheckResult? _latestCheckResult;

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        await RunUpdateCheckAsync(isManualClick: true);
    }

    private async void BtnDownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        var targetUrl = _latestCheckResult?.DownloadUrl ?? VersionController.DirectInstallerUrl;
        var version = _latestCheckResult?.LatestVersion ?? "latest";

        var confirm = MessageBox.Show(
            $"You are about to install Nexrein Printer Monitor v{version}.\n\n" +
            "🛡️ DATA PRESERVATION GUARANTEE:\n" +
            "• All print job records, pages, and logs remain 100% intact.\n" +
            "• Your Device ID, User Email, Shop Name, and Passwords will NOT be modified.\n" +
            "• An automated pre-update safety backup will be created.\n\n" +
            "Would you like to download and install this update now?",
            "Confirm Update Installation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (BtnDownloadUpdate != null) BtnDownloadUpdate.IsEnabled = false;
        if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = false;
        if (PbUpdateProgress != null)
        {
            PbUpdateProgress.Visibility = Visibility.Visible;
            PbUpdateProgress.Value = 0;
        }

        var progress = new Progress<double>(pct =>
        {
            if (PbUpdateProgress != null) PbUpdateProgress.Value = pct;
            if (TxtUpdateStatusDetails != null)
                TxtUpdateStatusDetails.Text = $"Downloading update package: {pct:F0}%...";
        });

        var success = await Task.Run(async () =>
        {
            return await VersionController.DownloadAndLaunchUpdateAsync(
                targetUrl,
                progress,
                status => Dispatcher.Invoke(() =>
                {
                    if (TxtUpdateStatusDetails != null)
                        TxtUpdateStatusDetails.Text = status;
                }));
        });

        if (success)
        {
            if (TxtUpdateStatusDetails != null)
            {
                TxtUpdateStatusDetails.Text = "Update installer started! Control Panel will now close so setup can update files cleanly. Existing data is preserved.";
                TxtUpdateStatusDetails.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
            }

            MessageBox.Show(
                "Update installer launched successfully!\n\n" +
                "The Control Panel will now close to allow the installer to update application files cleanly.\n" +
                "Your print records and settings have been safely preserved.",
                "Updating Nexrein Printer Monitor",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _allowExit = true;
            _trayManager?.Dispose();
            Close();
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            if (BtnDownloadUpdate != null) BtnDownloadUpdate.IsEnabled = true;
            if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = true;
            if (PbUpdateProgress != null) PbUpdateProgress.Visibility = Visibility.Collapsed;

            var openWeb = MessageBox.Show(
                "Automatic package download could not be completed directly.\n\n" +
                "Would you like to open the official download page instead to download the installer manually?",
                "Update Download Notice",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (openWeb == MessageBoxResult.Yes)
            {
                VersionController.OpenUrl(targetUrl);
            }
        }
    }

    private void BtnUpdateBadge_Click(object sender, RoutedEventArgs e)
    {
        if (MainTabs != null)
        {
            // Switch to Settings / Updates tab
            MainTabs.SelectedIndex = 3;
        }
        BtnDownloadUpdate_Click(sender, e);
    }

    private async Task RunUpdateCheckAsync(bool isManualClick)
    {
        if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = false;
        if (TxtUpdateStatusDetails != null && isManualClick)
        {
            TxtUpdateStatusDetails.Text = "Querying update server and GitHub channels for latest version...";
            TxtUpdateStatusDetails.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        }

        try
        {
            var apiBaseUrl = _settingsManager?.Settings?.ApiBaseUrl;
            var apiKey = _settingsManager?.Settings?.ApiKey;

            var result = await VersionController.CheckForUpdatesAsync(apiBaseUrl, apiKey);
            _latestCheckResult = result;

            if (TxtInstalledVer != null)
                TxtInstalledVer.Text = $"{result.CurrentVersion} (win-x64 Enterprise)";

            if (TxtLatestVer != null)
                TxtLatestVer.Text = $"v{result.LatestVersion}";

            if (result.UpdateAvailable)
            {
                if (BtnUpdateBadge != null)
                {
                    BtnUpdateBadge.Visibility = Visibility.Visible;
                    BtnUpdateBadge.Content = $"🚀 Update v{result.LatestVersion} Available";
                }

                if (BtnDownloadUpdate != null)
                {
                    BtnDownloadUpdate.Visibility = Visibility.Visible;
                }

                if (TxtUpdateStatusDetails != null)
                {
                    TxtUpdateStatusDetails.Text = $"A newer version (v{result.LatestVersion}) is available. Your print database, statistics, and settings will remain completely intact when updating.";
                    TxtUpdateStatusDetails.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
                }

                if (TxtVersionPill != null)
                {
                    TxtVersionPill.Text = $"Update v{result.LatestVersion} Available";
                }

                _trayManager?.ShowBalloon(
                    "Nexrein Printer Monitor Update Available",
                    $"Version v{result.LatestVersion} is available. All your print records and settings will remain intact.",
                    TrayNotificationType.Info);

                if (isManualClick)
                {
                    var prompt = MessageBox.Show(
                        $"A new update (v{result.LatestVersion}) for Nexrein Printer Monitor is available!\n\n" +
                        "All local print records and settings will be preserved.\n\n" +
                        "Would you like to install the update now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (prompt == MessageBoxResult.Yes)
                    {
                        BtnDownloadUpdate_Click(this, new RoutedEventArgs());
                    }
                }
            }
            else
            {
                if (BtnUpdateBadge != null) BtnUpdateBadge.Visibility = Visibility.Collapsed;
                if (BtnDownloadUpdate != null) BtnDownloadUpdate.Visibility = Visibility.Collapsed;

                if (TxtUpdateStatusDetails != null)
                {
                    TxtUpdateStatusDetails.Text = $"Nexrein Printer Monitor is up to date (v{result.CurrentVersion}). No updates are currently required.";
                    TxtUpdateStatusDetails.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                }

                if (TxtVersionPill != null)
                {
                    TxtVersionPill.Text = $"Up to date: v{result.CurrentVersion}";
                }

                if (isManualClick)
                {
                    MessageBox.Show(
                        $"Nexrein Printer Monitor is up to date (v{result.CurrentVersion}).\n\nNo updates are needed at this time.",
                        "Check for Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            if (TxtUpdateStatusDetails != null)
            {
                TxtUpdateStatusDetails.Text = $"Notice: Update check encountered an error: {ex.Message}";
                TxtUpdateStatusDetails.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            }

            if (isManualClick)
            {
                MessageBox.Show($"Could not check for updates: {ex.Message}", "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            if (BtnCheckUpdates != null) BtnCheckUpdates.IsEnabled = true;
        }
    }

    #endregion
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
    public string ModeBadgeText { get; set; } = string.Empty;
    public string StatusBadgeText { get; set; } = string.Empty;
    public DateTime SubmittedLocal { get; set; }
    public SolidColorBrush ColorBadgeBg { get; set; } = Brushes.Transparent;
    public SolidColorBrush ColorBadgeFg { get; set; } = Brushes.White;
    public string Duplex { get; set; } = string.Empty;
    public string PaperSize { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = string.Empty;
    public string SubmittedAtLocal { get; set; } = string.Empty;
}

public class DashPrinterRow
{
    public string Name { get; set; } = string.Empty;
    public string PagesDisplay { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
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
