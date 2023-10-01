using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class IceServer
{
    [DataMember(Name = "credential")]
    public string Credential { get; init; } = string.Empty;

    [DataMember(Name = "credentialType")]
    public string CredentialType { get; init; } = string.Empty;

    [DataMember(Name = "urls")]
    public string Urls { get; init; } = string.Empty;

    [DataMember(Name = "username")]
    public string Username { get; init; } = string.Empty;
}