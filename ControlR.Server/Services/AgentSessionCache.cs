using ControlR.Server.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ControlR.Server.Services;

public interface IAgentSessionCache
{
    ConcurrentDictionary<string, AgentHubSession> Sessions { get; }

    void AddOrUpdate(string connectionId, AgentHubSession session);
    bool TryGetValue(string connectionId, [NotNullWhen(true)] out AgentHubSession? session);
    bool TryRemove(string connectionId, [NotNullWhen(true)] out AgentHubSession? session);
}

internal class AgentSessionCache : IAgentSessionCache
{
    private static readonly ConcurrentDictionary<string, AgentHubSession> _sessions = new();

    public ConcurrentDictionary<string, AgentHubSession> Sessions => _sessions;

    public void AddOrUpdate(string connectionId, AgentHubSession session)
    {
        _sessions.AddOrUpdate(connectionId, session, (k, v) => session);
    }

    public bool TryGetValue(string connectionId, [NotNullWhen(true)] out AgentHubSession? session)
    {
        return _sessions.TryGetValue(connectionId, out session);
    }

    public bool TryRemove(string connectionId, [NotNullWhen(true)] out AgentHubSession? session)
    {
        return _sessions.TryRemove(connectionId, out session);
    }
}
