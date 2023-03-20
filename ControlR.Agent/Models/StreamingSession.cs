using ControlR.Shared.Helpers;
using EasyIpc;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace ControlR.Agent.Models;
internal class StreamingSession : IDisposable
{
    public StreamingSession(
        Process streamerProcess, 
        Guid sessionId, 
        string authorizedKey)
    {
        StreamerProcess = streamerProcess;
        SessionId = sessionId;
        AuthorizedKey = authorizedKey;

    }

    public Process StreamerProcess { get; set; }
    public Guid SessionId { get; }
    public string AuthorizedKey { get; }
    public Process? WatcherProcess { get; set; }
    public string LastDesktop { get; set; } = "Default";
    public IIpcServer? IpcServer { get; set; }
    public string AgentPipeName { get; } = Guid.NewGuid().ToString();

    public void Dispose()
    {
        DisposeHelper.DisposeAll(IpcServer, StreamerProcess, WatcherProcess);
    }
}
