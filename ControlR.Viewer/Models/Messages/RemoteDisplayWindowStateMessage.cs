using ControlR.Viewer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;
internal class RemoteDisplayWindowStateMessage
{
    public RemoteDisplayWindowStateMessage(Guid sessionId, WindowState state)
    {
        SessionId = sessionId;
        State = state;
    }

    public Guid SessionId { get; }
    public WindowState State { get; }
}
