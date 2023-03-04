using ControlR.Shared.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Services;

internal interface IDeviceCache
{
    IEnumerable<DeviceDto> Devices { get; }

    void AddOrUpdate(string key, DeviceDto device);
    void Clear();
}

internal class DeviceCache : IDeviceCache
{
    private static readonly ConcurrentDictionary<string, DeviceDto> _cache = new();

    public IEnumerable<DeviceDto> Devices => _cache.Values;

    public void AddOrUpdate(string key, DeviceDto device)
    {
        _cache.AddOrUpdate(key, device, (k, v) => device);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
