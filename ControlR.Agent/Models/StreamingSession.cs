using EasyIpc;
using System.IO.MemoryMappedFiles;

namespace ControlR.Agent.Models;
internal class StreamingSession : IDisposable
{
    public StreamingSession(
        int streamerProcessId, 
        Guid sessionId, 
        string authorizedKey)
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
    public IIpcServer? IpcServer { get; set; }

    public void Dispose()
    {
        IpcServer?.Dispose();
    }
}
