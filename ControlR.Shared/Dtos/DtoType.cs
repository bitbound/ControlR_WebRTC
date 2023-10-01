using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public enum DtoType
{
    None = 0,
    PublicKey = 1,
    StreamingSessionRequest = 2,
    WindowsSessions = 3,
    DeviceUpdateRequest = 4,
    RtcSessionDescription = 5,
    RtcIceCandidate = 6,
    CloseStreamingSession = 7,
    PowerStateChange = 8,
}
