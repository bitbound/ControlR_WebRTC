using ControlR.Shared.Models;

namespace ControlR.Viewer.Models.Messages;
internal class RtcSessionDescriptionMessage
{
    public RtcSessionDescriptionMessage(Guid sessionId, RtcSessionDescription sessionDescription)
    {
        SessionId = sessionId;
        SessionDescription = sessionDescription;
    }

    public Guid SessionId { get; }
    public RtcSessionDescription SessionDescription { get; }
}
