using ControlR.Shared.Helpers;
using EasyIpc;
using System.Diagnostics;

namespace ControlR.Agent.Models;
internal class StreamingSession : IDisposable
{
    public StreamingSession(Guid sessionId, string authorizedKey, int targetWindowsSession, string targetDesktop)
    {
        SessionId = sessionId;
        AuthorizedKey = authorizedKey;
        TargetWindowsSession = targetWindowsSession;
        LastDesktop = targetDesktop;
    }


    public Process? StreamerProcess { get; set; }
    public Guid SessionId { get; }
    public int TargetWindowsSession { get; }
    public string AuthorizedKey { get; }
    public Process? WatcherProcess { get; set; }
    public string LastDesktop { get; set; } = "Default";
    public IIpcServer? IpcServer { get; set; }
    public string AgentPipeName { get; } = Guid.NewGuid().ToString();

    public void Dispose()
    {
        try
        {
            StreamerProcess?.Kill();
        }
        catch { }
        try
        {
            WatcherProcess?.Kill();
        }
        catch { }
        DisposeHelper.DisposeAll(IpcServer, StreamerProcess, WatcherProcess);
    }
}
