using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrintMonitor.Manager.Utilities;

public class VersionCheckResult
{
    public bool Success { get; set; }
    public string CurrentVersion { get; set; } = "1.0.0";
    public string LatestVersion { get; set; } = "1.0.0";
    public bool UpdateAvailable { get; set; }
    public bool IsMandatory { get; set; }
    public string? DownloadUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}

public static class VersionController
{
    public static readonly Version CurrentVersion = new(1, 0, 0);
    public static readonly string CurrentVersionString = "1.0.0";
    public static readonly string ProductName = "Nexrein Print Monitor";

    public static string GetInstalledVersionString()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : CurrentVersionString;
    }

    public static async Task<VersionCheckResult> CheckForUpdatesAsync(string? apiBaseUrl, string? apiKey = null)
    {
        var result = new VersionCheckResult
        {
            CurrentVersion = CurrentVersionString,
            LatestVersion = CurrentVersionString
        };

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            result.Success = true;
            result.Message = "No API URL configured. Running current version v" + CurrentVersionString;
            return result;
        }

        try
        {
            var cleanUrl = apiBaseUrl.TrimEnd('/');
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            var response = await client.GetAsync($"{cleanUrl}/version/latest");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("latest_version", out var verProp))
                {
                    result.LatestVersion = verProp.GetString() ?? CurrentVersionString;
                }

                if (root.TryGetProperty("installer", out var instProp) && instProp.TryGetProperty("download_url", out var dlProp))
                {
                    result.DownloadUrl = dlProp.GetString();
                }

                if (Version.TryParse(result.LatestVersion, out var remoteVer))
                {
                    result.UpdateAvailable = remoteVer > CurrentVersion;
                }

                result.Success = true;
                result.Message = result.UpdateAvailable
                    ? $"New release v{result.LatestVersion} available."
                    : $"Nexrein Print Monitor is up to date (v{CurrentVersionString}).";
            }
            else
            {
                result.Success = false;
                result.Message = $"Version check server responded with HTTP {(int)response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Version check skipped: {ex.Message}";
        }

        return result;
    }

    public static bool TryLocateLocalInstaller(out string installerPath)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "PrintMonitor-Setup.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "release", "PrintMonitor-Setup.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "PrintMonitor", "release", "PrintMonitor-Setup.exe"),
            @"C:\Program Files\PrintMonitor\installer\PrintMonitor-Setup.exe"
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full))
            {
                installerPath = full;
                return true;
            }
        }

        installerPath = string.Empty;
        return false;
    }
}
