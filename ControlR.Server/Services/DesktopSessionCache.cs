using ControlR.Server.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ControlR.Server.Services;

public interface IDesktopSessionCache
{
    ConcurrentDictionary<Guid, DesktopHubSession> Sessions { get; }
    void AddOrUpdate(Guid sessionId, DesktopHubSession desktopHubSession);

    bool TryGetValue(Guid sessionId, [NotNullWhen(true)] out DesktopHubSession? session);
    bool TryRemove(Guid sessionId, [NotNullWhen(true)] out DesktopHubSession? session);
}

public class DesktopSessionCache : IDesktopSessionCache
{
    private static readonly ConcurrentDictionary<Guid, DesktopHubSession> _sessions = new();

    public ConcurrentDictionary<Guid, DesktopHubSession> Sessions => _sessions;

    public void AddOrUpdate(Guid sessionId, DesktopHubSession session)
    {
        _sessions.AddOrUpdate(sessionId, session, (k, v) => session);
    }

    public bool TryGetValue(Guid sessionId, [NotNullWhen(true)] out DesktopHubSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public bool TryRemove(Guid sessionId, [NotNullWhen(true)] out DesktopHubSession? session)
    {
        return _sessions.TryRemove(sessionId, out session);
    }
}
