using System.Runtime.Serialization;

namespace ControlR.Shared.Enums;

[DataContract]
public enum PowerStateChangeType
{
    [EnumMember]
    None,

    [EnumMember]
    Restart,

    [EnumMember]
    Shutdown
}
