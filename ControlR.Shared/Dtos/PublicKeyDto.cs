using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class PublicKeyDto
{
    [DataMember]
    public required string Username { get; init; }

    [DataMember]
    public required byte[] PublicKey { get; init; }
}
