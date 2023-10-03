using ControlR.Shared.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[MessagePackObject]
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

    [MsgPackKey]
    public Guid StreamingSessionId { get; init; }

    [MsgPackKey]
    public int TargetSystemSession { get; init; }

    [MsgPackKey]
    public string? ViewerConnectionId { get; init; }

    [MsgPackKey]
    public string? TargetDesktop { get; init; }
}
