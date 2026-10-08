using Microsoft.Extensions.Logging;
using PrintMonitor.Configuration;
using PrintMonitor.Data;
using PrintMonitor.Models;
using PrintMonitor.Utilities;

namespace PrintMonitor.Services;

public interface IDeviceRegistrationService
{
    Task<bool> EnsureRegisteredAsync(CancellationToken cancellationToken = default);
}

public class DeviceRegistrationService : IDeviceRegistrationService
{
    private readonly IApiClient _apiClient;
    private readonly PrintMonitorDbContext _dbContext;
    private readonly SettingsManager _settingsManager;
    private readonly ILogger<DeviceRegistrationService> _logger;

    public DeviceRegistrationService(
        IApiClient apiClient,
        PrintMonitorDbContext dbContext,
        SettingsManager settingsManager,
        ILogger<DeviceRegistrationService> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task<bool> EnsureRegisteredAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = _settingsManager.Settings.DeviceId;
        var comp = _dbContext.GetComputerInfo(deviceId);

        if (comp == null)
        {
            comp = MachineIdentifier.GetCurrentComputerInfo(deviceId);
            _dbContext.SaveComputerInfo(comp);
        }

        if (comp.IsRegistered)
        {
            _logger.LogInformation("Device {DeviceId} is already registered.", deviceId);
            return true;
        }

        _logger.LogInformation("Attempting registration for Device {DeviceId} ({ComputerName})...", deviceId, comp.ComputerName);

        var request = new DeviceRegisterRequest
        {
            DeviceId = comp.DeviceId,
            ComputerName = comp.ComputerName,
            WindowsUser = comp.WindowsUser,
            Domain = comp.Domain,
            OsVersion = comp.OsVersion,
            Architecture = comp.OsArchitecture,
            ApplicationVersion = comp.ApplicationVersion,
            IpAddress = comp.IpAddress,
            MacAddress = comp.MacAddress
        };

        var (success, response, error) = await _apiClient.RegisterDeviceAsync(request, cancellationToken);
        if (success && response != null && response.Success)
        {
            _logger.LogInformation("Device successfully registered with backend server!");
            _dbContext.SetDeviceRegistered(deviceId, true);

            if (!string.IsNullOrWhiteSpace(response.ApiKey) && string.IsNullOrWhiteSpace(_settingsManager.Settings.ApiKey))
            {
                _settingsManager.SaveSettings(s => s.ApiKey = response.ApiKey);
            }

            return true;
        }

        _logger.LogWarning("Device registration not completed yet: {Error}. Will operate offline and retry later.", error ?? "Unknown reason");
        return false;
    }
}
