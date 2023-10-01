using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class SignedPayloadDto
{
    [DataMember(Name = "dtoType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DtoType DtoType { get; init; }

    [DataMember(Name = "payload")]
    public required byte[] Payload { get; init; }

    [DataMember(Name = "publicKey")]
    public required byte[] PublicKey { get; init; }

    [IgnoreDataMember]
    public string PublicKeyBase64 => Convert.ToBase64String(PublicKey ?? Array.Empty<byte>());

    [DataMember(Name = "signature")]
    public required byte[] Signature { get; init; }
}
