using ControlR.Devices.Common.Services.Interfaces;
using ControlR.Shared.Extensions;
using ControlR.Shared.Models;
using ControlR.Shared.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Devices.Common.Services.Linux;
internal class DeviceDataGeneratorLinux : DeviceDataGeneratorBase, IDeviceDataGenerator
{
    private readonly IProcessInvoker _processInvoker;
    private readonly ILogger<DeviceDataGeneratorLinux> _logger;

    public DeviceDataGeneratorLinux(
        IProcessInvoker processInvoker,
        IEnvironmentHelper environmentHelper,
        ILogger<DeviceDataGeneratorLinux> logger)
        : base(environmentHelper, logger)
    {
        _processInvoker = processInvoker;
        _logger = logger;
    }

    public async Task<Device> CreateDevice(double cpuUtilization, IEnumerable<string> authorizedKeys)
    {
        var device = GetDeviceBase(authorizedKeys);

        try
        {
            var (usedStorage, totalStorage) = GetSystemDriveInfo();
            var (usedMemory, totalMemory) = await GetMemoryInGB();

            device.CurrentUser = await GetCurrentUser();
            device.Drives = GetAllDrives();
            device.UsedStorage = usedStorage;
            device.TotalStorage = totalStorage;
            device.UsedMemory = usedMemory;
            device.TotalMemory = totalMemory;
            device.CpuUtilization = cpuUtilization;
            device.AgentVersion = GetAgentVersion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device data.");
        }

        return device;
    }

    private async Task<string> GetCurrentUser()
    {
        var result = await _processInvoker.GetProcessOutput("users", "");
        if (!result.IsSuccess)
        {
            return string.Empty;
        }
        return result.Value.Split()?.FirstOrDefault()?.Trim() ?? string.Empty;
    }

    public async Task<(double usedGB, double totalGB)> GetMemoryInGB()
    {
        try
        {
            var result = await _processInvoker.GetProcessOutput("cat", "/proc/meminfo");

            if (!result.IsSuccess)
            {
                _logger.LogResult(result);
                return (0, 0);
            }

            var resultsArr = result.Value.Split("\n".ToCharArray());
            var freeKB = resultsArr
                        .FirstOrDefault(x => x.Trim().StartsWith("MemAvailable"))?
                        .Trim()
                        .Split(" ".ToCharArray(), 2)
                        .Last() // 9168236 kB
                        .Trim()
                        .Split(' ')
                        .First(); // 9168236

            var totalKB = resultsArr
                        .FirstOrDefault(x => x.Trim().StartsWith("MemTotal"))?
                        .Trim()
                        .Split(" ".ToCharArray(), 2)
                        .Last() // 16637468 kB
                        .Trim()
                        .Split(' ')
                        .First(); // 16637468

            if (double.TryParse(freeKB, out var freeKbDouble) &&
                double.TryParse(totalKB, out var totalKbDouble))
            {
                var freeGB = Math.Round(freeKbDouble / 1024 / 1024, 2);
                var totalGB = Math.Round(totalKbDouble / 1024 / 1024, 2);

                return (totalGB - freeGB, totalGB);
            }

            return (0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting device memory.");
            return (0, 0);
        }
    }
}
