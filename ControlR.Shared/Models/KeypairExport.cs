using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class KeypairExport
{
    [DataMember]
    public required byte[] EncryptedPrivateKey { get; init; }

    [DataMember]
    public required byte[] PublicKey { get; init; }

    [DataMember]
    public required string Username { get; init; }
}
