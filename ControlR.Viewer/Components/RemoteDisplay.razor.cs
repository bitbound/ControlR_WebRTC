using ControlR.Shared.Dtos;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using ControlR.Viewer.Models;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using ControlR.Viewer.Services;
using MudBlazor;
using ControlR.Shared.Models;
using CommunityToolkit.Mvvm.Messaging;
using ControlR.Viewer.Models.Messages;
using ControlR.Viewer.Enums;
using ControlR.Viewer.Extensions;
using ControlR.Shared.Extensions;

namespace ControlR.Viewer.Components;

[SupportedOSPlatform("browser")]
public partial class RemoteDisplay : IAsyncDisposable
{
    private readonly string _videoId = $"video-{Guid.NewGuid()}";
    private DotNetObjectReference<RemoteDisplay>? _componentRef;
    private IEnumerable<DisplayDto> _displays = Enumerable.Empty<DisplayDto>();
    private double _downloadProgress;
    private IJSObjectReference? _module;
    private DisplayDto? _selectedDisplay;
    private string _statusMessage = "Starting remote control session";
    private double _statusProgress = -1;
    private string _videoClass = "fit";
    private ElementReference _videoRef;
    private WindowState _windowState = WindowState.Maximized;

#nullable disable
    [Parameter, EditorRequired]
    public RemoteControlSession Session { get; set; }

    [Inject]
    private IAppState AppState { get; init; }

    [Inject]
    private IJSRuntime JsRuntime { get; init; }

    [Inject]
    private ILogger<RemoteDisplay> Logger { get; init; }
    [Inject]
    private IMessenger Messenger { get; init; }

    [Inject]
    private ISnackbar Snackbar { get; init; }

    [Inject]
    private IViewerHubConnection ViewerHub { get; init; }
#nullable enable

    public async ValueTask DisposeAsync()
    {
        await ViewerHub.CloseDesktopSession(Session.SessionId);
        Messenger.UnregisterAll(this);
        await _module!.InvokeVoidAsync("dispose", _videoId);
        _componentRef?.Dispose();
        GC.SuppressFinalize(this);
    }

    [JSInvokable]
    public Task LogInfo(string message)
    {
        Logger.LogInformation("JS Log: {message}", message);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task SendIceCandidate(string iceCandidateJson)
    {
        await ViewerHub.SendIceCandidate(Session.SessionId, iceCandidateJson);
    }

    [JSInvokable]
    public async Task SendRtcDescription(RtcSessionDescription sessionDescription)
    {
        await InvokeAsync(StateHasChanged);
        await ViewerHub.SendRtcSessionDescription(Session.SessionId, sessionDescription);
    }

    [JSInvokable]
    public async Task SetStatusMessage(string message)
    {
        _statusMessage = message;
        await InvokeAsync(StateHasChanged);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        _module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/RemoteDisplay.razor.js");

        if (firstRender)
        {
            _componentRef = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("initialize", _componentRef, _videoId);
            await RequestDesktopSessionFromAgent();
        }
    }

    protected override Task OnInitializedAsync()
    {
        Messenger.Register<RemoteControlDownloadProgressMessage>(this, HandleRemoteControlDownloadProgress);
        Messenger.Register<IceCandidateMessage>(this, HandleIceCandidateReceived);
        Messenger.Register<RtcSessionDescriptionMessage>(this, HandleRtcSessionDescription);
        Messenger.Register<RemoteDisplayWindowStateMessage>(this, HandleRemoteDisplayWindowStateChanged);
        Messenger.Register<DesktopChangedMessage>(this, HandleDesktopChanged);
        Messenger.RegisterParameterless(this, HandleParameterlessMessage);

        return base.OnInitializedAsync();
    }

    private async Task Close()
    {
        AppState.RemoteControlSessions.Remove(Session);
        await DisposeAsync();
    }

    private async void HandleDesktopChanged(object recipient, DesktopChangedMessage message)
    {
        if (message.SessionId != Session.SessionId || _module is null)
        {
            return;
        }

        await SetStatusMessage("Switching desktops");

        await ViewerHub.CloseDesktopSession(Session.SessionId);

        Session.CreateNewSessionId();

        await RequestDesktopSessionFromAgent(message.DesktopName);
    }

    private async void HandleIceCandidateReceived(object recipient, IceCandidateMessage message)
    {
        if (message.SessionId != Session.SessionId || _module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("receiveIceCandidate", message.CandidateJson, _videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while invoking JavaScript function: {name}", "receiveIceCandidate");
        }
    }

    private async void HandleParameterlessMessage(ParameterlessMessageKind kind)
    {
        if (kind == ParameterlessMessageKind.ShuttingDown)
        {
            await DisposeAsync();
        }
    }

    private async void HandleRemoteControlDownloadProgress(object recipient, RemoteControlDownloadProgressMessage message)
    {
        if (message.DesktopSessionId != Session.SessionId)
        {
            return;
        }

        _downloadProgress = message.DownloadProgress;
        await InvokeAsync(StateHasChanged);
    }

    private async void HandleRemoteDisplayWindowStateChanged(object recipient, RemoteDisplayWindowStateMessage message)
    {
        if (message.SessionId == Session.SessionId)
        {
            return;
        }

        if (message.State != WindowState.Minimized)
        {
            _windowState = WindowState.Minimized;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async void HandleRtcSessionDescription(object recipient, RtcSessionDescriptionMessage message)
    {
        if (message.SessionId != Session.SessionId || _module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("receiveRtcSessionDescription", message.SessionDescription, _videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while invoking JavaScript function: {name}", "receiveRtcSessionDescription");
        }
    }

    private async Task RequestDesktopSessionFromAgent(string desktopName = "Default")
    {
        if (_module is null)
        {
            Snackbar.Add("JavaScript services must be initialized before remote control");
            return;
        }

        var desktopSessionResult = await ViewerHub.GetDesktopSession(Session.Device.ConnectionId, Session.SessionId, Session.InitialSystemSession, desktopName);

        if (!desktopSessionResult.IsSuccess)
        {
            Snackbar.Add("Failed to create desktop session", Severity.Error);
            await Close();
            return;
        }

        _statusMessage = "Getting ICE servers";
        await InvokeAsync(StateHasChanged);

        Logger.LogInformation("Starting RTC offer");

        var iceServersResult = await ViewerHub.GetIceServers();

        Logger.LogInformation("Getting ICE servers.");

        if (!iceServersResult.IsSuccess || !iceServersResult.Value.Any())
        {
            Snackbar.Add("Failed to get ICE servers", Severity.Error);
            await Close();
            return;
        }

        _statusMessage = "Sending RTC offer";
        await InvokeAsync(StateHasChanged);
        await _module.InvokeVoidAsync("startRtcOffer", iceServersResult.Value.Cast<object>(), _videoId);
    }
    private void SetWindowState(WindowState state)
    {
        _windowState = state;
        Messenger.Send(new RemoteDisplayWindowStateMessage(Session.SessionId, state));
    }
}
