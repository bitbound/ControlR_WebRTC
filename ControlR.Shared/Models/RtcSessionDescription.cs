using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Models;

[DataContract]
public class RtcSessionDescription
{
    [DataMember(Name = "sdp")]
    public string Sdp { get; init; } = string.Empty;

    [DataMember(Name = "type")]
    public string Type { get; init; } = string.Empty;
}
