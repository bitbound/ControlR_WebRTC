using ControlR.Server.Models;
using ControlR.Server.Services;
using ControlR.Shared;
using ControlR.Shared.Extensions;
using ControlR.Shared.Interfaces.HubClients;
using ControlR.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ControlR.Server.Hubs;

public class DesktopHub : Hub<IDesktopHubClient>
{
    private readonly IDesktopSessionCache _desktopSessionCache;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IHubContext<ViewerHub, IViewerHubClient> _viewerHub;
    private readonly ILogger<DesktopHub> _logger;

    public DesktopHub(
        IDesktopSessionCache desktopSessionCache,
        IHubContext<ViewerHub, IViewerHubClient> viewerHubContext,
        IOptionsMonitor<AppOptions> appOptions,
        ILogger<DesktopHub> logger)
    {
        _desktopSessionCache = desktopSessionCache;
        _appOptions = appOptions;
        _viewerHub = viewerHubContext;
        _logger = logger;
    }

    public Task SetConnectionIdForSession(Guid sessionId)
    {
        var session = new DesktopHubSession(sessionId, Context.ConnectionId);
        _desktopSessionCache.AddOrUpdate(sessionId, session);
        return Task.CompletedTask;
    }

    public Task<IceServer[]> GetIceServers()
    {
        return _appOptions.CurrentValue.IceServers.ToArray().AsTaskResult();
    }

    public async Task SendIceCandidate(Guid sessionId, string candidateJson)
    {
        if (!_desktopSessionCache.TryGetValue(sessionId, out var session))
        {
            _logger.LogError("Could not find session for ID {id}.", sessionId);
            return;
        }

        await _viewerHub.Clients.Client(session.ViewerConnectionId!).ReceiveIceCandidate(sessionId, candidateJson);
    }

    public async Task SendRtcSessionDescription(Guid sessionId, RtcSessionDescription sessionDescription)
    {
        if (!_desktopSessionCache.TryGetValue(sessionId, out var session))
        {
            _logger.LogError("Could not find session for ID {id}.", sessionId);
            return;
        }
        await _viewerHub.Clients.Client(session.ViewerConnectionId!).ReceiveRtcSessionDescription(sessionId, sessionDescription);
    }
}
