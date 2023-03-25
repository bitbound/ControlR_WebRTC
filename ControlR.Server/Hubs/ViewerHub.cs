using ControlR.Server.Auth;
using ControlR.Server.Extensions;
using ControlR.Server.Models;
using ControlR.Server.Services;
using ControlR.Shared;
using ControlR.Shared.Dtos;
using ControlR.Shared.Extensions;
using ControlR.Shared.Helpers;
using ControlR.Shared.Interfaces.HubClients;
using ControlR.Shared.Models;
using ControlR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ControlR.Server.Hubs;

[Authorize]
public class ViewerHub : Hub<IViewerHubClient>
{
    private readonly IHubContext<AgentHub, IAgentHubClient> _agentHub;
    private readonly IHubContext<DesktopHub, IDesktopHubClient> _streamerHub;
    private readonly IAgentSessionCache _agentSessionCache;
    private readonly IDesktopSessionCache _desktopSessionCache;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger<ViewerHub> _logger;

    public ViewerHub(
        IHubContext<AgentHub, IAgentHubClient> agentHubContext,
        IHubContext<DesktopHub, IDesktopHubClient> desktopHubContext,
        IAgentSessionCache agentSessionCache,
        IDesktopSessionCache desktopSessionCache,
        IOptionsMonitor<AppOptions> appOptions,
        ILogger<ViewerHub> logger)
    {
        _agentHub = agentHubContext;
        _streamerHub = desktopHubContext;
        _agentSessionCache = agentSessionCache;
        _desktopSessionCache = desktopSessionCache;
        _appOptions = appOptions;
        _logger = logger;
    }

    public Task<IceServer[]> GetIceServers(SignedPayloadDto dto)
    {
        if (!VerifyPayload(dto, out _))
        {
            return Array.Empty<IceServer>().AsTaskResult();
        }

        return _appOptions.CurrentValue.IceServers.ToArray().AsTaskResult();
    }

    public async Task<WindowsSession[]> GetWindowsSessions(string agentConnectionId, SignedPayloadDto signedDto)
    {
        if (!VerifyPayload(signedDto, out _))
        {
            return Array.Empty<WindowsSession>();
        }

        return await _agentHub.Clients.Client(agentConnectionId).GetWindowsSessions(signedDto);
    }

    public async Task<Result<string>> GetDesktopSession(string agentConnectionId, Guid desktopSessionId, SignedPayloadDto sessionRequestDto)
    {
        try
        {
            if (!VerifyPayload(sessionRequestDto, out _))
            {
                return Result.Fail<string>(string.Empty);
            }

            var sessionSuccess = await _agentHub.Clients
                   .Client(agentConnectionId)
                   .GetDesktopSession(sessionRequestDto);

            if (!sessionSuccess)
            {
                return Result.Fail<string>("Failed to acquire desktop session.");
            }

            await WaitHelper.WaitForAsync(
                () => _desktopSessionCache.Sessions.ContainsKey(desktopSessionId),
                TimeSpan.FromSeconds(30));

            if (!_desktopSessionCache.TryGetValue(desktopSessionId, out var session))
            {
                return Result.Fail<string>("Failed to acquire desktop session.");
            }

            session.AgentConnectionId = agentConnectionId;
            session.ViewerConnectionId = Context.ConnectionId;
            return Result.Ok(session.DesktopConnectionId);
        }
        catch (Exception ex)
        {
            return Result.Fail<string>(ex);
        }
    }

    public async Task SendSignedDtoToAgent(string agentConnectionId, SignedPayloadDto signedDto)
    {
        using var scope = _logger.BeginMemberScope();

        if (!VerifyPayload(signedDto, out _))
        {
            return;
        }

        await _agentHub.Clients.Client(agentConnectionId).ReceiveDto(signedDto);
    }
    public async Task SendSignedDtoToStreamer(Guid desktopSessionId, SignedPayloadDto signedDto)
    {
        using var scope = _logger.BeginMemberScope();

        if (!VerifyPayload(signedDto, out _))
        {
            return;
        }

        if (!_desktopSessionCache.TryGetValue(desktopSessionId, out var session))
        {
            _logger.LogError("Session ID not found: {id}", desktopSessionId);
            return;
        }

        await _streamerHub.Clients
            .Client(session.DesktopConnectionId)
            .ReceiveDto(signedDto);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        if (Context.User?.TryGetClaim(ClaimNames.PublicKey, out var publicKey) != true)
        {
            _logger.LogWarning("Failed to get public key from viewer user.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, publicKey);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User?.TryGetClaim(ClaimNames.PublicKey, out var publicKey) == true)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, publicKey);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendSignedDtoToPublicKeyGroup(SignedPayloadDto signedDto)
    {
        using var _ = _logger.BeginMemberScope();

        if (!VerifyPayload(signedDto, out var publicKey))
        {
            return;
        }

        await _agentHub.Clients.Group(publicKey).ReceiveDto(signedDto);
    }

    private bool VerifyPayload(SignedPayloadDto signedDto, out string publicKey)
    {
        publicKey = string.Empty;

        if (Context.User?.TryGetClaim(ClaimNames.PublicKey, out publicKey) != true)
        {
            _logger.LogCritical("Failed to get public key from viewer user.");
            return false;
        }

        if (publicKey != signedDto.PublicKey)
        {
            _logger.LogCritical("Public key doesn't match what was retrieved during authentication.");
            return false;
        }

        return true;
    }
}
