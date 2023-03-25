using ControlR.Shared.Dtos;
using ControlR.Shared.Interfaces;
using ControlR.Shared.Models;
using ControlR.Shared;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlR.Devices.Common.Services;
using CommunityToolkit.Mvvm.Messaging;
using ControlR.Viewer.Extensions;
using ControlR.Shared.Interfaces.HubClients;
using ControlR.Shared.Helpers;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ControlR.Viewer.Models.Messages;
using MudBlazor;
using Microsoft.Maui.Controls;
using ControlR.Shared.Extensions;
using ControlR.Shared.Enums;

namespace ControlR.Viewer.Services;
public interface IViewerHubConnection : IViewerHubClient, IHubConnectionBase
{
    Task CloseDesktopSession(Guid sessionId);
    Task<Result<string>> GetDesktopSession(string agentConnectionId, Guid sessionId, int targetSystemSession, string targetDesktop = "Default");

    Task<Result<DisplayDto[]>> GetDisplays(string desktopConnectionId);
    Task<Result<IceServer[]>> GetIceServers();

    Task<Result<WindowsSession[]>> GetWindowsSessions(DeviceDto device);
    Task RequestDeviceUpdates();
    Task SendIceCandidate(Guid sessionId, string iceCandidateJson);
    Task SendRtcSessionDescription(Guid sessionId, RtcSessionDescription sessionDescription);
    Task Start(CancellationToken cancellationToken);
    Task SendPowerStateChange(DeviceDto device, PowerStateChangeType powerStateType);
}

internal class ViewerHubConnection : HubConnectionBase, IViewerHubConnection
{
    private readonly IAppState _appState;
    private readonly IDeviceCache _devicesCache;
    private readonly IHttpConfigurer _httpConfigurer;
    private readonly ILogger<ViewerHubConnection> _logger;
    private readonly IMessenger _messenger;
    private readonly ISettings _settings;
    public ViewerHubConnection(
        IServiceScopeFactory serviceScopeFactory,
        IHttpConfigurer httpConfigurer,
        IMessenger messenger,
        IAppState appState,
        ISettings settings,
        IDeviceCache devicesCache,
        ILogger<ViewerHubConnection> logger)
        : base(serviceScopeFactory, logger)
    {
        _httpConfigurer = httpConfigurer;
        _appState = appState;
        _messenger = messenger;
        _settings = settings;
        _devicesCache = devicesCache;
        _logger = logger;
    }

