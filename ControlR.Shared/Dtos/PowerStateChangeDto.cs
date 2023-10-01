using ControlR.Shared.Enums;
using MessagePack;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class PowerStateChangeDto
{
    [SerializationConstructor]
    [JsonConstructor]
    public PowerStateChangeDto(PowerStateChangeType type)
    {
        Type = type;
    }

    [DataMember]
    public PowerStateChangeType Type { get; init; }
}
