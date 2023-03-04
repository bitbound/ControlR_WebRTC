using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;

internal enum ParameterlessMessageKind
{
    PrivateKeyChanged,
    ServerUriChanged,
    ShuttingDown,
    AuthStateChanged,
    PendingOperationsChanged,
    DevicesCacheUpdated,
    HubConnectionStateChanged,
}
