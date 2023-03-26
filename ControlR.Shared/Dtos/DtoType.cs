using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public enum DtoType
{
    None,
    PublicKey,
    StreamingSessionRequest,
    WindowsSessions,
    DeviceUpdateRequest,
    RtcSessionDescription,
    RtcIceCandidate,
    CloseStreamingSession,
    PowerStateChange
}
