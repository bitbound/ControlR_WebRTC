using ControlR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Devices.Common.Services.Interfaces;
public interface IDeviceDataGenerator
{
    Task<Device> CreateDevice(double cpuUtilization, IEnumerable<string> authorizedKeys, string deviceId);
    Device GetDeviceBase(IEnumerable<string> authorizedKeys, string deviceId);
    (double usedStorage, double totalStorage) GetSystemDriveInfo();
    Task<(double usedGB, double totalGB)> GetMemoryInGB();
    string GetAgentVersion();
    List<Drive> GetAllDrives();
}