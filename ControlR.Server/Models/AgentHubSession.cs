using ControlR.Shared.Dtos;

namespace ControlR.Server.Models;

public class AgentHubSession
{
    public AgentHubSession(string connectionId, DeviceDto device)
    {
        ConnectionId = connectionId;
        Device = device;
    }

    public DeviceDto Device { get; }
    public string ConnectionId { get; }
}
