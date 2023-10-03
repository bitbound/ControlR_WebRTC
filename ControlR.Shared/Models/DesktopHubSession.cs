using ControlR.Shared.Dtos;
using ControlR.Shared.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Models;

[MessagePackObject]
public class StreamerHubSession
{
    [SerializationConstructor]
    [JsonConstructor]
    public StreamerHubSession(Guid sessionId, DisplayDto[] displays, string streamerConnectionId)
    {
        SessionId = sessionId;
        StreamerConnectionId = streamerConnectionId;
        Displays = displays;
    }

    [MsgPackKey]
    public string StreamerConnectionId { get; init; }

    [MsgPackKey]
    public DisplayDto[] Displays { get; init; }

    [MsgPackKey]
    public string? AgentConnectionId { get; set; }

    [MsgPackKey]
    public Guid SessionId { get; init; }

    [MsgPackKey]
    public string? ViewerConnectionId { get; set; }
}
