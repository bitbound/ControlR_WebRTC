using ControlR.Shared.Dtos;

namespace ControlR.Server.Models;

public class AgentHubSession(string connectionId, DeviceDto device)
{
    public DeviceDto Device { get; } = device;
    public string ConnectionId { get; } = connectionId;
}
