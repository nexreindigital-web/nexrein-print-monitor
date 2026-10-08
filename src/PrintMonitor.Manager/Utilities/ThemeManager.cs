using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PrintMonitor.Manager.Utilities;

public enum AppThemeMode
{
    System = 0,
    Dark = 1,
    Light = 2
}

public static class ThemeManager
{
    private static AppThemeMode _currentMode = AppThemeMode.System;
    private static bool _initialized = false;

    public static AppThemeMode CurrentMode => _currentMode;

    public static bool IsDarkThemeActive
    {
        get
        {
            if (_currentMode == AppThemeMode.Dark) return true;
            if (_currentMode == AppThemeMode.Light) return false;
            return IsWindowsDarkTheme();
        }
    }

    public static event Action<bool>? ThemeChanged;

    public static void Initialize(ResourceDictionary resources)
    {
        if (_initialized) return;
        _initialized = true;

        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (_currentMode == AppThemeMode.System)
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    ApplyTheme(resources, AppThemeMode.System);
                });
            }
        };

        ApplyTheme(resources, AppThemeMode.System);
    }

    public static bool IsWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
            {
                return val == 0;
            }
        }
        catch
        {
            // Default to dark if registry check is unavailable
        }
        return true;
    }

    public static void ApplyTheme(ResourceDictionary dict, AppThemeMode mode)
    {
        _currentMode = mode;
        bool isDark = IsDarkThemeActive;

        if (isDark)
        {
            SetDarkTheme(dict);
        }
        else
        {
            SetLightTheme(dict);
        }

        ThemeChanged?.Invoke(isDark);
    }

    private static void SetDarkTheme(ResourceDictionary dict)
    {
        // Image 1: Dark Mode Palette
        dict["BgWindow"] = new SolidColorBrush(Color.FromRgb(10, 15, 29));        // #0A0F1D Deep navy body
        dict["BgHeader"] = new SolidColorBrush(Color.FromRgb(12, 19, 34));        // #0C1322 Midnight header
        dict["BgCard"] = new SolidColorBrush(Color.FromRgb(17, 28, 53));          // #111C35 Dark card
        dict["BgCardHover"] = new SolidColorBrush(Color.FromRgb(22, 35, 66));     // #162342 Card hover
        dict["BgInput"] = new SolidColorBrush(Color.FromRgb(14, 23, 46));         // #0E172E Input box
        dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(30, 45, 74));     // #1E2D4A Card border
        dict["BorderLight"] = new SolidColorBrush(Color.FromRgb(42, 59, 94));     // #2A3B5E

        dict["TextPrimary"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));  // #FFFFFF Primary text
        dict["TextSecondary"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));// #94A3B8 Secondary text
        dict["TextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));    // #64748B Muted text

        // Actions & Accents
        dict["AccentPrimary"] = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // #38BDF8 Sky Blue
        dict["AccentPrimaryHover"] = new SolidColorBrush(Color.FromRgb(14, 165, 233));
        dict["AccentSuccess"] = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981 Emerald
        dict["AccentSuccessHover"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        dict["AccentWarning"] = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // #F59E0B
        dict["AccentDanger"] = new SolidColorBrush(Color.FromRgb(220, 38, 38));   // #DC2626 Red Stop button
        dict["AccentDangerHover"] = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        dict["AccentInfo"] = new SolidColorBrush(Color.FromRgb(34, 211, 238));    // #22D3EE Cyan

        // Navigation Tabs
        dict["TabActiveBorder"] = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // #38BDF8
        dict["TabActiveFg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        dict["TabInactiveFg"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));

        // Header Action Buttons
        dict["HeaderBtnBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));     // #1E293B
        dict["HeaderBtnBorder"] = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // #334155
        dict["HeaderBtnFg"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));  // #E2E8F0

        // Service Badge
        dict["ServiceBadgeBg"] = new SolidColorBrush(Color.FromRgb(2, 44, 34));   // #022C22
        dict["ServiceBadgeBorder"] = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // #059669
        dict["ServiceBadgeFg"] = new SolidColorBrush(Color.FromRgb(52, 211, 153));// #34D399

        // Action Buttons on Section Bar (Today, Simulate Print, Export CSV)
        dict["SectionBtnBg"] = new SolidColorBrush(Color.FromRgb(17, 28, 53));    // #111C35
        dict["SectionBtnBorder"] = new SolidColorBrush(Color.FromRgb(30, 45, 74));// #1E2D4A
        dict["SectionBtnFg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White

        // 6 KPI Metric Values
        dict["KpiVal1"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));       // #38BDF8 Total Pages
        dict["KpiVal2"] = new SolidColorBrush(Color.FromRgb(34, 211, 238));       // #22D3EE Color Pages
        dict["KpiVal3"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));      // #CBD5E1 B&W Pages
        dict["KpiVal4"] = new SolidColorBrush(Color.FromRgb(192, 132, 252));      // #C084FC Print Jobs
        dict["KpiVal5"] = new SolidColorBrush(Color.FromRgb(52, 211, 153));       // #34D399 Success Rate
        dict["KpiVal6"] = new SolidColorBrush(Color.FromRgb(251, 146, 60));       // #FB923C Avg Pages/Job

        // Table & Badges
        dict["DataGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(17, 28, 53));
        dict["DataGridRowBg"] = new SolidColorBrush(Color.FromRgb(17, 28, 53));
        dict["DataGridRowAlt"] = new SolidColorBrush(Color.FromRgb(14, 23, 46));
        dict["DataGridSelected"] = new SolidColorBrush(Color.FromRgb(30, 45, 74));
        dict["BadgeColorBg"] = new SolidColorBrush(Color.FromRgb(8, 145, 178));   // #0891B2
        dict["BadgeCompletedBg"] = new SolidColorBrush(Color.FromRgb(2, 44, 34)); // #022C22
        dict["BadgeCompletedBorder"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        dict["BadgeCompletedFg"] = new SolidColorBrush(Color.FromRgb(52, 211, 153));

        // Footer Bar
        dict["FooterBg"] = new SolidColorBrush(Color.FromRgb(12, 19, 34));
        dict["FooterFg"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    }

    private static void SetLightTheme(ResourceDictionary dict)
    {
        // Image 2: Light Mode Palette (Keeps matching dark navy header!)
        dict["BgWindow"] = new SolidColorBrush(Color.FromRgb(237, 242, 247));     // #EDF2F7 Clean light body
        dict["BgHeader"] = new SolidColorBrush(Color.FromRgb(12, 19, 34));        // #0C1322 Dark header preserved!
        dict["BgCard"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));       // #FFFFFF Pure white card
        dict["BgCardHover"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));  // #F8FAFC
        dict["BgInput"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // #FFFFFF
        dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));  // #E2E8F0
        dict["BorderLight"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));  // #CBD5E1

        dict["TextPrimary"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));     // #0F172A Dark primary text
        dict["TextSecondary"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));  // #475569
        dict["TextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));    // #64748B

        // Actions & Accents
        dict["AccentPrimary"] = new SolidColorBrush(Color.FromRgb(37, 99, 235));  // #2563EB Blue
        dict["AccentPrimaryHover"] = new SolidColorBrush(Color.FromRgb(29, 78, 216));
        dict["AccentSuccess"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));  // #059669 Emerald
        dict["AccentSuccessHover"] = new SolidColorBrush(Color.FromRgb(4, 120, 87));
        dict["AccentWarning"] = new SolidColorBrush(Color.FromRgb(217, 119, 6));  // #D97706
        dict["AccentDanger"] = new SolidColorBrush(Color.FromRgb(220, 38, 38));   // #DC2626
        dict["AccentDangerHover"] = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        dict["AccentInfo"] = new SolidColorBrush(Color.FromRgb(13, 148, 136));    // #0D9488

        // Navigation Tabs (On dark header)
        dict["TabActiveBorder"] = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // #38BDF8
        dict["TabActiveFg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        dict["TabInactiveFg"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));

        // Header Action Buttons
        dict["HeaderBtnBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        dict["HeaderBtnBorder"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));
        dict["HeaderBtnFg"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));

        // Service Badge
        dict["ServiceBadgeBg"] = new SolidColorBrush(Color.FromRgb(2, 44, 34));
        dict["ServiceBadgeBorder"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        dict["ServiceBadgeFg"] = new SolidColorBrush(Color.FromRgb(52, 211, 153));

        // Action Buttons on Section Bar (Today, Simulate Print, Export CSV)
        dict["SectionBtnBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White
        dict["SectionBtnBorder"] = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
        dict["SectionBtnFg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));    // Dark

        // 6 KPI Metric Values
        dict["KpiVal1"] = new SolidColorBrush(Color.FromRgb(29, 78, 216));        // #1D4ED8 Total Pages
        dict["KpiVal2"] = new SolidColorBrush(Color.FromRgb(13, 148, 136));       // #0D9488 Color Pages
        dict["KpiVal3"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));        // #475569 B&W Pages
        dict["KpiVal4"] = new SolidColorBrush(Color.FromRgb(124, 58, 237));       // #7C3AED Print Jobs
        dict["KpiVal5"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));        // #059669 Success Rate
        dict["KpiVal6"] = new SolidColorBrush(Color.FromRgb(217, 119, 6));        // #D97706 Avg Pages/Job

        // Table & Badges
        dict["DataGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        dict["DataGridRowBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        dict["DataGridRowAlt"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        dict["DataGridSelected"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
        dict["BadgeColorBg"] = new SolidColorBrush(Color.FromRgb(13, 148, 136));   // #0D9488
        dict["BadgeCompletedBg"] = new SolidColorBrush(Color.FromRgb(220, 252, 231));// #DCFCE7
        dict["BadgeCompletedBorder"] = new SolidColorBrush(Color.FromRgb(134, 239, 172));
        dict["BadgeCompletedFg"] = new SolidColorBrush(Color.FromRgb(22, 101, 52)); // #166534

        // Footer Bar
        dict["FooterBg"] = new SolidColorBrush(Color.FromRgb(237, 242, 247));
        dict["FooterFg"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
    }
}
