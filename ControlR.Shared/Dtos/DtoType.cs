using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public enum DtoType
{
    None,
    PublicKey,
    DesktopSessionRequest,
    DisplayDto,
    DisplayList,
    WindowsSessions,
    DeviceUpdateRequest,
    RtcSessionDescription,
    RtcIceCandidate,
    PointerMove,
    CloseDesktopSession
}
