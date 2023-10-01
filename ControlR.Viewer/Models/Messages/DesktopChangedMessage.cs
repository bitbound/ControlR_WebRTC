namespace ControlR.Viewer.Models.Messages;
internal class DesktopChangedMessage
{
    public DesktopChangedMessage(Guid sessionId, string desktopName)
    {
        SessionId = sessionId;
        DesktopName = desktopName;
    }

    public Guid SessionId { get; }
    public string DesktopName { get; }
}
