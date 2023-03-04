using ControlR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;
internal class RtcSessionDescriptionMessage
{
    public RtcSessionDescriptionMessage(Guid sessionId, RtcSessionDescription sessionDescription)
    {
        SessionId = sessionId;
        SessionDescription = sessionDescription;
    }

    public Guid SessionId { get; }
    public RtcSessionDescription SessionDescription { get; }
}
