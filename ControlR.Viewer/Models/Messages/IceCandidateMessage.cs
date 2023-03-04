using ControlR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Models.Messages;
internal class IceCandidateMessage
{
    public IceCandidateMessage(Guid sessionId, string candidateJson)
    {
        SessionId = sessionId;
        CandidateJson = candidateJson;
    }

    public Guid SessionId { get; }
    public string CandidateJson { get; }
}
