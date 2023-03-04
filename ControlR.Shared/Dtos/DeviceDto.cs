using ControlR.Shared.Enums;
using ControlR.Shared.Extensions;
using ControlR.Shared.Models;
using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class DeviceDto : Device
{
    public static Result<DeviceDto> TryCreateFrom(Device device, ConnectionType type, string connectionId)
    {
        var result = device.TryCloneAs<Device, DeviceDto>();
        if (result.IsSuccess)
        {
            result.Value.Type = type;
            result.Value.ConnectionId = connectionId;
        }
        return result;
    }

    [DataMember]
    public ConnectionType Type { get; set; }

    [DataMember]
    public string ConnectionId { get; set; } = string.Empty;
}
