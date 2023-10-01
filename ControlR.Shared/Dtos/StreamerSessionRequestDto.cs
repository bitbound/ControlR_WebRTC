using MessagePack;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class StreamerSessionRequestDto
{
    [JsonConstructor]
    [SerializationConstructor]
    public StreamerSessionRequestDto(Guid streamingSessionId, int targetSystemSession, string? targetDesktop, string? viewerConnectionId)
    {
        StreamingSessionId = streamingSessionId;
        TargetSystemSession = targetSystemSession;
        TargetDesktop = targetDesktop;
        ViewerConnectionId = viewerConnectionId;
    }

    [DataMember]
    public Guid StreamingSessionId { get; init; }

    [DataMember]
    public int TargetSystemSession { get; init; }

    [DataMember]
    public string? ViewerConnectionId { get; init; }

    [DataMember]
    public string? TargetDesktop { get; init; }
}
