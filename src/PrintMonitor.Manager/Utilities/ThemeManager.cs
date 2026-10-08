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
        dict["BgWindow"] = new SolidColorBrush(Color.FromRgb(11, 15, 25));       // #0B0F19
        dict["BgHeader"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));       // #111827
        dict["BgCard"] = new SolidColorBrush(Color.FromRgb(26, 34, 52));         // #1A2234
        dict["BgCardHover"] = new SolidColorBrush(Color.FromRgb(36, 48, 72));    // #243048
        dict["BgInput"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));        // #111827
        dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(38, 51, 77));    // #26334D
        dict["BorderLight"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));    // #334155

        dict["TextPrimary"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
        dict["TextSecondary"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));// #94A3B8
        dict["TextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));   // #64748B

        dict["AccentPrimary"] = new SolidColorBrush(Color.FromRgb(99, 102, 241));// #6366F1
        dict["AccentPrimaryHover"] = new SolidColorBrush(Color.FromRgb(79, 70, 229));
        dict["AccentSuccess"] = new SolidColorBrush(Color.FromRgb(16, 185, 129));// #10B981
        dict["AccentSuccessHover"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        dict["AccentWarning"] = new SolidColorBrush(Color.FromRgb(245, 158, 11));// #F59E0B
        dict["AccentDanger"] = new SolidColorBrush(Color.FromRgb(239, 68, 68));  // #EF4444
        dict["AccentDangerHover"] = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        dict["AccentInfo"] = new SolidColorBrush(Color.FromRgb(14, 165, 233));   // #0EA5E9

        dict["DataGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));
        dict["DataGridRowBg"] = new SolidColorBrush(Color.FromRgb(26, 34, 52));
        dict["DataGridRowAlt"] = new SolidColorBrush(Color.FromRgb(20, 28, 45));
        dict["DataGridSelected"] = new SolidColorBrush(Color.FromRgb(46, 56, 86));

        dict["LockBadgeBg"] = new SolidColorBrush(Color.FromRgb(49, 46, 129));   // #312E81
        dict["LockBadgeFg"] = new SolidColorBrush(Color.FromRgb(199, 210, 254)); // #C7D2FE
    }

    private static void SetLightTheme(ResourceDictionary dict)
    {
        dict["BgWindow"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));    // #F8FAFC
        dict["BgHeader"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));    // #FFFFFF
        dict["BgCard"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // #FFFFFF
        dict["BgCardHover"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
        dict["BgInput"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));     // #F8FAFC
        dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
        dict["BorderLight"] = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1

        dict["TextPrimary"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));     // #0F172A
        dict["TextSecondary"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));  // #475569
        dict["TextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));   // #64748B

        dict["AccentPrimary"] = new SolidColorBrush(Color.FromRgb(79, 70, 229)); // #4F46E5
        dict["AccentPrimaryHover"] = new SolidColorBrush(Color.FromRgb(67, 56, 202));
        dict["AccentSuccess"] = new SolidColorBrush(Color.FromRgb(5, 150, 105));  // #059669
        dict["AccentSuccessHover"] = new SolidColorBrush(Color.FromRgb(4, 120, 87));
        dict["AccentWarning"] = new SolidColorBrush(Color.FromRgb(217, 119, 6));  // #D97706
        dict["AccentDanger"] = new SolidColorBrush(Color.FromRgb(220, 38, 38));  // #DC2626
        dict["AccentDangerHover"] = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        dict["AccentInfo"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));    // #0284C7

        dict["DataGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        dict["DataGridRowBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        dict["DataGridRowAlt"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        dict["DataGridSelected"] = new SolidColorBrush(Color.FromRgb(224, 231, 255));

        dict["LockBadgeBg"] = new SolidColorBrush(Color.FromRgb(238, 242, 255)); // #EEF2FF
        dict["LockBadgeFg"] = new SolidColorBrush(Color.FromRgb(67, 56, 202));   // #4338CA
    }
}
