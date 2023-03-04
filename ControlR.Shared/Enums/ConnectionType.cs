using System.Runtime.Serialization;

namespace ControlR.Shared.Enums;

[DataContract]
public enum ConnectionType
{
    [EnumMember]
    Unknown,
    [EnumMember]
    Viewer,
    [EnumMember]
    Agent,
    [EnumMember]
    Desktop
}
