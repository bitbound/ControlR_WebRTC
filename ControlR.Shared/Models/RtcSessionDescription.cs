using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class RtcSessionDescription
{
    [DataMember(Name = "sdp")]
    public string Sdp { get; init; } = string.Empty;

    [DataMember(Name = "type")]
    public string Type { get; init; } = string.Empty;
}
