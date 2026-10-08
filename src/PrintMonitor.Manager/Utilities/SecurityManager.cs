using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PrintMonitor.Configuration;

namespace PrintMonitor.Manager.Utilities;

public static class SecurityManager
{
    public const string DefaultPassword = "admin";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "NexreinPrintMonitorManager";
    private const string LegacyRunValueName = "PrintMonitorManager";

    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifyPassword(string input, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var inputHash = HashPassword(input);

        if (string.IsNullOrWhiteSpace(storedHash))
        {
            // Fallback to default password "admin" or "admin123"
            return input == DefaultPassword || input == "admin123" || inputHash == HashPassword(DefaultPassword);
        }

        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            return key?.GetValue(RunValueName) != null || key?.GetValue(LegacyRunValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return false;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? @"C:\Program Files\PrintMonitor\PrintMonitor.Manager.exe";
                key.SetValue(RunValueName, $"\"{exePath}\"");
                key.DeleteValue(LegacyRunValueName, false);
            }
            else
            {
                key.DeleteValue(RunValueName, false);
                key.DeleteValue(LegacyRunValueName, false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
