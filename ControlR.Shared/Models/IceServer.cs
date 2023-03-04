using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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