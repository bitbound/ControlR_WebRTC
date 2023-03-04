using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Shared.Dtos;

[DataContract]
public class PublicKeyDto
{
    [DataMember]
    public string Username { get; init; } = string.Empty;

    [DataMember]
    public string PublicKey { get; init; } = string.Empty;
}
