using MessagePack;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class SignedPayloadDto
{
    [DataMember(Name = "payload")]
    public required string Payload { get; init; } = string.Empty;

    [DataMember(Name = "signature")]
    public required string Signature { get; init; } = string.Empty;

    [DataMember(Name = "dtoType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DtoType DtoType { get; init; }

    [DataMember(Name = "publicKey")]
    public required string PublicKey { get; init; }

    [DataMember(Name = "publicKeyPem")]
    public required string PublicKeyPem { get; init; }
}
