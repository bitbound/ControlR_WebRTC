using MessagePack;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class StreamerHubSession
{
    [SerializationConstructor]
    [JsonConstructor]
    public StreamerHubSession(Guid sessionId, Display[] displays, string streamerConnectionId)
    {
        SessionId = sessionId;
        StreamerConnectionId = streamerConnectionId;
        Displays = displays;
    }

    [DataMember]
    public string StreamerConnectionId { get; init; }

    [DataMember]
    public Display[] Displays { get; init; }

    [DataMember]
    public string? AgentConnectionId { get; set; }

    [DataMember]
    public Guid SessionId { get; init; }

    [DataMember]
    public string? ViewerConnectionId { get; set; }
}
