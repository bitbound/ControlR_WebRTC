using ControlR.Server.Models;
using ControlR.Server.Services;
using ControlR.Shared.Dtos;
using ControlR.Shared.Enums;
using ControlR.Shared.Extensions;
using ControlR.Shared.Interfaces.HubClients;
using ControlR.Shared.Models;
using ControlR.Shared.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ControlR.Server.Hubs;

public class AgentHub : Hub<IAgentHubClient>
{
    private readonly IHubContext<ViewerHub, IViewerHubClient> _viewerHub;
    private readonly IAgentSessionCache _agentSessionCache;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ISystemTime _systemTime;
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(
        IHubContext<ViewerHub, IViewerHubClient> viewerHubContext,
        IAgentSessionCache agentSessionCache,
        IOptionsMonitor<AppOptions> appOptions,
        ISystemTime systemTime,
        ILogger<AgentHub> logger)
    {
        _viewerHub = viewerHubContext;
        _agentSessionCache = agentSessionCache;
        _appOptions = appOptions;
        _systemTime = systemTime;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_agentSessionCache.TryRemove(Context.ConnectionId, out var session))
        {
            session.Device.IsOnline = false;
            session.Device.LastOnline = _systemTime.Now;

            var result = DeviceDto.TryCreateFrom(session.Device, ConnectionType.Agent, Context.ConnectionId);

            if (result.IsSuccess)
            {
                await _viewerHub.Clients.Groups(session.Device.AuthorizedKeys).ReceiveDeviceUpdate(result.Value);
            }
            else
            {
                _logger.LogResult(result);
            }

            foreach (var key in session.Device.AuthorizedKeys)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, key);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public IceServer[] GetIceServers()
    {
        return _appOptions.CurrentValue.IceServers.ToArray();
    }

    public async Task SendRemoteControlDownloadProgress(Guid desktopSessionId, string viewerConnectionId, double downloadProgress)
    {
        await _viewerHub.Clients.Client(viewerConnectionId).ReceiveRemoteControlDownloadProgress(desktopSessionId, downloadProgress);
    }

    public async Task UpdateDevice(Device device)
    {
        device.IsOnline = true;
        device.LastOnline = _systemTime.Now;

        if (_agentSessionCache.TryGetValue(Context.ConnectionId, out var session))
        {
            var oldKeys = session.Device.AuthorizedKeys.Except(device.AuthorizedKeys);
            foreach (var oldKey in oldKeys)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldKey);
            }

            var newKeys = device.AuthorizedKeys.Except(session.Device.AuthorizedKeys);
            foreach (var newKey in newKeys)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, newKey);
            }
        }
        else
        {
            foreach (var key in device.AuthorizedKeys)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, key);
            }
        }


        var result = DeviceDto.TryCreateFrom(device, ConnectionType.Agent, Context.ConnectionId);

        if (!result.IsSuccess)
        {
            _logger.LogResult(result);
            return;
        }

        session = new AgentHubSession(Context.ConnectionId, result.Value);
        _agentSessionCache.AddOrUpdate(Context.ConnectionId, session);
        await _viewerHub.Clients.Groups(device.AuthorizedKeys).ReceiveDeviceUpdate(result.Value);
    }
}
