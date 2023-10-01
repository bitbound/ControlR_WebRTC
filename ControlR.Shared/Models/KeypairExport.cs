using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class KeypairExport
{
    [DataMember]
    public string EncryptedPrivateKeyBase64 { get; init; } = string.Empty;

    [DataMember]
    public string PublicKeyBase64 { get; init; } = string.Empty;

    [DataMember]
    public string Username { get; init; } = string.Empty;
}
