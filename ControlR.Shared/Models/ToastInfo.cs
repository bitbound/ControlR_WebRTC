using ControlR.Shared.Enums;
using System.Runtime.Serialization;

namespace ControlR.Shared.Models;

[DataContract]
public class ToastInfo
{
    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public MessageLevel MessageLevel { get; set; }
}
