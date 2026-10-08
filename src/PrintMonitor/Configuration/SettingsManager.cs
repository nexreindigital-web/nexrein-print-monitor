using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PrintMonitor.Models;
using PrintMonitor.Utilities;

namespace PrintMonitor.Configuration;

public class SettingsManager
{
    private readonly string _overrideConfigPath;
    private AgentSettings _settings;
    private static readonly object SyncLock = new();

    public AgentSettings Settings => _settings;

    public SettingsManager(IConfiguration? configuration = null, string? customDataDir = null)
    {
        var defaultDataDir = customDataDir ?? @"C:\ProgramData\PrintMonitor";
        _overrideConfigPath = Path.Combine(defaultDataDir, "config.json");

        _settings = new AgentSettings();

        // 1. Bind from IConfiguration if provided
        if (configuration != null)
        {
            var section = configuration.GetSection(AgentSettings.SectionName);
            if (section.Exists())
            {
                section.Bind(_settings);
            }
        }

        // 2. Override with local persistent config if exists
        LoadOverrideConfig();

        // 3. Ensure hardcoded production endpoint and self-heal any legacy config
        const string productionEndpoint = "https://printmonitor.nexreindigital.co.ke/api";
        if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl) || _settings.ApiBaseUrl.Contains("your-domain.com", StringComparison.OrdinalIgnoreCase))
        {
            _settings.ApiBaseUrl = productionEndpoint;
            try
            {
                SaveSettings(s => s.ApiBaseUrl = productionEndpoint);
            }
            catch
            {
                // Ignore initial save error if directory permissions are pending
            }
        }

        // 4. Ensure DeviceId is established
        if (string.IsNullOrWhiteSpace(_settings.DeviceId))
        {
            var dataDir = Path.GetDirectoryName(_settings.DatabasePath) ?? defaultDataDir;
            _settings.DeviceId = MachineIdentifier.GetOrCreateDeviceId(dataDir, null);
        }
    }

    private void LoadOverrideConfig()
    {
        try
        {
            if (File.Exists(_overrideConfigPath))
            {
                var json = File.ReadAllText(_overrideConfigPath);
                var overrideSettings = JsonSerializer.Deserialize<AgentSettings>(json);
                if (overrideSettings != null)
                {
                    if (!string.IsNullOrWhiteSpace(overrideSettings.ApiBaseUrl) &&
                        !overrideSettings.ApiBaseUrl.Contains("your-domain.com", StringComparison.OrdinalIgnoreCase))
                    {
                        _settings.ApiBaseUrl = overrideSettings.ApiBaseUrl;
                    }
                    else
                    {
                        _settings.ApiBaseUrl = "https://printmonitor.nexreindigital.co.ke/api";
                    }

                    if (!string.IsNullOrWhiteSpace(overrideSettings.ApiKey))
                        _settings.ApiKey = overrideSettings.ApiKey;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.DeviceId))
                        _settings.DeviceId = overrideSettings.DeviceId;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.UserEmail))
                        _settings.UserEmail = overrideSettings.UserEmail;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.ShopName))
                        _settings.ShopName = overrideSettings.ShopName;

                    if (overrideSettings.SyncIntervalSeconds > 0)
                        _settings.SyncIntervalSeconds = overrideSettings.SyncIntervalSeconds;

                    if (overrideSettings.HeartbeatIntervalSeconds > 0)
                        _settings.HeartbeatIntervalSeconds = overrideSettings.HeartbeatIntervalSeconds;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.DatabasePath))
                        _settings.DatabasePath = overrideSettings.DatabasePath;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.LogDirectory))
                        _settings.LogDirectory = overrideSettings.LogDirectory;

                    if (!string.IsNullOrWhiteSpace(overrideSettings.AdminPasswordHash))
                        _settings.AdminPasswordHash = overrideSettings.AdminPasswordHash;

                    _settings.AutostartOnBoot = overrideSettings.AutostartOnBoot;
                }
            }
        }
        catch
        {
            // Fallback to base settings
        }
    }

    public void SaveSettings(Action<AgentSettings> updateAction)
    {
        lock (SyncLock)
        {
            updateAction(_settings);

            try
            {
                var dir = Path.GetDirectoryName(_overrideConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_overrideConfigPath, json);
            }
            catch
            {
                // Suppress or log
            }
        }
    }
}
