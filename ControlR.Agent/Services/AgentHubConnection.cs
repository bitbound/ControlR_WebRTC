using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ControlR.Shared.Dtos;
using ControlR.Shared.Enums;
using ControlR.Shared.Extensions;
using ControlR.Shared.Helpers;
using ControlR.Shared.Interfaces;
using ControlR.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlR.Devices.Common.Services.Interfaces;
using ControlR.Devices.Common.Services;
using ControlR.Devices.Common.Native.Windows;
using ControlR.Shared;
using Microsoft.AspNetCore.Http.Connections.Client;
using ControlR.Devices.Common;
using Microsoft.Extensions.Options;
using ControlR.Shared.Interfaces.HubClients;
using ControlR.Shared.Models;
using System.Runtime.Versioning;
using ControlR.Agent.Interfaces;
using ControlR.Agent.Services.Windows;
using ControlR.Agent.Models;

namespace ControlR.Agent.Services;

internal interface IAgentHubConnection : IAgentHubClient, IHubConnectionBase
{
    Task NotifyViewerDesktopChanged(Guid sessionId, string desktopName);
    Task SendDeviceHeartbeat();
    Task Start();
}

internal class AgentHubConnection : HubConnectionBase, IAgentHubConnection
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ICpuUtilizationSampler _cpuSampler;
    private readonly IDeviceDataGenerator _deviceCreator;
    private readonly IEncryptionSessionFactory _encryptionFactory;
    private readonly IRemoteControlLauncher _remoteControlLauncher;
    private readonly IAgentUpdater _updater;
    private readonly IEnvironmentHelper _environmentHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AgentHubConnection> _logger;
    private readonly IProcessInvoker _processes;
    public AgentHubConnection(
         IHostApplicationLifetime appLifetime,
         IServiceScopeFactory scopeFactory,
         IDeviceDataGenerator deviceCreator,
         IEnvironmentHelper environmentHelper,
         IProcessInvoker processes,
         IFileSystem fileSystem,
         IOptionsMonitor<AppOptions> appOptions,
         ICpuUtilizationSampler cpuSampler,
         IEncryptionSessionFactory encryptionFactory,
         IRemoteControlLauncher remoteControlLauncher,
         IAgentUpdater updater,
         ILogger<AgentHubConnection> logger)
        : base(scopeFactory, logger)
    {
        _appLifetime = appLifetime;
        _deviceCreator = deviceCreator;
        _processes = processes;
        _fileSystem = fileSystem;
        _environmentHelper = environmentHelper;
        _appOptions = appOptions;
        _cpuSampler = cpuSampler;
        _encryptionFactory = encryptionFactory;
        _remoteControlLauncher = remoteControlLauncher;
        _updater = updater;
        _logger = logger;
    }

    public async Task<bool> GetDesktopSession(SignedPayloadDto signedDto)
    {
        try
        {
            if (!VerifyPayload(signedDto))
            {
                return false;
            }

            var dto = MessagePackSerializer.Deserialize<DesktopSessionRequestDto>(Convert.FromBase64String(signedDto.Payload));

            double downloadProgress = 0;

            var result = await _remoteControlLauncher.CreateSession(
                dto.DesktopSessionId, 
                signedDto.PublicKey,
                dto.TargetSystemSession,
                dto.TargetDesktop ?? string.Empty,
                async progress =>
                {
                    if (progress == 1 || progress - downloadProgress > .05)
                    {
                        downloadProgress = progress;
                        await Connection
                            .InvokeAsync("SendRemoteControlDownloadProgress", dto.DesktopSessionId, dto.ViewerConnectionId, downloadProgress)
                            .ConfigureAwait(false);
                    }
                })
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to get desktop session.  Reason: {reason}", result.Reason);
            }

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating desktop session.");
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    public Task<WindowsSession[]> GetWindowsSessions(SignedPayloadDto signedDto)
    {
        if (!VerifyPayload(signedDto))
        {
            return Array.Empty<WindowsSession>().AsTaskResult();
        }

        if (_environmentHelper.Platform != SystemPlatform.Windows)
        {
            return Array.Empty<WindowsSession>().AsTaskResult();
        }

        return Win32.GetActiveSessions().ToArray().AsTaskResult();
    }

    public async Task NotifyViewerDesktopChanged(Guid sessionId, string desktopName)
    {
        try
        {
            await Connection.InvokeAsync("NotifyViewerDesktopChanged", sessionId, desktopName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending device update.");
        }
    }

    public async Task SendDeviceHeartbeat()
    {
        try
        {
            using var _ = _logger.BeginMemberScope();

            if (ConnectionState != HubConnectionState.Connected)
            {
                _logger.LogWarning("Not connected to hub when trying to send device update.");
                return;
            }

            if (!_appOptions.CurrentValue.AuthorizedKeys.Any())
            {
                _logger.LogWarning("There are no authorized keys in appsettings. Aborting heartbeat.");
                return;
            }

            var device = await _deviceCreator.CreateDevice(
                _cpuSampler.CurrentUtilization,
                _appOptions.CurrentValue.AuthorizedKeys);

            await Connection.InvokeAsync("UpdateDevice", device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending device update.");
        }
    }

    public async Task Start()
    {
        await Connect(
            $"{AppConstants.ServerUri}/hubs/agent",
            ConfigureConnection,
            ConfigureHttpOptions,
            _appLifetime.ApplicationStopping);

        await SendDeviceHeartbeat();
    }

    private void ConfigureConnection(HubConnection hubConnection)
    {
        hubConnection.Reconnected += HubConnection_Reconnected;

        if (_environmentHelper.Platform == SystemPlatform.Windows)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            hubConnection.On(nameof(GetWindowsSessions), (Func<SignedPayloadDto, Task<WindowsSession[]>>)GetWindowsSessions);
#pragma warning restore CA1416 // Validate platform compatibility
        }

        hubConnection.On(nameof(GetDesktopSession), (Func<SignedPayloadDto, Task<bool>>)GetDesktopSession);
    }

    private void ConfigureHttpOptions(HttpConnectionOptions options)
    {

    }

    private async Task HubConnection_Reconnected(string? arg)
    {
        await SendDeviceHeartbeat();
        await _updater.CheckForUpdate();
    }


    private bool VerifyPayload(SignedPayloadDto signedDto)
    {
        using var session = _encryptionFactory.CreateSession();

        if (!session.Verify(signedDto))
        {
            _logger.LogCritical("Verification failed for payload with public key: {key}", signedDto.PublicKey);
            return false;
        }

        if (!_appOptions.CurrentValue.AuthorizedKeys.Contains(signedDto.PublicKey))
        {
            _logger.LogCritical("Public key does not exist in authorized keys list: {key}", signedDto.PublicKey);
            return false;
        }

        return true;
    }
    private class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var waitSeconds = Math.Min(30, Math.Pow(retryContext.PreviousRetryCount, 2));
            return TimeSpan.FromSeconds(waitSeconds);
        }
    }
}
