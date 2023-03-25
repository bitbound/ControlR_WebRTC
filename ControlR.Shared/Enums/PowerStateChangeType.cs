using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
