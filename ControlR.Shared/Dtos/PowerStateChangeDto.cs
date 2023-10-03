using ControlR.Shared.Enums;
using ControlR.Shared.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[MessagePackObject]
public class PowerStateChangeDto
{
    [SerializationConstructor]
    [JsonConstructor]
    public PowerStateChangeDto(PowerStateChangeType type)
    {
        Type = type;
    }

    [MsgPackKey]
    public PowerStateChangeType Type { get; init; }
}
