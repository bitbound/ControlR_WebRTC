using ControlR.Agent.Models;
using ControlR.Shared.Dtos;
using ControlR.Shared.Extensions;
using ControlR.Shared.Interfaces;
using ControlR.Shared.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Agent.Services;
internal class DtoHandler : IHostedService
{
    private readonly IEncryptionSessionFactory _encryptionFactory;
    private readonly IAgentHubConnection _agentHub;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger<DtoHandler> _logger;

    public DtoHandler(
        IEncryptionSessionFactory encryptionFactory,
        IAgentHubConnection agentHub,
        IOptionsMonitor<AppOptions> appOptions,
        ILogger<DtoHandler> logger)
    {
        _encryptionFactory = encryptionFactory;
        _agentHub = agentHub;
        _appOptions = appOptions;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _agentHub.DtoReceived += AgentHub_DtoReceived;
        return Task.CompletedTask;
    }

    private async void AgentHub_DtoReceived(object? sender, SignedPayloadDto dto)
    {
        using var _ = _logger.BeginMemberScope();
        using var session = _encryptionFactory.CreateSession();

        if (!session.Verify(dto))
        {
            _logger.LogCritical("Key verification failed for public key: {key}", dto.PublicKey);
            return;
        }

        if (!_appOptions.CurrentValue.AuthorizedKeys.Contains(dto.PublicKey))
        {
            _logger.LogCritical("Public key does not exist in authorized keys: {key}", dto.PublicKey);
            return;
        }

        switch (dto.DtoType)
        {
            case DtoType.DeviceUpdateRequest:
                await _agentHub.SendDeviceHeartbeat();
                break;
            default:
                _logger.LogWarning("Unhandled DTO type: {type}", dto.DtoType);
                break;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _agentHub.DtoReceived -= AgentHub_DtoReceived;
        return Task.CompletedTask;
    }
}
