using ControlR.Shared.Enums;
using System.Runtime.Serialization;

namespace ControlR.Shared.Dtos;

[DataContract]
public class ToastDto
{
    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public MessageLevel MessageLevel { get; set; }
}
