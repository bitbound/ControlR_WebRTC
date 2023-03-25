using ControlR.Shared.Enums;
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
