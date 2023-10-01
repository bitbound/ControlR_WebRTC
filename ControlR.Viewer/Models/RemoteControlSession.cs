using ControlR.Shared.Dtos;

namespace ControlR.Viewer.Models;
public class RemoteControlSession
{
    public RemoteControlSession(DeviceDto device, int initialSystemSession)
    {
        Device = device;
        InitialSystemSession = initialSystemSession;
    }

    public DeviceDto Device { get; }
    public int InitialSystemSession { get; }
    public Guid SessionId { get; private set; } = Guid.NewGuid();

    public void CreateNewSessionId()
    {
        SessionId = Guid.NewGuid();
    }
}
