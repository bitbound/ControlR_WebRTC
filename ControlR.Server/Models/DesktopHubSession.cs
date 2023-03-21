using ControlR.Shared.Dtos;

namespace ControlR.Server.Models;

public class DesktopHubSession
{
    public DesktopHubSession(Guid sessionId, string desktopConnectionId)
    {
        SessionId = sessionId;
        DesktopConnectionId = desktopConnectionId;
    }

    public string DesktopConnectionId { get; }
    public string? AgentConnectionId { get; set; }
    public Guid SessionId { get; }
    public string? ViewerConnectionId { get; set; }
}
