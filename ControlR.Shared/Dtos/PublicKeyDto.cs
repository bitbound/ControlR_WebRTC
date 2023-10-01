using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class PublicKeyDto
{
    [DataMember]
    public string Username { get; init; } = string.Empty;

    [DataMember]
    public string PublicKey { get; init; } = string.Empty;
}
