using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ControlR.Shared.Dtos;

[DataContract]
public class DesktopSessionRequestDto
{
    [JsonConstructor]
    [SerializationConstructor]
    public DesktopSessionRequestDto(Guid desktopSessionId, int targetSystemSession, string? targetDesktop, string? viewerConnectionId)
    {
        DesktopSessionId = desktopSessionId;
        TargetSystemSession = targetSystemSession;
        TargetDesktop = targetDesktop;
        ViewerConnectionId = viewerConnectionId;
    }

    [DataMember]
    public Guid DesktopSessionId { get; init; }

    [DataMember]
    public int TargetSystemSession { get; init; }

    [DataMember]
    public string? ViewerConnectionId { get; init; }

    [DataMember]
    public string? TargetDesktop { get; init; }
}
