using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
