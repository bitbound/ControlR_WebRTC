using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Models;
internal class StreamingSession
{
    public StreamingSession(int streamerProcessId, Guid sessionId, string authorizedKey)
    {
        StreamerProcessId = streamerProcessId;
        SessionId = sessionId;
        AuthorizedKey = authorizedKey;
    }

    public int StreamerProcessId { get; }
    public Guid SessionId { get; }
    public string AuthorizedKey { get; }
    public int WatcherProcessId { get; set; } = -1;
    public string LastDesktop { get; set; } = "Default";
}