    public async Task CloseDesktopSession(Guid sessionId)
    {
        var signedDto = _appState.Encryptor.CreateRandomSignedDto(DtoType.CloseDesktopSession, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToStreamer", sessionId, signedDto);
    }

    public async Task<Result<string>> GetDesktopSession(string agentConnectionId, Guid sessionId, int targetSystemSession, string targetDesktop = "Default")
    {
        try
        {
            var desktopSessionRequest = new DesktopSessionRequestDto(
                sessionId,
                targetSystemSession,
                targetDesktop,
                Connection.ConnectionId);

            var signedDto = _appState.Encryptor.CreateSignedDto(desktopSessionRequest, DtoType.DesktopSessionRequest, _settings.PublicKey);

            var result = await Connection.InvokeAsync<Result<string>>("GetDesktopSession", agentConnectionId, sessionId, signedDto);
            if (!result.IsSuccess)
            {
                _logger.LogResult(result);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting remote desktop session.");
            return Result.Fail<string>(ex);
        }
    }

    public Task<Result<DisplayDto[]>> GetDisplays(string desktopConnectionId)
    {
        return Result.Fail<DisplayDto[]>("Not implemented.").AsTaskResult();
    }

    public async Task<Result<IceServer[]>> GetIceServers()
    {
        try
        {
            var signedDto = _appState.Encryptor.CreateRandomSignedDto(DtoType.None, _settings.PublicKey);
            var iceServers = await Connection.InvokeAsync<IceServer[]>("GetIceServers", signedDto);
            return Result.Ok(iceServers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting ICE servers..");
            return Result.Fail<IceServer[]>(ex);
        }
    }

    public async Task<Result<WindowsSession[]>> GetWindowsSessions(DeviceDto device)
    {
        try
        {
            var signedDto = _appState.Encryptor.CreateRandomSignedDto(DtoType.WindowsSessions, _settings.PublicKey);
            var sessions = await Connection.InvokeAsync<WindowsSession[]>("GetWindowsSessions", device.ConnectionId, signedDto);
            return Result.Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting windows sessions.");
            return Result.Fail<WindowsSession[]>(ex);
        }
    }

    public Task ReceiveDesktopChanged(Guid sessionId, string desktopName)
    {
        _messenger.Send(new DesktopChangedMessage(sessionId, desktopName));
        return Task.CompletedTask;
    }

    public Task ReceiveDeviceUpdate(DeviceDto device)
    {
        var key = device.ConnectionId;
        _devicesCache.AddOrUpdate(key, device);
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.DevicesCacheUpdated);
        return Task.CompletedTask;
    }

    public Task ReceiveIceCandidate(Guid sessionId, string candidateJson)
    {
        _messenger.Send(new IceCandidateMessage(sessionId, candidateJson));
        return Task.CompletedTask;
    }

    public Task ReceiveRemoteControlDownloadProgress(Guid desktopSessionId, double downloadProgress)
    {
        _messenger.Send(new RemoteControlDownloadProgressMessage(desktopSessionId, downloadProgress));
        return Task.CompletedTask;
    }

    public Task ReceiveRtcSessionDescription(Guid sessionId, RtcSessionDescription sessionDescription)
    {
        _messenger.Send(new RtcSessionDescriptionMessage(sessionId, sessionDescription));
        return Task.CompletedTask;
    }

    public async Task RequestDeviceUpdates()
    {
        await WaitForConnection();
        var signedDto = _appState.Encryptor.CreateRandomSignedDto(DtoType.DeviceUpdateRequest, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToPublicKeyGroup", signedDto);
    }

    public async Task SendIceCandidate(Guid sessionId, string iceCandidateJson)
    {
        var signedDto = _appState.Encryptor.CreateSignedDto(iceCandidateJson, DtoType.RtcIceCandidate, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToStreamer", sessionId, signedDto);
    }

    public async Task SendPointerMove(Guid sessionId, double percentX, double percentY)
    {
        var pointerMoveDto = new PointerMoveDto(percentX, percentY);
        var signedDto = _appState.Encryptor.CreateSignedDto(pointerMoveDto, DtoType.PointerMove, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToStreamer", sessionId, signedDto);
    }

    public async Task SendPowerStateChange(DeviceDto device, PowerStateChangeType powerStateType)
    {
        var powerDto = new PowerStateChangeDto(powerStateType);
        var signedDto = _appState.Encryptor.CreateSignedDto(powerDto, DtoType.PowerStateChange, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToAgent", device.ConnectionId, signedDto);
    }

    public async Task SendRtcSessionDescription(Guid sessionId, RtcSessionDescription sessionDescription)
    {
        var signedDto = _appState.Encryptor.CreateSignedDto(sessionDescription, DtoType.RtcSessionDescription, _settings.PublicKey);
        await Connection.InvokeAsync("SendSignedDtoToStreamer", sessionId, signedDto);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        _messenger.UnregisterAll(this);

        await WaitHelper.WaitForAsync(() => _appState.IsAuthenticated, TimeSpan.MaxValue);

        using var _ = _appState.IncrementBusyCounter();

        await Connect(
            $"{AppConstants.ServerUri}/hubs/viewer",
            ConfigureConnection,
            ConfigureHttpOptions,
            OnConnectFailure,
            cancellationToken);

        _messenger.RegisterParameterless(this, ParameterlessMessageKind.AuthStateChanged, HandleAuthStateChanged);
    }

    private void ConfigureConnection(HubConnection connection)
    {
        connection.Closed += Connection_Closed;
        connection.Reconnecting += Connection_Reconnecting;
        connection.Reconnected += Connection_Reconnected;
        connection.On<DeviceDto>(nameof(ReceiveDeviceUpdate), ReceiveDeviceUpdate);
        connection.On<Guid, double>(nameof(ReceiveRemoteControlDownloadProgress), ReceiveRemoteControlDownloadProgress);
        connection.On<Guid, string>(nameof(ReceiveIceCandidate), ReceiveIceCandidate);
        connection.On<Guid, RtcSessionDescription>(nameof(ReceiveRtcSessionDescription), ReceiveRtcSessionDescription);
        connection.On<Guid, string>(nameof(ReceiveDesktopChanged), ReceiveDesktopChanged);
    }

    private void ConfigureHttpOptions(HttpConnectionOptions options)
    {
        var signature = _httpConfigurer.GetDigitalSignature();
        options.Headers["Authorization"] = $"{AuthSchemes.DigitalSignature} {signature}";
    }

    private Task Connection_Closed(Exception? arg)
    {
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.HubConnectionStateChanged);
        return Task.CompletedTask;
    }

    private Task Connection_Reconnected(string? arg)
    {
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.HubConnectionStateChanged);
        return Task.CompletedTask;
    }

    private Task Connection_Reconnecting(Exception? arg)
    {
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.HubConnectionStateChanged);
        return Task.CompletedTask;
    }

    private async Task HandleAuthStateChanged()
    {
        await Stop(_appState.AppExiting);

        if (_appState.AuthenticationState == Enums.AuthenticationState.PrivateKeyLoaded)
        {
            await Start(_appState.AppExiting);
        }
    }

    private Task OnConnectFailure(string reason)
    {
        _messenger.Send(new ToastMessage(reason, Severity.Error));
        return Task.CompletedTask;
    }
    private class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(3);
        }
    }
}
