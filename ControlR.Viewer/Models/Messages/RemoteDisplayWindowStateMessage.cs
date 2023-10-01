using ControlR.Viewer.Enums;

namespace ControlR.Viewer.Models.Messages;
internal class RemoteDisplayWindowStateMessage
{
    public RemoteDisplayWindowStateMessage(Guid sessionId, WindowState state)
    {
        SessionId = sessionId;
        State = state;
    }

    public Guid SessionId { get; }
    public WindowState State { get; }
}
